using System.Data.Common;
using System.Text;
using Logging.Abstractions;
using Logging.Configuration;
using Logging.Extensions;   // Extensiones como .Indent(...) y LogScope
using Logging.Helpers;      // LogHelper (archivo/csv/utilidades)
using Logging.Models;       // SqlLogModel (StartTime, Duration, etc.)
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Logging.Services;

/// <summary>
/// Servicio central de logging: calcula rutas de archivo, escribe bloques fijos/dinámicos
/// y mantiene compatibilidad con la agregación cronológica (por INICIO) vía Items.
/// </summary>
/// <remarks>
/// - Constructor primario: dependencias inyectadas sin boilerplate.
/// - new() y []: inicialización moderna de colecciones/objetos.
/// - Comentarios XML y comentarios inline describen la **funcionalidad** del servicio.
/// </remarks>
public class LoggingService(
    IHttpContextAccessor httpContextAccessor,
    IHostEnvironment hostEnvironment,
    IOptions<LoggingOptions> loggingOptions) : ILoggingService
{
    // ========= Dependencias y configuración (constructor primario) =========

    /// <summary>Accessor del contexto HTTP para resolver archivo de log por-request.</summary>
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;

    /// <summary>Opciones de logging (rutas, banderas de .txt/.csv, etc.).</summary>
    private readonly LoggingOptions _loggingOptions = loggingOptions.Value;

    /// <summary>
    /// Carpeta base por API: <c>BaseLogDirectory/ApplicationName</c>. Se usa para
    /// componer subcarpetas por controlador/endpoint/fecha y guardar los archivos.
    /// </summary>
    private readonly string _logDirectory =
        Path.Combine(loggingOptions.Value.BaseLogDirectory,
                     string.IsNullOrWhiteSpace(hostEnvironment.ApplicationName) ? "Desconocido" : hostEnvironment.ApplicationName);

    /// <summary>
    /// Marca de inicialización del directorio base. Al instanciar el servicio se
    /// ejecuta <see cref="EnsureLogDirectory"/> para garantizar que la carpeta exista.
    /// (Patrón de inicialización perezosa, sin segundo constructor).
    /// </summary>
    private readonly bool _logDirReady = EnsureLogDirectory();

    // =================== Constantes y claves de interoperabilidad ===================

    /// <summary>
    /// Clave compartida con el wrapper SQL para propagar el INICIO (UTC) de una ejecución,
    /// de modo que el orden final sea cronológico por el inicio real.
    /// </summary>
    private const string SqlStartedKey = "__SqlStartedUtc";

    /// <summary>
    /// Clave de Items para la lista de bloques dinámicos “timed” (HTTP/SQL/etc.),
    /// consumida por el middleware para ordenar por <c>TsUtc</c>.
    /// </summary>
    private const string TimedItemsKey = "HttpClientLogsTimed";

    // ================================ API pública ================================

    /// <summary>
    /// Devuelve la ruta absoluta del archivo de log de la request actual.
    /// Usa un patrón estable: <c>{controller}/{endpoint}/{fecha}/{endpoint}_{executionId}[_custom]_{timestamp}.txt</c>.
    /// La ruta se cachea en <c>Items["LogFileName"]</c> para evitar recomputar.
    /// </summary>
    public string GetCurrentLogFile()
    {
        try
        {
            var context = _httpContextAccessor.HttpContext;
            if (context is null) return BuildErrorFilePath("manual", null); // Fallback sin contexto

            // Si hay un path cacheado y el “custom part” cambió, invalidar el cache.
            if (context.Items.TryGetValue("LogFileName", out var cachedObj) &&
                cachedObj is string cachedPath &&
                context.Items.TryGetValue("LogCustomPart", out var partObj) &&
                partObj is string part &&
                !string.IsNullOrWhiteSpace(part) &&
                !cachedPath.Contains(part, StringComparison.OrdinalIgnoreCase))
            {
                context.Items.Remove("LogFileName");
            }

            // Reusar si ya está cacheado (caso típico durante la request).
            if (context.Items.TryGetValue("LogFileName", out var existingObj) &&
                existingObj is string existing && !string.IsNullOrWhiteSpace(existing))
            {
                return existing;
            }

            // Nombre de endpoint (último segmento de la ruta).
            var endpoint = (context.Request.Path.Value ?? "/").Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries).LastOrDefault() ?? "UnknownEndpoint";

            // Intento de obtener Controller desde metadata del endpoint (MVC).
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

            // Carpeta final: <base>/<controller>/<endpoint>/<fecha>
            var finalDirectory = Path.Combine(_logDirectory, Sanitize(controllerName), Sanitize(endpoint), fecha);
            Directory.CreateDirectory(finalDirectory); // Garantiza existencia

            var fileName = $"{Sanitize(endpoint)}_{executionId}{customPart}_{timestamp}.txt";
            var fullPath = Path.Combine(finalDirectory, fileName);

            // Cachear para el resto del ciclo de vida del request.
            context.Items["LogFileName"] = fullPath;
            return fullPath;
        }
        catch (Exception ex)
        {
            LogInternalError(ex);
            return BuildErrorFilePath("manual", _httpContextAccessor.HttpContext);
        }
    }

    /// <summary>
    /// Escribe contenido en el archivo “actual” del request. En la primera escritura
    /// del archivo añade cabecera automática, y si la respuesta ya inició, añade el pie.
    /// Para entradas grandes (&gt;128 KB) cambia a escritura en background para no bloquear.
    /// </summary>
    public void WriteLog(HttpContext? context, string logContent)
    {
        try
        {
            var filePath = GetCurrentLogFile();
            var isNewFile = !File.Exists(filePath);

            var sb = new StringBuilder();

            // Cabecera automática solo la primera vez que se escribe en el archivo.
            if (isNewFile) sb.AppendLine(LogFormatter.FormatBeginLog());

            // Contenido aportado por el consumidor.
            sb.AppendLine(logContent);

            // Si la respuesta ya comenzó (headers enviados), cerramos el bloque de log.
            if (context is not null && context.Response.HasStarted)
                sb.AppendLine(LogFormatter.FormatEndLog());

            var fullText = sb.ToString();
            var isLarge = fullText.Length > (128 * 1024); // Umbral simple en caracteres

            if (isLarge)
            {
                // Escritura asincrónica: no bloquea el request (resiliencia).
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
        catch (Exception ex)
        {
            // Nunca interferir con la request por un fallo de logging.
            LogInternalError(ex);
        }
    }

    /// <summary>
    /// Agrega una línea simple (texto) al log actual.
    /// </summary>
    public void AddSingleLog(string message)
    {
        try
        {
            var formatted = LogFormatter.FormatSingleLog(message).Indent(LogScope.CurrentLevel);
            LogHelper.SafeWriteLog(_logDirectory, GetCurrentLogFile(), formatted);
        }
        catch (Exception ex) { LogInternalError(ex); }
    }

    /// <summary>
    /// Agrega un objeto con nombre al log actual (se serializa legible).
    /// </summary>
    public void AddObjLog(string objectName, object logObject)
    {
        try
        {
            var formatted = LogFormatter.FormatObjectLog(objectName, logObject).Indent(LogScope.CurrentLevel);
            LogHelper.SafeWriteLog(_logDirectory, GetCurrentLogFile(), formatted);
        }
        catch (Exception ex) { LogInternalError(ex); }
    }

    /// <summary>
    /// Agrega un objeto al log actual usando su tipo como nombre.
    /// </summary>
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

    /// <summary>
    /// Agrega detalles de excepción al log actual (canal transversal).
    /// </summary>
    public void AddExceptionLog(Exception ex)
    {
        try
        {
            var formatted = LogFormatter.FormatExceptionDetails(ex.ToString()).Indent(LogScope.CurrentLevel);
            LogHelper.SafeWriteLog(_logDirectory, GetCurrentLogFile(), formatted);
        }
        catch (Exception e) { LogInternalError(e); }
    }

    /// <summary>
    /// Registra un bloque de SQL exitoso:
    /// — Si hay <see cref="HttpContext"/>: encola el bloque “timed” (orden por INICIO).
    /// — Si no hay contexto: escribe directo al archivo actual (compatibilidad).
    /// </summary>
    public void LogDatabaseSuccess(SqlLogModel model, HttpContext? context = null)
    {
        try
        {
            var formatted = LogFormatter.FormatDbExecution(model); // Mantiene tu formato visual

            if (context is not null)
            {
                // Convertimos el StartTime del modelo a UTC para ordenar globalmente.
                var startedUtc = model.StartTime.Kind == DateTimeKind.Utc
                    ? model.StartTime
                    : model.StartTime.ToUniversalTime();

                EnqueueTimed(context, startedUtc, formatted);
            }
            else
            {
                // Fallback sin contexto: no se pierde el log.
                WriteLog(context, formatted);
            }
        }
        catch (Exception loggingEx)
        {
            LogInternalError(loggingEx);
        }
    }

    /// <summary>
    /// Registra un bloque de SQL con error:
    /// — Usa el INICIO propagado por el wrapper (<c>Items["__SqlStartedUtc"]</c>) si existe,
    ///   o bien ahora (UTC) como mejor esfuerzo.
    /// — Mantiene un rastro general de la excepción.
    /// </summary>
    public void LogDatabaseError(DbCommand command, Exception ex, HttpContext? context = null)
    {
        try
        {
            // Extraemos datos de conexión para enriquecer el bloque.
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
                horaError: DateTime.Now
            );

            if (context is not null)
            {
                // Tomar el INICIO real si fue propagado por el wrapper; si no, ahora.
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

            // Rastro transversal (stack) para facilitar diagnóstico global.
            AddExceptionLog(ex);
        }
        catch (Exception fail)
        {
            LogInternalError(fail);
        }
    }

    // ============================== Helpers privados ==============================

    /// <summary>
    /// Garantiza la existencia del directorio base de logs. Registra en archivo de
    /// errores internos si algo falla (no interrumpe la aplicación).
    /// </summary>
    private bool EnsureLogDirectory()
    {
        try
        {
            if (!Directory.Exists(_logDirectory))
                Directory.CreateDirectory(_logDirectory); // Crea jerarquía completa si falta

            return true; // Indicador informativo
        }
        catch (Exception ex)
        {
            LogInternalError(ex);
            return false;
        }
    }

    /// <summary>
    /// Encola un bloque “timed” (con sello de INICIO UTC) en Items para que el middleware
    /// lo escriba **entre Request(4) y Response(5)** y en **orden cronológico**.
    /// </summary>
    private static void EnqueueTimed(HttpContext context, DateTime startedUtc, string content)
    {
        if (!context.Items.ContainsKey(TimedItemsKey)) context.Items[TimedItemsKey] = new List<object>();
        if (context.Items[TimedItemsKey] is List<object> list)
            list.Add(new TimedEntry(startedUtc, content));
    }

    /// <summary>
    /// Normaliza un nombre (controller/endpoint/archivo) eliminando caracteres inválidos.
    /// </summary>
    private static string Sanitize(string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return "Unknown";
        var invalid = Path.GetInvalidFileNameChars().Concat(Path.GetInvalidPathChars()).Distinct().ToArray();
        var cleaned = new string([.. name.Where(c => !invalid.Contains(c))]).Trim();
        return string.IsNullOrWhiteSpace(cleaned) ? "Unknown" : cleaned;
    }

    /// <summary>
    /// Crea (si no existe) la carpeta de errores del día y devuelve su ruta.
    /// </summary>
    private string GetErrorsDirectory(DateTime nowLocal)
    {
        var dir = Path.Combine(_logDirectory, "Errores", nowLocal.ToString("yyyy-MM-dd"));
        Directory.CreateDirectory(dir);
        return dir;
    }

    /// <summary>
    /// Construye una ruta segura para logs de error internos o manuales (fallback).
    /// </summary>
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

    /// <summary>
    /// Escribe un registro en el archivo de errores internos de la librería (no afecta la API).
    /// </summary>
    public void LogInternalError(Exception ex)
    {
        try
        {
            var context = _httpContextAccessor.HttpContext;
            var errorPath = BuildErrorFilePath("internal", context);
            var msg = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Error en LoggingService: {ex}{Environment.NewLine}";
            File.AppendAllText(errorPath, msg);
        }
        catch
        {
            // Silencio para evitar loops en el propio logger.
        }
    }

    // ===================== Tipos internos auxiliares (Items “timed”) =====================

    /// <summary>
    /// Estructura privada almacenada en Items para ordenar por INICIO (UTC) en el middleware.
    /// No se expone fuera del servicio para mantener bajo acoplamiento.
    /// </summary>
    private sealed class TimedEntry(DateTime tsUtc, string content)
    {
        /// <summary>Instante (UTC) en que INICIÓ el evento dinámico (HTTP/SQL/etc.).</summary>
        public DateTime TsUtc { get; } = tsUtc;

        /// <summary>Contenido ya formateado listo para escritura.</summary>
        public string Content { get; } = content;
    }
}
