using System.Data.Common;
using Logging.Models;
using Microsoft.AspNetCore.Http;

namespace Logging.Abstractions;

/// <summary>
/// Define el contrato del servicio de logging: cálculo de rutas, escritura de bloques
/// fijos/dinámicos, registro de SQL y utilidades de bloques manuales.
/// </summary>
public interface ILoggingService
{
    /// <summary>
    /// Devuelve el path absoluto del archivo de log para la request actual
    /// (cacheado en Items["LogFileName"]).
    /// </summary>
    string GetCurrentLogFile();

    /// <summary>
    /// Escribe texto en el archivo de log “actual”. Agrega cabecera/pie según corresponda.
    /// </summary>
    void WriteLog(HttpContext? context, string logContent);

    /// <summary>Agrega una línea simple de texto.</summary>
    void AddSingleLog(string message);

    /// <summary>Agrega un objeto con nombre.</summary>
    void AddObjLog(string objectName, object logObject);

    /// <summary>Agrega un objeto usando su tipo como nombre.</summary>
    void AddObjLog(object logObject);

    /// <summary>Agrega detalles de excepción (stack) al log actual.</summary>
    void AddExceptionLog(Exception ex);

    /// <summary>
    /// Registra ejecución SQL exitosa. Si hay contexto, se encola con timestamp de INICIO
    /// para que el middleware lo inserte ordenado entre Request y Response.
    /// </summary>
    void LogDatabaseSuccess(SqlLogModel model, HttpContext? context = null);

    /// <summary>
    /// Registra ejecución SQL con error. Si el wrapper propagó el INICIO en Items,
    /// se respeta para ordenar cronológicamente.
    /// </summary>
    void LogDatabaseError(DbCommand command, Exception ex, HttpContext? context = null);

    /// <summary>
    /// Crea un bloque manual de log. El contenido se acumula y, al liberar el bloque (Dispose),
    /// se publica como un único segmento dinámico posicionado por el instante de INICIO.
    /// </summary>
    ILogBlock StartLogBlock(string title, HttpContext? context = null);

    /// <summary>
    /// Log de error interno del propio servicio (no interrumpe la app).
    /// </summary>
    void LogInternalError(Exception ex);
}





using System.Data.Common;
using System.Text;
using Logging.Abstractions;
using Logging.Configuration;
using Logging.Extensions;   // .Indent(...) y LogScope
using Logging.Helpers;      // LogHelper (archivo/csv/utilidades)
using Logging.Models;       // SqlLogModel (StartTime, Duration, etc.)
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Logging.Services;

