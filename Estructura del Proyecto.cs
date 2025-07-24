namespace QueryBuilder.Models;

/// <summary>
/// Representa una expresión de tabla común (CTE) para consultas SQL.
/// </summary>
public class CommonTableExpression
{
    /// <summary>
    /// Nombre de la CTE.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Sentencia SQL de la subconsulta que define la CTE.
    /// </summary>
    public string Sql { get; set; } = string.Empty;

    /// <summary>
    /// Devuelve la representación SQL de la CTE.
    /// </summary>
    public override string ToString()
    {
        return $"{Name} AS ({Sql})";
    }
}

private readonly List<CommonTableExpression> _ctes = new();



/// <summary>
/// Agrega una o más expresiones CTE a la consulta.
/// </summary>
/// <param name="ctes">CTEs a incluir en la cláusula WITH.</param>
/// <returns>Instancia actual de <see cref="SelectQueryBuilder"/>.</returns>
public SelectQueryBuilder With(params CommonTableExpression[] ctes)
{
    if (ctes != null && ctes.Length > 0)
        _ctes.AddRange(ctes);

    return this;
}


public QueryResult Build()
{
    var sb = new StringBuilder();

    // Si hay CTEs, agregarlas antes del SELECT
    if (_ctes.Count > 0)
    {
        sb.Append("WITH ");
        sb.Append(string.Join(", ", _ctes.Select(cte => cte.ToString())));
        sb.AppendLine();
    }

    sb.Append("SELECT ");
    ...
var cte = new CommonTableExpression
{
    Name = "UsuariosActivos",
    Sql = "SELECT USUARIO, ESTADO FROM USUADMIN WHERE ESTADO = 'A'"
};

var query = QueryBuilder.Core.QueryBuilder
    .From("UsuariosActivos")
    .With(cte)
    .Select("USUARIO", "ESTADO")
    .Build();
