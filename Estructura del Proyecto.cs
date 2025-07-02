using System;
using System.Globalization;

namespace RestUtilities.QueryBuilder.Extensions
{
    /// <summary>
    /// Métodos de extensión para manipulación de cadenas útiles en la generación de queries SQL.
    /// </summary>
    public static class StringExtensions
    {
        /// <summary>
        /// Convierte una cadena a PascalCase eliminando guiones bajos y capitalizando cada palabra.
        /// </summary>
        /// <param name="value">Texto de entrada.</param>
        /// <returns>Texto convertido a PascalCase.</returns>
        public static string ToPascalCase(this string value)
        {
            if (string.IsNullOrEmpty(value))
                return value;

            var words = value.Split('_');
            for (int i = 0; i < words.Length; i++)
                words[i] = CultureInfo.InvariantCulture.TextInfo.ToTitleCase(words[i].ToLower());

            return string.Concat(words);
        }

        /// <summary>
        /// Escapa comillas simples para evitar errores o inyección en valores de tipo string.
        /// </summary>
        /// <param name="value">Valor de entrada.</param>
        /// <returns>Valor escapado para SQL.</returns>
        public static string EscapeSql(this string value)
        {
            return value?.Replace("'", "''");
        }
    }
}

using System;
using System.Linq.Expressions;
using System.Text;

namespace RestUtilities.QueryBuilder.Extensions
{
    /// <summary>
    /// Métodos de extensión para análisis de expresiones lambda.
    /// Útil para convertir expresiones como c => c.Nombre == "Pedro" a SQL.
    /// </summary>
    public static class ExpressionExtensions
    {
        /// <summary>
        /// Convierte una expresión lambda simple en una condición SQL.
        /// </summary>
        /// <typeparam name="T">Tipo del objeto origen.</typeparam>
        /// <param name="expression">Expresión lambda.</param>
        /// <returns>Condición SQL equivalente.</returns>
        public static string ToSqlCondition<T>(this Expression<Func<T, bool>> expression)
        {
            var visitor = new SqlExpressionVisitor();
            visitor.Visit(expression);
            return visitor.Condition;
        }
    }

    /// <summary>
    /// Visitor personalizado para analizar árboles de expresión y traducirlos a SQL.
    /// </summary>
    internal class SqlExpressionVisitor : ExpressionVisitor
    {
        private readonly StringBuilder _sb = new();

        /// <summary>
        /// Condición SQL resultante.
        /// </summary>
        public string Condition => _sb.ToString();

        protected override Expression VisitBinary(BinaryExpression node)
        {
            _sb.Append("(");
            Visit(node.Left);
            _sb.Append($" {GetSqlOperator(node.NodeType)} ");
            Visit(node.Right);
            _sb.Append(")");
            return node;
        }

        protected override Expression VisitMember(MemberExpression node)
        {
            _sb.Append(node.Member.Name);
            return node;
        }

        protected override Expression VisitConstant(ConstantExpression node)
        {
            _sb.Append(node.Type == typeof(string)
                ? $"'{node.Value}'"
                : node.Value?.ToString());
            return node;
        }

        private static string GetSqlOperator(ExpressionType type) => type switch
        {
            ExpressionType.Equal => "=",
            ExpressionType.NotEqual => "<>",
            ExpressionType.GreaterThan => ">",
            ExpressionType.LessThan => "<",
            ExpressionType.GreaterThanOrEqual => ">=",
            ExpressionType.LessThanOrEqual => "<=",
            ExpressionType.AndAlso => "AND",
            ExpressionType.OrElse => "OR",
            _ => throw new NotSupportedException($"Operador no soportado: {type}")
        };
    }
}

