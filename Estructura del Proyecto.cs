using Logging.Abstractions;
using Logging.Configuration;
using Logging.Extensions;
using Logging.Helpers;
using Logging.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using System.Data.Common;
using System.Text;

namespace Logging.Services;

/// <summary>
/// Servicio de logging que centraliza la construcción de rutas, escritura en archivos
/// y compatibilidad con la agregación cronológica (por inicio) de bloques dinámicos.
/// </summary>
public class LoggingService(IHttpContextAccessor httpContextAccessor, IHostEnvironment hostEnvironment, IOptions<LoggingOptions> loggingOptions) : ILoggingService
{
    // Dependencias y configuración inyectadas (constructor primario).
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;
    private readonly LoggingOptions _loggingOptions = loggingOptions.Value;

    // Carpeta base por API (p.ej. BaseLogDirectory/<ApiName>)
    private readonly string _logDirectory = Path.Combine(loggingOptions.Value.BaseLogDirectory,
                                                         !string.IsNullOrWhiteSpace(hostEnvironment.ApplicationName) ? hostEnvironment.ApplicationName : "Desconocido");

    /// <summary>
    /// Clase local para acumular bloques “timed” en Items. No se expone.
    /// </summary>
    private sealed class TimedEntry(DateTime tsUtc, string content)
    {
        /// <summary>Instante (UTC) en que INICIÓ el evento. Usado para ordenar cronológicamente.</summary>
        public DateTime TsUtc { get; } = tsUtc;

        /// <summary>Bloque listo para escribir.</summary>
        public string Content { get; } = content;
    }

    /// <summary>
    /// Clave de Items para la lista de bloques dinámicos con timestamp (HTTP/SQL/etc.).
    /// Se mantiene el nombre “HttpClientLogsTimed” por compatibilidad con tu handler actual.
    /// </summary>
    private const string TimedItemsKey = "HttpClientLogsTimed";

    /// <summary>
    /// Clave de Items para propagar el instante de inicio de la ejecución SQL desde el wrapper.
    /// </summary>
    private const string SqlStartedKey = "__SqlStartedUtc";

    /// <summary>
    /// Inicialización defensiva: asegura la carpeta base de logs.
    /// </summary>
    public LoggingService : this(httpContextAccessor, hostEnvironment, loggingOptions)
    {
        try
        {
            if (!Directory.Exists(_logDirectory))
                Directory.CreateDirectory(_logDirectory);
        }
        catch (Exception ex)
        {
            LogInternalError(ex); // Log de error interno, no detiene la app.
        }
    }

    /// <summary>
    /// Obtiene el path absoluto del archivo de log de la request actual. Mantiene consistencia
    /// por ExecutionId/Controller/Endpoint/Fecha. Cachea el path en Items["LogFileName"].
    /// </summary>
    public string GetCurrentLogFile()
    {
        try
        {
            var context = _httpContextAccessor.HttpContext;
            if (context is null) return BuildErrorFilePath(kind: "manual", context: null);

            // Si existía cache y cambió el custom part, invalídalo para regenerar.
            if (context.Items.TryGetValue("LogFileName", out var existingObj) &&
                existingObj is string existingPath &&
                context.Items.TryGetValue("LogCustomPart", out var partObj) &&
                partObj is string part && !string.IsNullOrWhiteSpace(part) &&
                !existingPath.Contains(part, StringComparison.OrdinalIgnoreCase))
            {
                context.Items.Remove("LogFileName");
            }

            // Reusar si ya está cacheado el FULL PATH.
            if (context.Items.TryGetValue("LogFileName", out var cached) &&
                cached is string cachedPath && !string.IsNullOrWhiteSpace(cachedPath))
            {
                return cachedPath;
            }

            // Nombre de endpoint (último segmento).
            string endpoint = context.Request.Path.Value?.Trim('/').Split('/').LastOrDefault() ?? "UnknownEndpoint";

            // Controller desde CAD cuando está disponible.
            var endpointMetadata = context.GetEndpoint();
            string controllerName = endpointMetadata?.Metadata
                .OfType<Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor>()
                .FirstOrDefault()?.ControllerName ?? "UnknownController";

            // Fecha/hora local + ExecutionId.
            string fecha = DateTime.Now.ToString("yyyy-MM-dd");
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string executionId = context.Items["ExecutionId"]?.ToString() ?? Guid.NewGuid().ToString();

            // Sufijo opcional (custom) para distinguir archivos.
            string customPart = "";
            if (context.Items.TryGetValue("LogCustomPart", out var partValue) &&
                partValue is string partStr && !string.IsNullOrWhiteSpace(partStr))
            {
                customPart = $"_{partStr}";
            }

            // Carpeta final: <base>/<controller>/<endpoint>/<fecha>
            string finalDirectory = Path.Combine(_logDirectory, controllerName, endpoint, fecha);
            Directory.CreateDirectory(finalDirectory); // Garantía de existencia

            // Nombre de archivo: Endpoint_ExecutionId[_Custom]_Timestamp.txt
            string fileName = $"{endpoint}_{executionId}{customPart}_{timestamp}.txt";
            string fullPath = Path.Combine(finalDirectory, fileName);

            // Cachear el path completo.
            context.Items["LogFileName"] = fullPath;
            return fullPath;
        }
        catch (Exception ex)
        {
            LogInternalError(ex);
            return BuildErrorFilePath(kind: "manual", context: _httpContextAccessor.HttpContext);
        }
    }

