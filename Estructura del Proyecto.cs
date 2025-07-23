private class SimpleLambdaParser : ExpressionVisitor
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
            // Es una propiedad del tipo (como c.Nombre)
            _stack.Push(node.Member.Name);
        }
        else
        {
            // Es una variable externa capturada (como "username")
            object? value = GetValue(node);
            PushValue(value);
        }
        return node;
    }

    protected override Expression VisitConstant(ConstantExpression node)
    {
        PushValue(node.Value);
        return node;
    }

    protected override Expression VisitUnary(UnaryExpression node)
    {
        if (node.Operand is MemberExpression member)
        {
            object? value = GetValue(member);
            PushValue(value);
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

    private void PushValue(object? value)
    {
        if (value is string s)
            _stack.Push($"'{s}'");
        else if (value is null)
            _stack.Push("NULL");
        else
            _stack.Push(value.ToString() ?? "NULL");
    }
}
