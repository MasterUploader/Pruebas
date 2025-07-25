El codigo le hice modificaciones por que el que me entregaste no funcionaba, te muestro como lo llevo

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

            // Extrae información del path: /Bts/Consulta → controller=Bts, endpoint=Consulta
            string rawPath = context.Request.Path.Value?.Trim('/') ?? "Unknown/Unknown";
            var pathParts = rawPath.Split('/');
            string endpoint = context.Request.Path.ToString().Replace("/", "_").Trim('_');

            // Intenta sobrescribir con metadatos (opcional)
            var endpointMetadata = context.GetEndpoint();
            var controllerName = endpointMetadata?.Metadata
                .OfType<Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor>()
                .FirstOrDefault()?.ControllerName ?? "UnknownController";

            

            // Fecha, timestamp y ejecución
            string fecha = DateTime.UtcNow.ToString("yyyy-MM-dd");
            string timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
            string executionId = context.Items["ExecutionId"]?.ToString() ?? Guid.NewGuid().ToString();

            // _logDirectory YA contiene el nombre de la API
            string finalDirectory = Path.Combine(_logDirectory, controllerName, endpoint, fecha);
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

Esta Generando la ruta total así C:\Logs\API_1_TERCEROS_REMESADORAS\Bts\v1_Bts_Autenticacion\2025-07-25,
Pero deberia ser así C:\Logs\API_1_TERCEROS_REMESADORAS\Bts\Autenticacion\2025-07-25,

Estamos más cerca, solo hay que corregir el punto del endpoint, para que no quede así v1_Bts_Autenticacion, sino que así Autenticacion
