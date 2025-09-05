Así como esta el codigo graba el log, pero no en el orden en que se ejecuto, te dejo el codigo actual:


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
    /// se guarde en el mismo archivo. Organiza por API, controlador, endpoint (desde Path) y fecha.
    /// Agrega el LogCustomPart si existe. Usa hora local.
    /// </summary>
    public string GetCurrentLogFile()
    {
        try
        {
            var context = _httpContextAccessor.HttpContext;
            if (context == null)
                return BuildErrorFilePath(kind: "manual", context: null);

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

            // Nombre del Endpoint
            string endpoint = context.Request.Path.Value?.Trim('/').Split('/').LastOrDefault() ?? "UnknownEndpoint";

            // ✅ Controller desde CAD (si está), si no, “UnknownController”
            var endpointMetadata = context.GetEndpoint();
            string controllerName = endpointMetadata?.Metadata
                .OfType<Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor>()
                .FirstOrDefault()?.ControllerName ?? "UnknownController";

            // 📅 Hora local
            string fecha = DateTime.Now.ToString("yyyy-MM-dd");
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
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
            string fileName = $"{endpoint}_{executionId}{customPart}_{timestamp}.txt";
            string fullPath = Path.Combine(finalDirectory, fileName);

            // ✅ Cachear SIEMPRE el FULL PATH (antes guardabas solo el fileName)
            context.Items["LogFileName"] = fullPath;

            return fullPath;
        }
        catch (Exception ex)
        {
            LogInternalError(ex);
        }
        return BuildErrorFilePath(kind: "manual", context: _httpContextAccessor.HttpContext);
    }

    /// <summary>
    /// Registra errores internos en un archivo dentro de /Errores/&lt;fecha&gt;/ con nombre:
    /// ExecutionId_Endpoint_yyyyMMdd_HHmmss_internal.txt
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

    /// <summary>
    /// Registra información de ejecución de una operación SQL en formato estructurado.
    /// Este método genera una representación textual (a través del formateador)
    /// y la persiste en el archivo de log asociado al ciclo de la petición actual,
    /// garantizando coherencia con el resto de eventos registrados durante la misma solicitud.
    /// </summary>
    /// <param name="model">Datos de la ejecución (duración, SQL, conexiones, filas afectadas, etc.).</param>
    /// <param name="context">
    /// Contexto HTTP actual (opcional). Cuando está presente, permite resolver y reutilizar
    /// el archivo de log asociado al request/endpoint en curso.
    /// </param>
    public void LogDatabaseSuccess(SqlLogModel model, HttpContext? context = null)
    {
        try
        {
            // Construye el bloque en texto plano utilizando el formateador existente,
            // manteniendo el mismo esquema visual y de campos.
            var formatted = LogFormatter.FormatDbExecution(model);

            // Escribe en el MISMO archivo asociado al request/endpoint (si existe contexto),
            // preservando la coherencia del rastro completo sin crear archivos alternos.
            WriteLog(context, formatted);
        }
        catch (Exception loggingEx)
        {
            // El logging nunca debe interrumpir el flujo de la aplicación.
            // registra internamente cualquier fallo de escritura/formateo.
            LogInternalError(loggingEx);
        }
    }

    /// <summary>
    /// Registra información estructurada de una ejecución SQL fallida.
    /// Incluye datos de conexión, sentencia y detalle de la excepción,
    /// y persiste la salida en el archivo asociado al ciclo de la petición actual.
    /// Opcionalmente, puede derivar un rastro de excepción general para análisis transversal.
    /// </summary>
    /// <param name="command">Comando que falló.</param>
    /// <param name="ex">Excepción capturada.</param>
    /// <param name="context">
    /// Contexto HTTP actual (opcional). Cuando está presente, permite resolver y reutilizar
    /// el archivo de log asociado al request/endpoint en curso.
    /// </param>
    public void LogDatabaseError(DbCommand command, Exception ex, HttpContext? context = null)
    {
        try
        {
            // Extrae metadatos disponibles de la conexión para enriquecer el bloque.
            var connectionInfo = LogHelper.ExtractDbConnectionInfo(command.Connection?.ConnectionString);
            var tabla = LogHelper.ExtractTableName(command.CommandText);

            // Mantiene el mismo formato de error estructurado que ya utilizas.
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

            // Escribe el bloque en el archivo activo del request/endpoint.
            WriteLog(context, formatted);

            // Además, conserva el rastro de excepción general si tu estrategia lo requiere
            // (por ejemplo, un canal paralelo de errores globales).
            AddExceptionLog(ex);
        }
        catch (Exception errorAlLoguear)
        {
            // Registro defensivo para fallos durante el propio proceso de logging.
            LogInternalError(errorAlLoguear);
        }
    }

    #region Métodos Privados

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
    /// Obtiene un nombre de endpoint seguro desde el HttpContext. Si no existe contexto, devuelve "NoContext".
    /// </summary>
    private static string GetEndpointSafe(HttpContext? context)
    {
        if (context == null) return "NoContext";

        // Intentar usar CAD (ActionName); si no, caer al último segmento del Path
        var cad = context.GetEndpoint()?.Metadata
            .OfType<Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor>()
            .FirstOrDefault();

        var endpoint = cad?.ActionName
                       ?? (context.Request.Path.Value ?? "/").Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries).LastOrDefault()
                       ?? "UnknownEndpoint";

        return Sanitize(endpoint);
    }

    /// <summary>
    /// Devuelve la carpeta base de errores con la subcarpeta de fecha local: &lt;_logDirectory&gt;/Errores/&lt;yyyy-MM-dd&gt;
    /// </summary>
    private string GetErrorsDirectory(DateTime nowLocal)
    {
        var dir = Path.Combine(_logDirectory, "Errores", nowLocal.ToString("yyyy-MM-dd"));
        Directory.CreateDirectory(dir);
        return dir;
    }

    /// <summary>
    /// Construye un path de archivo de error con ExecutionId, Endpoint y timestamp local.
    /// Sufijo: "internal" para errores internos; "manual" para global manual logs.
    /// </summary>
    private string BuildErrorFilePath(string kind, HttpContext? context)
    {
        var now = DateTime.Now; // hora local
        var dir = GetErrorsDirectory(now);

        // ExecutionId (si hay contexto), si no un Guid nuevo
        var executionId = context?.Items?["ExecutionId"]?.ToString() ?? Guid.NewGuid().ToString();

        var endpoint = GetEndpointSafe(context);
        var timestamp = now.ToString("yyyyMMdd_HHmmss");

        var suffix = string.Equals(kind, "internal", StringComparison.OrdinalIgnoreCase) ? "_internal" : "";
        var fileName = $"{executionId}_{endpoint}_{timestamp}{suffix}.txt";

        return Path.Combine(dir, fileName);
    }

    #endregion


    #region Métodos para AddSingleLog en bloque

    /// <summary>
    /// Inicia un bloque de log. Escribe una cabecera común y permite ir agregando filas
    /// con <see cref="ILogBlock.Add(string)"/>. Al finalizar, llamar <see cref="ILogBlock.End()"/>
    /// o disponer el objeto (using) para escribir el cierre del bloque.
    /// </summary>
    /// <param name="title">Título o nombre del bloque (ej. "Proceso de conciliación").</param>
    /// <param name="context">Contexto HTTP (opcional). Si es null, se usa el contexto actual si existe.</param>
    /// <returns>Instancia del bloque para agregar filas.</returns>
    public ILogBlock StartLogBlock(string title, HttpContext? context = null)
    {
        _ = context ?? _httpContextAccessor.HttpContext;
        var filePath = GetCurrentLogFile(); // asegura que compartimos el mismo archivo de la request

        // Cabecera del bloque
        var header = LogFormatter.BuildBlockHeader(title);
        LogHelper.SafeWriteLog(_logDirectory, filePath, header);

        return new LogBlock(this, filePath, title);
    }

    /// <summary>
    /// Implementación concreta de un bloque de log.
    /// </summary>
    private sealed class LogBlock(LoggingService svc, string filePath, string title) : ILogBlock
    {
        private readonly LoggingService _svc = svc;
        private readonly string _filePath = filePath;
        private readonly string _title = title;
        private int _ended; // 0 no, 1 sí (para idempotencia)

        /// <inheritdoc />
        public void Add(string message, bool includeTimestamp = false)
        {
            // cada "Add" es una fila en el mismo archivo, dentro del bloque
            var line = includeTimestamp
                ? $"[{DateTime.Now:HH:mm:ss}]•{message}"
                : $"• {message}";
            LogHelper.SafeWriteLog(_svc._logDirectory, _filePath, line + Environment.NewLine);
        }

        /// <inheritdoc />
        public void AddObj(string name, object obj)
        {
            var formatted = LogFormatter.FormatObjectLog(name, obj);
            LogHelper.SafeWriteLog(_svc._logDirectory, _filePath, formatted);
        }

        /// <inheritdoc />
        public void AddException(Exception ex)
        {
            var formatted = LogFormatter.FormatExceptionDetails(ex.ToString());
            LogHelper.SafeWriteLog(_svc._logDirectory, _filePath, formatted);
        }

        /// <inheritdoc />
        public void End()
        {
            if (Interlocked.Exchange(ref _ended, 1) == 1) return; // ya finalizado
            var footer = LogFormatter.BuildBlockFooter();
            LogHelper.SafeWriteLog(_svc._logDirectory, _filePath, footer);
        }

        public void Dispose() => End();
    }
}

