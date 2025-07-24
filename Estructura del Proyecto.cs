namespace QueryBuilder.Helpers;

/// <summary>
/// Builder para construir expresiones CASE WHEN SQL de forma tipada.
/// </summary>
public class CaseWhenBuilder
{
    private readonly List<(string Condition, string Result)> _cases = new();
    private string? _elseResult;

    private CaseWhenBuilder() { }

    /// <summary>
    /// Inicializa una nueva instancia del builder.
    /// </summary>
    public static CaseWhenBuilder When(string condition)
    {
        var builder = new CaseWhenBuilder();
        builder._cases.Add((condition, ""));
        return builder;
    }

    /// <summary>
    /// Define el resultado del último WHEN.
    /// </summary>
    public CaseWhenBuilder Then(string result)
    {
        if (_cases.Count == 0)
            throw new InvalidOperationException("Debe llamar a When() antes de Then().");

        _cases[^1] = (_cases[^1].Condition, result);
        return this;
    }

    /// <summary>
    /// Agrega un nuevo bloque WHEN.
    /// </summary>
    public CaseWhenBuilder WhenNext(string condition)
    {
        _cases.Add((condition, ""));
        return this;
    }

    /// <summary>
    /// Define la cláusula ELSE.
    /// </summary>
    public CaseWhenBuilder Else(string elseResult)
    {
        _elseResult = elseResult;
        return this;
    }

    /// <summary>
    /// Genera la cadena SQL de la expresión CASE.
    /// </summary>
    public string Build()
    {
        if (_cases.Count == 0)
            throw new InvalidOperationException("Debe definir al menos una condición WHEN.");

        var sb = new StringBuilder("CASE");
        foreach (var (condition, result) in _cases)
            sb.Append($" WHEN {condition} THEN {Format(result)}");

        if (!string.IsNullOrWhiteSpace(_elseResult))
            sb.Append($" ELSE {Format(_elseResult)}");

        sb.Append(" END");
        return sb.ToString();
    }

    private string Format(string value)
    {
        return value.StartsWith("'") ? value : $"'{value}'";
    }
}

/// <summary>
/// Agrega una cláusula WHERE con una expresión CASE WHEN directamente como string.
/// Ej: "CASE WHEN TIPO = 'A' THEN 1 ELSE 0 END = 1"
/// </summary>
/// <param name="sqlCaseCondition">Condición CASE completa.</param>
public SelectQueryBuilder WhereCase(string sqlCaseCondition)
{
    if (string.IsNullOrWhiteSpace(sqlCaseCondition))
        return this;

    if (string.IsNullOrWhiteSpace(WhereClause))
        WhereClause = sqlCaseCondition;
    else
        WhereClause += $" AND {sqlCaseCondition}";

    return this;
}

/// <summary>
/// Agrega una cláusula WHERE con una expresión CASE WHEN generada desde <see cref="CaseWhenBuilder"/>.
/// </summary>
/// <param name="caseBuilder">Instancia del builder de CASE.</param>
/// <param name="comparison">Condición adicional (por ejemplo: "= 1").</param>
public SelectQueryBuilder WhereCase(CaseWhenBuilder caseBuilder, string comparison)
{
    if (caseBuilder is null || string.IsNullOrWhiteSpace(comparison))
        return this;

    string expression = $"{caseBuilder.Build()} {comparison}";

    if (string.IsNullOrWhiteSpace(WhereClause))
        WhereClause = expression;
    else
        WhereClause += $" AND {expression}";

    return this;
}


// Opción 1: directamente como string CASE WHEN
builder.WhereCase("CASE WHEN TIPO = 'A' THEN 1 ELSE 0 END = 1");

// Opción 2: usando CaseWhenBuilder
builder.WhereCase(
    CaseWhenBuilder.When("TIPO = 'A'").Then("1").Else("0"),
    "= 1"
);
