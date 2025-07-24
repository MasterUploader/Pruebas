/// <summary>
/// Agrega una condición EXISTS a la cláusula WHERE con una subconsulta generada dinámicamente.
/// </summary>
/// <param name="subqueryBuilderAction">
/// Acción que configura un nuevo <see cref="SelectQueryBuilder"/> para representar la subconsulta dentro de EXISTS.
/// </param>
/// <returns>Instancia modificada de <see cref="SelectQueryBuilder"/>.</returns>
public SelectQueryBuilder WhereExists(Action<SelectQueryBuilder> subqueryBuilderAction)
{
    var subqueryBuilder = new SelectQueryBuilder("DUMMY"); // El nombre se sobreescribirá
    subqueryBuilderAction(subqueryBuilder);
    var subquerySql = subqueryBuilder.Build().Sql;

    var existsClause = $"EXISTS ({subquerySql})";

    if (string.IsNullOrWhiteSpace(WhereClause))
        WhereClause = existsClause;
    else
        WhereClause += $" AND {existsClause}";

    return this;
}

/// <summary>
/// Agrega una condición NOT EXISTS a la cláusula WHERE con una subconsulta generada dinámicamente.
/// </summary>
/// <param name="subqueryBuilderAction">
/// Acción que configura un nuevo <see cref="SelectQueryBuilder"/> para representar la subconsulta dentro de NOT EXISTS.
/// </param>
/// <returns>Instancia modificada de <see cref="SelectQueryBuilder"/>.</returns>
public SelectQueryBuilder WhereNotExists(Action<SelectQueryBuilder> subqueryBuilderAction)
{
    var subqueryBuilder = new SelectQueryBuilder("DUMMY");
    subqueryBuilderAction(subqueryBuilder);
    var subquerySql = subqueryBuilder.Build().Sql;

    var notExistsClause = $"NOT EXISTS ({subquerySql})";

    if (string.IsNullOrWhiteSpace(WhereClause))
        WhereClause = notExistsClause;
    else
        WhereClause += $" AND {notExistsClause}";

    return this;
}
