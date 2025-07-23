using QueryBuilder.Models;
using Microsoft.AspNetCore.Http;
using System.Data.Common;

namespace RestUtilities.Connections
{
    public partial class As400ConnectionProvider // o tu clase real
    {
        /// <summary>
        /// Obtiene un <see cref="DbCommand"/> configurado con la consulta y los parámetros generados por QueryBuilder.
        /// </summary>
        /// <param name="queryResult">Consulta SQL generada mediante QueryBuilder, incluyendo parámetros.</param>
        /// <param name="context">Contexto HTTP actual, necesario para trazabilidad o uso interno de conexión.</param>
        /// <returns>Una instancia de <see cref="DbCommand"/> con SQL y parámetros listos para ejecutarse.</returns>
        public DbCommand GetDbCommand(QueryResult queryResult, HttpContext context)
        {
            // Obtiene el comando base desde la implementación existente
            var command = GetDbCommand(context);

            // Asigna el SQL generado por QueryBuilder
            command.CommandText = queryResult.Sql;

            // Asigna los parámetros generados por QueryBuilder
            foreach (var param in queryResult.Parameters)
            {
                var dbParam = command.CreateParameter();
                dbParam.ParameterName = param.Key;
                dbParam.Value = param.Value ?? DBNull.Value;
                command.Parameters.Add(dbParam);
            }

            return command;
        }
    }
}

namespace QueryBuilder.Models;

/// <summary>
/// Representa el resultado de una consulta SQL generada por QueryBuilder.
/// </summary>
public class QueryResult
{
    /// <summary>
    /// Consulta SQL generada como texto, con parámetros representados como @p0, @p1, etc.
    /// </summary>
    public string Sql { get; set; } = string.Empty;

    /// <summary>
    /// Diccionario de parámetros a ser utilizados por DbCommand.
    /// Llaves como @p0, @p1... y sus respectivos valores.
    /// </summary>
    public Dictionary<string, object?> Parameters { get; set; } = new();
}

using QueryBuilder.Models;
using System.Linq.Expressions;

namespace QueryBuilder.Expressions;

/// <summary>
/// Traduce una expresión lambda a SQL dinámico con parámetros (ej. @p0, @p1) y valores asociados.
/// </summary>
public static class LambdaWhereTranslator
{
    /// <summary>
    /// Traduce la expresión y devuelve la cláusula WHERE con parámetros y sus valores.
    /// </summary>
    /// <param name="expression">Expresión lambda (ej: x => x.Nombre == nombre)</param>
    /// <returns>Objeto QueryResult con SQL parcial y parámetros.</returns>
    public static (string whereClause, Dictionary<string, object?> parameters) Translate(Expression expression)
    {
        var parser = new ParameterizedLambdaParser();
        var whereClause = parser.Parse(expression);
        return (whereClause, parser.Parameters);
    }

    /// <summary>
    /// Clase interna encargada de recorrer y traducir la expresión a SQL parametrizado.
    /// </summary>
    private class ParameterizedLambdaParser : ExpressionVisitor
    {
        private readonly Stack<string> _stack = new();
        private readonly Dictionary<string, object?> _parameters = new();
        private int _paramCounter = 0;

        public Dictionary<string, object?> Parameters => _parameters;

        public string Parse(Expression expression)
        {
            Visit(expression);
            return _stack.Count > 0 ? _stack.Pop() : string.Empty;
        }

        protected override Expression VisitBinary(BinaryExpression node)
        {
            Visit(node.Left);
            string left = _stack.Pop();

            Visit(node.Right);
            string right = _stack.Pop();

            string op = node.NodeType switch
            {
                ExpressionType.Equal => "=",
                ExpressionType.NotEqual => "<>",
                ExpressionType.GreaterThan => ">",
                ExpressionType.GreaterThanOrEqual => ">=",
                ExpressionType.LessThan => "<",
                ExpressionType.LessThanOrEqual => "<=",
                ExpressionType.AndAlso => "AND",
                ExpressionType.OrElse => "OR",
                _ => throw new NotSupportedException($"Operador no soportado: {node.NodeType}")
            };

            _stack.Push($"({left} {op} {right})");
            return node;
        }