    /// <summary>
    /// Escribe el contenido en el archivo de la request actual (.txt) y su .csv (si está habilitado).
    /// Para contenido grande (&gt;128KB) usa Task.Run para no bloquear el request.
    /// </summary>
    public void WriteLog(HttpContext? context, string logContent)
    {
        try
        {
            string filePath = GetCurrentLogFile();
            bool isNewFile = !File.Exists(filePath);

            var logBuilder = new StringBuilder();

            // Cabecera automática solo en el primer write del archivo.
            if (isNewFile) logBuilder.AppendLine(LogFormatter.FormatBeginLog());

            // Contenido aportado por el llamador.
            logBuilder.AppendLine(logContent);

            // Pie automático: si la respuesta YA inició (headers escritos), cierra el log.
            if (context is not null && context.Response.HasStarted)
                logBuilder.AppendLine(LogFormatter.FormatEndLog());

            string fullText = logBuilder.ToString();

            bool isLargeLog = fullText.Length > (128 * 1024); // ~128 KB
            if (isLargeLog)
            {
                // Escritura asíncrona (no bloquear), con txt/csv según opciones.
                Task.Run(() =>
                {
                    if (_loggingOptions.GenerateTxt) LogHelper.WriteLogToFile(_logDirectory, filePath, fullText);
                    if (_loggingOptions.GenerateCsv) LogHelper.SaveLogAsCsv(_logDirectory, filePath, logContent);
                });
            }
            else
            {
                // Escritura directa, respetando orden de llamadas del request.
                if (_loggingOptions.GenerateTxt) LogHelper.WriteLogToFile(_logDirectory, filePath, fullText);
                if (_loggingOptions.GenerateCsv) LogHelper.SaveLogAsCsv(_logDirectory, filePath, logContent);
            }
        }
        catch (Exception ex)
        {
            LogInternalError(ex); // Nunca interrumpir por fallos de logging
        }
    }

    /// <summary>
    /// Agrega una línea simple al log actual.
    /// </summary>
    public void AddSingleLog(string message)
    {
        try
        {
            string formatted = LogFormatter.FormatSingleLog(message).Indent(LogScope.CurrentLevel);
            LogHelper.SafeWriteLog(_logDirectory, GetCurrentLogFile(), formatted);
        }
        catch (Exception ex) { LogInternalError(ex); }
    }

    /// <summary>
    /// Agrega un objeto con nombre al log actual.
    /// </summary>
    public void AddObjLog(string objectName, object logObject)
    {
        try
        {
            string formatted = LogFormatter.FormatObjectLog(objectName, logObject).Indent(LogScope.CurrentLevel);
            LogHelper.SafeWriteLog(_logDirectory, GetCurrentLogFile(), formatted);
        }
        catch (Exception ex) { LogInternalError(ex); }
    }

    /// <summary>
    /// Agrega un objeto (sin nombre) al log actual.
    /// </summary>
    public void AddObjLog(object logObject)
    {
        try
        {
            string objectName = logObject?.GetType()?.Name ?? "ObjetoDesconocido";
            object safeObject = logObject ?? new { };
            string formatted = LogFormatter.FormatObjectLog(objectName, safeObject).Indent(LogScope.CurrentLevel);

            LogHelper.SafeWriteLog(_logDirectory, GetCurrentLogFile(), formatted);
        }
        catch (Exception ex) { LogInternalError(ex); }
    }

    /// <summary>
    /// Registra una excepción en el archivo actual.
    /// </summary>
    public void AddExceptionLog(Exception ex)
    {
        try
        {
            string formatted = LogFormatter.FormatExceptionDetails(ex.ToString()).Indent(LogScope.CurrentLevel);
            LogHelper.SafeWriteLog(_logDirectory, GetCurrentLogFile(), formatted);
        }
        catch (Exception e) { LogInternalError(e); }
    }

    /// <summary>
    /// Log de ejecución SQL EXITOSA:
    /// ahora no se escribe directo, sino que se encola con su timestamp de INICIO
    /// en Items["HttpClientLogsTimed"] para que el middleware lo ordene antes de Response.
    /// </summary>
    public void LogDatabaseSuccess(SqlLogModel model, HttpContext? context = null)
    {
        try
        {
            // Bloque formateado original (no alteramos la forma visual).
            var formatted = LogFormatter.FormatDbExecution(model);

            // Si tenemos contexto → encolar “timed” (ordenará por inicio). Si no, escribir directo.
            if (context is not null)
            {
                var startedUtc = model.StartTime.Kind == DateTimeKind.Utc ? model.StartTime : model.StartTime.ToUniversalTime();
                EnqueueTimed(context, startedUtc, formatted);
            }
            else
            {
                WriteLog(context, formatted);
            }
        }
        catch (Exception loggingEx)
        {
            LogInternalError(loggingEx);
        }
    }

