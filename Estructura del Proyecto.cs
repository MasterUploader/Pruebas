/// <summary>
/// Agrega una condición WHERE del tipo "CAMPO IN (...)".
/// </summary>
/// <param name="column">Nombre de la columna a comparar.</param>
/// <param name="values">Lista de valores a incluir.</param>
public SelectQueryBuilder WhereIn(string column, IEnumerable<object> values)
{
    if (values is null || !values.Any()) return this;

    string formatted = string.Join(", ", values.Select(SqlHelper.FormatValue));
    string clause = $"{column} IN ({formatted})";

    WhereClause = string.IsNullOrWhiteSpace(WhereClause) ? clause : $"{WhereClause} AND {clause}";
    return this;
}

/// <summary>
/// Agrega una condición WHERE del tipo "CAMPO NOT IN (...)".
/// </summary>
/// <param name="column">Nombre de la columna a comparar.</param>
/// <param name="values">Lista de valores a excluir.</param>
public SelectQueryBuilder WhereNotIn(string column, IEnumerable<object> values)
{
    if (values is null || !values.Any()) return this;

    string formatted = string.Join(", ", values.Select(SqlHelper.FormatValue));
    string clause = $"{column} NOT IN ({formatted})";

    WhereClause = string.IsNullOrWhiteSpace(WhereClause) ? clause : $"{WhereClause} AND {clause}";
    return this;
}

/// <summary>
/// Agrega una condición WHERE del tipo "CAMPO BETWEEN VALOR1 AND VALOR2".
/// </summary>
/// <param name="column">Nombre de la columna a comparar.</param>
/// <param name="start">Valor inicial del rango.</param>
/// <param name="end">Valor final del rango.</param>
public SelectQueryBuilder WhereBetween(string column, object start, object end)
{
    string formattedStart = SqlHelper.FormatValue(start);
    string formattedEnd = SqlHelper.FormatValue(end);
    string clause = $"{column} BETWEEN {formattedStart} AND {formattedEnd}";

    WhereClause = string.IsNullOrWhiteSpace(WhereClause) ? clause : $"{WhereClause} AND {clause}";
    return this;
}


/// <summary>
/// Agrega un JOIN con una subconsulta como tabla.
/// </summary>
/// <param name="subquery">Instancia de subconsulta a usar como tabla.</param>
/// <param name="alias">Alias de la tabla derivada.</param>
/// <param name="left">Columna izquierda para la condición ON.</param>
/// <param name="right">Columna derecha para la condición ON.</param>
/// <param name="joinType">Tipo de JOIN: INNER, LEFT, etc.</param>
public SelectQueryBuilder Join(Subquery subquery, string alias, string left, string right, string joinType = "INNER")
{
    _joins.Add(new JoinClause
    {
        JoinType = joinType.ToUpperInvariant(),
        TableName = $"({subquery.Sql})",
        Alias = alias,
        LeftColumn = left,
        RightColumn = right
    });
    return this;
}

public static class SqlHelper
{
    /// <summary>
    /// Convierte un valor .NET en su representación SQL (con comillas si es string, formato ISO para DateTime).
    /// </summary>
    public static string FormatValue(object value)
    {
        if (value is null) return "NULL";

        return value switch
        {
            string s => $"'{s.Replace("'", "''")}'",
            DateTime dt => $"'{dt:yyyy-MM-dd HH:mm:ss}'",
            bool b => b ? "1" : "0",
            _ => value.ToString()
        };
    }
}
