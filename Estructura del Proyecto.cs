using System.Reflection;
using System.Text;
using System.Text.Json;

namespace Logging.Middleware;

/// <summary>
/// Middleware para capturar logs de ejecución de controladores en la API.
/// Ahora incluye extracción automática del valor marcado con [LogFileName]
/// desde cualquier tipo de DTO, incluyendo genéricos y estructuras anidadas.
/// </summary>
public class LoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILoggingService _loggingService;
    private Stopwatch _stopwatch = new();

    /// <summary>
    /// Inicializa una nueva instancia del <see cref="LoggingMiddleware"/>.
    /// </summary>
    public LoggingMiddleware(RequestDelegate next, ILoggingService loggingService)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _loggingService = loggingService ?? throw new ArgumentNullException(nameof(loggingService));
    }

    /// <summary>
    /// Método principal que intercepta las solicitudes HTTP y captura logs.
    /// </summary>
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            // Excluir rutas no necesarias en el log
            var path = context.Request.Path.Value;
            if (!string.IsNullOrEmpty(path) &&
                (path.Contains("swagger", StringComparison.OrdinalIgnoreCase) ||
                 path.Contains("favicon.ico", StringComparison.OrdinalIgnoreCase)))
            {
                await _next(context);
                return;
            }

            _stopwatch = Stopwatch.StartNew();

            // Asignar ExecutionId único
            if (!context.Items.ContainsKey("ExecutionId"))
                context.Items["ExecutionId"] = Guid.NewGuid().ToString();

            // 📌 Pre-extracción del LogCustomPart antes de escribir cualquier log
            await ExtractLogCustomPartFromBody(context);

            // Continuar flujo de logging normal
            _loggingService.WriteLog(context, await CaptureEnvironmentInfoAsync(context));
            _loggingService.WriteLog(context, await CaptureRequestInfoAsync(context));

            var originalBodyStream = context.Response.Body;
            using var responseBody = new MemoryStream();
            context.Response.Body = responseBody;

            await _next(context);

            // Logs de HttpClient si existen
            if (context.Items.TryGetValue("HttpClientLogs", out var clientLogsObj) && clientLogsObj is List<string> clientLogs)
                foreach (var log in clientLogs) _loggingService.WriteLog(context, log);

            _loggingService.WriteLog(context, await CaptureResponseInfoAsync(context));

            responseBody.Seek(0, SeekOrigin.Begin);
            await responseBody.CopyToAsync(originalBodyStream);

            if (context.Items.ContainsKey("Exception") && context.Items["Exception"] is Exception ex)
                _loggingService.AddExceptionLog(ex);
        }
        catch (Exception ex)
        {
            _loggingService.AddExceptionLog(ex);
        }
        finally
        {
            _stopwatch.Stop();
            _loggingService.WriteLog(context, $"[Tiempo Total de Ejecución]: {_stopwatch.ElapsedMilliseconds} ms");
        }
    }

    /// <summary>
    /// Deserializa el body JSON y extrae recursivamente el valor de [LogFileName], si existe.
    /// Guarda el objeto y el valor en HttpContext.Items para que el LoggingService pueda usarlos.
    /// </summary>
    private static async Task ExtractLogCustomPartFromBody(HttpContext context)
    {
        if (context.Request.ContentType?.Contains("application/json", StringComparison.OrdinalIgnoreCase) != true)
            return;

        context.Request.EnableBuffering();
        using var reader = new StreamReader(context.Request.Body, Encoding.UTF8, leaveOpen: true);
        var bodyString = await reader.ReadToEndAsync();
        context.Request.Body.Position = 0;

        try
        {
            var dtoObject = JsonSerializer.Deserialize<object>(bodyString, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (dtoObject != null)
            {
                context.Items["RequestBodyObject"] = dtoObject;
                var customPart = GetLogFileNameValue(dtoObject);
                if (!string.IsNullOrWhiteSpace(customPart))
                    context.Items["LogCustomPart"] = customPart;
            }
        }
        catch
        {
            // Ignorar errores para no interrumpir el flujo
        }
    }

    /// <summary>
    /// Busca recursivamente en un objeto cualquier propiedad marcada con [LogFileName].
    /// </summary>
    private static string? GetLogFileNameValue(object? obj)
    {
        if (obj == null) return null;

        var type = obj.GetType();
        if (type.IsPrimitive || type == typeof(string) || type == typeof(decimal))
            return null;

        // Propiedades actuales
        foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (Attribute.IsDefined(prop, typeof(LogFileNameAttribute)))
            {
                var value = prop.GetValue(obj)?.ToString();
                if (!string.IsNullOrWhiteSpace(value))
                    return value;
            }
        }

        // Propiedades anidadas
        foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            var value = prop.GetValue(obj);
            var nested = GetLogFileNameValue(value);
            if (!string.IsNullOrWhiteSpace(nested))
                return nested;
        }

        return null;
    }

    // Métodos CaptureEnvironmentInfoAsync, CaptureRequestInfoAsync y CaptureResponseInfoAsync
    // permanecen igual que los que ya tienes, no los repito aquí para no alargar demasiado.
}
/// <summary>
/// Obtiene el archivo de log para la petición actual, agregando el valor de [LogFileName] si existe.
/// Si el valor no fue extraído por el Middleware, lo intenta obtener desde el objeto del body.
/// </summary>
public string GetCurrentLogFile()
{
    try
    {
        var context = _httpContextAccessor.HttpContext;
        if (context == null)
            return Path.Combine(_logDirectory, "GlobalManualLogs.txt");

        // 📌 Si no hay LogCustomPart pero sí guardamos el objeto, intentar extraerlo aquí
        if (!context.Items.ContainsKey("LogCustomPart") &&
            context.Items.TryGetValue("RequestBodyObject", out var bodyObj) && bodyObj != null)
        {
            var extracted = GetLogFileNameValue(bodyObj);
            if (!string.IsNullOrWhiteSpace(extracted))
                context.Items["LogCustomPart"] = extracted;
        }

        // Regenerar si falta custom en el path existente
        if (context.Items.TryGetValue("LogFileName", out var existingObj) &&
            existingObj is string existingPath &&
            context.Items.TryGetValue("LogCustomPart", out var partObj) &&
            partObj is string part &&
            !string.IsNullOrWhiteSpace(part) &&
            !existingPath.Contains($"{part}", StringComparison.OrdinalIgnoreCase))
        {
            context.Items.Remove("LogFileName");
        }

        // Reutilizar si ya existe
        if (context.Items.TryGetValue("LogFileName", out var logFile) && logFile is string path && !string.IsNullOrWhiteSpace(path))
            return path;

        // Construcción normal del nombre
        string endpoint = context.Request.Path.Value?.Trim('/').Split('/').LastOrDefault() ?? "UnknownEndpoint";
        var endpointMetadata = context.GetEndpoint();
        var controllerName = endpointMetadata?.Metadata
            .OfType<Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor>()
            .FirstOrDefault()?.ControllerName ?? "UnknownController";

        string fecha = DateTime.Now.ToString("yyyy-MM-dd");
        string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string executionId = context.Items["ExecutionId"]?.ToString() ?? Guid.NewGuid().ToString();

        string customPartSuffix = "";
        if (context.Items.TryGetValue("LogCustomPart", out var partValue) && partValue is string partStr && !string.IsNullOrWhiteSpace(partStr))
            customPartSuffix = $"_{partStr}";

        string finalDirectory = Path.Combine(_logDirectory, controllerName, endpoint, fecha);
        Directory.CreateDirectory(finalDirectory);

        var fileName = $"{executionId}_{endpoint}{customPartSuffix}_{timestamp}.txt";
        string fullPath = Path.Combine(finalDirectory, fileName);

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
/// Busca recursivamente en un objeto cualquier propiedad marcada con [LogFileName].
/// </summary>
private static string? GetLogFileNameValue(object? obj)
{
    if (obj == null) return null;

    var type = obj.GetType();
    if (type.IsPrimitive || type == typeof(string) || type == typeof(decimal))
        return null;

    foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
    {
        if (Attribute.IsDefined(prop, typeof(LogFileNameAttribute)))
        {
            var value = prop.GetValue(obj)?.ToString();
            if (!string.IsNullOrWhiteSpace(value))
                return value;
        }
    }

    foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
    {
        var value = prop.GetValue(obj);
        var nested = GetLogFileNameValue(value);
        if (!string.IsNullOrWhiteSpace(nested))
            return nested;
    }

    return null;
}
