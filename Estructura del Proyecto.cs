Toma a consideración como tengo las clases actualmente:
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

namespace Logging.Services
{
    /// <summary>
    /// Servicio de logging que captura y almacena eventos en archivos de log.
    /// </summary>
    public class LoggingService : ILoggingService
    {
        private readonly string _logDirectory;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly LoggingOptions _loggingOptions;

        /// <summary>
        /// Constructor que inicializa el servicio de logging con la configuración de rutas.
        /// </summary>
        public LoggingService(IHttpContextAccessor httpContextAccessor, IHostEnvironment hostEnvironment, IOptions<LoggingOptions> loggingOptions)
        {
            _httpContextAccessor = httpContextAccessor;
            _loggingOptions = loggingOptions.Value;
            string baseLogDir = loggingOptions.Value.BaseLogDirectory;
            string apiName = !string.IsNullOrWhiteSpace(hostEnvironment.ApplicationName) ? hostEnvironment.ApplicationName : "Desconocido";
            _logDirectory = Path.Combine(baseLogDir, apiName);

            try
            {
                if (!Directory.Exists(_logDirectory))
                    Directory.CreateDirectory(_logDirectory);
            }
            catch (Exception ex)
            {
                LogInternalError(ex);
            }
        }

        /// <summary>
        /// Obtiene el archivo de log de la petición actual, garantizando que toda la información
        /// se guarde en el mismo archivo. Se organiza por API, controlador, endpoint y fecha.
        /// </summary>
        public string GetCurrentLogFile()
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

        /// <summary>
        /// Obtiene el archivo de log de la petición actual, garantizando que toda la información
        /// se guarde en el mismo archivo. Se organiza por API, controlador, endpoint y fecha.
        /// </summary>
        public string GetCurrentLogFile2()
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
                    string endpoint = context.Request.Path.Value?.Trim('/').Split('/').LastOrDefault() ?? "UnknownEndpoint";

                    // Intenta sobrescribir con metadatos (opcional)
                    var endpointMetadata = context.GetEndpoint();
                    var controllerName = endpointMetadata?.Metadata
                        .OfType<Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor>()
                        .FirstOrDefault()?.ControllerName ?? "UnknownController";

                    // Fecha, timestamp y ejecución
                    string fecha = DateTime.UtcNow.ToString("yyyy-MM-dd");
                    string timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
                    string executionId = context.Items["ExecutionId"]?.ToString() ?? Guid.NewGuid().ToString();

                    //Obtener parte extra
                    string? customPart = null;
                    if (context.Items.TryGetValue("LogCustomPart", out var partObj) && partObj is string partValue)
                    {
                        customPart = partValue;
                    }

                    // _logDirectory YA contiene el nombre de la API
                    string finalDirectory = Path.Combine(_logDirectory, controllerName, endpoint, fecha);
                    Directory.CreateDirectory(finalDirectory);

                    

                    string fileName = $"{executionId}_{endpoint}{(string.IsNullOrWhiteSpace(customPart) ? "" : "_" + customPart)}_{timestamp}.txt";
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

        /// <summary>
        /// Registra errores internos en un archivo separado sin afectar la API.
        /// </summary>
        public void LogInternalError(Exception ex)
        {
            try
            {
                string errorLogPath = Path.Combine(_logDirectory, "InternalErrorLog.txt");
                string errorMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Error en LoggingService: {ex}{Environment.NewLine}";
                File.AppendAllText(errorLogPath, errorMessage);
            }
            catch
            {
                // Evita bucles de error
            }
        }

        /// <summary>
        /// Escribe un log en el archivo correspondiente de la petición actual (.txt)
        /// y en su respectivo archivo .csv. Si el contenido excede cierto tamaño,
        /// se ejecuta en un hilo aparte para no afectar el flujo de la API.
        /// </summary>
        /// <param name="context">Contexto HTTP actual (opcional, para asociar el archivo de log).</param>
        /// <param name="logContent">Contenido del log a registrar.</param>
        public void WriteLog(HttpContext? context, string logContent)
        {
            try
            {
                string filePath = GetCurrentLogFile();
                bool isNewFile = !File.Exists(filePath);

                var logBuilder = new StringBuilder();

                // Agregar inicio si es el primer log
                if (isNewFile)
                    logBuilder.AppendLine(LogFormatter.FormatBeginLog());

                // Agregar el contenido del log
                logBuilder.AppendLine(logContent);

                // Agregar cierre si ya inició la respuesta
                if (context != null && context.Response.HasStarted)
                    logBuilder.AppendLine(LogFormatter.FormatEndLog());

                string fullText = logBuilder.ToString();

                // Si el log es mayor a 128 KB, delegar a un hilo (Task.Run) para no bloquear
                bool isLargeLog = fullText.Length > (128 * 1024); // ~128 KB

                if (isLargeLog)
                {
                    Task.Run(() =>
                    {
                        if (_loggingOptions.GenerateTxt)
                        {
                            LogHelper.WriteLogToFile(_logDirectory, filePath, fullText);
                        }
                        if (_loggingOptions.GenerateCsv)
                        {
                            LogHelper.SaveLogAsCsv(_logDirectory, filePath, logContent);
                        }
                    });
                }
                else
                {
                    // Escritura directa en orden (preserva el flujo)
                    if (_loggingOptions.GenerateTxt)
                    {
                        LogHelper.WriteLogToFile(_logDirectory, filePath, fullText);
                    }
                    if (_loggingOptions.GenerateCsv)
                    {
                        LogHelper.SaveLogAsCsv(_logDirectory, filePath, logContent);
                    }
                }
            }
            catch (Exception ex)
            {
                LogInternalError(ex);
            }
        }

