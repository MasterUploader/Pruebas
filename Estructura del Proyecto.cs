namespace QueryBuilder.Helpers;

/// <summary>
/// Representa funciones SQL agregadas como COUNT, SUM, etc.
/// </summary>
public static class SqlFunction
{
    /// <summary>
    /// Devuelve COUNT(columna).
    /// </summary>
    public static (string Column, string? Alias) Count(string column, string? alias = null) =>
        ($"COUNT({column})", alias);

    /// <summary>
    /// Devuelve SUM(columna).
    /// </summary>
    public static (string Column, string? Alias) Sum(string column, string? alias = null) =>
        ($"SUM({column})", alias);

    /// <summary>
    /// Devuelve AVG(columna).
    /// </summary>
    public static (string Column, string? Alias) Avg(string column, string? alias = null) =>
        ($"AVG({column})", alias);

    /// <summary>
    /// Devuelve MIN(columna).
    /// </summary>
    public static (string Column, string? Alias) Min(string column, string? alias = null) =>
        ($"MIN({column})", alias);

    /// <summary>
    /// Devuelve MAX(columna).
    /// </summary>
    public static (string Column, string? Alias) Max(string column, string? alias = null) =>
        ($"MAX({column})", alias);
}