    /// <summary>
    /// Log de ejecución SQL con ERROR:
    /// toma el instante de INICIO cargado por el wrapper en Items["__SqlStartedUtc"] (si existe)
    /// para ordenar correctamente; si no existe, usa ahora (UTC) como mejor esfuerzo.
    /// </summary>
    public void LogDatabaseError(DbCommand command, Exception ex, HttpContext? context = null)
    {
        try
        {
            var connectionInfo = LogHelper.ExtractDbConnectionInfo(command.Connection?.ConnectionString);
            var tabla = LogHelper.ExtractTableName(command.CommandText);

            var formatted = LogFormatter.FormatDbExecutionError(
                nombreBD: connectionInfo.Database,
                ip: connectionInfo.Ip,
                puerto: connectionInfo.Port,
                biblioteca: connectionInfo.Library,
                tabla: tabla,
                sentenciaSQL: command.CommandText,
                exception: ex,
                horaError: DateTime.Now
            );

            if (context is not null)
            {
                // Instante de inicio propagado por el wrapper (si estuvo disponible)
                var startedUtc = context.Items.TryGetValue(SqlStartedKey, out var obj) && obj is DateTime dt
                                 ? dt
                                 : DateTime.UtcNow;

                EnqueueTimed(context, startedUtc, formatted);
            }
            else
            {
                WriteLog(context, formatted);
            }

            // Rastro general de excepción (canal transversal).
            AddExceptionLog(ex);
        }
        catch (Exception errorAlLoguear)
        {
            LogInternalError(errorAlLoguear);
        }
    }

    // ========================= Helpers privados =========================

    /// <summary>
    /// Encola un bloque “timed” en Items para que el middleware lo escriba ordenado por INICIO.
    /// </summary>
    private static void EnqueueTimed(HttpContext context, DateTime startedUtc, string content)
    {
        if (!context.Items.ContainsKey(TimedItemsKey)) context.Items[TimedItemsKey] = new List<object>();
        if (context.Items[TimedItemsKey] is List<object> list) list.Add(new TimedEntry(startedUtc, content));
    }

    /// <summary>
    /// Devuelve un nombre seguro para usar en rutas/archivos.
    /// </summary>
    private static string Sanitize(string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return "Unknown";
        var invalid = Path.GetInvalidFileNameChars().Concat(Path.GetInvalidPathChars()).Distinct().ToArray();
        var cleaned = new string([.. name.Where(c => !invalid.Contains(c))]).Trim();
        return string.IsNullOrWhiteSpace(cleaned) ? "Unknown" : cleaned;
    }

    /// <summary>
    /// Obtiene el nombre de endpoint de forma segura.
    /// </summary>
    private static string GetEndpointSafe(HttpContext? context)
    {
        if (context is null) return "NoContext";

        var cad = context.GetEndpoint()?.Metadata
            .OfType<Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor>()
            .FirstOrDefault();

        var endpoint = cad?.ActionName
                       ?? (context.Request.Path.Value ?? "/").Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries).LastOrDefault()
                       ?? "UnknownEndpoint";

        return Sanitize(endpoint);
    }

    /// <summary>
    /// Carpeta base de errores por fecha local.
    /// </summary>
    private string GetErrorsDirectory(DateTime nowLocal)
    {
        var dir = Path.Combine(_logDirectory, "Errores", nowLocal.ToString("yyyy-MM-dd"));
        Directory.CreateDirectory(dir);
        return dir;
    }

    /// <summary>
    /// Construye ruta de archivo para errores internos o manuales.
    /// </summary>
    private string BuildErrorFilePath(string kind, HttpContext? context)
    {
        var now = DateTime.Now;
        var dir = GetErrorsDirectory(now);

        var executionId = context?.Items?["ExecutionId"]?.ToString() ?? Guid.NewGuid().ToString();
        var endpoint = GetEndpointSafe(context);
        var timestamp = now.ToString("yyyyMMdd_HHmmss");

        var suffix = string.Equals(kind, "internal", StringComparison.OrdinalIgnoreCase) ? "_internal" : "";
        var fileName = $"{executionId}_{endpoint}_{timestamp}{suffix}.txt";

        return Path.Combine(dir, fileName);
    }

    /// <summary>
    /// Log de error interno del servicio (no detiene la app ni propaga).
    /// </summary>
    public void LogInternalError(Exception ex)
    {
        try
        {
            var context = _httpContextAccessor.HttpContext;
            var errorPath = BuildErrorFilePath(kind: "internal", context: context);

            var msg = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Error en LoggingService: {ex}{Environment.NewLine}";
            File.AppendAllText(errorPath, msg);
        }
        catch
        {
            // Silencio para evitar loops de error.
        }
    }
}





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
/// Middleware que captura Environment (2), Request (4), bloques dinámicos ordenados (HTTP/SQL/…)
/// y Response (5). Mantiene compatibilidad con Items existentes y no rompe integraciones.
/// </summary>
public class LoggingMiddleware(RequestDelegate next, ILoggingService loggingService)
{
    private readonly RequestDelegate _next = next ?? throw new ArgumentNullException(nameof(next));
    private readonly ILoggingService _loggingService = loggingService ?? throw new ArgumentNullException(nameof(loggingService));