        /// <summary>
        /// Agrega un log manual de texto en el archivo de log actual.
        /// </summary>
        public void AddSingleLog(string message)
        {
            try
            {
                string formatted = LogFormatter.FormatSingleLog(message).Indent(LogScope.CurrentLevel);
                LogHelper.SafeWriteLog(_logDirectory, GetCurrentLogFile(), formatted);
            }
            catch (Exception ex)
            {
                LogInternalError(ex);
            }
        }

        /// <summary>
        /// Registra un objeto en los logs con un nombre descriptivo.
        /// </summary>
        /// <param name="objectName">Nombre descriptivo del objeto.</param>
        /// <param name="logObject">Objeto a registrar.</param>
        public void AddObjLog(string objectName, object logObject)
        {
            try
            {
                string formatted = LogFormatter.FormatObjectLog(objectName, logObject).Indent(LogScope.CurrentLevel);
                LogHelper.SafeWriteLog(_logDirectory, GetCurrentLogFile(), formatted);
            }
            catch (Exception ex)
            {
                LogInternalError(ex);
            }
        }

        /// <summary>
        /// Registra un objeto en los logs sin necesidad de un nombre específico.
        /// Se intentará capturar automáticamente el tipo de objeto.
        /// </summary>
        /// <param name="logObject">Objeto a registrar.</param>
        public void AddObjLog(object logObject)
        {
            try
            {
                // Obtener el nombre del tipo del objeto
                string objectName = logObject?.GetType()?.Name ?? "ObjetoDesconocido";
                object safeObject = logObject ?? new { };

                // Convertir objeto a JSON o XML según el formato
                string formatted = LogFormatter.FormatObjectLog(objectName, safeObject).Indent(LogScope.CurrentLevel);

                // Guardar el log en archivo
                LogHelper.SafeWriteLog(_logDirectory, GetCurrentLogFile(), formatted);
            }
            catch (Exception ex)
            {
                LogInternalError(ex);
            }
        }

        /// <summary>
        /// Registra excepciones en los logs.
        /// </summary>
        public void AddExceptionLog(Exception ex)
        {
            try
            {
                string formatted = LogFormatter.FormatExceptionDetails(ex.ToString()).Indent(LogScope.CurrentLevel);
                LogHelper.SafeWriteLog(_logDirectory, GetCurrentLogFile(), formatted);
            }
            catch (Exception e)
            {
                LogInternalError(e);
            }
        }

        /// <inheritdoc />
        public void LogDatabaseSuccess(SqlLogModel model, HttpContext? context = null)
        {
            // Usa el formateador que ya tienes para texto plano, si lo deseas
            var formatted = LogFormatter.FormatDbExecution(model);

            LogHelper.SaveStructuredLog(formatted, context);
        }

        /// <summary>
        /// Método para registrar comandos SQL fallidos
        /// </summary>
        /// <param name="command"></param>
        /// <param name="ex"></param>
        /// <param name="context"></param>
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

                WriteLog(context, formatted);
                AddExceptionLog(ex); // También lo guardás como log general si usás esa ruta
            }
            catch (Exception errorAlLoguear)
            {
                LogInternalError(errorAlLoguear);
            }
        }
    }
}

