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
            _ => $"'{(value?.ToString() ?? string.Empty).Replace("'", "''")}'"
        };
    }
}
