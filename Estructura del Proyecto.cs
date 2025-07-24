/// <summary>
/// Agrega una o varias expresiones CASE WHEN al SELECT.
/// </summary>
/// <param name="caseColumns">
/// Tuplas donde el primer valor es la expresión CASE WHEN generada por <see cref="CaseWhenBuilder"/>,
/// y el segundo valor es el alias de la columna.
/// </param>
/// <returns>Instancia modificada de <see cref="SelectQueryBuilder"/>.</returns>
public SelectQueryBuilder SelectCase(params (string ColumnSql, string? Alias)[] caseColumns)
{
    foreach (var (column, alias) in caseColumns)
    {
        _columns.Add((column, alias));
        if (!string.IsNullOrWhiteSpace(alias))
            _aliasMap[column] = alias;
    }

    return this;
}

var query = QueryBuilder.Core.QueryBuilder
    .From("USUADMIN", "BCAH96DTA")
    .Select("USUARIO", "TIPUSU")
    .SelectCase(
        CaseWhenBuilder.Build(cb => cb
            .When<USUADMIN>(x => x.TIPUSU == "A").Then("'Administrador'")
            .When<USUADMIN>(x => x.TIPUSU == "U").Then("'Usuario'")
            .Else("'Otro'"), "DESCRIPCION")
    )
    .Build();
