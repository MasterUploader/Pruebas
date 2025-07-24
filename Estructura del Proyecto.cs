/// <summary>
/// Define las columnas a seleccionar (sin alias explícito).
/// Si detecta funciones agregadas, genera alias automáticos como "SUM_CAMPO".
/// </summary>
/// <param name="columns">Nombres de columnas o funciones agregadas.</param>
public SelectQueryBuilder Select(params string[] columns)
{
    foreach (var column in columns)
    {
        if (TryGenerateAlias(column, out var alias))
            _columns.Add((column, alias));
        else
            _columns.Add((column, null));
    }

    return this;
}

/// <summary>
/// Genera un alias automático para funciones agregadas como SUM(CAMPO), COUNT(*), etc.
/// </summary>
/// <param name="column">Expresión de columna a analizar.</param>
/// <param name="alias">Alias generado si aplica.</param>
/// <returns>True si se generó un alias; false en caso contrario.</returns>
private static bool TryGenerateAlias(string column, out string alias)
{
    alias = string.Empty;

    if (string.IsNullOrWhiteSpace(column) || !column.Contains('(') || !column.Contains(')'))
        return false;

    int start = column.IndexOf('(');
    int end = column.IndexOf(')');

    if (start < 1 || end <= start)
        return false;

    var function = column.Substring(0, start).Trim().ToUpperInvariant();
    var argument = column.Substring(start + 1, end - start - 1).Trim();

    var validFunctions = new[] { "SUM", "COUNT", "AVG", "MIN", "MAX" };
    if (!validFunctions.Contains(function))
        return false;

    if (string.IsNullOrWhiteSpace(argument))
        return false;

    alias = $"{function}_{argument.Replace("*", "ALL")}";
    return true;
}
