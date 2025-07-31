/// <summary>
/// Agrega una condición WHERE en formato SQL crudo (string libre).
/// Ejemplo: "ESTADO = 'ACTIVO' OR TIPO = 'X'"
/// </summary>
/// <param name="rawCondition">Condición SQL sin procesar.</param>
/// <returns>Instancia modificada de <see cref="SelectQueryBuilder"/>.</returns>
public SelectQueryBuilder WhereRaw(string rawCondition)
{
    if (string.IsNullOrWhiteSpace(rawCondition))
        return this;

    if (string.IsNullOrWhiteSpace(WhereClause))
        WhereClause = rawCondition;
    else
        WhereClause += $" AND {rawCondition}";

    return this;
}
