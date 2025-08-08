/// <summary>
/// Obtiene la ruta **completa** del archivo de log para la petición actual,
/// organizando por **API/Controller/Action/FechaLocal** y agregando, si existe,
/// el valor personalizado capturado en <c>HttpContext.Items["LogCustomPart"]</c>.
/// </summary>
/// <remarks>
/// - Usa **hora local** (no UTC).
/// - Evita registrar rutas de Swagger y favicon.
/// - Si el nombre ya estaba cacheado en <c>HttpContext.Items["LogFileName"]</c> pero
///   aún no contenía el custom part, se **regenera** automáticamente.
/// </remarks>
/// <returns>Ruta de archivo completa donde se debe escribir el log actual.</returns>
public string GetCurrentLogFile()
{
    try
    {
        var context = _httpContextAccessor.HttpContext;
        if (context is null)
            return Path.Combine(_logDirectory, "GlobalManualLogs.txt");

        // 0) Excluir rutas no deseadas (Swagger / favicon)
        var reqPath = context.Request.Path.Value ?? string.Empty;
        if (reqPath.Contains("swagger", StringComparison.OrdinalIgnoreCase) ||
            reqPath.Contains("favicon", StringComparison.OrdinalIgnoreCase))
        {
            return Path.Combine(_logDirectory, "GlobalManualLogs.txt");
        }

        // 1) Fallback: si el Middleware guardó el objeto del body y aún no hay LogCustomPart, intentar extraerlo aquí
        if (!context.Items.ContainsKey("LogCustomPart") &&
            context.Items.TryGetValue("RequestBodyObject", out var bodyObj) &&
            bodyObj is not null)
        {
            var extracted = GetLogFileNameValue(bodyObj); // <- tu helper recursivo
            if (!string.IsNullOrWhiteSpace(extracted))
                context.Items["LogCustomPart"] = extracted;
        }

        // 2) Si ya había un nombre cacheado, pero no incluye el custom, forzar regeneración
        if (context.Items.TryGetValue("LogFileName", out var existingObj) &&
            existingObj is string existingPath &&
            context.Items.TryGetValue("LogCustomPart", out var partObj) &&
            partObj is string part &&
            !string.IsNullOrWhiteSpace(part) &&
            !existingPath.Contains(part, StringComparison.OrdinalIgnoreCase))
        {
            context.Items.Remove("LogFileName"); // fuerza regeneración
        }

        // 3) Reutilizar cache si aplica
        if (context.Items.TryGetValue("LogFileName", out var cached) &&
            cached is string cachedPath &&
            !string.IsNullOrWhiteSpace(cachedPath))
        {
            return cachedPath;
        }

        // 4) Nombres de Controller/Action seguros
        var (controllerName, actionName) = GetControllerAndAction(context);
        controllerName = Sanitize(controllerName);
        actionName     = Sanitize(actionName);

        // 5) Fecha/Hora **LOCAL**
        var fechaLocal = DateTime.Now.ToString("yyyy-MM-dd");
        var timestamp  = DateTime.Now.ToString("yyyyMMdd_HHmmss");

        // 6) ExecutionId (establecido por el middleware; si no, generar)
        var executionId = context.Items["ExecutionId"]?.ToString() ?? Guid.NewGuid().ToString();

        // 7) Custom part (opcional)
        string customSuffix = string.Empty;
        if (context.Items.TryGetValue("LogCustomPart", out var cp) &&
            cp is string cpStr &&
            !string.IsNullOrWhiteSpace(cpStr))
        {
            customSuffix = "_" + Sanitize(cpStr);
        }

        // 8) Carpeta final: <Base>/<API>/<Controller>/<Action>/<yyyy-MM-dd>
        var finalDirectory = Path.Combine(_logDirectory, controllerName, actionName, fechaLocal);
        Directory.CreateDirectory(finalDirectory);

        // 9) Nombre final
        var fileName = $"{executionId}_{actionName}{customSuffix}_{timestamp}.txt";
        var fullPath = Path.Combine(finalDirectory, fileName);

        // 10) Cachear para el resto del ciclo de la request
        context.Items["LogFileName"] = fullPath;
        return fullPath;
    }
    catch (Exception ex)
    {
        LogInternalError(ex);
        return Path.Combine(_logDirectory, "GlobalManualLogs.txt");
    }
}

/// <summary>
/// Obtiene nombres **seguros** de Controller y Action para la request actual.
/// Si no hay metadatos de MVC, cae al Path del request.
/// </summary>
private static (string Controller, string Action) GetControllerAndAction(HttpContext context)
{
    var cad = context.GetEndpoint()?.Metadata
        .OfType<Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor>()
        .FirstOrDefault();

    if (cad is not null)
        return (cad.ControllerName ?? "UnknownController", cad.ActionName ?? "UnknownEndpoint");

    // Fallback a la ruta del request (último segmento como "Action", penúltimo como "Controller")
    var segments = (context.Request.Path.Value ?? "/").Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries);
    var action   = segments.LastOrDefault() ?? "UnknownEndpoint";
    var ctrl     = (segments.Length >= 2 ? segments[^2] : "UnknownController");

    return (ctrl, action);
}

/// <summary>
/// Sanea un nombre para usarlo en carpeta/archivo (quita caracteres inválidos y recorta espacios).
/// </summary>
private static string Sanitize(string? name)
{
    if (string.IsNullOrWhiteSpace(name))
        return "Unknown";

    var invalid = Path.GetInvalidFileNameChars().Concat(Path.GetInvalidPathChars()).Distinct().ToArray();
    var cleaned = new string(name.Where(c => !invalid.Contains(c)).ToArray());

    // Evitar nombres vacíos tras limpieza
    return string.IsNullOrWhiteSpace(cleaned) ? "Unknown" : cleaned.Trim();
}
