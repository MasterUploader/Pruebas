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
