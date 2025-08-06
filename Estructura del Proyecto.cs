/// <summary>
/// Obtiene el archivo de log de la petición actual, garantizando que toda la información
/// se guarde en el mismo archivo. Se organiza por API, controlador, endpoint y fecha.
/// Si `LogFileNameCustom` aparece después del primer acceso, se regenera el nombre.
/// </summary>
public string GetCurrentLogFile()
{
    try
    {
        var context = _httpContextAccessor.HttpContext;

        if (context is not null)
        {
            // 🚨 Si ya se generó antes
            if (context.Items.TryGetValue("LogFileName", out var existing) && existing is string existingPath)
            {
                // Validar si se generó como Unknown y ahora hay un valor válido en LogFileNameCustom
                if (existingPath.Contains("Unknown") && context.Items.TryGetValue("LogFileNameCustom", out var customName) && customName is string customStr && !string.IsNullOrWhiteSpace(customStr))
                {
                    Console.WriteLine("[LOGGING] Forzando regeneración de archivo por nuevo LogFileNameCustom.");
                    context.Items.Remove("LogFileName"); // ⚠️ Elimina para regenerar
                }
                else
                {
                    Console.WriteLine($"[LOGGING] Reutilizando archivo existente: {existingPath}");
                    return existingPath;
                }
            }

            // Extrae información del path: /Bts/Consulta → controller=Bts, endpoint=Consulta
            string rawPath = context.Request.Path.Value?.Trim('/') ?? "Unknown/Unknown";
            var pathParts = rawPath.Split('/');
            string endpoint = pathParts.LastOrDefault() ?? "UnknownEndpoint";

            // Intenta sobrescribir con metadatos (opcional)
            var endpointMetadata = context.GetEndpoint();
            var controllerName = endpointMetadata?.Metadata
                .OfType<Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor>()
                .FirstOrDefault()?.ControllerName ?? "UnknownController";

            // Fecha, timestamp y ejecución
            string fecha = DateTime.UtcNow.ToString("yyyy-MM-dd");
            string timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
            string executionId = context.Items["ExecutionId"]?.ToString() ?? Guid.NewGuid().ToString();

            // 🔁 Componente extra opcional para el nombre del archivo
            string customNamePart = "";
            if (context.Items.TryGetValue("LogFileNameCustom", out var customValue) && customValue is string customStr && !string.IsNullOrWhiteSpace(customStr))
            {
                customNamePart = $"_{customStr}";
                Console.WriteLine($"[LOGGING] Se aplicó LogFileNameCustom: {customStr}");
            }

            // Construcción de ruta final: /API/Controller/Endpoint/Fecha/
            string finalDirectory = Path.Combine(_logDirectory, controllerName, endpoint, fecha);
            Directory.CreateDirectory(finalDirectory);

            // 📝 Nombre del archivo incluye ID de ejecución, endpoint, customName y timestamp
            string fileName = $"{executionId}_{endpoint}{customNamePart}_{timestamp}.txt";
            string fullPath = Path.Combine(finalDirectory, fileName);

            Console.WriteLine($"[LOGGING FINAL] Archivo generado: {fileName}");

            // Guarda en contexto para reutilización en toda la petición
            context.Items["LogFileName"] = fullPath;

            return fullPath;
        }
    }
    catch (Exception ex)
    {
        LogInternalError(ex);
    }

    return Path.Combine(_logDirectory, "GlobalManualLogs.txt");
}
