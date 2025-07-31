/// <summary>
/// Agrega una cláusula JOIN a la consulta SQL, permitiendo especificar la tabla con o sin librería.
/// </summary>
/// <param name="table">Nombre de la tabla a unir. Puede incluir la librería (ej. LIBRERIA.TABLA).</param>
/// <param name="library">Nombre de la librería si no se especifica en la tabla. Se ignora si <paramref name="table"/> ya incluye la librería.</param>
/// <param name="alias">Alias opcional para la tabla unida.</param>
/// <param name="left">Columna del lado izquierdo de la condición ON.</param>
/// <param name="right">Columna del lado derecho de la condición ON.</param>
/// <param name="joinType">Tipo de JOIN (INNER, LEFT, RIGHT, FULL).</param>
/// <returns>Instancia actual de <see cref="SelectQueryBuilder"/>.</returns>
public SelectQueryBuilder Join(string table, string? library, string alias, string left, string right, string joinType = "INNER")
{
    if (string.IsNullOrWhiteSpace(table))
        throw new ArgumentNullException(nameof(table));

    if (string.IsNullOrWhiteSpace(left))
        throw new ArgumentNullException(nameof(left));

    if (string.IsNullOrWhiteSpace(right))
        throw new ArgumentNullException(nameof(right));

    string finalLibrary = library;
    string finalTable = table;

    // Si el nombre incluye un punto (.), asumimos que ya viene con la librería: LIBRERIA.TABLA
    if (table.Contains('.'))
    {
        var parts = table.Split('.', 2, StringSplitOptions.TrimEntries);
        finalLibrary = parts[0];
        finalTable = parts[1];
    }

    _joins.Add(new JoinClause
    {
        JoinType = joinType.ToUpperInvariant(),
        TableName = finalTable,
        Library = finalLibrary,
        Alias = alias,
        LeftColumn = left,
        RightColumn = right
    });

    return this;
}
