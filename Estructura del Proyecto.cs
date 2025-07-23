namespace QueryBuilder.Models;

/// <summary>
/// Representa una cláusula JOIN para una consulta SQL.
/// </summary>
public class JoinClause
{
    /// <summary>
    /// Tipo de JOIN (ej. INNER, LEFT).
    /// </summary>
    public string JoinType { get; set; } = "INNER";

    /// <summary>
    /// Nombre de la tabla a unir.
    /// </summary>
    public string TableName { get; set; } = string.Empty;

    /// <summary>
    /// Librería (esquema) de la tabla a unir.
    /// </summary>
    public string? Library { get; set; }

    /// <summary>
    /// Alias de la tabla unida.
    /// </summary>
    public string? Alias { get; set; }

    /// <summary>
    /// Campo izquierdo en la condición ON.
    /// </summary>
    public string LeftColumn { get; set; } = string.Empty;

    /// <summary>
    /// Campo derecho en la condición ON.
    /// </summary>
    public string RightColumn { get; set; } = string.Empty;
}

private readonly List<JoinClause> _joins = new();


/// <summary>
/// Agrega una cláusula JOIN a la consulta.
/// </summary>
/// <param name="table">Nombre de la tabla a unir.</param>
/// <param name="library">Nombre de la librería (opcional).</param>
/// <param name="alias">Alias para la tabla unida.</param>
/// <param name="leftColumn">Campo izquierdo de la condición ON.</param>
/// <param name="rightColumn">Campo derecho de la condición ON.</param>
/// <param name="joinType">Tipo de JOIN (INNER, LEFT, etc.).</param>
public SelectQueryBuilder Join(
    string table,
    string? library,
    string alias,
    string leftColumn,
    string rightColumn,
    string joinType = "INNER")
{
    _joins.Add(new JoinClause
    {
        JoinType = joinType.ToUpper(),
        TableName = table,
        Library = library,
        Alias = alias,
        LeftColumn = leftColumn,
        RightColumn = rightColumn
    });

    return this;
}


foreach (var join in _joins)
{
    sb.Append($" {join.JoinType} JOIN ");

    if (!string.IsNullOrWhiteSpace(join.Library))
        sb.Append($"{join.Library}.");

    sb.Append(join.TableName);

    if (!string.IsNullOrWhiteSpace(join.Alias))
        sb.Append($" {join.Alias}");

    sb.Append($" ON {join.LeftColumn} = {join.RightColumn}");
}
