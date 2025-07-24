private int? _offset;
private int? _fetchNext;

/// <summary>
/// Establece el número de filas a omitir antes de comenzar a devolver filas (OFFSET).
/// Compatible con paginación en AS400 y otros motores que lo soporten.
/// </summary>
/// <param name="rowCount">Número de filas a omitir.</param>
public SelectQueryBuilder Offset(int rowCount)
{
    _offset = rowCount;
    return this;
}

/// <summary>
/// Establece la cantidad de filas a devolver después del OFFSET (FETCH NEXT).
/// Compatible con paginación en AS400 y otros motores que lo soporten.
/// </summary>
/// <param name="rowCount">Número de filas a devolver.</param>
public SelectQueryBuilder FetchNext(int rowCount)
{
    _fetchNext = rowCount;
    return this;
}

/// <summary>
/// Construye la cláusula de paginación usando OFFSET y FETCH NEXT si están definidos.
/// </summary>
/// <returns>Texto SQL correspondiente a la cláusula de paginación.</returns>
private string GetLimitClause()
{
    if (_offset.HasValue && _fetchNext.HasValue)
        return $" OFFSET {_offset.Value} ROWS FETCH NEXT {_fetchNext.Value} ROWS ONLY";

    if (_fetchNext.HasValue)
        return $" FETCH FIRST {_fetchNext.Value} ROWS ONLY";

    return string.Empty;
}