#endregion


/// <summary>
/// Atributo para indicar qué propiedad del modelo debe usarse como parte del nombre del archivo de log.
/// Debe aplicarse únicamente a propiedades públicas sin parámetros (no indexers).
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public sealed class LogFileNameAttribute : Attribute
{ }



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
/// <remarks>
/// Inicializa una nueva instancia del <see cref="LoggingMiddleware"/>.
/// </remarks>
public class LoggingMiddleware(RequestDelegate next, ILoggingService loggingService)
{
    private readonly RequestDelegate _next = next ?? throw new ArgumentNullException(nameof(next));
    private readonly ILoggingService _loggingService = loggingService ?? throw new ArgumentNullException(nameof(loggingService));

    /// <summary>
    /// Cronómetro utilizado para medir el tiempo de ejecución de la acción.
    /// Se inicializa cuando la acción comienza a ejecutarse.
    /// </summary>
    private Stopwatch _stopwatch = new();

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
    /// Obtiene el valor para LogCustomPart deserializando el body al tipo REAL del parámetro del Action
    /// (si hay JSON) o hidratando el DTO desde Query/Route (para GET/sin body). Guarda el resultado
    /// en <c>HttpContext.Items["LogCustomPart"]</c>.
    /// </summary>
    private static async Task ExtractLogCustomPartFromBody(HttpContext context)
    {
        string? bodyString = null;

        // Si viene JSON, lo leemos (para POST/PUT/PATCH, etc.)
        if (context.Request.ContentType?.Contains("application/json", StringComparison.OrdinalIgnoreCase) == true)
        {
            context.Request.EnableBuffering();
            using var reader = new StreamReader(context.Request.Body, Encoding.UTF8, leaveOpen: true);
            bodyString = await reader.ReadToEndAsync();
            context.Request.Body.Position = 0;
        }

        try
        {
            // 👉 El extractor soporta tanto JSON (tipado) como GET (Query/Route) si bodyString es null o vacío
            var customPart = StrongTypedLogFileNameExtractor.Extract(context, bodyString);
            if (!string.IsNullOrWhiteSpace(customPart))
            {
                context.Items["LogCustomPart"] = customPart;
            }
        }
        catch
        {
            // No interrumpir el pipeline por fallos de extracción
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
        foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(prop => Attribute.IsDefined(prop, typeof(LogFileNameAttribute)))
        // Propiedades actuales
        )
        {
            var value = prop.GetValue(obj)?.ToString();
            if (!string.IsNullOrWhiteSpace(value)) return value;
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








using Logging.Abstractions;
using Logging.Models;
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
/// Decorador para interceptar y registrar información de comandos SQL con alto grado de fidelidad.
/// - Emite un bloque ESTRUCTURADO por CADA ejecución (ExecuteReader/ExecuteNonQuery/ExecuteScalar).
/// - Renderiza la sentencia SQL con los VALORES REALES (comillas simples en literales),
///   sustituyendo parámetros posicionales (?) y nombrados (@p0, :UserId), ignorando ocurrencias dentro de literales.
/// - Tolera ausencia del servicio de logging sin interrumpir la ejecución.
/// </summary>
/// <remarks>
/// Crea el decorador con soporte opcional de servicios de logging y contexto HTTP.
/// </remarks>
public class LoggingDbCommandWrapper(
    DbCommand innerCommand,
    ILoggingService? loggingService = null,
    IHttpContextAccessor? httpContextAccessor = null) : DbCommand
{
    private readonly DbCommand _innerCommand = innerCommand ?? throw new ArgumentNullException(nameof(innerCommand));
    private readonly ILoggingService? _loggingService = loggingService;
    private readonly IHttpContextAccessor? _httpContextAccessor = httpContextAccessor;

    #region Ejecuciones (bloque por ejecución)

    /// <inheritdoc />
    protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior)
    {
        var startedAt = DateTime.Now;
        var sw = Stopwatch.StartNew();

        try
        {
            var reader = _innerCommand.ExecuteReader(behavior);

            sw.Stop();
            // SELECT/lecturas: registramos filas afectadas = 0
            LogOneExecution(
                startedAt: startedAt,
                duration: sw.Elapsed,
                affectedRows: 0,
                sqlRendered: RenderSqlWithParametersSafe(_innerCommand.CommandText, _innerCommand.Parameters)
            );

            return reader;
        }
        catch (Exception ex)
        {
            _loggingService?.LogDatabaseError(_innerCommand, ex, _httpContextAccessor?.HttpContext);
            throw;
        }
    }

    /// <inheritdoc />
    public override int ExecuteNonQuery()
    {
        var startedAt = DateTime.Now;
        var sw = Stopwatch.StartNew();

        var result = _innerCommand.ExecuteNonQuery();

        sw.Stop();
        LogOneExecution(
            startedAt: startedAt,
            duration: sw.Elapsed,
            affectedRows: result,
            sqlRendered: RenderSqlWithParametersSafe(_innerCommand.CommandText, _innerCommand.Parameters)
        );

        return result;
    }

    /// <inheritdoc />
    public override object? ExecuteScalar()
    {
        var startedAt = DateTime.Now;
        var sw = Stopwatch.StartNew();

        var result = _innerCommand.ExecuteScalar();

        sw.Stop();
        // Scalar: no DML → filas afectadas = 0
        LogOneExecution(
            startedAt: startedAt,
            duration: sw.Elapsed,
            affectedRows: 0,
            sqlRendered: RenderSqlWithParametersSafe(_innerCommand.CommandText, _innerCommand.Parameters)
        );

        return result;
    }

    #endregion

    #region Logging por ejecución

    /// <summary>
    /// Emite un bloque “LOG DE EJECUCIÓN SQL” para una ejecución específica.
    /// </summary>
    private void LogOneExecution(DateTime startedAt, TimeSpan duration, int affectedRows, string sqlRendered)
    {
        if (_loggingService is null)
            return;

        try
        {
            var conn = _innerCommand.Connection;

            var model = new SqlLogModel
            {
                Sql = sqlRendered,
                ExecutionCount = 1,                  // bloque por ejecución
                TotalAffectedRows = affectedRows,       // 0 para SELECT/SCALAR
                StartTime = startedAt,
                Duration = duration,
                DatabaseName = conn?.Database ?? "Desconocida",
                Ip = conn?.DataSource ?? "Desconocida",
                Port = 0,
                TableName = ExtraerNombreTablaDesdeSql(_innerCommand.CommandText),
                Schema = ExtraerEsquemaDesdeSql(_innerCommand.CommandText)
            };

            _loggingService.LogDatabaseSuccess(model, _httpContextAccessor?.HttpContext);
        }
        catch (Exception logEx)
        {
            try { _loggingService?.WriteLog(_httpContextAccessor?.HttpContext, $"[LoggingDbCommandWrapper] Error al escribir el log SQL: {logEx.Message}"); } catch {
            //Para evitar que se dentanga la escritura.
            }
        }
    }

    #endregion

    #region Render de SQL con parámetros (seguro con literales, soporta IEnumerable)

    /// <summary>
    /// Devuelve el SQL con parámetros sustituidos por sus valores reales (comillas simples en literales).
    /// Ignora placeholders que estén dentro de comillas simples en el propio SQL.
    /// </summary>
    private static string RenderSqlWithParametersSafe(string? sql, DbParameterCollection parameters)
    {
        if (string.IsNullOrEmpty(sql) || parameters.Count == 0)
            return sql ?? string.Empty;

        // 1) Detectamos rangos de literales '...'
        var literalRanges = ComputeSingleQuoteRanges(sql);

        // 2) Si hay "?" → reemplazo POSICIONAL respetando literales
        if (sql.Contains('?'))
            return ReplacePositionalIgnoringLiterals(sql, parameters, literalRanges);

        // 3) Si no, probamos parámetros NOMBRADOS (@name / :name), también ignorando literales
        return ReplaceNamedIgnoringLiterals(sql, parameters, literalRanges);
    }

    /// <summary>
    /// Identifica rangos [start, end] (inclusive) que están dentro de comillas simples,
    /// respetando el escape estándar SQL de comillas duplicadas ('').
    /// </summary>
    private static List<(int start, int end)> ComputeSingleQuoteRanges(string sql)
    {
        var ranges = new List<(int, int)>();
        bool inString = false;
        int start = -1;

        // Usamos while con índice manual para evitar S127 (no modificar contador de un for dentro del cuerpo)
        int idx = 0;
        while (idx < sql.Length)
        {
            char c = sql[idx];
            if (c == '\'')
            {
                bool isEscaped = (idx + 1 < sql.Length) && sql[idx + 1] == '\'';

                if (!inString)
                {
                    inString = true;
                    start = idx;
                }
                else if (!isEscaped)
                {
                    inString = false;
                    ranges.Add((start, idx));
                }

                idx += isEscaped ? 2 : 1;
                continue;
            }

            idx++;
        }

        return ranges;
    }

    /// <summary>Indica si un índice está contenido dentro de alguno de los rangos de literales.</summary>
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
    /// Reemplaza '?' por valores en orden, ignorando cualquier '?' que caiga dentro de literales '...'.
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
    /// Reemplazo de parámetros nombrados (@name, :name) ignorando coincidencias dentro de literales.
    /// </summary>
    private static string ReplaceNamedIgnoringLiterals(string sql, DbParameterCollection parameters, List<(int start, int end)> literalRanges)
    {
        if (parameters.Count == 0) return sql;

        string result = sql;
        foreach (DbParameter p in parameters)
        {
            var name = p.ParameterName?.Trim();
            if (string.IsNullOrEmpty(name))
                continue;

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
    /// Formatea un parámetro a literal SQL:
    /// - IEnumerable (no string/byte[]) → (v1, v2, v3)  // útil en IN (...)
    /// - String/Guid → 'escapado'
    /// - Date/DateTime/Offset → 'yyyy-MM-dd HH:mm:ss'
    /// - Bool → 1/0
    /// - Numéricos → invariante
    /// - byte[] → &lt;binary N bytes&gt;
    /// - null → NULL
    /// </summary>
    private static string FormatParameterValue(DbParameter p)
    {
        var value = p.Value;

        if (value is null || value == DBNull.Value)
            return "NULL";

        if (value is IEnumerable enumerable && value is not string && value is not byte[])
        {
            var parts = new List<string>();
            foreach (var item in enumerable)
                parts.Add(FormatScalar(item));
            return "(" + string.Join(", ", parts) + ")";
        }

        return FormatScalar(value, p.DbType);
    }

    /// <summary>
    /// Formateo escalar defensivo con heurística por DbType y/o tipo CLR.
    /// </summary>
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

    #region Utilidades para nombre de tabla/esquema (heurísticas)

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

    #region Delegación al comando interno (transparencia)

    /// <inheritdoc />
    public override string? CommandText { get => _innerCommand.CommandText; set => _innerCommand.CommandText = value; }

    /// <inheritdoc />
    public override int CommandTimeout { get => _innerCommand.CommandTimeout; set => _innerCommand.CommandTimeout = value; }

    /// <inheritdoc />
    public override CommandType CommandType { get => _innerCommand.CommandType; set => _innerCommand.CommandType = value; }

    /// <inheritdoc />
    public override bool DesignTimeVisible { get => _innerCommand.DesignTimeVisible; set => _innerCommand.DesignTimeVisible = value; }

    /// <inheritdoc />
    public override UpdateRowSource UpdatedRowSource { get => _innerCommand.UpdatedRowSource; set => _innerCommand.UpdatedRowSource = value; }

    /// <inheritdoc />
    protected override DbConnection? DbConnection { get => _innerCommand.Connection; set => _innerCommand.Connection = value; } // <- nullable

    /// <inheritdoc />
    protected override DbTransaction? DbTransaction { get => _innerCommand.Transaction; set => _innerCommand.Transaction = value; }

    /// <inheritdoc />
    protected override DbParameterCollection DbParameterCollection => _innerCommand.Parameters;

    /// <inheritdoc />
    public override void Cancel() => _innerCommand.Cancel();

    /// <inheritdoc />
    protected override DbParameter CreateDbParameter() => _innerCommand.CreateParameter();

    /// <inheritdoc />
    public override void Prepare() => _innerCommand.Prepare();

    #endregion

    /// <summary>
    /// Importante: el log se emite por ejecución; no imprimimos resumen aquí.
    /// </summary>
    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        _innerCommand.Dispose();
    }
}






using Microsoft.AspNetCore.Http;
using System.Collections.Concurrent;
using System.Text;

namespace Logging.Helpers;

/// <summary>
/// Extensión (partial) de LogHelper que agrega una "ventana dinámica" entre
/// 4) Request Info y 5) Response Info, y ordena eventos por el INICIO real.
/// Incluye compatibilidad: si no hay buffer por-request, se comporta como antes.
/// </summary>
public static partial class LogHelper
{
    // ===================== Infraestructura interna =====================

    /// <summary>
    /// Clave interna para guardar el buffer por-request en HttpContext.Items.
    /// </summary>
    private const string BufferKey = "__ReqLogBuffer";

    /// <summary>
    /// Claves legacy para HTTP: listas en HttpContext.Items usadas por el middleware antiguo.
    /// Mantener ambas para compatibilidad.
    /// </summary>
    private const string HttpItemsKey = "HttpClientLogs";       // List<string>
    private const string HttpItemsTimedKey = "HttpClientLogsTimed";  // List<object> con TsUtc/Content

    // ===================== Modelo de eventos dinámicos =====================

    /// <summary>
    /// Tipos de eventos dinámicos (solo etiqueta, no altera la posición fija).
    /// </summary>
    public enum DynamicLogKind
    {
        /// <summary>Eventos de clientes HTTP salientes.</summary>
        HttpClient = 1,

        /// <summary>Ejecuciones de comandos SQL.</summary>
        Sql = 2,

        /// <summary>Entradas manuales simples (AddSingleLog).</summary>
        ManualSingle = 3,

        /// <summary>Entradas manuales de objeto/estructura (AddObjLog).</summary>
        ManualObject = 4,

        /// <summary>Bloques manuales (StartLogBlock/Add/End).</summary>
        ManualBlock = 5,

        /// <summary>Reservado.</summary>
        Custom = 99
    }

    /// <summary>
    /// Segmento dinámico con sello de INICIO (UTC) y secuencia para ordenar de forma estable.
    /// </summary>
    private sealed class DynamicLogSegment(DynamicLogKind kind, DateTime timestampUtc, int sequence, string content)
    {
        /// <summary>Clasificación del segmento (HTTP/SQL/etc.).</summary>
        public DynamicLogKind Kind { get; } = kind;

        /// <summary>Instante UTC en que comenzó el evento (se usa para ordenar correctamente).</summary>
        public DateTime TimestampUtc { get; } = timestampUtc;

        /// <summary>Secuencia incremental por-request para desempate.</summary>
        public int Sequence { get; } = sequence;

        /// <summary>Contenido final del bloque (ya formateado por el productor).</summary>
        public string Content { get; } = content;

        /// <summary>Fábrica con sello UTC actual (para casos sin “startedUtc” explícito).</summary>
        public static DynamicLogSegment Create(DynamicLogKind k, int seq, string text)
            => new(k, DateTime.UtcNow, seq, text);
    }

    /// <summary>
    /// Buffer por-request que concentra:
    /// - Slots fijos 2..6 (Environment, Controller, Request, Response, Errors).
    /// - Eventos dinámicos siempre entre 4 y 5, ordenados por inicio real.
    /// </summary>
    private sealed class RequestLogBuffer(string filePath)
    {
        /// <summary>Ruta del archivo final de este request.</summary>
        public string FilePath { get; } = filePath;

        // Secuencia incremental local para desempates y estabilidad.
        private int _seq;

        // Cola de segmentos dinámicos producidos durante el request.
        private readonly ConcurrentQueue<DynamicLogSegment> _dynamic = new();

        // Slots FIJOS 2..6 (1: Inicio y 7: Fin siguen fuera, como hoy).
        public string? FixedEnvironment { get; private set; }
        public string? FixedController { get; private set; }
        public string? FixedRequest { get; private set; }
        public string? FixedResponse { get; private set; }
        public List<string> FixedErrors { get; } = [];

        /// <summary>Coloca Environment Info (2).</summary>
        public void SetEnvironmentSlot(string content) => FixedEnvironment = content;

        /// <summary>Coloca Controlador (3).</summary>
        public void SetControllerSlot(string content) => FixedController = content;

        /// <summary>Coloca Request Info (4).</summary>
        public void SetRequestSlot(string content) => FixedRequest = content;

        /// <summary>Coloca Response Info (5).</summary>
        public void SetResponseSlot(string content) => FixedResponse = content;

        /// <summary>Agrega Error (6).</summary>
        public void AddErrorSlot(string content) => FixedErrors.Add(content);

        /// <summary>
        /// Inserta evento dinámico con sello de tiempo actual (UTC).
        /// </summary>
        public void AppendDynamicSlot(DynamicLogKind kind, string content)
        {
            var seq = Interlocked.Increment(ref _seq);
            _dynamic.Enqueue(DynamicLogSegment.Create(kind, seq, content));
        }

        /// <summary>
        /// Inserta evento dinámico con sello de INICIO explícito (UTC) — ideal para HTTP/SQL.
        /// </summary>
        public void AppendDynamicAt(DynamicLogKind kind, string content, DateTime timestampUtc)
        {
            var seq = Interlocked.Increment(ref _seq);
            _dynamic.Enqueue(new DynamicLogSegment(kind, timestampUtc, seq, content));
        }

        /// <summary>
        /// Construye el bloque central 2→6 con la ventana dinámica en medio (entre 4 y 5).
        /// </summary>
        public string BuildCore()
        {
            // 1) Ordenar la porción dinámica por INICIO real y por secuencia.
            List<DynamicLogSegment> dyn = [];
            while (_dynamic.TryDequeue(out var seg)) dyn.Add(seg);

            var dynOrdered = dyn
                .OrderBy(s => s.TimestampUtc)
                .ThenBy(s => s.Sequence)
                .ToList();

            // 2) Ensamblar el bloque central en el orden fijo.
            var sb = new StringBuilder(capacity: 64 * 1024);

            if (FixedEnvironment is not null) sb.Append(FixedEnvironment);
            if (FixedController is not null) sb.Append(FixedController);
            if (FixedRequest is not null) sb.Append(FixedRequest);

            foreach (var d in dynOrdered) sb.Append(d.Content);

            if (FixedResponse is not null) sb.Append(FixedResponse);

            if (FixedErrors.Count > 0)
                foreach (var e in FixedErrors) sb.Append(e);

            return sb.ToString();
        }
    }

    // ===================== Helpers internos del buffer =====================

    /// <summary>
    /// Devuelve el buffer si ya existe en Items. No crea uno nuevo.
    /// </summary>
    private static RequestLogBuffer? TryGetExistingBuffer(HttpContext? ctx)
    {
        if (ctx is not null &&
            ctx.Items.TryGetValue(BufferKey, out var existing) &&
            existing is RequestLogBuffer ok)
            return ok;

        return null;
    }

    /// <summary>
    /// Crea u obtiene el buffer por-request desde Items. Si no hay HttpContext, devuelve null.
    /// </summary>
    private static RequestLogBuffer? GetOrCreateBuffer(HttpContext? ctx, string? filePath)
    {
        if (ctx is null) return null;

        var path = string.IsNullOrWhiteSpace(filePath) ? GetPathFromContext(ctx) : filePath;

        if (ctx.Items.TryGetValue(BufferKey, out var existing) && existing is RequestLogBuffer ok)
            return ok;

        var created = new RequestLogBuffer(path!);
        ctx.Items[BufferKey] = created;
        return created;
    }

    // ===================== API pública de slots fijos =====================

    /// <summary>Coloca Environment Info (2) en el buffer.</summary>
    public static void SetEnvironment(HttpContext ctx, string? filePath, string text)
        => GetOrCreateBuffer(ctx, filePath)?.SetEnvironmentSlot(text);

    /// <summary>Coloca Controlador (3) en el buffer.</summary>
    public static void SetController(HttpContext ctx, string? filePath, string text)
        => GetOrCreateBuffer(ctx, filePath)?.SetControllerSlot(text);

    /// <summary>Coloca Request Info (4) en el buffer.</summary>
    public static void SetRequest(HttpContext ctx, string? filePath, string text)
        => GetOrCreateBuffer(ctx, filePath)?.SetRequestSlot(text);

    /// <summary>Coloca Response Info (5) en el buffer.</summary>
    public static void SetResponse(HttpContext ctx, string? filePath, string text)
        => GetOrCreateBuffer(ctx, filePath)?.SetResponseSlot(text);

    /// <summary>Agrega Error (6) en el buffer.</summary>
    public static void AddError(HttpContext ctx, string? filePath, string text)
        => GetOrCreateBuffer(ctx, filePath)?.AddErrorSlot(text);

    /// <summary>
    /// Agrega un evento dinámico con sello de tiempo ACTUAL. Queda entre 4 y 5.
    /// </summary>
    public static void AppendDynamic(HttpContext? ctx, string? filePath, DynamicLogKind kind, string text)
        => GetOrCreateBuffer(ctx, filePath)?.AppendDynamicSlot(kind, text);

    // ===================== API pública de compatibilidad =====================

    /// <summary>
    /// ¿El buffer por-request está activo en este HttpContext?
    /// </summary>
    public static bool HasRequestBuffer(HttpContext? ctx)
        => ctx is not null && ctx.Items.ContainsKey(BufferKey);

    /// <summary>
    /// Agrega un evento dinámico con timestamp explícito (UTC) con compatibilidad:
    /// - Si hay buffer, encola (orden por INICIO).
    /// - Si no hay buffer:
    ///   - HTTP → Items legacy (con lista "timed" y lista simple).
    ///   - Otros (ej. SQL) → escritura directa a archivo (comportamiento clásico).
    /// </summary>
    public static void AppendDynamicCompatAt(HttpContext? ctx, string? filePath, DynamicLogKind kind, string text, DateTime startedUtc)
    {
        var buf = TryGetExistingBuffer(ctx);
        if (buf is not null)
        {
            buf.AppendDynamicAt(kind, text, startedUtc);
            return;
        }

        // Sin buffer → compatibilidad total
        if (kind == DynamicLogKind.HttpClient && ctx is not null)
        {
            // 1) Lista “timed” para que Flush pueda importar y ordenar si más tarde hay buffer.
            if (!ctx.Items.ContainsKey(HttpItemsTimedKey)) ctx.Items[HttpItemsTimedKey] = new List<object>();
            if (ctx.Items[HttpItemsTimedKey] is List<object> timed) timed.Add(new LegacyHttpEntry(startedUtc, text));

            // 2) Lista antigua (string) para middleware legacy que aún lee esta clave.
            if (!ctx.Items.ContainsKey(HttpItemsKey)) ctx.Items[HttpItemsKey] = new List<string>();
            if (ctx.Items[HttpItemsKey] is List<string> raw) raw.Add(text);

            return;
        }

        // Para SQL u otros: escritura directa (no dependemos del middleware).
        var path = string.IsNullOrWhiteSpace(filePath) ? GetPathFromContext(ctx) : filePath!;
        var dir = Path.GetDirectoryName(path) ?? AppDomain.CurrentDomain.BaseDirectory;
        WriteLogToFile(dir, path, text);
    }

    /// <summary>
    /// Agrega un evento dinámico con compatibilidad usando el sello de tiempo actual.
    /// </summary>
    public static void AppendDynamicCompat(HttpContext? ctx, string? filePath, DynamicLogKind kind, string text)
        => AppendDynamicCompatAt(ctx, filePath, kind, text, DateTime.UtcNow);

    // ===================== Flush central con import legacy =====================

    /// <summary>
    /// FLUSH único: importa HTTP legacy desde Items (si existe), compone 2→6 y lo escribe.
    /// Mantén Inicio(1) y Fin(7) como ya los manejas.
    /// </summary>
    public static void Flush(HttpContext ctx, string? filePath)
    {
        if (ctx is null) return;

        var buf = GetOrCreateBuffer(ctx, filePath);
        if (buf is null) return;

        // Importar HTTP legacy antes de ordenar.
        ImportLegacyHttpItems(ctx, buf);

        var core = buf.BuildCore();
        var path = buf.FilePath;
        var dir = Path.GetDirectoryName(path) ?? AppDomain.CurrentDomain.BaseDirectory;

        WriteLogToFile(dir, path, core);
        // Si quieres CSV automático, descomenta:
        // SaveLogAsCsv(dir, path, core)
    }

    /// <summary>
    /// Entrada temporal para Items “timed”. No se expone fuera.
    /// </summary>
    private sealed class LegacyHttpEntry(DateTime tsUtc, string content)
    {
        public DateTime TsUtc { get; } = tsUtc;
        public string Content { get; } = content;
    }

    /// <summary>
    /// Importa entradas de Items "timed" y antiguas (string) para que también se ordenen.
    /// </summary>
    private static void ImportLegacyHttpItems(HttpContext ctx, RequestLogBuffer buf)
    {
        // 1) Lista “timed”: TsUtc + Content (preferida)
        if (ctx.Items.TryGetValue(HttpItemsTimedKey, out var obj) && obj is List<object> timed && timed.Count > 0)
        {
            foreach (var o in timed)
            {
                // Late-binding para aceptar cualquier tipo con TsUtc/Content (p.ej. del handler).
                var ts = (DateTime?)o.GetType().GetProperty("TsUtc")?.GetValue(o);
                var tx = (string?)o.GetType().GetProperty("Content")?.GetValue(o);
                if (ts is null || tx is null) continue;

                buf.AppendDynamicAt(DynamicLogKind.HttpClient, tx, ts.Value);
            }

            ctx.Items.Remove(HttpItemsTimedKey); // evitar duplicados
        }

        // 2) Lista antigua de strings (sin timestamp): se importan con el tiempo de importación
        //    — último recurso por compatibilidad.
        if (ctx.Items.TryGetValue(HttpItemsKey, out var raw) && raw is List<string> oldList && oldList.Count > 0)
        {
            foreach (var s in oldList)
                buf.AppendDynamicSlot(DynamicLogKind.HttpClient, s);

            ctx.Items.Remove(HttpItemsKey); // evitar duplicados
        }
    }
}







using Logging.Helpers;
using Microsoft.AspNetCore.Http;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using static Logging.Helpers.LogHelper;

namespace Logging.Handlers;

/// <summary>
/// DelegatingHandler que registra HTTP saliente con compatibilidad total:
/// - Si hay buffer por-request: encola entre 4 y 5, ordenado por INICIO.
/// - Si no hay buffer: acumula en Items (legacy) para que el middleware lo escriba,
///   y además guarda variante "timed" para permitir orden cuando se use Flush.
/// </summary>
public sealed class HttpClientLoggingHandler(IHttpContextAccessor httpContextAccessor) : DelegatingHandler
{
    private readonly IHttpContextAccessor _accessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));

    private const int MaxBodyChars = 20000; // Límite de seguridad para cuerpos grandes

    /// <summary>
    /// Intercepta la petición/respuesta, arma el bloque y lo publica por buffer o por Items.
    /// </summary>
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var ctx = _accessor.HttpContext; // Puede ser null fuera del pipeline
        var traceId = ctx?.TraceIdentifier ?? Guid.NewGuid().ToString();

        // Sello del INICIO del request HTTP — fundamental para el orden correcto.
        var startedUtc = DateTime.UtcNow;

        // ===== Preparar datos de la petición =====
        string? requestBody = null;
        if (request.Content is not null)
        {
            try
            {
                var raw = await request.Content.ReadAsStringAsync(cancellationToken);
                requestBody = LogHelper.PrettyPrintAuto(Limit(raw, MaxBodyChars), request.Content.Headers.ContentType?.MediaType);
            }
            catch
            {
                // Cuerpos no re-leibles o streams cerrados no deben romper la llamada real.
                requestBody = "[No disponible para logging]";
            }
        }

        var requestHeaders = RenderHeaders(request.Headers);

        var sw = Stopwatch.StartNew();
        try
        {
            var response = await base.SendAsync(request, cancellationToken);
            sw.Stop();

            // ===== Preparar datos de la respuesta =====
            string responseBody;
            try
            {
                var raw = response.Content is null
                    ? "[Sin contenido]"
                    : await response.Content.ReadAsStringAsync(cancellationToken);

                responseBody = LogHelper.PrettyPrintAuto(Limit(raw, MaxBodyChars), response.Content?.Headers?.ContentType?.MediaType);
            }
            catch
            {
                responseBody = "[No disponible para logging]";
            }

            var responseHeaders = RenderHeaders(response.Headers);

            // ===== Construir bloque final (request + response + métrica) =====
            var block = new StringBuilder(capacity: 2048)
                .AppendLine("============== INICIO HTTP CLIENT ==============")
                .Append("TraceId        : ").AppendLine(traceId)
                .Append("Fecha/Hora     : ").AppendLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"))
                .Append("Método         : ").AppendLine(request.Method.Method)
                .Append("URL            : ").AppendLine(request.RequestUri?.ToString() ?? "URI no definida")
                .AppendLine("---- Request Headers ----")
                .AppendLine(requestHeaders)
                .AppendLine("---- Request Body ----")
                .AppendLine(string.IsNullOrWhiteSpace(requestBody) ? "[Sin cuerpo]" : requestBody)
                .AppendLine("---- Response ----")
                .Append("Status Code    : ").Append((int)response.StatusCode).Append(' ').AppendLine(response.StatusCode.ToString())
                .AppendLine("---- Response Headers ----")
                .AppendLine(responseHeaders)
                .AppendLine("---- Response Body ----")
                .AppendLine(responseBody)
                .Append("Duración (ms)  : ").AppendLine(sw.ElapsedMilliseconds.ToString())
                .AppendLine("=============== FIN HTTP CLIENT ================")
                .AppendLine()
                .ToString();

            // Publicación con compatibilidad (buffer o Items).
            LogHelper.AppendDynamicCompatAt(ctx, filePath: null, DynamicLogKind.HttpClient, block, startedUtc);

            return response;
        }
        catch (Exception ex)
        {
            sw.Stop();

            // Bloque de error con contexto de la petición.
            var errorBlock = new StringBuilder(capacity: 1024)
                .AppendLine("============== INICIO HTTP CLIENT (ERROR) ==============")
                .Append("TraceId        : ").AppendLine(traceId)
                .Append("Fecha/Hora     : ").AppendLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"))
                .Append("Método         : ").AppendLine(request.Method.Method)
                .Append("URL            : ").AppendLine(request.RequestUri?.ToString() ?? "URI no definida")
                .AppendLine("---- Request Headers ----")
                .AppendLine(requestHeaders)
                .AppendLine("---- Request Body ----")
                .AppendLine(string.IsNullOrWhiteSpace(requestBody) ? "[Sin cuerpo]" : requestBody)
                .Append("Duración (ms)  : ").AppendLine(sw.ElapsedMilliseconds.ToString())
                .AppendLine("Excepción:")
                .AppendLine(ex.ToString())
                .AppendLine("=============== FIN HTTP CLIENT (ERROR) ================")
                .AppendLine()
                .ToString();

            LogHelper.AppendDynamicCompatAt(ctx, filePath: null, DynamicLogKind.HttpClient, errorBlock, startedUtc);
            throw;
        }
    }

