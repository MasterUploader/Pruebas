/// <summary>
/// Obtiene el archivo de log de la petición actual, garantizando que toda la información
/// se guarde en el mismo archivo. Se organiza por API, controlador, endpoint y fecha.
/// </summary>
public string GetCurrentLogFile()
{
    try
    {
        var context = _httpContextAccessor.HttpContext;

        if (context is not null)
        {
            // Reutiliza si ya se definió previamente
            if (context.Items.TryGetValue("LogFileName", out var existing) && existing is string existingPath)
                return existingPath;

            // Extrae información del path: /Bts/Consulta → controller=Bts, endpoint=Consulta
            string rawPath = context.Request.Path.Value?.Trim('/') ?? "Unknown/Unknown";
            var pathParts = rawPath.Split('/');
            string endpoint = pathParts.LastOrDefault() ?? "UnknownEndpoint";

            // Intenta obtener nombre del controlador
            var endpointMetadata = context.GetEndpoint();
            var controllerName = endpointMetadata?.Metadata
                .OfType<Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor>()
                .FirstOrDefault()?.ControllerName ?? "UnknownController";

            // Fecha y timestamp
            string fecha = DateTime.UtcNow.ToString("yyyy-MM-dd");
            string timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
            string traceId = context.Items["ExecutionId"]?.ToString() ?? Guid.NewGuid().ToString();

            // 🧩 Valor opcional adicional definido en el Middleware (como "id-12345")
            string customSuffix = "";
            if (context.Items.TryGetValue("LogFileNameCustom", out var custom) && custom is string str && !string.IsNullOrWhiteSpace(str))
            {
                customSuffix = $"_{str}";
                Console.WriteLine($"[LOGGING] Usando LogFileNameCustom desde HttpContext.Items: {str}");
            }

            // Construcción de la ruta y nombre final del archivo
            string finalDirectory = Path.Combine(_logDirectory, controllerName, endpoint, fecha);
            Directory.CreateDirectory(finalDirectory);

            string fileName = $"{traceId}_{endpoint}{customSuffix}_{timestamp}.txt";
            string fullPath = Path.Combine(finalDirectory, fileName);

            // Almacena para uso posterior durante toda la petición
            context.Items["LogFileName"] = fullPath;

            Console.WriteLine($"[LOGGING FINAL] Archivo generado: {fileName}");
            return fullPath;
        }
    }
    catch (Exception ex)
    {
        LogInternalError(ex);
    }

    // Fallback si ocurre un error
    return Path.Combine(_logDirectory, "GlobalManualLogs.txt");
}
