if (_orderBy.Count > 0)
{
    sb.Append(" ORDER BY ");
    var orderParts = _orderBy.Select(order =>
    {
        var (col, dir) = order;
        return dir switch
        {
            SortDirection.Asc => $"{col} ASC",
            SortDirection.Desc => $"{col} DESC",
            SortDirection.None or _ => col // No se incluye ASC ni DESC
        };
    });
    sb.Append(string.Join(", ", orderParts));
}