    /// <summary>
    /// Cronómetro por-request para medir tiempo total.
    /// </summary>
    private Stopwatch _stopwatch = new();

    /// <summary>
    /// Intercepta la request, escribe bloques fijos y agrega los dinámicos en orden por INICIO.
    /// </summary>
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            // Filtrado básico (swagger/favicon).
            var path = context.Request.Path.Value;
            if (!string.IsNullOrEmpty(path) &&
                (path.Contains("swagger", StringComparison.OrdinalIgnoreCase) ||
                 path.Contains("favicon.ico", StringComparison.OrdinalIgnoreCase)))
            {
                await _next(context);
                return;
            }

            _stopwatch = Stopwatch.StartNew();

            // ExecutionId por-request para correlación.
            if (!context.Items.ContainsKey("ExecutionId")) context.Items["ExecutionId"] = Guid.NewGuid().ToString();

            // Intento temprano de extraer LogCustomPart (DTO/Query).
            await ExtractLogCustomPartFromBody(context);

            // (2) Environment Info — bloque fijo (se escribe en el archivo ahora).
            _loggingService.WriteLog(context, await CaptureEnvironmentInfoAsync(context));

            // (4) Request Info — bloque fijo (se escribe en el archivo ahora).
            _loggingService.WriteLog(context, await CaptureRequestInfoAsync(context));

            // Interceptar body de respuesta en memoria para no iniciar headers todavía.
            var originalBodyStream = context.Response.Body;
            using var responseBody = new MemoryStream();
            context.Response.Body = responseBody;

            // Ejecutar el pipeline.
            await _next(context);

            // === BLOQUES DINÁMICOS ORDENADOS (entre 4 y 5) ===
            // 1) Lista “timed” (preferida: trae TsUtc/Content para ordenar)
            if (context.Items.TryGetValue("HttpClientLogsTimed", out var timedObj) &&
                timedObj is List<object> timedList && timedList.Count > 0)
            {
                // Ordenar por TsUtc ASC con reflexión simple (no acoplamos tipos).
                var ordered = timedList
                    .Select(o => new
                    {
                        Ts = (DateTime)(o.GetType().GetProperty("TsUtc")?.GetValue(o) ?? DateTime.UtcNow),
                        Tx = (string)(o.GetType().GetProperty("Content")?.GetValue(o) ?? string.Empty)
                    })
                    .OrderBy(x => x.Ts)
                    .ToList();

                foreach (var e in ordered)
                    _loggingService.WriteLog(context, e.Tx);

                // Limpia para evitar duplicados posteriores
                context.Items.Remove("HttpClientLogsTimed");
            }
            else
            {
                // 2) Fallback: lista antigua (solo strings, sin timestamp)
                if (context.Items.TryGetValue("HttpClientLogs", out var clientLogsObj) &&
                    clientLogsObj is List<string> clientLogs && clientLogs.Count > 0)
                {
                    foreach (var log in clientLogs)
                        _loggingService.WriteLog(context, log);
                }
            }

            // (5) Response Info — bloque fijo (todavía no hemos enviado headers).
            _loggingService.WriteLog(context, await CaptureResponseInfoAsync(context));

            // Restaurar el stream original y enviar realmente la respuesta al cliente.
            responseBody.Seek(0, SeekOrigin.Begin);
            await responseBody.CopyToAsync(originalBodyStream);

