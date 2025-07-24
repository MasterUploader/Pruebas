/// <summary>
/// Agrega una cláusula WHERE NOT EXISTS con una subconsulta, aplicable en INSERT ... SELECT.
/// </summary>
/// <param name="subquery">Subconsulta a evaluar en NOT EXISTS.</param>
/// <returns>Instancia modificada de <see cref="InsertQueryBuilder"/>.</returns>
public InsertQueryBuilder WhereNotExists(Subquery subquery)
{
    _whereClause = $"NOT EXISTS ({subquery.Sql})";
    return this;
}

/// <summary>
/// Agrega una fila de valores sin formato automático, útil para funciones como GETDATE().
/// </summary>
/// <param name="values">Valores SQL en crudo, como funciones o expresiones directas.</param>
/// <returns>Instancia modificada de <see cref="InsertQueryBuilder"/>.</returns>
public InsertQueryBuilder ValuesRaw(params string[] values)
{
    _values.Add(values.ToList());
    return this;
}

private string? _comment;

/// <summary>
/// Agrega un comentario SQL al inicio del INSERT para trazabilidad o debugging.
/// </summary>
/// <param name="comment">Texto del comentario.</param>
/// <returns>Instancia modificada de <see cref="InsertQueryBuilder"/>.</returns>
public InsertQueryBuilder WithComment(string comment)
{
    if (!string.IsNullOrWhiteSpace(comment))
        _comment = $"-- {comment}";
    return this;
}

var sb = new StringBuilder();

if (!string.IsNullOrWhiteSpace(_comment))
    sb.AppendLine(_comment);
