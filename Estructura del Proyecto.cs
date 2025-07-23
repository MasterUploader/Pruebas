using System.Linq.Expressions;

namespace QueryBuilder.Expressions;

/// <summary>
/// Traduce una expresión lambda a SQL embebido directamente (sin parámetros).
/// </summary>
public static class LambdaWhereTranslator
{
    public static string Translate(Expression expression)
    {
        return new InlineLambdaParser().Parse(expression);
    }

    private class InlineLambdaParser : ExpressionVisitor
    {
        private readonly Stack<string> _stack = new();

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
                PushLiteral(value);
            }
            return node;
        }

        protected override Expression VisitConstant(ConstantExpression node)
        {
            PushLiteral(node.Value);
            return node;
        }

        protected override Expression VisitUnary(UnaryExpression node)
        {
            if (node.Operand is MemberExpression member)
            {
                object? value = GetValue(member);
                PushLiteral(value);
            }
            return base.VisitUnary(node);
        }

        private static object? GetValue(MemberExpression member)
        {
            var objectMember = Expression.Convert(member, typeof(object));
            var getterLambda = Expression.Lambda<Func<object>>(objectMember);
            var getter = getterLambda.Compile();
            return getter();
        }

        private void PushLiteral(object? value)
        {
            if (value is string s)
                _stack.Push($"'{s}'");
            else if (value is DateTime dt)
                _stack.Push($"'{dt:yyyy-MM-dd HH:mm:ss}'");
            else if (value is null)
                _stack.Push("NULL");
            else
                _stack.Push(value.ToString() ?? "NULL");
        }
    }
}

using QueryBuilder.Expressions;
using QueryBuilder.Models;
using System.Linq.Expressions;
using System.Text;

namespace QueryBuilder.Builders;

/// <summary>
/// Generador de consultas SELECT que incrusta directamente los valores en el SQL.
/// </summary>
public class SelectQueryBuilder
{
    private readonly string _fullTableName;
    private readonly List<string> _columns = new();
    private string? _whereClause;

    /// <summary>
    /// Inicializa un nuevo generador SELECT para una tabla específica.
    /// </summary>
    /// <param name="tableName">Nombre de la tabla.</param>
    /// <param name="library">Nombre de la biblioteca (opcional, útil en AS400).</param>
    public SelectQueryBuilder(string tableName, string? library = null)
    {
        _fullTableName = string.IsNullOrWhiteSpace(library)
            ? tableName
            : $"{library}.{tableName}";
    }

    /// <summary>
    /// Define las columnas a seleccionar.
    /// </summary>
    /// <param name="columns">Nombres de columnas a incluir en el SELECT.</param>
    /// <returns>Instancia del generador para encadenamiento.</returns>
    public SelectQueryBuilder Select(params string[] columns)
    {
        _columns.AddRange(columns);
        return this;
    }

    /// <summary>
    /// Define la cláusula WHERE usando una expresión lambda con valores embebidos.
    /// </summary>
    /// <typeparam name="T">Tipo del objeto para análisis de expresión.</typeparam>
    /// <param name="expression">Expresión condicional.</param>
    /// <returns>Instancia del generador para encadenamiento.</returns>
    public SelectQueryBuilder Where<T>(Expression<Func<T, bool>> expression)
    {
        _whereClause = LambdaWhereTranslator.Translate(expression);
        return this;
    }

    /// <summary>
    /// Construye la consulta SQL como texto completo.
    /// </summary>
    /// <returns>Instancia de QueryResult con el SQL generado.</returns>
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
            Sql = sb.ToString()
            // No se necesita Parameters
        };
    }
}
