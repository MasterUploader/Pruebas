/// <summary>
/// Agrega una condición WHERE con una función SQL directamente como string.
/// Ejemplo: "UPPER(NOMBRE) = 'PEDRO'"
/// </summary>
/// <param name="sqlFunctionCondition">Condición completa en SQL.</param>
public SelectQueryBuilder WhereFunction(string sqlFunctionCondition)
{
    if (string.IsNullOrWhiteSpace(sqlFunctionCondition))
        return this;

    if (string.IsNullOrWhiteSpace(WhereClause))
        WhereClause = sqlFunctionCondition;
    else
        WhereClause += $" AND {sqlFunctionCondition}";

    return this;
}

/// <summary>
/// Agrega una condición HAVING con una función SQL directamente como string.
/// Ejemplo: "SUM(CANTIDAD) > 10"
/// </summary>
/// <param name="sqlFunctionCondition">Condición completa en SQL.</param>
public SelectQueryBuilder HavingFunction(string sqlFunctionCondition)
{
    if (string.IsNullOrWhiteSpace(sqlFunctionCondition))
        return this;

    if (string.IsNullOrWhiteSpace(HavingClause))
        HavingClause = sqlFunctionCondition;
    else
        HavingClause += $" AND {sqlFunctionCondition}";

    return this;
}

var ordered = _orderBy.Select(o =>
{
    var col = _aliasMap.TryGetValue(o.Column, out var alias) ? alias : o.Column;
    return $"{col} {o.Direction.ToString().ToUpper()}";
});