    /// <summary>
    /// Redacta cabeceras sensibles y produce una representación legible para el log.
    /// </summary>
    private static string RenderHeaders(HttpHeaders headers)
    {
        var sb = new StringBuilder(capacity: 512);
        foreach (var h in headers)
        {
            var key = h.Key;
            var value = string.Join(",", h.Value);

            // Redacción de secretos: evita exponer tokens.
            if (key.Equals("Authorization", StringComparison.OrdinalIgnoreCase) ||
                key.Equals("Proxy-Authorization", StringComparison.OrdinalIgnoreCase))
                value = "[REDACTED]";

            sb.Append(key).Append(": ").AppendLine(value);
        }
        return sb.ToString();
    }

    /// <summary>Limita tamaño de texto para evitar logs desmesurados.</summary>
    private static string Limit(string? text, int maxChars)
    {
        if (string.IsNullOrEmpty(text)) return string.Empty;
        if (text.Length <= maxChars) return text;
        return text[..maxChars] + "… [truncado]";
    }
}




Creo que el problema esta en el LoggingService,  en el LoggingMiddleware y en el LoggingDbCommandWrapper, porque creo que no he aplicado algun cambio que me indicaste, revisalo y me comentas que es, recuerda que tal como esta guarda el log, pero no en el orden en que se ejecuto.
