namespace QueryBuilder.Helpers;

/// <summary>
/// Builder fluido para generar expresiones CASE WHEN en SQL.
/// </summary>
public class CaseWhenBuilder
{
    private readonly List<(string When, string Then)> _cases = [];
    private string? _elseValue;

    private CaseWhenBuilder() { }

    /// <summary>
    /// Crea una nueva instancia del builder con la primera condición WHEN.
    /// </summary>
    /// <param name="condition">Condición SQL para el WHEN (ej. "TIPO = 'A'").</param>
    public static CaseWhenBuilder When(string condition)
    {
        return new CaseWhenBuilder().AddWhen(condition);
    }

    /// <summary>
    /// Agrega una condición WHEN adicional.
    /// </summary>
    public CaseWhenBuilder AddWhen(string condition)
    {
        _cases.Add((condition, string.Empty));
        return this;
    }

    /// <summary>
    /// Define el valor del THEN para el último WHEN agregado.
    /// </summary>
    public CaseWhenBuilder Then(string value)
    {
        if (_cases.Count == 0)
            throw new InvalidOperationException("Debe agregar una condición WHEN antes de THEN.");

        _cases[_cases.Count - 1] = (_cases[^1].When, value);
        return this;
    }

    /// <summary>
    /// Define el valor ELSE para el CASE.
    /// </summary>
    public CaseWhenBuilder Else(string value)
    {
        _elseValue = value;
        return this;
    }

    /// <summary>
    /// Construye la expresión CASE WHEN completa.
    /// </summary>
    public string Build()
    {
        var sb = new StringBuilder("CASE");
        foreach (var (when, then) in _cases)
        {
            sb.Append($" WHEN {when} THEN {then}");
        }

        if (!string.IsNullOrWhiteSpace(_elseValue))
        {
            sb.Append($" ELSE {_elseValue}");
        }

        sb.Append(" END");
        return sb.ToString();
    }

    /// <summary>
    /// Conversión implícita a string para usar directamente en SelectCase().
    /// </summary>
    public static implicit operator string(CaseWhenBuilder builder) => builder.Build();
}

/// <summary>
/// Agrega una expresión CASE WHEN como columna seleccionada, con un alias explícito.
/// </summary>
/// <param name="caseExpression">Expresión CASE generada con <see cref="CaseWhenBuilder"/>.</param>
/// <param name="alias">Alias para la columna resultante.</param>
public SelectQueryBuilder SelectCase(string caseExpression, string alias)
{
    _columns.Add((caseExpression, alias));
    _aliasMap[caseExpression] = alias;
    return this;
}

var query = QueryBuilder.Core.QueryBuilder
    .From("USUARIOS", "BCAH96DTA")
    .Select("ID", "NOMBRE")
    .SelectCase(
        CaseWhenBuilder
            .When("TIPO = 'A'").Then("'Administrador'")
            .When("TIPO = 'U'").Then("'Usuario'")
            .Else("'Otro'"),
        "DESCRIPCION")
    .Build();
