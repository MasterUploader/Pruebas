Esto es lo que tu me entregas:

public SelectQueryBuilder Join(string table, string onCondition, JoinType joinType = JoinType.Inner)
{
    if (string.IsNullOrWhiteSpace(table))
        throw new ArgumentNullException(nameof(table));

    if (string.IsNullOrWhiteSpace(onCondition))
        throw new ArgumentNullException(nameof(onCondition));

    string tableRef = table.Contains('.') ? table : $"{_library}.{table}";

    _joins.Add(new JoinClause
    {
        Table = tableRef,
        Condition = onCondition,
        Type = joinType
    });

    return this;
}
Mira como esta el codigo actual, me alegra la mejora incluyela, pero toma encuenta lo que ya esta:
 /// <summary>
 /// Agrega un JOIN a la consulta.
 /// </summary>
 public SelectQueryBuilder Join(string table, string? library, string alias, string left, string right, string joinType = "INNER")
 {
     _joins.Add(new JoinClause
     {
         JoinType = joinType.ToUpper(),
         TableName = table,
         Library = library,
         Alias = alias,
         LeftColumn = left,
         RightColumn = right
     });
     return this;
 }
