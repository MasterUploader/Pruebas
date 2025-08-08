Sigue generando mal el archivo, te dejo la versión actual y la ultima que funcionaba

/// <summary>
/// Devuelve el path completo del archivo de log de la request actual,
/// usando Controller/Action y agregando el LogCustomPart si existe.
/// </summary>
public string GetCurrentLogFile()
{
    try
    {
        var context = _httpContextAccessor.HttpContext;
        if (context is null)
            return Path.Combine(_logDirectory, "GlobalManualLogs.txt");

        // Excluir swagger / favicon
        var p = context.Request.Path.Value ?? string.Empty;
        if (p.Contains("swagger", StringComparison.OrdinalIgnoreCase) ||
            p.Contains("favicon", StringComparison.OrdinalIgnoreCase))
            return Path.Combine(_logDirectory, "GlobalManualLogs.txt");

        // (Fallback) si el middleware guardó el body y aún no hay custom, intentar extraerlo aquí
        if (!context.Items.ContainsKey("LogCustomPart") &&
            context.Items.TryGetValue("RequestBodyObject", out var bodyObj) && bodyObj is not null)
        {
            var extracted = GetLogFileNameValue(bodyObj); // tu helper recursivo
            if (!string.IsNullOrWhiteSpace(extracted))
                context.Items["LogCustomPart"] = extracted;
        }

        // Si ya había un path pero sin el custom, forzar regeneración
        if (context.Items.TryGetValue("LogFileName", out var existingObj) &&
            existingObj is string existingPath &&
            context.Items.TryGetValue("LogCustomPart", out var partObj) &&
            partObj is string part && !string.IsNullOrWhiteSpace(part) &&
            !existingPath.Contains(part, StringComparison.OrdinalIgnoreCase))
        {
            context.Items.Remove("LogFileName");
        }

        // Reutilizar si ya está cacheado
        if (context.Items.TryGetValue("LogFileName", out var cached) &&
            cached is string cachedPath && !string.IsNullOrWhiteSpace(cachedPath))
            return cachedPath;

        // ********* AQUÍ LO IMPORTANTE: Controller/Action desde CAD, NO RoutePattern *********
        var cad = context.GetEndpoint()?.Metadata
            .OfType<Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor>()
            .FirstOrDefault();

        string controllerName = cad?.ControllerName ?? "UnknownController";
        // si no hay CAD, caer a último segmento del Path
        string actionName = cad?.ActionName 
            ?? (p.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries).LastOrDefault() ?? "UnknownEndpoint");

        // Hora LOCAL
        string fecha = DateTime.Now.ToString("yyyy-MM-dd");
        string ts    = DateTime.Now.ToString("yyyyMMdd_HHmmss");

        string executionId = context.Items["ExecutionId"]?.ToString() ?? Guid.NewGuid().ToString();

        string customSuffix = "";
        if (context.Items.TryGetValue("LogCustomPart", out var cp) &&
            cp is string cpStr && !string.IsNullOrWhiteSpace(cpStr))
            customSuffix = "_" + cpStr;

        // Directorio final y nombre
        string finalDir = Path.Combine(_logDirectory, controllerName, actionName, fecha);
        Directory.CreateDirectory(finalDir);

        string fileName = $"{executionId}_{actionName}{customSuffix}_{ts}.txt";
        string fullPath = Path.Combine(finalDir, fileName);

        // Cachear SIEMPRE el path completo (no el nombre suelto)
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
/// Obtiene el archivo de log de la petición actual, garantizando que toda la información
/// se guarde en el mismo archivo. Se organiza por API, controlador, endpoint y fecha.
/// </summary>
public string GetCurrentLogFile1()
{
    try
    {
        var context = _httpContextAccessor.HttpContext;

        if (context == null)
            return Path.Combine(_logDirectory, "GlobalManualLogs.txt");

        // 🔹 Validación inicial para regenerar si falta el custom part
        if (context.Items.TryGetValue("LogFileName", out var existingObj) && existingObj is string existingPath && context.Items.TryGetValue("LogCustomPart", out var partObj) && partObj is string part && !string.IsNullOrWhiteSpace(part) && !existingPath.Contains($"{part}", StringComparison.OrdinalIgnoreCase))
        {
            context.Items.Remove("LogFileName");
        }

        // 🔹 Si ya existe, lo devolvemos directamente
        if (context.Items.TryGetValue("LogFileName", out var logFile) && logFile is string path && !string.IsNullOrWhiteSpace(path))
        {
            return path;
        }
        string endpoint = context.Request.Path.Value?.Trim('/').Split('/').LastOrDefault() ?? "UnknownEndpoint";

        // Intenta sobrescribir con metadatos (opcional)
        var endpointMetadata = context.GetEndpoint();
        var controllerName = endpointMetadata?.Metadata
            .OfType<Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor>()
            .FirstOrDefault()?.ControllerName ?? "UnknownController";

        // Fecha, timestamp y ejecución
        string fecha = DateTime.Now.ToString("yyyy-MM-dd");
        string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string executionId = context.Items["ExecutionId"]?.ToString() ?? Guid.NewGuid().ToString();

        // 🔹 Agregamos la parte personalizada si existe
        string customPart = "";
        if (context.Items.TryGetValue("LogCustomPart", out var partValue) && partValue is string partStr && !string.IsNullOrWhiteSpace(partStr))
        {
            customPart = $"_{partStr}";
        }

        // _logDirectory YA contiene el nombre de la API
        string finalDirectory = Path.Combine(_logDirectory, controllerName, endpoint, fecha);
        Directory.CreateDirectory(finalDirectory);

        // 🔹 Nombre final del archivo
        var fileName = $"{executionId}_{endpoint}{customPart}_{timestamp}.txt";

        // 🔹 Guardamos en Items para reutilizar en la misma request
        context.Items["LogFileName"] = fileName;

        string fullPath = Path.Combine(finalDirectory, fileName);

        return fullPath;
    }
    catch (Exception ex)
    {
        LogInternalError(ex);
    }
    return Path.Combine(_logDirectory, "GlobalManualLogs.txt");
}