            // Si se acumuló alguna Exception en Items, persiste su detalle.
            if (context.Items.ContainsKey("Exception") && context.Items["Exception"] is Exception ex)
                _loggingService.AddExceptionLog(ex);
        }
        catch (Exception ex)
        {
            _loggingService.AddExceptionLog(ex); // El logging no debe romper el request
        }
        finally
        {
            _stopwatch.Stop();

            // Registro final: tiempo total. En este punto, lo usual es que HasStarted sea true,
            // por lo que WriteLog añadirá el Fin de Log (7) junto con esta línea.
            _loggingService.WriteLog(context, $"[Tiempo Total de Ejecución]: {_stopwatch.ElapsedMilliseconds} ms");
        }
    }

    /// <summary>
    /// Extrae un valor “custom” para el nombre del archivo de log desde el DTO
    /// o desde Query/Route (GET). Lo deja en Items["LogCustomPart"] si existe.
    /// </summary>
    private static async Task ExtractLogCustomPartFromBody(HttpContext context)
    {
        string? bodyString = null;

        // Soporte JSON de entrada: habilita rebobinado (buffering) para no interferir con el pipeline.
        if (context.Request.ContentType?.Contains("application/json", StringComparison.OrdinalIgnoreCase) == true)
        {
            context.Request.EnableBuffering();
            using var reader = new StreamReader(context.Request.Body, Encoding.UTF8, leaveOpen: true);
            bodyString = await reader.ReadToEndAsync();
            context.Request.Body.Position = 0;
        }

        try
        {
            var customPart = StrongTypedLogFileNameExtractor.Extract(context, bodyString);
            if (!string.IsNullOrWhiteSpace(customPart))
                context.Items["LogCustomPart"] = customPart;
        }
        catch
        {
            // La extracción no debe interrumpir el request.
        }
    }

    /// <summary>
    /// Busca recursivamente en un objeto cualquier propiedad marcada con [LogFileName].
    /// </summary>
    private static string? GetLogFileNameValue(object? obj)
    {
        if (obj is null) return null;

        var type = obj.GetType();
        if (type.IsPrimitive || type == typeof(string) || type == typeof(decimal)) return null;

        // Búsqueda directa en propiedades marcadas
        foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                 .Where(prop => Attribute.IsDefined(prop, typeof(LogFileNameAttribute))))
        {
            var value = prop.GetValue(obj)?.ToString();
            if (!string.IsNullOrWhiteSpace(value)) return value;
        }

        // Búsqueda en propiedades anidadas
        foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            var value = prop.GetValue(obj);
            var nested = GetLogFileNameValue(value);
            if (!string.IsNullOrWhiteSpace(nested)) return nested;
        }

        return null;
    }

    /// <summary>
    /// Construye el bloque “Environment Info (2)”.
    /// </summary>
    private static async Task<string> CaptureEnvironmentInfoAsync(HttpContext context)
    {
        await Task.Delay(TimeSpan.FromMilliseconds(1)); // mantener firma async sin bloquear

        var request = context.Request;
        var connection = context.Connection;
        var hostEnvironment = context.RequestServices.GetService<IHostEnvironment>();

        // Origen de "distribution" preferente: Header → Claim → Subdominio.
        var distributionFromHeader = context.Request.Headers["Distribucion"].FirstOrDefault();
        var distributionFromClaim  = context.User?.Claims?.FirstOrDefault(c => c.Type == "distribution")?.Value;
        var host = context.Request.Host.Host;
        var distributionFromSubdomain = !string.IsNullOrWhiteSpace(host) && host.Contains('.')
            ? host.Split('.')[0]
            : null;

        var distribution = distributionFromHeader ?? distributionFromClaim ?? distributionFromSubdomain ?? "N/A";

        // Metadatos de host
        string application = hostEnvironment?.ApplicationName ?? "Desconocido";
        string env = hostEnvironment?.EnvironmentName ?? "Desconocido";
        string contentRoot = hostEnvironment?.ContentRootPath ?? "Desconocido";
        string executionId = context.TraceIdentifier ?? "Desconocido";
        string clientIp = connection?.RemoteIpAddress?.ToString() ?? "Desconocido";
        string userAgent = request.Headers.UserAgent.ToString() ?? "Desconocido";
        string machineName = Environment.MachineName;
        string os = Environment.OSVersion.ToString();
        var fullHost = request.Host.ToString() ?? "Desconocido";

        // Extras compactos
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
            host: fullHost,
            distribution: distribution,
            extras: extras
        );
    }

    /// <summary>
    /// Construye el bloque “Request Info (4)”.
    /// </summary>
    private static async Task<string> CaptureRequestInfoAsync(HttpContext context)
    {
        context.Request.EnableBuffering(); // lectura sin consumir el stream

        using var reader = new StreamReader(context.Request.Body, Encoding.UTF8, leaveOpen: true);
        string body = await reader.ReadToEndAsync();
        context.Request.Body.Position = 0;

        // Extrae y deja un posible valor para el nombre del archivo.
        var customPart = LogFileNameExtractor.ExtractLogFileNameFromContext(context, body);
        if (!string.IsNullOrWhiteSpace(customPart))
            context.Items["LogCustomPart"] = customPart;

        return LogFormatter.FormatRequestInfo(context,
            method: context.Request.Method,
            path: context.Request.Path,
            queryParams: context.Request.QueryString.ToString(),
            body: body
        );
    }

    /// <summary>
    /// Construye el bloque “Response Info (5)” sin forzar el envío de headers.
    /// </summary>
    private static async Task<string> CaptureResponseInfoAsync(HttpContext context)
    {
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(context.Response.Body, Encoding.UTF8, leaveOpen: true);
        string body = await reader.ReadToEndAsync();
        context.Response.Body.Seek(0, SeekOrigin.Begin);

        string formattedResponse;

        if (context.Items.ContainsKey("ResponseObject"))
        {
            var responseObject = context.Items["ResponseObject"];
            formattedResponse = LogFormatter.FormatResponseInfo(context,
                statusCode: context.Response.StatusCode.ToString(),
                headers: string.Join("; ", context.Response.Headers),
                body: responseObject is not null
                    ? JsonSerializer.Serialize(responseObject, JsonHelper.PrettyPrintCamelCase)
                    : "null"
            );
        }
        else
        {
            formattedResponse = LogFormatter.FormatResponseInfo(context,
                statusCode: context.Response.StatusCode.ToString(),
                headers: string.Join("; ", context.Response.Headers),
                body: body
            );
        }

        return formattedResponse;
    }
}



