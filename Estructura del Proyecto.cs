Así tengo las clases actualmente y aun no coloca el Where, por favor no crees clases nuevas, ni reutilices las de la versión antigua.

using QueryBuilder.Enums;
using QueryBuilder.Expressions;
using QueryBuilder.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace QueryBuilder.Builders;

/// <summary>
/// Generador de consultas SELECT compatible con AS400.
/// Soporta alias de tabla y columnas, ORDER BY y FETCH FIRST.
/// </summary>
public class SelectQueryBuilder
{
    /// <summary>
    /// Cláusula WHERE acumulada.
    /// </summary>
    internal string? WhereClause { get; set; }

    private readonly string _tableName;
    private readonly string? _library;
    private string? _tableAlias;
    private readonly List<(string Column, string? Alias)> _columns = [];
    private string? _whereClause;
    private readonly List<string> _orderBy = [];
    private int? _limit;
    private readonly List<JoinClause> _joins = [];

    /// <summary>
    /// Inicializa una nueva instancia de <see cref="SelectQueryBuilder"/>.
    /// </summary>
    /// <param name="tableName">Nombre de la tabla.</param>
    /// <param name="library">Nombre de la librería en AS400 (opcional).</param>
    public SelectQueryBuilder(string tableName, string? library = null)
    {
        _tableName = tableName;
        _library = library;
    }

    /// <summary>
    /// Define un alias para la tabla.
    /// </summary>
    public SelectQueryBuilder As(string alias)
    {
        _tableAlias = alias;
        return this;
    }

    /// <summary>
    /// Define las columnas a seleccionar (sin alias).
    /// </summary>
    public SelectQueryBuilder Select(params string[] columns)
    {
        foreach (var column in columns)
        {
            _columns.Add((column, null));
        }
        return this;
    }

    /// <summary>
    /// Define columnas con alias.
    /// </summary>
    /// <param name="columns">Tuplas de columna original y alias.</param>
    public SelectQueryBuilder Select(params (string Column, string Alias)[] columns)
    {
        foreach (var (col, alias) in columns)
        {
            _columns.Add((col, alias));
        }
        return this;
    }

    /// <summary>
    /// Agrega una condición WHERE a la consulta.
    /// </summary>
    /// <typeparam name="T">Tipo de entidad (para acceso tipado).</typeparam>
    /// <param name="expression">Expresión lambda booleana a traducir.</param>
    /// <returns>Instancia del builder para encadenamiento fluido.</returns>
    public SelectQueryBuilder Where<T>(Expression<Func<T, bool>> expression)
    {
        LambdaWhereTranslator.Translate(this, expression);
        return this;
    }

    /// <summary>
    /// Ordena por una sola columna.
    /// </summary>
    public SelectQueryBuilder OrderBy(string column, SortDirection direction = SortDirection.Asc)
    {
        _orderBy.Add($"{column} {direction.ToString().ToUpper()}");
        return this;
    }

    /// <summary>
    /// Ordena por múltiples columnas.
    /// </summary>
    public SelectQueryBuilder OrderBy(params (string Column, SortDirection Direction)[] columns)
    {
        foreach (var (column, direction) in columns)
        {
            _orderBy.Add($"{column} {direction.ToString().ToUpper()}");
        }
        return this;
    }

    /// <summary>
    /// Establece el límite de filas a devolver.
    /// </summary>
    public SelectQueryBuilder Limit(int rowCount)
    {
        _limit = rowCount;
        return this;
    }

    /// <summary>
    /// Agrega una cláusula JOIN a la consulta.
    /// </summary>
    /// <param name="table">Nombre de la tabla a unir.</param>
    /// <param name="library">Nombre de la librería (opcional).</param>
    /// <param name="alias">Alias para la tabla unida.</param>
    /// <param name="leftColumn">Campo izquierdo de la condición ON.</param>
    /// <param name="rightColumn">Campo derecho de la condición ON.</param>
    /// <param name="joinType">Tipo de JOIN (INNER, LEFT, etc.).</param>
    public SelectQueryBuilder Join(
        string table,
        string? library,
        string alias,
        string leftColumn,
        string rightColumn,
        string joinType = "INNER")
    {
        _joins.Add(new JoinClause
        {
            JoinType = joinType.ToUpper(),
            TableName = table,
            Library = library,
            Alias = alias,
            LeftColumn = leftColumn,
            RightColumn = rightColumn
        });

        return this;
    }

