private static string ParseBinary(BinaryExpression binary)
{
    string left = ParseExpression(binary.Left);
    string right = ParseExpression(binary.Right);

    // Evaluamos el valor real del lado derecho (aunque no sea ConstantExpression)
    var rightValue = GetValue(binary.Right);
    bool isRightNull = rightValue == null;

    if (isRightNull)
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

private static object? GetValue(Expression expr)
{
    var lambda = Expression.Lambda(expr);
    return lambda.Compile().DynamicInvoke();
}