using Logging.Abstractions;
using Microsoft.AspNetCore.Http;
using System.Collections;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Logging.Decorators;

/// <summary>
/// Decorador de DbCommand que:
/// - Mide cada ejecución (Reader/NonQuery/Scalar) y construye un bloque estructurado.
/// - Sustituye parámetros por valores reales en el SQL sin romper literales.
/// - Propaga el INICIO (UTC) al contexto para ordenar cronológicamente en el middleware.
/// </summary>
public class LoggingDbCommandWrapper(
    DbCommand innerCommand,
    ILoggingService? loggingService = null,
    IHttpContextAccessor? httpContextAccessor = null) : DbCommand
{
    private readonly DbCommand _innerCommand = innerCommand ?? throw new ArgumentNullException(nameof(innerCommand));
    private readonly ILoggingService? _loggingService = loggingService;
    private readonly IHttpContextAccessor? _httpContextAccessor = httpContextAccessor;

    // Clave compartida con el servicio para propagar el INICIO del SQL
    // (el servicio la usa si ocurre error para mantener el orden correcto).
    private const string SqlStartedKey = "__SqlStartedUtc";

    #region Ejecuciones (bloque por ejecución)

    /// <inheritdoc />
    protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior)
    {
        var startedLocal = DateTime.Now;                     // INICIO en hora local
        var startedUtc   = startedLocal.ToUniversalTime();   // Sello UTC para ordenar
        var ctx          = _httpContextAccessor?.HttpContext;

        if (ctx is not null) ctx.Items[SqlStartedKey] = startedUtc; // ⇦ Propaga inicio al contexto

        var sw = Stopwatch.StartNew();
        try
        {
            var reader = _innerCommand.ExecuteReader(behavior);
            sw.Stop();

            // SELECT: filas afectadas = 0
            LogOneExecution(
                startedAt: startedLocal,
                duration: sw.Elapsed,
                affectedRows: 0,
                sqlRendered: RenderSqlWithParametersSafe(_innerCommand.CommandText, _innerCommand.Parameters)
            );

            return reader;
        }
        catch (Exception ex)
        {
            _loggingService?.LogDatabaseError(_innerCommand, ex, ctx);
            throw;
        }
    }

    /// <inheritdoc />
    public override int ExecuteNonQuery()
    {
        var startedLocal = DateTime.Now;
        var startedUtc   = startedLocal.ToUniversalTime();
        var ctx          = _httpContextAccessor?.HttpContext;

        if (ctx is not null) ctx.Items[SqlStartedKey] = startedUtc;

        var sw = Stopwatch.StartNew();

        var result = _innerCommand.ExecuteNonQuery();

        sw.Stop();
        LogOneExecution(
            startedAt: startedLocal,
            duration: sw.Elapsed,
            affectedRows: result,
            sqlRendered: RenderSqlWithParametersSafe(_innerCommand.CommandText, _innerCommand.Parameters)
        );

        return result;
    }

    /// <inheritdoc />
    public override object? ExecuteScalar()
    {
        var startedLocal = DateTime.Now;
        var startedUtc   = startedLocal.ToUniversalTime();
        var ctx          = _httpContextAccessor?.HttpContext;

        if (ctx is not null) ctx.Items[SqlStartedKey] = startedUtc;

        var sw = Stopwatch.StartNew();

        var result = _innerCommand.ExecuteScalar();

        sw.Stop();
        LogOneExecution(
            startedAt: startedLocal,
            duration: sw.Elapsed,
            affectedRows: 0, // Scalar no afecta filas
            sqlRendered: RenderSqlWithParametersSafe(_innerCommand.CommandText, _innerCommand.Parameters)
        );

        return result;
    }

    #endregion

    #region Logging por ejecución (con agregación cronológica)

    /// <summary>
    /// Construye el modelo de log SQL y lo envía al servicio para que lo agregue a la lista “timed”.
    /// </summary>
    private void LogOneExecution(DateTime startedAt, TimeSpan duration, int affectedRows, string sqlRendered)
    {
        if (_loggingService is null) return; // Resiliencia: sin servicio, no interfiere.

        try
        {
            var conn = _innerCommand.Connection;

            var model = new SqlLogModel
            {
                Sql = sqlRendered,
                ExecutionCount = 1,               // bloque por ejecución
                TotalAffectedRows = affectedRows, // 0 para SELECT/SCALAR
                StartTime = startedAt,            // ⇦ INICIO real de la ejecución
                Duration = duration,
                DatabaseName = conn?.Database ?? "Desconocida",
                Ip = conn?.DataSource ?? "Desconocida",
                Port = 0, // Si puedes inferirlo, complétalo.
                TableName = ExtraerNombreTablaDesdeSql(_innerCommand.CommandText),
                Schema = ExtraerEsquemaDesdeSql(_innerCommand.CommandText)
            };

            _loggingService.LogDatabaseSuccess(model, _httpContextAccessor?.HttpContext);
        }
        catch (Exception logEx)
        {
            // El logging no debe detener la app: registramos en el mismo archivo como fallback.
            try
            {
                _loggingService?.WriteLog(_httpContextAccessor?.HttpContext,
                    $"[LoggingDbCommandWrapper] Error al escribir el log SQL: {logEx.Message}");
            }
            catch { /* silencio para evitar loops */ }
        }
    }

    #endregion

    #region Render de SQL con parámetros (respetando literales)

    /// <summary>
    /// Devuelve el SQL con parámetros sustituidos por sus valores reales (comillas en literales).
    /// Ignora coincidencias dentro de literales '...'.
    /// </summary>
    private static string RenderSqlWithParametersSafe(string? sql, DbParameterCollection parameters)
    {
        if (string.IsNullOrEmpty(sql) || parameters.Count == 0) return sql ?? string.Empty;

        var literalRanges = ComputeSingleQuoteRanges(sql);

        if (sql.Contains('?'))
            return ReplacePositionalIgnoringLiterals(sql, parameters, literalRanges);

        return ReplaceNamedIgnoringLiterals(sql, parameters, literalRanges);
    }

    /// <summary>
    /// Identifica rangos [start,end] dentro de comillas simples (maneja escape '').
    /// </summary>
    private static List<(int start, int end)> ComputeSingleQuoteRanges(string sql)
    {
        var ranges = new List<(int, int)>();
        bool inString = false;
        int start = -1;

        int idx = 0;
        while (idx < sql.Length)
        {
            char c = sql[idx];
            if (c == '\'')
            {
                bool isEscaped = (idx + 1 < sql.Length) && sql[idx + 1] == '\'';

                if (!inString) { inString = true; start = idx; }
                else if (!isEscaped) { inString = false; ranges.Add((start, idx)); }

                idx += isEscaped ? 2 : 1;
                continue;
            }

            idx++;
        }

        return ranges;
    }

    /// <summary>Indica si un índice está dentro de alguno de los rangos de literales.</summary>
    private static bool IsInsideAnyRange(int index, List<(int start, int end)> ranges)
    {
        for (int i = 0; i < ranges.Count; i++)
        {
            var (s, e) = ranges[i];
            if (index >= s && index <= e) return true;
        }
        return false;
    }

    /// <summary>
    /// Reemplaza '?' por valores en orden, ignorando las que caen dentro de literales.
    /// </summary>
    private static string ReplacePositionalIgnoringLiterals(string sql, DbParameterCollection parameters, List<(int start, int end)> literalRanges)
    {
        var sb = new StringBuilder(sql.Length + parameters.Count * 10);
        int paramIndex = 0;

        for (int i = 0; i < sql.Length; i++)
        {
            char c = sql[i];
            if (c == '?' && !IsInsideAnyRange(i, literalRanges) && paramIndex < parameters.Count)
            {
                var p = parameters[paramIndex++];
                sb.Append(FormatParameterValue(p));
            }
            else
            {
                sb.Append(c);
            }
        }

        return sb.ToString();
    }

    /// <summary>
    /// Reemplazo de parámetros nombrados (@name, :name) ignorando literales.
    /// </summary>
    private static string ReplaceNamedIgnoringLiterals(string sql, DbParameterCollection parameters, List<(int start, int end)> literalRanges)
    {
        if (parameters.Count == 0) return sql;

        string result = sql;
        foreach (DbParameter p in parameters)
        {
            var name = p.ParameterName?.Trim();
            if (string.IsNullOrEmpty(name)) continue;

            foreach (var token in new[] { "@" + name, ":" + name })
            {
                var rx = new Regex($@"(?<!\w){Regex.Escape(token)}(?!\w)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
                result = rx.Replace(result, m =>
                {
                    if (IsInsideAnyRange(m.Index, literalRanges)) return m.Value;
                    return FormatParameterValue(p);
                });
            }
        }

        return result;
    }

    /// <summary>
    /// Formatea un parámetro a literal SQL. Maneja IEnumerable, binarios y tipos escalar.
    /// </summary>
    private static string FormatParameterValue(DbParameter p)
    {
        var value = p.Value;

        if (value is null || value == DBNull.Value) return "NULL";

        if (value is IEnumerable enumerable && value is not string && value is not byte[])
        {
            var parts = new List<string>();
            foreach (var item in enumerable) parts.Add(FormatScalar(item));
            return "(" + string.Join(", ", parts) + ")";
        }

        return FormatScalar(value, p.DbType);
    }

    /// <summary>Formateo escalar con heurística por DbType/tipo CLR.</summary>
    private static string FormatScalar(object? value, DbType? hinted = null)
    {
        if (value is null || value == DBNull.Value) return "NULL";
        if (value is byte[] bytes) return $"<binary {bytes.Length} bytes>";

        if (hinted.HasValue)
        {
            switch (hinted.Value)
            {
                case DbType.AnsiString:
                case DbType.AnsiStringFixedLength:
                case DbType.String:
                case DbType.StringFixedLength:
                case DbType.Xml:
                    return "'" + EscapeSqlString(Convert.ToString(value) ?? string.Empty) + "'";

                case DbType.Date:
                case DbType.Time:
                case DbType.DateTime:
                case DbType.DateTime2:
                case DbType.DateTimeOffset:
                    return $"'{ToDateTime(value):yyyy-MM-dd HH:mm:ss}'";

                case DbType.Boolean:
                    return Convert.ToBoolean(value) ? "1" : "0";

                case DbType.Byte:
                case DbType.SByte:
                case DbType.Int16:
                case DbType.Int32:
                case DbType.Int64:
                case DbType.UInt16:
                case DbType.UInt32:
                case DbType.UInt64:
                case DbType.Single:
                case DbType.Double:
                case DbType.Decimal:
                case DbType.VarNumeric:
                    return Convert.ToString(value, CultureInfo.InvariantCulture) ?? "0";

                case DbType.Guid:
                    return "'" + Convert.ToString(value) + "'";

                case DbType.Object:
                default:
                    break;
            }
        }

        return value switch
        {
            string s => "'" + EscapeSqlString(s) + "'",
            char ch => "'" + EscapeSqlString(ch.ToString()) + "'",
            DateTime dt => $"'{dt:yyyy-MM-dd HH:mm:ss}'",
            DateTimeOffset dto => $"'{dto:yyyy-MM-dd HH:mm:ss}'",
            bool b => b ? "1" : "0",
            float or double or decimal => Convert.ToString(value, CultureInfo.InvariantCulture) ?? "0",
            sbyte or byte or short or int or long or ushort or uint or ulong => Convert.ToString(value, CultureInfo.InvariantCulture) ?? "0",
            Guid guid => "'" + guid.ToString() + "'",
            IEnumerable e => "(" + string.Join(", ", EnumerateFormatted(e)) + ")",
            _ => "'" + EscapeSqlString(Convert.ToString(value) ?? string.Empty) + "'"
        };
    }

    private static IEnumerable<string> EnumerateFormatted(IEnumerable e)
    {
        foreach (var it in e) yield return FormatScalar(it);
    }

    private static string EscapeSqlString(string s) => s.Replace("'", "''");

    private static DateTime ToDateTime(object value)
    {
        if (value is DateTime d) return d;
        if (value is DateTimeOffset dto) return dto.LocalDateTime;
        if (DateTime.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture), CultureInfo.InvariantCulture,
                              DateTimeStyles.AllowWhiteSpaces, out var parsed))
            return parsed;
        return DateTime.MinValue;
    }

    #endregion

    #region Heurísticas para nombre de tabla/esquema

    private static string ExtraerNombreTablaDesdeSql(string sql)
    {
        try
        {
            var tokens = sql.ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var idx = Array.FindIndex(tokens, t => t is "into" or "from" or "update");
            return idx >= 0 && tokens.Length > idx + 1 ? tokens[idx + 1] : "Desconocida";
        }
        catch { return "Desconocida"; }
    }

    private static string ExtraerEsquemaDesdeSql(string sql)
    {
        var tabla = ExtraerNombreTablaDesdeSql(sql);
        var partes = tabla.Split('.', StringSplitOptions.RemoveEmptyEntries);
        return partes.Length > 1 ? partes[0] : "Desconocida";
    }

    #endregion

    #region Delegación transparente al comando interno

    public override string? CommandText { get => _innerCommand.CommandText; set => _innerCommand.CommandText = value; }
    public override int CommandTimeout { get => _innerCommand.CommandTimeout; set => _innerCommand.CommandTimeout = value; }
    public override CommandType CommandType { get => _innerCommand.CommandType; set => _innerCommand.CommandType = value; }
    public override bool DesignTimeVisible { get => _innerCommand.DesignTimeVisible; set => _innerCommand.DesignTimeVisible = value; }
    public override UpdateRowSource UpdatedRowSource { get => _innerCommand.UpdatedRowSource; set => _innerCommand.UpdatedRowSource = value; }
    protected override DbConnection? DbConnection { get => _innerCommand.Connection; set => _innerCommand.Connection = value; }
    protected override DbTransaction? DbTransaction { get => _innerCommand.Transaction; set => _innerCommand.Transaction = value; }
    protected override DbParameterCollection DbParameterCollection => _innerCommand.Parameters;
    public override void Cancel() => _innerCommand.Cancel();
    protected override DbParameter CreateDbParameter() => _innerCommand.CreateParameter();
    public override void Prepare() => _innerCommand.Prepare();

    #endregion

    /// <summary>El decorador no imprime resumen en Dispose; los bloques son por ejecución.</summary>
    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        _innerCommand.Dispose();
    }
}






