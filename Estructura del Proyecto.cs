private int? _offset;
private int? _fetch;

/// <summary>
/// Define el valor de desplazamiento de filas (OFFSET).
/// </summary>
/// <param name="offset">Cantidad de filas a omitir.</param>
/// <returns>Instancia modificada de <see cref="SelectQueryBuilder"/>.</returns>
public SelectQueryBuilder Offset(int offset)
{
    _offset = offset;
    return this;
}

/// <summary>
/// Define la cantidad de filas a recuperar después del OFFSET (FETCH NEXT).
/// </summary>
/// <param name="rowCount">Cantidad de filas a recuperar.</param>
/// <returns>Instancia modificada de <see cref="SelectQueryBuilder"/>.</returns>
public SelectQueryBuilder FetchNext(int rowCount)
{
    _fetch = rowCount;
    return this;
}

private string GetLimitClause()
{
    if (_offset.HasValue && _fetch.HasValue)
        return $" OFFSET {_offset.Value} ROWS FETCH NEXT {_fetch.Value} ROWS ONLY";
    if (_fetch.HasValue)
        return $" FETCH FIRST {_fetch.Value} ROWS ONLY";
    return string.Empty;
}
