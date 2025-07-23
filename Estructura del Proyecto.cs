/// <summary>
/// Dirección de ordenamiento para una columna en ORDER BY.
/// </summary>
public enum SortDirection
{
    Asc,
    Desc
}

/// <summary>
/// Ordena por una sola columna especificando la dirección.
/// </summary>
/// <param name="column">Nombre de la columna.</param>
/// <param name="direction">Dirección del ordenamiento (Asc o Desc).</param>
public SelectQueryBuilder OrderBy(string column, SortDirection direction = SortDirection.Asc)
{
    _orderBy.Add($"{column} {direction.ToString().ToUpper()}");
    return this;
}

/// <summary>
/// Ordena por múltiples columnas especificando la dirección de cada una.
/// </summary>
/// <param name="columns">Lista de columnas con su dirección de orden.</param>
public SelectQueryBuilder OrderBy(params (string Column, SortDirection Direction)[] columns)
{
    foreach (var (column, direction) in columns)
    {
        _orderBy.Add($"{column} {direction.ToString().ToUpper()}");
    }
    return this;
}
