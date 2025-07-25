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
            // Reutiliza si ya se definió
            if (context.Items.TryGetValue("LogFileName", out var existing) && existing is string existingPath)
                return existingPath;

            // Extrae información necesaria
            string executionId = context.Items["ExecutionId"]?.ToString() ?? Guid.NewGuid().ToString();
            string rawPath = context.Request.Path.Value?.Trim('/') ?? "UnknownEndpoint";
            string[] pathParts = rawPath.Split('/');

            string controller = pathParts.Length > 0 ? pathParts[0] : "UnknownController";
            string endpoint = pathParts.Length > 1 ? pathParts[1] : "UnknownEndpoint";
            string fecha = DateTime.UtcNow.ToString("yyyy-MM-dd");
            string timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");

            // Alternativa más precisa para obtener el nombre del controlador desde los metadatos
            var endpointMetadata = context.GetEndpoint();
            string? metadataController = endpointMetadata?.Metadata
                .OfType<Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor>()
                .FirstOrDefault()?.ControllerName;

            if (!string.IsNullOrWhiteSpace(metadataController))
                controller = metadataController;

            // Nombre de la API
            string apiName = AppDomain.CurrentDomain.FriendlyName;

            // Construcción del path completo
            var finalDirectory = Path.Combine(_logDirectory, apiName, controller, endpoint, fecha);
            Directory.CreateDirectory(finalDirectory);

            string fileName = $"{executionId}_{endpoint}_{timestamp}.txt";
            string fullPath = Path.Combine(finalDirectory, fileName);

            // Guardar en Items para reutilizar
            context.Items["LogFileName"] = fullPath;

            return fullPath;
        }
    }
    catch (Exception ex)
    {
        LogInternalError(ex);
    }

    // Fallback global
    return Path.Combine(_logDirectory, "GlobalManualLogs.txt");
}
