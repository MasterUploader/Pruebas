private static string ParseExpression(Expression expr)
{
    return expr switch
    {
        BinaryExpression binary => ParseBinary(binary),
        MethodCallExpression method => ParseMethodCall(method),
        UnaryExpression unary => ParseUnary(unary),
        MemberExpression member =>
        {
            if (member.Expression is ParameterExpression)
                return member.Member.Name;

            var value = GetValue(member);
            return FormatConstant(value);
        },
        ConstantExpression constant => FormatConstant(constant.Value),
        _ => throw new NotSupportedException($"Expresión no soportada: {expr.NodeType}")
    };
}

