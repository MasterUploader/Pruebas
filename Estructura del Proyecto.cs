/// <summary>
/// Obtiene el archivo de log de la petición actual, estructurado como:
/// Logs/ApiName/Controlador/Endpoint/AAAA-MM-DD/Archivo.txt
/// </summary>
public string GetCurrentLogFile()
{
    try
    {
        var context = _httpContextAccessor.HttpContext;

        if (context is not null)
        {
            // Reutiliza si ya se definió
            if (context.Items.TryGetValue("LogFileName", out var existing) && existing is string existingPath)
                return existingPath;

            // Extrae información del path: /Bts/Consulta → controller=Bts, endpoint=Consulta
            string rawPath = context.Request.Path.Value?.Trim('/') ?? "Unknown/Unknown";
            var pathParts = rawPath.Split('/');
            string controller = pathParts.Length > 0 ? pathParts[0] : "UnknownController";
            string endpoint = pathParts.Length > 1 ? pathParts[1] : "UnknownEndpoint";

            // Intenta sobrescribir con metadatos (opcional)
            var endpointMetadata = context.GetEndpoint();
            string? metadataController = endpointMetadata?.Metadata
                .OfType<Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor>()
                .FirstOrDefault()?.ControllerName;

            if (!string.IsNullOrWhiteSpace(metadataController))
                controller = metadataController;

            // Fecha, timestamp y ejecución
            string fecha = DateTime.UtcNow.ToString("yyyy-MM-dd");
            string timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
            string executionId = context.Items["ExecutionId"]?.ToString() ?? Guid.NewGuid().ToString();

            // _logDirectory YA contiene el nombre de la API
            string finalDirectory = Path.Combine(_logDirectory, controller, endpoint, fecha);
            Directory.CreateDirectory(finalDirectory);

            string fileName = $"{executionId}_{endpoint}_{timestamp}.txt";
            string fullPath = Path.Combine(finalDirectory, fileName);

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