using Logging.Abstractions;
using Logging.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Diagnostics;
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
    /// Constructor del Middleware que recibe el servicio de logs inyectado.
    /// </summary>
    public LoggingMiddleware(RequestDelegate next, ILoggingService loggingService)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _loggingService = loggingService ?? throw new ArgumentNullException(nameof(loggingService));
    }

    /// <summary>
    /// Método principal del Middleware que intercepta las solicitudes HTTP.
    /// </summary>
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            //Evitar logs innecesarios
            var path = context.Request.Path.Value;
            if (!string.IsNullOrEmpty(path) &&
                (path.Contains("swagger", StringComparison.OrdinalIgnoreCase) ||
                 path.Contains("favicon.ico", StringComparison.OrdinalIgnoreCase)))
            {
                await _next(context); // No loguear estas rutas
                return;
            }
           
            _stopwatch = Stopwatch.StartNew(); // Iniciar medición de tiempo            

            // 1️⃣ Asegurar que exista un ExecutionId único para la solicitud
            if (!context.Items.ContainsKey("ExecutionId"))
            {
                context.Items["ExecutionId"] = Guid.NewGuid().ToString();
            }

            // ✅ 1) PRE-EXTRACCIÓN (antes de cualquier WriteLog)
            string? preBody = null;
            try
            {
                if (!(string.Equals(context.Request.Method, "GET", StringComparison.OrdinalIgnoreCase) &&
                      (context.Request.ContentLength ?? 0) == 0))
                {
                    context.Request.EnableBuffering();
                    using var r = new StreamReader(context.Request.Body, Encoding.UTF8, leaveOpen: true);
                    preBody = await r.ReadToEndAsync();
                    context.Request.Body.Position = 0;
                }

                var preCustom = LogFileNameExtractor.ExtractLogFileNameFromContext(context, preBody);
                Console.WriteLine($"[LOGGING] (PRE) LogCustomPart: '{preCustom ?? "(null)"}'");
                if (!string.IsNullOrWhiteSpace(preCustom))
                    context.Items["LogCustomPart"] = preCustom;
            }
            catch (Exception ex1)
            {
                Console.WriteLine($"[LOGGING] (PRE) Error extrayendo LogCustomPart: {ex1.Message}");
            }

            // 2️⃣ Capturar información del entorno y escribirlo en el log
            string envLog = await CaptureEnvironmentInfoAsync(context);
            _loggingService.WriteLog(context, envLog);

            // 3️⃣ Capturar y escribir en el log la información de la solicitud HTTP
            string requestLog = await CaptureRequestInfoAsync(context);
            _loggingService.WriteLog(context, requestLog);

            // 4️⃣ Reemplazar el Stream original de respuesta para capturarla
            var originalBodyStream = context.Response.Body;
            using (var responseBody = new MemoryStream())
            {
                context.Response.Body = responseBody;

                // 5️⃣ Continuar con la ejecución del pipeline
                await _next(context);

                // 5.5 Capturar logs del HttpClient si existen
                if (context.Items.TryGetValue("HttpClientLogs", out var clientLogsObj) && clientLogsObj is List<string> clientLogs)
                {
                    foreach (var log in clientLogs)
                    {
                        _loggingService.WriteLog(context, log);
                    }
                }

                // 6️⃣ Capturar la respuesta y agregarla al log
                string responseLog = await CaptureResponseInfoAsync(context);
                _loggingService.WriteLog(context, responseLog);

                // 7️⃣ Restaurar el stream original para que el API pueda responder correctamente
                responseBody.Seek(0, SeekOrigin.Begin);
                await responseBody.CopyToAsync(originalBodyStream);
            }

            // 8️⃣ Verificar si hubo alguna excepción en la ejecución y loguearla
            if (context.Items.ContainsKey("Exception") && context.Items["Exception"] is Exception ex)
            {
                _loggingService.AddExceptionLog(ex);
            }
        }
        catch (Exception ex)
        {
            // 9️⃣ Manejo de excepciones para evitar que el middleware interrumpa la API
            _loggingService.AddExceptionLog(ex);
        }
        finally
        {
            // 🔟 Detener el cronómetro y registrar el tiempo total de ejecución
            _stopwatch.Stop();
            _loggingService.WriteLog(context, $"[Tiempo Total de Ejecución]: {_stopwatch.ElapsedMilliseconds} ms");
        }
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

using Logging.Attributes;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Controllers;
using System.Reflection;
using System.Text.Json;

namespace Logging.Helpers;

