using System;
using System.Linq.Expressions;

namespace QueryBuilder.Helpers;

/// <summary>
/// Utilidad para convertir expresiones lambda en condiciones SQL simples.
/// Esta versión soporta expresiones binarias básicas (ej. x => x.Id == 5).
/// </summary>
public static class SqlExpressionParser
{
    /// <summary>
    /// Convierte una expresión lambda booleana en una cláusula SQL WHERE.
    /// </summary>
    /// <typeparam name="T">Tipo de modelo.</typeparam>
    /// <param name="expression">Expresión booleana.</param>
    /// <returns>Cadena SQL con la condición WHERE equivalente.</returns>
    public static string Parse<T>(Expression<Func<T, bool>> expression)
    {
        return new SimpleSqlVisitor().VisitExpression(expression.Body);
    }

    private class SimpleSqlVisitor : ExpressionVisitor
    {
        public string VisitExpression(Expression exp)
        {
            return exp switch
            {
                BinaryExpression bin => VisitBinary(bin),
                MemberExpression member => member.Member.Name,
                ConstantExpression constant => FormatConstant(constant.Value),
                _ => throw new NotSupportedException($"Expresión no soportada: {exp.GetType().Name}")
            };
        }

        private string VisitBinary(BinaryExpression node)
        {
            var left = VisitExpression(node.Left);
            var right = VisitExpression(node.Right);
            var op = node.NodeType switch
            {
                ExpressionType.Equal => "=",
                ExpressionType.NotEqual => "<>",
                ExpressionType.GreaterThan => ">",
                ExpressionType.LessThan => "<",
                ExpressionType.GreaterThanOrEqual => ">=",
                ExpressionType.LessThanOrEqual => "<=",
                _ => throw new NotSupportedException($"Operador no soportado: {node.NodeType}")
            };

            return $"{left} {op} {right}";
        }

        private string FormatConstant(object? value)
        {
            return value switch
            {
                null => "NULL",
                string s => $"'{s}'",
                DateTime dt => $"'{dt:yyyy-MM-dd HH:mm:ss}'",
                bool b => b ? "1" : "0",
                _ => value.ToString()
            };
        }
    }
}

/// <summary>
/// Obtiene un diccionario con los pares columna/valor para el modelo especificado.
/// </summary>
public static Dictionary<string, object> GetColumnValuePairs<T>(T instance)
{
    var result = new Dictionary<string, object>();
    var props = typeof(T).GetProperties();

    foreach (var prop in props)
    {
        if (prop.GetCustomAttribute<SqlIgnoreAttribute>() != null)
            continue;

        var nameAttr = prop.GetCustomAttribute<SqlColumnNameAttribute>();
        var columnName = nameAttr?.Name ?? prop.Name;
        var value = prop.GetValue(instance);
        result[columnName] = value ?? DBNull.Value;
    }

    return result;
}




