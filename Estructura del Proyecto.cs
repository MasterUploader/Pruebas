/// <summary>
/// Agrega una expresión CASE WHEN al SELECT.
/// Puedes definir una condición SQL personalizada y su alias correspondiente.
/// </summary>
/// <param name="caseExpression">Expresión CASE WHEN completa en SQL.</param>
/// <param name="alias">Alias para el resultado de la expresión.</param>
/// <returns>Instancia fluida de <see cref="SelectQueryBuilder"/>.</returns>
public SelectQueryBuilder SelectCase(string caseExpression, string alias)
{
    if (string.IsNullOrWhiteSpace(caseExpression))
        throw new ArgumentException("La expresión CASE no puede estar vacía.");

    if (string.IsNullOrWhiteSpace(alias))
        throw new ArgumentException("El alias para la expresión CASE es obligatorio.");

    _columns.Add(($"CASE {caseExpression} END", alias));
    return this;
}

var query = QueryBuilder.Core.QueryBuilder
    .From("USUARIOS", "BCAH96DTA")
    .Select("NOMBRE", "TIPO")
    .SelectCase("WHEN TIPO = 'A' THEN 'Administrador' WHEN TIPO = 'U' THEN 'Usuario' ELSE 'Otro'", "DESCRIPCION")
    .Build();

