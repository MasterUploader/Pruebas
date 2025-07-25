/// <summary>
/// Agrega una expresión CASE WHEN al ORDER BY, con soporte para condiciones tipadas.
/// </summary>
/// <param name="caseWhen">Expresión CASE WHEN construida mediante <see cref="CaseWhenBuilder"/>.</param>
/// <param name="alias">Alias opcional para la expresión CASE (no se usa en ORDER BY pero puede ser útil si se reutiliza).</param>
/// <returns>Instancia actual de <see cref="SelectQueryBuilder"/>.</returns>
public SelectQueryBuilder OrderByCase(CaseWhenBuilder caseWhen, string? alias = null)
{
    if (caseWhen == null)
        throw new ArgumentNullException(nameof(caseWhen));

    var expression = caseWhen.Build();

    // No se requiere alias en ORDER BY, pero lo soportamos si se quiere reutilizar
    if (!string.IsNullOrWhiteSpace(alias))
        expression += $" AS {alias}";

    _orderBy.Add(caseWhen.Build()); // Se agrega sin alias para efecto de ordenamiento
    return this;
}