    /// <summary>
    /// Construye y devuelve la consulta SQL generada.
    /// </summary>
    public QueryResult Build()
    {
        var sb = new StringBuilder();

        // SELECT
        sb.Append("SELECT ");

        if (_columns.Count == 0)
        {
            sb.Append("*");
        }
        else
        {
            var columnSql = _columns.Select(c =>
                string.IsNullOrWhiteSpace(c.Alias)
                    ? c.Column
                    : $"{c.Column} AS {c.Alias}"
            );
            sb.Append(string.Join(", ", columnSql));
        }

        // FROM
        sb.Append(" FROM ");
        if (!string.IsNullOrWhiteSpace(_library))
            sb.Append($"{_library}.");
        sb.Append(_tableName);
        if (!string.IsNullOrWhiteSpace(_tableAlias))
            sb.Append($" {_tableAlias}");

        // JOINs
        foreach (var join in _joins)
        {
            sb.Append($" {join.JoinType} JOIN ");
            if (!string.IsNullOrWhiteSpace(join.Library))
                sb.Append($"{join.Library}.");
            sb.Append(join.TableName);
            if (!string.IsNullOrWhiteSpace(join.Alias))
                sb.Append($" {join.Alias}");
            sb.Append($" ON {join.LeftColumn} = {join.RightColumn}");
        }

        // WHERE
        if (!string.IsNullOrWhiteSpace(_whereClause))
            sb.Append(" WHERE ").Append(_whereClause);

        // ORDER BY
        if (_orderBy.Count > 0)
            sb.Append(" ORDER BY ").Append(string.Join(", ", _orderBy));

        // LIMIT (AS400)
        if (_limit.HasValue)
            sb.Append($" FETCH FIRST {_limit.Value} ROWS ONLY");

        return new QueryResult
        {
            Sql = sb.ToString()
        };
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace QueryBuilder.Helpers;

/// <summary>
/// Convierte expresiones lambda a cláusulas SQL para usar en sentencias WHERE.
/// </summary>
public static class ExpressionToSqlConverter
{
    /// <summary>
    /// Convierte una expresión lambda a una cláusula WHERE SQL en formato embebido (sin parámetros).
    /// </summary>
    /// <typeparam name="T">Tipo de entidad evaluada.</typeparam>
    /// <param name="expression">Expresión booleana lambda.</param>
    /// <returns>Cláusula SQL generada.</returns>
    public static string Convert<T>(Expression<Func<T, bool>> expression)
    {
        return ParseExpression(expression.Body);
    }

    /// <summary>
    /// Analiza cualquier expresión y la convierte a SQL.
    /// </summary>
    private static string ParseExpression(Expression expr)
    {
        return expr switch
        {
            BinaryExpression binary => ParseBinary(binary),
            MethodCallExpression method => ParseMethodCall(method),
            UnaryExpression unary => ParseUnary(unary),
            MemberExpression member => ParseMember(member),
            ConstantExpression constant => FormatConstant(constant.Value),
            _ => throw new NotSupportedException($"Expresión no soportada: {expr.NodeType}")
        };
    }

    /// <summary>
    /// Convierte expresiones binarias como ==, !=, >, <, AND, OR, etc.
    /// </summary>
    private static string ParseBinary(BinaryExpression binary)
    {
        string left = ParseExpression(binary.Left);
        string right = ParseExpression(binary.Right);

        // Comparaciones con NULL → IS NULL / IS NOT NULL
        if (binary.Right is ConstantExpression constRight && constRight.Value == null)
        {
            return binary.NodeType switch
            {
                ExpressionType.Equal => $"{left} IS NULL",
                ExpressionType.NotEqual => $"{left} IS NOT NULL",
                _ => throw new NotSupportedException($"Comparación con null no soportada: {binary.NodeType}")
            };
        }

        string op = binary.NodeType switch
        {
            ExpressionType.Equal => "=",
            ExpressionType.NotEqual => "<>",
            ExpressionType.GreaterThan => ">",
            ExpressionType.LessThan => "<",
            ExpressionType.GreaterThanOrEqual => ">=",
            ExpressionType.LessThanOrEqual => "<=",
            ExpressionType.AndAlso => "AND",
            ExpressionType.OrElse => "OR",
            _ => throw new NotSupportedException($"Operador no soportado: {binary.NodeType}")
        };

        return $"({left} {op} {right})";
    }

    /// <summary>
    /// Convierte expresiones como !x.Propiedad
    /// </summary>
    private static string ParseUnary(UnaryExpression unary)
    {
        return unary.NodeType switch
        {
            ExpressionType.Not => $"NOT ({ParseExpression(unary.Operand)})",
            _ => ParseExpression(unary.Operand)
        };
    }

    /// <summary>
    /// Convierte llamadas a métodos como Contains, StartsWith, EndsWith, y listas.Contains().
    /// </summary>
    private static string ParseMethodCall(MethodCallExpression method)
    {
        // Soporte para lista.Contains(x.Prop) → IN (...)
        if (method.Method.Name == "Contains" && method.Arguments.Count == 1 && method.Object == null)
        {
            var member = ParseExpression(method.Arguments[0]);
            var values = GetValuesFromExpression(method.Arguments[0]);

            return $"{member} IN ({string.Join(", ", values)})";
        }

        // Soporte para x.Prop.Contains(valor)
        if (method.Method.Name is "Contains" or "StartsWith" or "EndsWith")
        {
            var column = ParseExpression(method.Object!);
            var value = GetValue(method.Arguments[0]);

            return method.Method.Name switch
            {
                "Contains" => $"{column} LIKE '%{value}%'",
                "StartsWith" => $"{column} LIKE '{value}%'",
                "EndsWith" => $"{column} LIKE '%{value}'",
                _ => throw new NotSupportedException($"Método no soportado: {method.Method.Name}")
            };
        }

        throw new NotSupportedException($"Llamada a método no soportada: {method.Method.Name}");
    }

    /// <summary>
    /// Convierte acceso a propiedades o constantes externas.
    /// </summary>
    private static string ParseMember(MemberExpression member)
    {
        // Si es una propiedad del parámetro, devolvemos su nombre
        if (member.Expression is ParameterExpression)
            return member.Member.Name;

        // Si es una constante capturada → evaluamos su valor
        var value = GetValue(member);
        return FormatConstant(value);
    }

    /// <summary>
    /// Evalúa una expresión y devuelve su valor en tiempo de ejecución.
    /// </summary>
    private static object? GetValue(Expression expr)
    {
        var lambda = Expression.Lambda(expr);
        return lambda.Compile().DynamicInvoke();
    }

    /// <summary>
    /// Obtiene los valores dentro de una lista para generar IN (...).
    /// </summary>
    private static IEnumerable<string> GetValuesFromExpression(Expression expr)
    {
        var value = GetValue(expr);

        if (value is IEnumerable<object> collection)
            return collection.Select(FormatConstant);

        if (value is IEnumerable enumerable)
            return enumerable.Cast<object>().Select(FormatConstant);

        return new[] { FormatConstant(value) };
    }

    /// <summary>
    /// Da formato a constantes para SQL.
    /// </summary>
    private static string FormatConstant(object? value)
    {
        return value switch
        {
            null => "NULL",
            string s => $"'{s}'",
            bool b => b ? "1" : "0",
            DateTime dt => $"'{dt:yyyy-MM-dd HH:mm:ss}'",
            _ => value!.ToString()!
        };
    }
}

using QueryBuilder.Builders;
using QueryBuilder.Helpers;
using System;
using System.Linq.Expressions;

namespace QueryBuilder.Expressions;

/// <summary>
/// Traductor de expresiones lambda para cláusulas WHERE.
/// </summary>
public static class LambdaWhereTranslator
{
    /// <summary>
    /// Traduce una expresión lambda a SQL y la agrega al builder.
    /// </summary>
    /// <typeparam name="T">Tipo de la entidad.</typeparam>
    /// <param name="builder">Instancia de SelectQueryBuilder.</param>
    /// <param name="expression">Expresión lambda booleana.</param>
    public static void Translate<T>(SelectQueryBuilder builder, Expression<Func<T, bool>> expression)
    {
        string condition = ExpressionToSqlConverter.Convert(expression);

        if (string.IsNullOrWhiteSpace(builder.WhereClause))
            builder.WhereClause = condition;
        else
            builder.WhereClause += $" AND {condition}";
    }
}
