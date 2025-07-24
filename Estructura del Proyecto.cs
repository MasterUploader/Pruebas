/// <summary>
/// Agrega una cláusula HAVING EXISTS con una subconsulta.
/// </summary>
/// <param name="subquery">Subconsulta que se evaluará con EXISTS.</param>
/// <returns>Instancia modificada de <see cref="SelectQueryBuilder"/>.</returns>
public SelectQueryBuilder HavingExists(Subquery subquery)
{
    if (subquery == null || string.IsNullOrWhiteSpace(subquery.Sql))
        return this;

    var clause = $"EXISTS ({subquery.Sql})";

    if (string.IsNullOrWhiteSpace(HavingClause))
        HavingClause = clause;
    else
        HavingClause += $" AND {clause}";

    return this;
}

/// <summary>
/// Agrega una cláusula HAVING NOT EXISTS con una subconsulta.
/// </summary>
/// <param name="subquery">Subconsulta que se evaluará con NOT EXISTS.</param>
/// <returns>Instancia modificada de <see cref="SelectQueryBuilder"/>.</returns>
public SelectQueryBuilder HavingNotExists(Subquery subquery)
{
    if (subquery == null || string.IsNullOrWhiteSpace(subquery.Sql))
        return this;

    var clause = $"NOT EXISTS ({subquery.Sql})";

    if (string.IsNullOrWhiteSpace(HavingClause))
        HavingClause = clause;
    else
        HavingClause += $" AND {clause}";

    return this;
}

var subquery = new Subquery("SELECT 1 FROM LOGS L WHERE L.USERID = U.USERID");

var query = QueryBuilder.Core.QueryBuilder
    .From("USUARIOS", "MYLIB")
    .Select("USERID", "NOMBRE")
    .GroupBy("USERID", "NOMBRE")
    .HavingExists(subquery)
    .Build();
