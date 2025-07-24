/// <summary>
/// Define las columnas a seleccionar (sin alias).
/// Si una columna contiene una función agregada reconocida, se asigna un alias automático.
/// </summary>
/// <param name="columns">Lista de nombres de columnas o funciones SQL.</param>
public SelectQueryBuilder Select(params string[] columns)
{
    foreach (var column in columns)
    {
        // Si contiene función agregada: SUM(), COUNT(), etc.
        string? alias = TryGenerateAliasForFunction(column);
        _columns.Add((column, alias));
    }
    return this;
}

/// <summary>
/// Intenta generar un alias automáticamente si se detecta una función agregada como SUM(CAMPO).
/// </summary>
/// <param name="expression">Texto de la función SQL.</param>
/// <returns>Alias sugerido o null.</returns>
private static string? TryGenerateAliasForFunction(string expression)
{
    var functions = new[] { "SUM", "COUNT", "AVG", "MIN", "MAX" };

    foreach (var func in functions)
    {
        if (expression.StartsWith($"{func}(", StringComparison.OrdinalIgnoreCase) &&
            expression.EndsWith(")"))
        {
            var inside = expression.Substring(func.Length + 1, expression.Length - func.Length - 2);
            var clean = inside.Trim().Replace(".", "_");
            return $"{func.ToUpper()}_{clean}";
        }
    }

    return null;
}
