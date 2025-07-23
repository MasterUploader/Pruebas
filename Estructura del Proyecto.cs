/// <summary>
/// Construye y devuelve la consulta SQL generada.
/// </summary>
public QueryResult Build()
{
    var sb = new StringBuilder();

    // SELECT
    sb.Append("SELECT ");

    if (_columns.Count == 0)
    {
        sb.Append("*");
    }
    else
    {
        var columnSql = _columns.Select(c =>
            string.IsNullOrWhiteSpace(c.Alias)
                ? c.Column
                : $"{c.Column} AS {c.Alias}"
        );
        sb.Append(string.Join(", ", columnSql));
    }

    // FROM
    sb.Append(" FROM ");
    if (!string.IsNullOrWhiteSpace(_library))
        sb.Append($"{_library}.");
    sb.Append(_tableName);
    if (!string.IsNullOrWhiteSpace(_tableAlias))
        sb.Append($" {_tableAlias}");

    // JOINs
    foreach (var join in _joins)
    {
        sb.Append($" {join.JoinType} JOIN ");
        if (!string.IsNullOrWhiteSpace(join.Library))
            sb.Append($"{join.Library}.");
        sb.Append(join.TableName);
        if (!string.IsNullOrWhiteSpace(join.Alias))
            sb.Append($" {join.Alias}");
        sb.Append($" ON {join.LeftColumn} = {join.RightColumn}");
    }

    // WHERE
    if (!string.IsNullOrWhiteSpace(_whereClause))
        sb.Append(" WHERE ").Append(_whereClause);

    // ORDER BY
    if (_orderBy.Count > 0)
        sb.Append(" ORDER BY ").Append(string.Join(", ", _orderBy));

    // LIMIT (AS400)
    if (_limit.HasValue)
        sb.Append($" FETCH FIRST {_limit.Value} ROWS ONLY");

    return new QueryResult
    {
        Sql = sb.ToString()
    };
}
