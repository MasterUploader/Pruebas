/// <summary>
/// Define las columnas a seleccionar (sin alias explícito).
/// Si detecta funciones agregadas, genera alias automáticos como "SUM_CAMPO".
/// </summary>
/// <param name="columns">Nombres de columnas o funciones agregadas.</param>
public SelectQueryBuilder Select(params string[] columns)
{
    foreach (var column in columns)
    {
        // Detectar funciones agregadas como SUM(), COUNT(), etc.
        if (IsSqlFunction(column, out var functionName, out var argument))
        {
            // Si no hay alias explícito, generar uno automáticamente
            string alias = $"{functionName}_{argument}".Replace("*", "ALL");
            _columns.Add((column, alias));
        }
        else
        {
            _columns.Add((column, null));
        }
    }

    return this;
}

/// <summary>
/// Determina si una columna es una función agregada SQL (como SUM, COUNT, etc.).
/// Extrae el nombre de la función y su argumento.
/// </summary>
/// <param name="expression">Expresión a evaluar (ej. "SUM(MONTO)").</param>
/// <param name="functionName">Salida: nombre de la función (ej. "SUM").</param>
/// <param name="argument">Salida: argumento dentro de los paréntesis (ej. "MONTO").</param>
private static bool IsSqlFunction(string expression, out string functionName, out string argument)
{
    functionName = string.Empty;
    argument = string.Empty;

    if (string.IsNullOrWhiteSpace(expression) || !expression.Contains('(') || !expression.Contains(')'))
        return false;

    int parenStart = expression.IndexOf('(');
    int parenEnd = expression.IndexOf(')');

    if (parenStart < 1 || parenEnd <= parenStart)
        return false;

    functionName = expression.Substring(0, parenStart).Trim().ToUpperInvariant();
    argument = expression.Substring(parenStart + 1, parenEnd - parenStart - 1).Trim();

    // Lista de funciones agregadas válidas
    var knownFunctions = new[] { "SUM", "COUNT", "AVG", "MIN", "MAX" };
    return knownFunctions.Contains(functionName);
}


var query = QueryBuilder.Core.QueryBuilder
    .From("FACTURAS", "BD001")
    .Select("SUM(MONTO)", "COUNT(*)")
    .Build();
