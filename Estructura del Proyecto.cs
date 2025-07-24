namespace QueryBuilder.Models;

/// <summary>
/// Representa una subconsulta SQL utilizada como columna o tabla derivada.
/// </summary>
public class Subquery
{
    /// <summary>
    /// SQL generado por un SelectQueryBuilder anidado.
    /// </summary>
    public string Sql { get; }

    /// <summary>
    /// Alias de la subconsulta (si aplica).
    /// </summary>
    public string? Alias { get; }

    public Subquery(string sql, string? alias = null)
    {
        Sql = sql;
        Alias = alias;
    }

    public override string ToString() => Alias is null ? $"({Sql})" : $"({Sql}) {Alias}";
}

/// <summary>
/// Agrega una subconsulta como una columna seleccionada.
/// </summary>
/// <param name="subquery">Subconsulta construida previamente.</param>
/// <param name="alias">Alias de la columna resultante.</param>
public SelectQueryBuilder Select(Subquery subquery, string alias)
{
    _columns.Add(($"({subquery.Sql})", alias));
    return this;
}
private readonly Subquery? _derivedTable;

/// <summary>
/// Inicializa una nueva instancia de <see cref="SelectQueryBuilder"/> con una tabla derivada.
/// </summary>
/// <param name="derivedTable">Subconsulta que actúa como tabla.</param>
public SelectQueryBuilder(Subquery derivedTable)
{
    _derivedTable = derivedTable;
}

sb.Append(" FROM ");
if (_derivedTable != null)
{
    sb.Append(_derivedTable.ToString());
}
else
{
    if (!string.IsNullOrWhiteSpace(_library))
        sb.Append($"{_library}.");
    sb.Append(_tableName);
    if (!string.IsNullOrWhiteSpace(_tableAlias))
        sb.Append($" {_tableAlias}");
}

var subTotal = new SelectQueryBuilder("DETALLES", "BCAH96DTA")
    .Select("SUM(MONTO)")
    .Where<DETALLES>(x => x.USUARIO == "admin")
    .Build();

var subqueryCol = new Subquery(subTotal.Sql);

var query = new SelectQueryBuilder("USUADMIN", "BCAH96DTA")
    .Select("USUARIO")
    .Select(subqueryCol, "TOTAL_MONTO")
    .Build();

var subquery = new Subquery(
    new SelectQueryBuilder("VENTAS", "BCAH96DTA")
        .Select("AGENTE", "SUM(MONTO) AS TOTAL")
        .GroupBy("AGENTE")
        .Build().Sql,
    "V" // Alias de la tabla derivada
);

var query = new SelectQueryBuilder(subquery)
    .Select("AGENTE", "TOTAL")
    .WhereRaw("TOTAL > 10000")
    .Build();