/// <summary>
/// Servicio central de logging: calcula rutas, escribe bloques fijos/dinámicos
/// y mantiene compatibilidad con agregación cronológica (por INICIO) vía Items.
/// </summary>
/// <remarks>
/// - Constructor primario (menos boilerplate).
/// - Uso de new() y [] para inicializaciones.
/// - Comentarios xml e inline describen **funcionalidades**.
/// </remarks>
public class LoggingService(
    IHttpContextAccessor httpContextAccessor,
    IHostEnvironment hostEnvironment,
    IOptions<LoggingOptions> loggingOptions) : ILoggingService
{
    // ========= Dependencias y configuración =========

    /// <summary>Acceso al contexto HTTP (correlación y nombre de archivo por-request).</summary>
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;

    /// <summary>Opciones de logging (rutas, banderas de .txt/.csv, etc.).</summary>
    private readonly LoggingOptions _loggingOptions = loggingOptions.Value;

    /// <summary>Carpeta base por API: BaseLogDirectory/ApplicationName.</summary>
    private readonly string _logDirectory =
        Path.Combine(loggingOptions.Value.BaseLogDirectory,
                     string.IsNullOrWhiteSpace(hostEnvironment.ApplicationName) ? "Desconocido" : hostEnvironment.ApplicationName);

    /// <summary>
    /// Dispara verificación/creación de la carpeta base (inicialización perezosa).
    /// </summary>
    private readonly bool _logDirReady = EnsureLogDirectory();

    // ===== Constantes de interoperabilidad =====

    /// <summary>Clave para propagar el INICIO de una ejecución SQL en Items.</summary>
    private const string SqlStartedKey = "__SqlStartedUtc";

    /// <summary>Lista en Items con bloques dinámicos “timed” (HTTP/SQL/etc.).</summary>
    private const string TimedItemsKey = "HttpClientLogsTimed";

    // ================================ API pública ================================

    /// <inheritdoc />
    public string GetCurrentLogFile()
    {
        try
        {
            var context = _httpContextAccessor.HttpContext;
            if (context is null) return BuildErrorFilePath("manual", null);

            // Invalida cache si cambió el sufijo custom.
            if (context.Items.TryGetValue("LogFileName", out var cachedObj) &&
                cachedObj is string cachedPath &&
                context.Items.TryGetValue("LogCustomPart", out var partObj) &&
                partObj is string part &&
                !string.IsNullOrWhiteSpace(part) &&
                !cachedPath.Contains(part, StringComparison.OrdinalIgnoreCase))
            {
                context.Items.Remove("LogFileName");
            }

            if (context.Items.TryGetValue("LogFileName", out var existingObj) &&
                existingObj is string existing && !string.IsNullOrWhiteSpace(existing))
            {
                return existing;
            }

            // Endpoint y controller (cuando hay metadata MVC).
            var endpoint = (context.Request.Path.Value ?? "/").Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries).LastOrDefault() ?? "UnknownEndpoint";
            var cad = context.GetEndpoint()
                             ?.Metadata
                             .OfType<Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor>()
                             .FirstOrDefault();
            var controllerName = cad?.ControllerName ?? "UnknownController";

            // Identificadores y fecha/hora para nombre de archivo.
            var executionId = context.Items["ExecutionId"]?.ToString() ?? Guid.NewGuid().ToString();
            var customPart = context.Items.TryGetValue("LogCustomPart", out var cpObj) && cpObj is string cp && !string.IsNullOrWhiteSpace(cp)
                             ? $"_{cp}"
                             : "";
            var fecha = DateTime.Now.ToString("yyyy-MM-dd");
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");

            // Carpeta final y nombre.
            var finalDirectory = Path.Combine(_logDirectory, Sanitize(controllerName), Sanitize(endpoint), fecha);
            Directory.CreateDirectory(finalDirectory);

            var fileName = $"{Sanitize(endpoint)}_{executionId}{customPart}_{timestamp}.txt";
            var fullPath = Path.Combine(finalDirectory, fileName);

            context.Items["LogFileName"] = fullPath;
            return fullPath;
        }
        catch (Exception ex)
        {
            LogInternalError(ex);
            return BuildErrorFilePath("manual", _httpContextAccessor.HttpContext);
        }
    }

    /// <inheritdoc />
    public void WriteLog(HttpContext? context, string logContent)
    {
        try
        {
            var filePath = GetCurrentLogFile();
            var isNewFile = !File.Exists(filePath);

            StringBuilder sb = new(); // acumulamos para aplicar cabecera/pie cuando toque

            if (isNewFile) sb.AppendLine(LogFormatter.FormatBeginLog()); // cabecera automática

            sb.AppendLine(logContent); // cuerpo aportado por el llamador

            // Si la respuesta ya inició, cerramos el log automáticamente.
            if (context is not null && context.Response.HasStarted)
                sb.AppendLine(LogFormatter.FormatEndLog());

            var fullText = sb.ToString();
            var isLarge = fullText.Length > (128 * 1024);

            if (isLarge)
            {
                // No bloquear requests con logs gigantes
                Task.Run(() =>
                {
                    if (_loggingOptions.GenerateTxt) LogHelper.WriteLogToFile(_logDirectory, filePath, fullText);
                    if (_loggingOptions.GenerateCsv) LogHelper.SaveLogAsCsv(_logDirectory, filePath, logContent);
                });
            }
            else
            {
                if (_loggingOptions.GenerateTxt) LogHelper.WriteLogToFile(_logDirectory, filePath, fullText);
                if (_loggingOptions.GenerateCsv) LogHelper.SaveLogAsCsv(_logDirectory, filePath, logContent);
            }
        }
        catch (Exception ex) { LogInternalError(ex); }
    }

    /// <inheritdoc />
    public void AddSingleLog(string message)
    {
        try
        {
            var formatted = LogFormatter.FormatSingleLog(message).Indent(LogScope.CurrentLevel);
            LogHelper.SafeWriteLog(_logDirectory, GetCurrentLogFile(), formatted);
        }
        catch (Exception ex) { LogInternalError(ex); }
    }

    /// <inheritdoc />
    public void AddObjLog(string objectName, object logObject)
    {
        try
        {
            var formatted = LogFormatter.FormatObjectLog(objectName, logObject).Indent(LogScope.CurrentLevel);
            LogHelper.SafeWriteLog(_logDirectory, GetCurrentLogFile(), formatted);
        }
        catch (Exception ex) { LogInternalError(ex); }
    }

    /// <inheritdoc />
    public void AddObjLog(object logObject)
    {
        try
        {
            var name = logObject?.GetType()?.Name ?? "ObjetoDesconocido";
            var safe = logObject ?? new { };
            var formatted = LogFormatter.FormatObjectLog(name, safe).Indent(LogScope.CurrentLevel);

            LogHelper.SafeWriteLog(_logDirectory, GetCurrentLogFile(), formatted);
        }
        catch (Exception ex) { LogInternalError(ex); }
    }

    /// <inheritdoc />
    public void AddExceptionLog(Exception ex)
    {
        try
        {
            var formatted = LogFormatter.FormatExceptionDetails(ex.ToString()).Indent(LogScope.CurrentLevel);
            LogHelper.SafeWriteLog(_logDirectory, GetCurrentLogFile(), formatted);
        }
        catch (Exception e) { LogInternalError(e); }
    }

    /// <inheritdoc />
    public void LogDatabaseSuccess(SqlLogModel model, HttpContext? context = null)
    {
        try
        {
            var formatted = LogFormatter.FormatDbExecution(model); // mantiene visual actual

            if (context is not null)
            {
                // Orden por INICIO real (UTC)
                var startedUtc = model.StartTime.Kind == DateTimeKind.Utc
                    ? model.StartTime
                    : model.StartTime.ToUniversalTime();

                EnqueueTimed(context, startedUtc, formatted);
            }
            else
            {
                WriteLog(context, formatted); // compatibilidad sin contexto
            }
        }
        catch (Exception loggingEx) { LogInternalError(loggingEx); }
    }

    /// <inheritdoc />
    public void LogDatabaseError(DbCommand command, Exception ex, HttpContext? context = null)
    {
        try
        {
            var info = LogHelper.ExtractDbConnectionInfo(command.Connection?.ConnectionString);
            var tabla = LogHelper.ExtractTableName(command.CommandText);

            var formatted = LogFormatter.FormatDbExecutionError(
                nombreBD: info.Database,
                ip: info.Ip,
                puerto: info.Port,
                biblioteca: info.Library,
                tabla: tabla,
                sentenciaSQL: command.CommandText,
                exception: ex,
                horaError: DateTime.now
            );

            if (context is not null)
            {
                // Preferimos el INICIO propagado por el wrapper; si no, ahora (UTC).
                var startedUtc =
                    context.Items.TryGetValue(SqlStartedKey, out var obj) && obj is DateTime dt
                        ? dt
                        : DateTime.UtcNow;

                EnqueueTimed(context, startedUtc, formatted);
            }
            else
            {
                WriteLog(context, formatted);
            }

            AddExceptionLog(ex); // rastro transversal
        }
        catch (Exception fail) { LogInternalError(fail); }
    }

    /// <inheritdoc />
    public ILogBlock StartLogBlock(string title, HttpContext? context = null)
    {
        // Crea un bloque manual que se publicará como un único segmento dinámico
        // usando el instante de INICIO como ancla para el orden cronológico.
        return new LogBlock(this, context ?? _httpContextAccessor.HttpContext, title);
    }

    // ============================== Helpers privados ==============================

    /// <summary>Garantiza la existencia del directorio base.</summary>
    private bool EnsureLogDirectory()
    {
        try
        {
            if (!Directory.Exists(_logDirectory))
                Directory.CreateDirectory(_logDirectory);
            return true;
        }
        catch (Exception ex) { LogInternalError(ex); return false; }
    }

    /// <summary>
    /// Encola un bloque “timed” (INICIO UTC) en Items para que el middleware lo inserte
    /// entre Request(4) y Response(5) respetando el orden cronológico.
    /// </summary>
    private static void EnqueueTimed(HttpContext context, DateTime startedUtc, string content)
    {
        if (!context.Items.ContainsKey(TimedItemsKey)) context.Items[TimedItemsKey] = new List<object>();
        if (context.Items[TimedItemsKey] is List<object> list)
            list.Add(new TimedEntry(startedUtc, content));
    }

    /// <summary>Normaliza nombres de controlador/endpoint/archivo.</summary>
    private static string Sanitize(string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return "Unknown";
        var invalid = Path.GetInvalidFileNameChars().Concat(Path.GetInvalidPathChars()).Distinct().ToArray();
        var cleaned = new string([.. name.Where(c => !invalid.Contains(c))]).Trim();
        return string.IsNullOrWhiteSpace(cleaned) ? "Unknown" : cleaned;
    }

    /// <summary>Crea (si no existe) la carpeta de errores del día.</summary>
    private string GetErrorsDirectory(DateTime nowLocal)
    {
        var dir = Path.Combine(_logDirectory, "Errores", nowLocal.ToString("yyyy-MM-dd"));
        Directory.CreateDirectory(dir);
        return dir;
    }

    /// <summary>Construye ruta para logs de error internos o manuales.</summary>
    private string BuildErrorFilePath(string kind, HttpContext? context)
    {
        var now = DateTime.Now;
        var dir = GetErrorsDirectory(now);

        var executionId = context?.Items?["ExecutionId"]?.ToString() ?? Guid.NewGuid().ToString();
        var endpoint = (context?.Request.Path.Value ?? "/").Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries).LastOrDefault() ?? "UnknownEndpoint";
        var timestamp = now.ToString("yyyyMMdd_HHmmss");
        var suffix = string.Equals(kind, "internal", StringComparison.OrdinalIgnoreCase) ? "_internal" : "";

        var fileName = $"{Sanitize(endpoint)}_{executionId}_{timestamp}{suffix}.txt";
        return Path.Combine(dir, fileName);
    }

    /// <inheritdoc />
    public void LogInternalError(Exception ex)
    {
        try
        {
            var context = _httpContextAccessor.HttpContext;
            var errorPath = BuildErrorFilePath("internal", context);
            var msg = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Error en LoggingService: {ex}{Environment.NewLine}";
            File.AppendAllText(errorPath, msg);
        }
        catch { /* silencio para evitar loops */ }
    }

    // ===== Tipos internos auxiliares =====

    /// <summary>
    /// Entrada usada en Items para ordenar por INICIO (UTC) dentro del middleware.
    /// </summary>
    private sealed class TimedEntry(DateTime tsUtc, string content)
    {
        public DateTime TsUtc { get; } = tsUtc;
        public string Content { get; } = content;
    }

    /// <summary>
    /// Implementación de <see cref="ILogBlock"/>: acumula texto y, al liberar,
    /// publica un único segmento dinámico posicionado por el INICIO del bloque.
    /// </summary>
    private sealed class LogBlock(LoggingService owner, HttpContext? context, string title) : ILogBlock
    {
        private readonly LoggingService _owner = owner;
        private readonly HttpContext? _context = context;
        private readonly DateTime _startedUtc = DateTime.UtcNow; // ancla para el orden
        private readonly string _title = title;

        private readonly StringBuilder _sb = new(); // buffer del bloque

        public LogBlock : this(owner, context, title)
        {
            // Cabecera del bloque manual (formato consistente con el resto del log).
            _sb.AppendLine("============== INICIO BLOQUE ==============")
               .Append("Título         : ").AppendLine(_title)
               .Append("Fecha/Hora     : ").AppendLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"))
               .AppendLine("-------------------------------------------");
        }

        /// <inheritdoc />
        public void Add(string line)
        {
            // Agrega líneas respetando sangrías del nivel actual.
            _sb.AppendLine(line.Indent(LogScope.CurrentLevel));
        }

        /// <inheritdoc />
        public void Dispose()
        {
            // Cierre del bloque y publicación como segmento dinámico “timed”.
            _sb.AppendLine("-------------------------------------------")
               .AppendLine("=============== FIN BLOQUE ================")
               .AppendLine();

            var ctx = _context ?? _owner._httpContextAccessor.HttpContext;
            if (ctx is not null)
            {
                EnqueueTimed(ctx, _startedUtc, _sb.ToString());
            }
            else
            {
                // Sin contexto: caída a archivo directo para no perder el bloque.
                _owner.WriteLog(null, _sb.ToString());
            }
        }
    }
}


