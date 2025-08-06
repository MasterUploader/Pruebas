/// <summary>
/// Obtiene el archivo de log de la petición actual, garantizando que toda la información
/// se guarde en el mismo archivo. Se organiza por API, controlador, endpoint y fecha.
/// </summary>
public string GetCurrentLogFile()
{
    var context = _httpContextAccessor.HttpContext;

    if (context is not null)
    {
        // Si ya se definió, lo reutiliza
        if (context.Items.TryGetValue("LogFileName", out var existing) && existing is string existingPath)
            return existingPath;

        // Extraer nombre de endpoint
        string rawPath = context.Request.Path.Value?.Trim('/') ?? "Unknown/Unknown";
        var endpoint = rawPath.Split('/').LastOrDefault() ?? "Unknown";

        // Buscar si se extrajo algún valor personalizado
        context.Items.TryGetValue("ExtractedLogFileName", out var extracted); // <-- este valor viene de TryExtractLogFileNameFromBody

        string customPart = extracted is string s && !string.IsNullOrWhiteSpace(s) ? $"_{s}" : "";

        // Formato base: TraceId_Feature_Fecha.txt
        string traceId = context.TraceIdentifier;
        string date = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");

        string fileName = $"{traceId}{customPart}_{endpoint}_{date}.txt";

        string fullPath = Path.Combine(_logDirectoryRoot, context.Request.Path.Value?.Trim('/').Replace("/", "_") ?? "General", fileName);

        // Guardar en el contexto para reutilización
        context.Items["LogFileName"] = fullPath;

        return fullPath;
    }

    // Si no hay contexto, ruta por defecto
    return Path.Combine(_logDirectoryRoot, $"Log_{DateTime.UtcNow:yyyyMMdd_HHmmss}.txt");
}
