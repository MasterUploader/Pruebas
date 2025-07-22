using QueryBuilder.Builders;

namespace QueryBuilder.Core;

/// <summary>
/// Punto de entrada principal para construir consultas SQL.
/// </summary>
public static class QueryBuilder
{
    /// <summary>
    /// Inicia la construcción de una consulta SELECT.
    /// </summary>
    /// <param name="tableName">Nombre de la tabla.</param>
    public static SelectQueryBuilder From(string tableName)
    {
        return new SelectQueryBuilder(tableName);
    }
}

using QueryBuilder.Expressions;
using QueryBuilder.Models;
using System.Linq.Expressions;
using System.Text;

namespace QueryBuilder.Builders;

/// <summary>
/// Constructor para consultas SELECT dinámicas.
/// </summary>
public class SelectQueryBuilder
{
    private readonly string _tableName;
    private readonly List<string> _columns = new();
    private string? _whereClause;

    public SelectQueryBuilder(string tableName)
    {
        _tableName = tableName;
    }

    /// <summary>
    /// Establece las columnas a seleccionar.
    /// </summary>
    public SelectQueryBuilder Select(params string[] columns)
    {
        _columns.AddRange(columns);
        return this;
    }

    /// <summary>
    /// Establece la cláusula WHERE mediante expresión lambda.
    /// </summary>
    public SelectQueryBuilder Where<T>(Expression<Func<T, bool>> expression)
    {
        _whereClause = LambdaWhereTranslator.Translate(expression);
        return this;
    }

    /// <summary>
    /// Construye y retorna el SQL resultante.
    /// </summary>
    public QueryResult Build()
    {
        var sb = new StringBuilder();
        sb.Append("SELECT ");
        sb.Append(_columns.Any() ? string.Join(", ", _columns) : "*");
        sb.Append(" FROM ").Append(_tableName);

        if (!string.IsNullOrWhiteSpace(_whereClause))
        {
            sb.Append(" WHERE ").Append(_whereClause);
        }

        return new QueryResult
        {
            Sql = sb.ToString()
        };
    }
}

using System.Linq.Expressions;

namespace QueryBuilder.Expressions;

/// <summary>
/// Traduce expresiones lambda a cláusulas WHERE SQL simples.
/// </summary>
public static class LambdaWhereTranslator
{
    public static string Translate(Expression expression)
    {
        return new SimpleLambdaParser().Parse(expression);
    }

    private class SimpleLambdaParser : ExpressionVisitor
    {
        private Stack<string> _stack = new();

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
            _stack.Push(node.Member.Name);
            return node;
        }

        protected override Expression VisitConstant(ConstantExpression node)
        {
            var value = node.Type == typeof(string) ? $"'{node.Value}'" : node.Value?.ToString() ?? "NULL";
            _stack.Push(value);
            return node;
        }
    }
}

namespace QueryBuilder.Models;

/// <summary>
/// Representa el resultado de una consulta generada.
/// </summary>
public class QueryResult
{
    /// <summary>
    /// SQL generado.
    /// </summary>
    public string Sql { get; set; } = string.Empty;

    /// <summary>
    /// Parámetros (para futuras versiones).
    /// </summary>
    public Dictionary<string, object?> Parameters { get; set; } = new();
}

