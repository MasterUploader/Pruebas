/// <summary>
/// Ordena por una sola columna.
/// </summary>
/// <param name="column">Nombre de la columna.</param>
/// <param name="descending">Indica si se ordena en forma descendente.</param>
public SelectQueryBuilder OrderBy(string column, bool descending = false)
{
    var direction = descending ? "DESC" : "ASC";
    _orderBy.Add($"{column} {direction}");
    return this;
}

/// <summary>
/// Ordena por múltiples columnas especificando si cada una es descendente.
/// </summary>
/// <param name="columns">Lista de columnas y su dirección (true = DESC).</param>
public SelectQueryBuilder OrderBy(params (string Column, bool Descending)[] columns)
{
    foreach (var (column, descending) in columns)
    {
        var direction = descending ? "DESC" : "ASC";
        _orderBy.Add($"{column} {direction}");
    }
    return this;
}
