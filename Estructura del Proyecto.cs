using System.Linq.Expressions;
using System.Text;

namespace QueryBuilder.Helpers;

/// <summary>
/// Convierte expresiones lambda a cláusulas SQL.
/// </summary>
public static class ExpressionToSqlConverter
{
    /// <summary>
    /// Convierte una expresión lambda a una cláusula WHERE SQL.
    /// </summary>
    /// <typeparam name="T">Tipo de entidad.</typeparam>
    /// <param name="expression">Expresión booleana.</param>
    public static string Convert<T>(Expression<Func<T, bool>> expression)
    {
        return ParseExpression(expression.Body);
    }

    private static string ParseExpression(Expression expr)
    {
        return expr switch
        {
            BinaryExpression binary => ParseBinary(binary),
            MethodCallExpression method => ParseMethodCall(method),
            UnaryExpression unary => ParseUnary(unary),
            MemberExpression member => member.Member.Name,
            ConstantExpression constant => FormatConstant(constant.Value),
            _ => throw new NotSupportedException($"Expresión no soportada: {expr.NodeType}")
        };
    }

    private static string ParseBinary(BinaryExpression binary)
    {
        string left = ParseExpression(binary.Left);
        string right = ParseExpression(binary.Right);

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

        // Para condiciones lógicas compuestas (AND, OR)
        if (op is "AND" or "OR")
            return $"({left} {op} {right})";

        return $"({left} {op} {right})";
    }

    private static string ParseUnary(UnaryExpression unary)
    {
        return unary.NodeType switch
        {
            ExpressionType.Not => $"NOT ({ParseExpression(unary.Operand)})",
            _ => throw new NotSupportedException($"Unary no soportado: {unary.NodeType}")
        };
    }

    private static string ParseMethodCall(MethodCallExpression method)
    {
        // caso: lista.Contains(x.Prop) → IN (...)
        if (method.Method.Name == "Contains" && method.Arguments.Count == 1)
        {
            if (method.Object == null)
            {
                var member = ParseExpression(method.Arguments[0]);
                var values = GetValuesFromExpression(method.Object ?? method.Arguments[0]);
                return $"{member} IN ({string.Join(", ", values)})";
            }

            var column = ParseExpression(method.Object);
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

    private static object? GetValue(Expression expr)
    {
        var lambda = Expression.Lambda(expr);
        return lambda.Compile().DynamicInvoke();
    }

    private static IEnumerable<string> GetValuesFromExpression(Expression expr)
    {
        var lambda = Expression.Lambda(expr);
        var value = lambda.Compile().DynamicInvoke();

        if (value is IEnumerable<object> collection)
            return collection.Select(FormatConstant);

        if (value is IEnumerable enumerable)
            return enumerable.Cast<object>().Select(FormatConstant);

        return new[] { FormatConstant(value) };
    }

    private static string FormatConstant(object? value)
    {
        return value switch
        {
            null => "NULL",
            string s => $"'{s}'",
            bool b => b ? "1" : "0",
            _ => value!.ToString()!
        };
    }
}
