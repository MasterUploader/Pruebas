Actualmente tengo el middleware así, de esta forma es como funciona actualmente:

using Logging.Abstractions;
using Logging.Attributes;
using Logging.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Text.Json;

namespace Logging.Middleware;


/// <summary>
/// Middleware para capturar logs de ejecución de controladores en la API.
/// Captura información de Request, Response, Excepciones y Entorno.
/// </summary>
public class LoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILoggingService _loggingService;

    /// <summary>
    /// Cronómetro utilizado para medir el tiempo de ejecución de la acción.
    /// Se inicializa cuando la acción comienza a ejecutarse.
    /// </summary>
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

    /// <summary>
    /// Captura la información del entorno del servidor y del cliente.
    /// </summary>
    private static async Task<string> CaptureEnvironmentInfoAsync(HttpContext context)
    {
        await Task.Delay(TimeSpan.FromMilliseconds(1));

        var request = context.Request;
        var connection = context.Connection;
        var hostEnvironment = context.RequestServices.GetService<IHostEnvironment>();

        // 1. Intentar obtener de un header HTTP
        var distributionFromHeader = context.Request.Headers["Distribucion"].FirstOrDefault();

        // 2. Intentar obtener de los claims del usuario (si existe autenticación JWT)
        var distributionFromClaim = context.User?.Claims?
            .FirstOrDefault(c => c.Type == "distribution")?.Value;

        // 3. Intentar extraer del subdominio (ejemplo: cliente1.api.com)
        var host = context.Request.Host.Host;
        var distributionFromSubdomain = !string.IsNullOrWhiteSpace(host) && host.Contains('.')
            ? host.Split('.')[0]
            : null;

        // 4. Seleccionar la primera fuente válida o asignar "N/A"
        var distribution = distributionFromHeader
                           ?? distributionFromClaim
                           ?? distributionFromSubdomain
                           ?? "N/A";

        // Preparar información extendida
        string application = hostEnvironment?.ApplicationName ?? "Desconocido";
        string env = hostEnvironment?.EnvironmentName ?? "Desconocido";
        string contentRoot = hostEnvironment?.ContentRootPath ?? "Desconocido";
        string executionId = context.TraceIdentifier ?? "Desconocido";
        string clientIp = connection?.RemoteIpAddress?.ToString() ?? "Desconocido";
        string userAgent = request.Headers.UserAgent.ToString() ?? "Desconocido";
        string machineName = Environment.MachineName;
        string os = Environment.OSVersion.ToString();
        host = request.Host.ToString() ?? "Desconocido";

        // Información adicional del contexto
        var extras = new Dictionary<string, string>
            {
                { "Scheme", request.Scheme },
                { "Protocol", request.Protocol },
                { "Method", request.Method },
                { "Path", request.Path },
                { "Query", request.QueryString.ToString() },
                { "ContentType", request.ContentType ?? "N/A" },
                { "ContentLength", request.ContentLength?.ToString() ?? "N/A" },
                { "ClientPort", connection?.RemotePort.ToString() ?? "Desconocido" },
                { "LocalIp", connection?.LocalIpAddress?.ToString() ?? "Desconocido" },
                { "LocalPort", connection?.LocalPort.ToString() ?? "Desconocido" },
                { "ConnectionId", connection?.Id ?? "Desconocido" },
                { "Referer", request.Headers.Referer.ToString() ?? "N/A" }
            };

        return LogFormatter.FormatEnvironmentInfo(
                application: application,
                env: env,
                contentRoot: contentRoot,
                executionId: executionId,
                clientIp: clientIp,
                userAgent: userAgent,
                machineName: machineName,
                os: os,
                host: host,
                distribution: distribution,
                extras: extras
        );
    }

    /// <summary>
    /// Captura la información de la solicitud HTTP antes de que sea procesada por los controladores.
    /// </summary>
    private static async Task<string> CaptureRequestInfoAsync(HttpContext context)
    {
        Console.WriteLine("[LOGGING] CaptureRequestInfoAsync");
        context.Request.EnableBuffering(); // Permite leer el cuerpo de la petición sin afectar la ejecución

        using var reader = new StreamReader(context.Request.Body, Encoding.UTF8, leaveOpen: true);
        string body = await reader.ReadToEndAsync();
        context.Request.Body.Position = 0; // Restablece la posición para que el controlador pueda leerlo

        // Extraer identificador para el nombre del log y guardarlo en context.Items
        var customPart = LogFileNameExtractor.ExtractLogFileNameFromContext(context, body);
        if (!string.IsNullOrWhiteSpace(customPart))
        {
            context.Items["LogCustomPart"] = customPart;

            Console.WriteLine($"CustomParts {customPart}");
        }
        else
        {
            Console.WriteLine("No encontro ningun valor o atributo [LogFileName]");
        }

        return LogFormatter.FormatRequestInfo(context,
            method: context.Request.Method,
            path: context.Request.Path,
            queryParams: context.Request.QueryString.ToString(),
            body: body
        );
    }

    /// <summary>
    /// Captura la información de la respuesta HTTP antes de enviarla al cliente.
    /// </summary>
    private static async Task<string> CaptureResponseInfoAsync(HttpContext context)
    {
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(context.Response.Body, Encoding.UTF8, leaveOpen: true);
        string body = await reader.ReadToEndAsync();
        context.Response.Body.Seek(0, SeekOrigin.Begin);

        string formattedResponse;

        // Usar el objeto guardado en context.Items si existe
        if (context.Items.ContainsKey("ResponseObject"))
        {
            var responseObject = context.Items["ResponseObject"];
            formattedResponse = LogFormatter.FormatResponseInfo(context,
                statusCode: context.Response.StatusCode.ToString(),
                headers: string.Join("; ", context.Response.Headers),
                body: responseObject != null
                    ? JsonSerializer.Serialize(responseObject, JsonHelper.PrettyPrintCamelCase)
                    : "null"
            );
        }
        else
        {
            // Si no se interceptó el ObjectResult, usar el cuerpo normal
            formattedResponse = LogFormatter.FormatResponseInfo(context,
                statusCode: context.Response.StatusCode.ToString(),
                headers: string.Join("; ", context.Response.Headers),
                body: body
            );
        }

        return formattedResponse;
    }
}

