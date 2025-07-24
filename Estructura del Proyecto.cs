/// <summary>
/// Agrega una cláusula HAVING con una expresión CASE WHEN directamente como string.
/// Ejemplo: "CASE WHEN SUM(CANTIDAD) > 10 THEN 1 ELSE 0 END = 1"
/// </summary>
/// <param name="sqlCaseCondition">Expresión CASE completa en SQL.</param>
public SelectQueryBuilder HavingCase(string sqlCaseCondition)
{
    if (string.IsNullOrWhiteSpace(sqlCaseCondition))
        return this;

    if (string.IsNullOrWhiteSpace(HavingClause))
        HavingClause = sqlCaseCondition;
    else
        HavingClause += $" AND {sqlCaseCondition}";

    return this;
}

/// <summary>
/// Agrega una cláusula HAVING con una expresión CASE WHEN generada con <see cref="CaseWhenBuilder"/>.
/// </summary>
/// <param name="caseBuilder">Builder de CASE WHEN.</param>
/// <param name="comparison">Comparación adicional (ej: "= 1").</param>
public SelectQueryBuilder HavingCase(CaseWhenBuilder caseBuilder, string comparison)
{
    if (caseBuilder is null || string.IsNullOrWhiteSpace(comparison))
        return this;

    string expression = $"{caseBuilder.Build()} {comparison}";

    if (string.IsNullOrWhiteSpace(HavingClause))
        HavingClause = expression;
    else
        HavingClause += $" AND {expression}";

    return this;
}

builder
    .GroupBy("TIPO")
    .HavingCase("CASE WHEN COUNT(*) > 3 THEN 1 ELSE 0 END = 1");

builder
    .GroupBy("TIPO")
    .HavingCase(
        CaseWhenBuilder
            .When("SUM(CANTIDAD) > 10")
            .Then("1")
            .Else("0"),
        "= 1"
    );