/// <summary>
/// Clase auxiliar para detectar automáticamente propiedades marcadas con el atributo
/// [LogFileName] dentro del body JSON, sin conocer el tipo DTO en tiempo de compilación.
/// </summary>
public static class LogFileNameExtractor
{
    /// <summary>
    /// Extrae valores de propiedades marcadas con [LogFileName] desde el body JSON, query string o route values.
    /// Soporta propiedades anidadas.
    /// </summary>
    public static string? ExtractLogFileNameFromContext(HttpContext context, string? requestBody = null)
    {
        var parts = new List<string>();

        try
        {
            var endpoint = context.GetEndpoint();
            var actionDescriptor = endpoint?.Metadata.GetMetadata<ControllerActionDescriptor>();
            if (actionDescriptor == null)
            {
                Console.WriteLine("[LOGGING] ❌ No se pudo obtener ControllerActionDescriptor.");
                return null;
            }

            Console.WriteLine($"[LOGGING] Controlador: {actionDescriptor.ControllerName}, Acción: {actionDescriptor.ActionName}");

            foreach (var param in actionDescriptor.Parameters)
            {
                Console.WriteLine($"[LOGGING] Parámetro: {param.Name}, Tipo: {param.ParameterType.Name}");
                var paramType = param.ParameterType;

                if (paramType == null)
                    continue;

                // 1️⃣ Propiedades raíz
                foreach (var prop in paramType.GetProperties())
                {
                    var attr = prop.GetCustomAttribute<LogFileNameAttribute>();
                    if (attr != null)
                    {
                        Console.WriteLine($"[LOGGING] ✅ Atributo encontrado en propiedad raíz: {prop.Name}");
                        var value = TryGetValue(prop.Name, context, requestBody);
                        Console.WriteLine($"[LOGGING] Valor para {prop.Name}: '{value ?? "(null)"}'");
                        AddPart(parts, attr, value);
                    }

                    // 2️⃣ Propiedades anidadas
                    foreach (var subProp in prop.PropertyType.GetProperties())
                    {
                        var nestedAttr = subProp.GetCustomAttribute<LogFileNameAttribute>();
                        if (nestedAttr != null)
                        {
                            Console.WriteLine($"[LOGGING] ✅ Atributo encontrado en propiedad anidada: {prop.Name}.{subProp.Name}");
                            var value = TryGetValue(subProp.Name, context, requestBody);
                            Console.WriteLine($"[LOGGING] Valor para {prop.Name}.{subProp.Name}: '{value ?? "(null)"}'");
                            AddPart(parts, nestedAttr, value);
                        }
                    }
                }
            }

            if (parts.Count > 0)
            {
                string result = string.Join("_", parts);
                Console.WriteLine($"[LOGGING] ✅ Resultado final del LogCustomPart: {result}");
                return result;
            }

            Console.WriteLine("[LOGGING] ❌ No se encontró ninguna propiedad con [LogFileName]");
            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[LOGGING] ❌ Error en LogFileNameExtractor: {ex}");
            return null;
        }
    }

    private static void AddPart(List<string> parts, LogFileNameAttribute attr, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            parts.Add(!string.IsNullOrWhiteSpace(attr.Label)
                ? $"{attr.Label}-{value}"
                : value);
        }
    }

    /// <summary>
    /// Busca un valor por nombre de propiedad en body JSON (case-insensitive), query string y route values.
    /// </summary>
    private static string? TryGetValue(string key, HttpContext context, string? body)
    {
        // 1️⃣ Body JSON
        var valueFromBody = TryGetValueFromBody(body, context, key);
        if (!string.IsNullOrWhiteSpace(valueFromBody))
            return valueFromBody;

        // 2️⃣ Query string
        if (context.Request.Query.TryGetValue(key, out var queryValue) && !string.IsNullOrWhiteSpace(queryValue))
            return queryValue.ToString();

        // 3️⃣ Route values
        if (context.Request.RouteValues.TryGetValue(key, out var routeValue) && routeValue != null)
            return routeValue.ToString();

        return null;
    }

    /// <summary>
    /// Busca un valor en el JSON del body de forma case-insensitive.
    /// Soporta propiedades raíz y un nivel anidado.
    /// </summary>
    private static string? TryGetValueFromBody(string? body, HttpContext context, string key)
    {
        if (string.IsNullOrWhiteSpace(body)) return null;
        if (!(context.Request.ContentType?.Contains("application/json", StringComparison.OrdinalIgnoreCase) ?? false))
            return null;

        try
        {
            var json = JsonSerializer.Deserialize<JsonElement>(body);
            if (json.ValueKind != JsonValueKind.Object) return null;

            return TryGetValueCaseInsensitive(json, key);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[LOGGING] ⚠️ Error al leer JSON del body: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Busca en un JsonElement por nombre de propiedad sin importar mayúsculas/minúsculas.
    /// Soporta nivel raíz y un nivel anidado.
    /// </summary>
    private static string? TryGetValueCaseInsensitive(JsonElement obj, string key)
    {
        // Nivel raíz
        foreach (var p in obj.EnumerateObject())
            if (string.Equals(p.Name, key, StringComparison.OrdinalIgnoreCase))
                return p.Value.ToString();

        // Un nivel anidado
        foreach (var p in obj.EnumerateObject())
        {
            if (p.Value.ValueKind == JsonValueKind.Object)
            {
                foreach (var p2 in p.Value.EnumerateObject())
                    if (string.Equals(p2.Name, key, StringComparison.OrdinalIgnoreCase))
                        return p2.Value.ToString();
            }
        }

        return null;
    }
}
