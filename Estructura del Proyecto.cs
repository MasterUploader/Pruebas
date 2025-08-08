/// <summary>
/// Devuelve un nombre seguro para usar en rutas/archivos.
/// </summary>
private static string Sanitize(string? name)
{
    if (string.IsNullOrWhiteSpace(name)) return "Unknown";
    var invalid = Path.GetInvalidFileNameChars().Concat(Path.GetInvalidPathChars()).Distinct().ToArray();
    var cleaned = new string(name.Where(c => !invalid.Contains(c)).ToArray()).Trim();
    return string.IsNullOrWhiteSpace(cleaned) ? "Unknown" : cleaned;
}

/// <summary>
/// Obtiene un nombre de endpoint seguro desde el HttpContext. Si no existe contexto, devuelve "NoContext".
/// </summary>
private static string GetEndpointSafe(HttpContext? context)
{
    if (context == null) return "NoContext";

    // Intentar usar CAD (ActionName); si no, caer al último segmento del Path
    var cad = context.GetEndpoint()?.Metadata
        .OfType<Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor>()
        .FirstOrDefault();

    var endpoint = cad?.ActionName 
                   ?? (context.Request.Path.Value ?? "/").Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries).LastOrDefault()
                   ?? "UnknownEndpoint";

    return Sanitize(endpoint);
}

/// <summary>
/// Devuelve la carpeta base de errores con la subcarpeta de fecha local: &lt;_logDirectory&gt;/Errores/&lt;yyyy-MM-dd&gt;
/// </summary>
private string GetErrorsDirectory(DateTime nowLocal)
{
    var dir = Path.Combine(_logDirectory, "Errores", nowLocal.ToString("yyyy-MM-dd"));
    Directory.CreateDirectory(dir);
    return dir;
}

/// <summary>
/// Construye un path de archivo de error con ExecutionId, Endpoint y timestamp local.
/// Sufijo: "internal" para errores internos; "manual" para global manual logs.
/// </summary>
private string BuildErrorFilePath(string kind, HttpContext? context)
{
    var now = DateTime.Now; // hora local
    var dir = GetErrorsDirectory(now);

    // ExecutionId (si hay contexto), si no un Guid nuevo
    var executionId = context?.Items?["ExecutionId"]?.ToString() ?? Guid.NewGuid().ToString();

    var endpoint = GetEndpointSafe(context);
    var timestamp = now.ToString("yyyyMMdd_HHmmss");

    var suffix = string.Equals(kind, "internal", StringComparison.OrdinalIgnoreCase) ? "_internal" : "";
    var fileName = $"{executionId}_{endpoint}_{timestamp}{suffix}.txt";

    return Path.Combine(dir, fileName);
}

/// <summary>
/// Registra errores internos en un archivo dentro de /Errores/&lt;fecha&gt;/ con nombre:
/// ExecutionId_Endpoint_yyyyMMdd_HHmmss_internal.txt
/// </summary>
public void LogInternalError(Exception ex)
{
    try
    {
        var context = _httpContextAccessor.HttpContext;
        var errorPath = BuildErrorFilePath(kind: "internal", context: context);

        var msg = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Error en LoggingService: {ex}{Environment.NewLine}";
        File.AppendAllText(errorPath, msg);
    }
    catch
    {
        // Evita bucles de error
    }
}
if (context == null)
    return BuildErrorFilePath(kind: "manual", context: null);



catch (Exception ex)
{
    LogInternalError(ex);
}
return BuildErrorFilePath(kind: "manual", context: _httpContextAccessor.HttpContext);
