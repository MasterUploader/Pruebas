if (_orderBy.Count > 0)
{
    sb.Append(" ORDER BY ");
    sb.Append(string.Join(", ", _orderBy.Select(o =>
        o.Direction == SortDirection.None
            ? o.Column
            : $"{o.Column} {o.Direction.ToString().ToUpper()}")));
}