        protected override Expression VisitMember(MemberExpression node)
        {
            if (node.Expression is ParameterExpression)
            {
                _stack.Push(node.Member.Name);
            }
            else
            {
                object? value = GetValue(node);
                PushParameter(value);
            }
            return node;
        }

        protected override Expression VisitConstant(ConstantExpression node)
        {
            PushParameter(node.Value);
            return node;
        }

        protected override Expression VisitUnary(UnaryExpression node)
        {
            if (node.Operand is MemberExpression member)
            {
                object? value = GetValue(member);
                PushParameter(value);
            }
            return base.VisitUnary(node);
        }

        private void PushParameter(object? value)
        {
            string paramName = $"@p{_paramCounter++}";
            _parameters[paramName] = value;
            _stack.Push(paramName);
        }

        private static object? GetValue(MemberExpression member)
        {
            var objectMember = Expression.Convert(member, typeof(object));
            var getterLambda = Expression.Lambda<Func<object>>(objectMember);
            var getter = getterLambda.Compile();
            return getter();
        }
    }
}

using QueryBuilder.Expressions;
using QueryBuilder.Models;
using System.Linq.Expressions;
using System.Text;

namespace QueryBuilder.Builders;

/// <summary>
/// Constructor especializado para generar consultas SELECT dinámicamente con parámetros.
/// </summary>
public class SelectQueryBuilder
{
    private readonly string _fullTableName;
    private readonly List<string> _columns = new();
    private string? _whereClause;
    private Dictionary<string, object?> _parameters = new();

    /// <summary>
    /// Inicializa un nuevo generador SELECT para una tabla específica.
    /// </summary>
    /// <param name="tableName">Nombre de la tabla.</param>
    /// <param name="library">Nombre opcional de la biblioteca (por ejemplo, "CYBERDTA").</param>
    public SelectQueryBuilder(string tableName, string? library = null)
    {
        _fullTableName = string.IsNullOrWhiteSpace(library)
            ? tableName
            : $"{library}.{tableName}";
    }

    /// <summary>
    /// Define las columnas a seleccionar.
    /// </summary>
    /// <param name="columns">Lista de columnas SQL.</param>
    /// <returns>Instancia del generador SELECT.</returns>
    public SelectQueryBuilder Select(params string[] columns)
    {
        _columns.AddRange(columns);
        return this;
    }

    /// <summary>
    /// Define la cláusula WHERE a partir de una expresión lambda con parámetros.
    /// </summary>
    /// <typeparam name="T">Tipo de DTO o clase base.</typeparam>
    /// <param name="expression">Expresión booleana (ej. x => x.Nombre == valor).</param>
    /// <returns>Instancia del generador SELECT.</returns>
    public SelectQueryBuilder Where<T>(Expression<Func<T, bool>> expression)
    {
        var (clause, parameters) = LambdaWhereTranslator.Translate(expression);
        _whereClause = clause;
        _parameters = parameters;
        return this;
    }

    /// <summary>
    /// Construye el resultado de la consulta incluyendo SQL y parámetros.
    /// </summary>
    /// <returns>Objeto QueryResult con el SQL y sus parámetros asociados.</returns>
    public QueryResult Build()
    {
        var sb = new StringBuilder();
        sb.Append("SELECT ");
        sb.Append(_columns.Count > 0 ? string.Join(", ", _columns) : "*");
        sb.Append(" FROM ").Append(_fullTableName);

        if (!string.IsNullOrWhiteSpace(_whereClause))
        {
            sb.Append(" WHERE ").Append(_whereClause);
        }

        return new QueryResult
        {
            Sql = sb.ToString(),
            Parameters = _parameters
        };
    }
}
