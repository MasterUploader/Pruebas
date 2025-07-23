using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace QueryBuilder.Helpers
{
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
}

using System;
using System.Linq.Expressions;
using QueryBuilder.Helpers;
using QueryBuilder.Models;

namespace QueryBuilder.Translators;

/// <summary>
/// Traductor de expresiones lambda para cláusulas WHERE.
/// </summary>
public static class LambdaWhereTranslator
{
    /// <summary>
    /// Traduce una expresión lambda y la agrega a la cláusula WHERE del contexto de consulta.
    /// </summary>
    /// <typeparam name="T">Tipo de entidad sobre la que se construye la consulta.</typeparam>
    /// <param name="context">Contexto de construcción que contiene la cláusula WHERE.</param>
    /// <param name="expression">Expresión lambda booleana que representa una condición.</param>
    public static void Translate<T>(QueryTranslationContext context, Expression<Func<T, bool>> expression)
    {
        string condition = ExpressionToSqlConverter.Convert(expression);

        if (string.IsNullOrWhiteSpace(context.WhereClause))
            context.WhereClause = condition;
        else
            context.WhereClause += $" AND {condition}";
    }
}

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;
using QueryBuilder.Models;
using QueryBuilder.Translators;

namespace QueryBuilder.Builders;

/// <summary>
/// Constructor de consultas SQL tipo SELECT.
/// </summary>
public class SelectQueryBuilder
{
    private readonly QueryTranslationContext _context;

    /// <summary>
    /// Inicializa una nueva instancia del generador de consultas SELECT.
    /// </summary>
    /// <param name="tableName">Nombre de la tabla.</param>
    /// <param name="schema">Esquema o biblioteca de la tabla.</param>
    public SelectQueryBuilder(string tableName, string? schema = null)
    {
        _context = new QueryTranslationContext
        {
            TableName = tableName,
            Schema = schema
        };
    }

    /// <summary>
    /// Agrega columnas a la cláusula SELECT.
    /// </summary>
    /// <param name="columns">Nombres de columnas a seleccionar.</param>
    public SelectQueryBuilder Select(params string[] columns)
    {
        _context.SelectColumns.AddRange(columns);
        return this;
    }

    /// <summary>
    /// Agrega una cláusula WHERE a partir de una expresión lambda.
    /// Si se llama varias veces, se concatenan con AND.
    /// </summary>
    /// <typeparam name="T">Tipo de entidad usada para la expresión.</typeparam>
    /// <param name="predicate">Expresión lambda que representa la condición.</param>
    public SelectQueryBuilder Where<T>(Expression<Func<T, bool>> predicate)
    {
        LambdaWhereTranslator.Translate(_context, predicate);
        return this;
    }

    /// <summary>
    /// Genera el SQL final como texto.
    /// </summary>
    public string Build()
    {
        var sb = new StringBuilder();
        string table = string.IsNullOrWhiteSpace(_context.Schema)
            ? _context.TableName
            : $"{_context.Schema}.{_context.TableName}";

        string columns = _context.SelectColumns.Count > 0
            ? string.Join(", ", _context.SelectColumns)
            : "*";

        sb.Append($"SELECT {columns} FROM {table}");

        if (!string.IsNullOrWhiteSpace(_context.WhereClause))
            sb.Append($" WHERE {_context.WhereClause}");

        return sb.ToString();
    }
}
