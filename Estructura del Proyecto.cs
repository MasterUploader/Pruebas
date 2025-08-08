/// <summary>
/// Obtiene el archivo de log de la petición actual, garantizando que toda la información
/// se guarde en el mismo archivo. Organiza por API, controlador, endpoint (desde Path) y fecha.
/// Agrega el LogCustomPart si existe. Usa hora local.
/// </summary>
public string GetCurrentLogFile()
{
    try
    {
        var context = _httpContextAccessor.HttpContext;
        if (context == null)
            return Path.Combine(_logDirectory, "GlobalManualLogs.txt");

        // 🔹 Regenerar si el path cacheado no contiene el custom part
        if (context.Items.TryGetValue("LogFileName", out var existingObj) &&
            existingObj is string existingPath &&
            context.Items.TryGetValue("LogCustomPart", out var partObj) &&
            partObj is string part && !string.IsNullOrWhiteSpace(part) &&
            !existingPath.Contains(part, StringComparison.OrdinalIgnoreCase))
        {
            context.Items.Remove("LogFileName");
        }

        // 🔹 Reutilizar si ya estaba cacheado (ojo: aquí esperamos el FULL PATH)
        if (context.Items.TryGetValue("LogFileName", out var cached) &&
            cached is string cachedPath && !string.IsNullOrWhiteSpace(cachedPath))
        {
            return cachedPath;
        }

        // ✅ Endpoint SIEMPRE desde la URL (evita RoutePatternLiteralPart)
        string endpoint = context.Request.Path.Value?
                              .Trim('/')
                              .Split('/', StringSplitOptions.RemoveEmptyEntries)
                              .LastOrDefault()
                          ?? "UnknownEndpoint";

        // ✅ Controller desde CAD (si está), si no, “UnknownController”
        var endpointMetadata = context.GetEndpoint();
        string controllerName = endpointMetadata?.Metadata
            .OfType<Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor>()
            .FirstOrDefault()?.ControllerName ?? "UnknownController";

        // 📅 Hora local
        string fecha      = DateTime.Now.ToString("yyyy-MM-dd");
        string timestamp  = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string executionId = context.Items["ExecutionId"]?.ToString() ?? Guid.NewGuid().ToString();

        // 🧩 Sufijo custom opcional
        string customPart = "";
        if (context.Items.TryGetValue("LogCustomPart", out var partValue) &&
            partValue is string partStr && !string.IsNullOrWhiteSpace(partStr))
        {
            customPart = $"_{partStr}";
        }

        // 📁 Carpeta final
        string finalDirectory = Path.Combine(_logDirectory, controllerName, endpoint, fecha);
        Directory.CreateDirectory(finalDirectory);

        // 📝 Nombre final
        string fileName = $"{executionId}_{endpoint}{customPart}_{timestamp}.txt";
        string fullPath = Path.Combine(finalDirectory, fileName);

        // ✅ Cachear SIEMPRE el FULL PATH (antes guardabas solo el fileName)
        context.Items["LogFileName"] = fullPath;

        return fullPath;
    }
    catch (Exception ex)
    {
        LogInternalError(ex);
        return Path.Combine(_logDirectory, "GlobalManualLogs.txt");
    }
}
