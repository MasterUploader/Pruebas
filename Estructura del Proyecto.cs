using System.Data.Common;
using System.Text;
using Logging.Abstractions;
using Logging.Configuration;
using Logging.Extensions;   // Extensiones como .Indent(...) y LogScope
using Logging.Helpers;      // LogHelper (archivo/csv/utilidades)
using Logging.Models;       // Modelos de log (SQL, etc.)
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Logging.Services;

/// <summary>
/// Servicio de logging que captura y almacena eventos en archivos de log.
/// - Calcula y cachea la ruta de archivo por-request.
/// - Escribe bloques fijos y entradas dinámicas sin bloquear el hilo principal.
/// - Mantiene utilidades para logs de objeto, texto y excepciones.
/// - Expone helpers para logging de SQL (éxito y error).
/// - Permite bloques manuales (StartLogBlock).
/// </summary>
public class LoggingService(
    IHttpContextAccessor httpContextAccessor,
    IHostEnvironment hostEnvironment,
    IOptions<LoggingOptions> loggingOptions) : ILoggingService
{
    // ===================== Dependencias y configuración (constructor primario) =====================

    /// <summary>Accessor del contexto HTTP para derivar el archivo de log por-request.</summary>
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;

    /// <summary>Opciones de logging (rutas base y switches de .txt/.csv).</summary>
    private readonly LoggingOptions _loggingOptions = loggingOptions.Value;

    /// <summary>Directorio base de logs para la API actual: BaseLogDirectory/ApplicationName.</summary>
    private readonly string _logDirectory =
        Path.Combine(loggingOptions.Value.BaseLogDirectory,
                     !string.IsNullOrWhiteSpace(hostEnvironment.ApplicationName) ? hostEnvironment.ApplicationName : "Desconocido");

    // ===================== API pública =====================

    /// <summary>
    /// Obtiene el archivo de log de la petición actual, garantizando que toda la información
    /// se guarde en el mismo archivo. Organiza por API, controlador, endpoint (desde Path) y fecha.
    /// Respeta <c>Items["LogCustomPart"]</c> si está presente. Usa hora local.
    /// </summary>
    public string GetCurrentLogFile()
    {
        try
        {
            var context = _httpContextAccessor.HttpContext;
            if (context is null)
                return BuildErrorFilePath(kind: "manual", context: null); // Fallback sin contexto

            // Si hay un path cacheado y apareció/cambió el sufijo custom, invalidamos el cache.
            if (context.Items.TryGetValue("LogFileName", out var existingObj) &&
                existingObj is string existingPath &&
                context.Items.TryGetValue("LogCustomPart", out var partObj) &&
                partObj is string part && !string.IsNullOrWhiteSpace(part) &&
                !existingPath.Contains(part, StringComparison.OrdinalIgnoreCase))
            {
                context.Items.Remove("LogFileName");
            }

            // Reutilizar si ya está cacheado (guardamos SIEMPRE el path completo).
            if (context.Items.TryGetValue("LogFileName", out var cached) &&
                cached is string cachedPath && !string.IsNullOrWhiteSpace(cachedPath))
            {
                return cachedPath;
            }

            // Nombre del endpoint (último segmento del Path) y Controller (si existe metadata MVC).
            var endpoint = context.Request.Path.Value?.Trim('/').Split('/').LastOrDefault() ?? "UnknownEndpoint";
            var cad = context.GetEndpoint()
                             ?.Metadata
                             .OfType<Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor>()
                             .FirstOrDefault();
            var controllerName = cad?.ControllerName ?? "UnknownController";

            // Identificadores y fecha/hora local para componer nombre de archivo.
            var fecha = DateTime.Now.ToString("yyyy-MM-dd");
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var executionId = context.Items["ExecutionId"]?.ToString() ?? Guid.NewGuid().ToString();

            // Sufijo custom opcional (inyectado por tu middleware/extractor).
            var customPart = context.Items.TryGetValue("LogCustomPart", out var partValue) &&
                             partValue is string partStr && !string.IsNullOrWhiteSpace(partStr)
                             ? $"_{partStr}"
                             : "";

            // Carpeta final: <base>/<controller>/<endpoint>/<fecha>
            var finalDirectory = Path.Combine(_logDirectory, controllerName, endpoint, fecha);
            Directory.CreateDirectory(finalDirectory); // Garantiza existencia (crea toda la jerarquía)

            // Nombre final y path completo
            var fileName = $"{endpoint}_{executionId}{customPart}_{timestamp}.txt";
            var fullPath = Path.Combine(finalDirectory, fileName);

            // Cachear el path para el resto del ciclo de vida del request.
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
    /// Escribe un log en el archivo correspondiente de la petición actual (.txt)
    /// y en su respectivo archivo .csv. Si el contenido excede cierto tamaño,
    /// se delega a <c>Task.Run</c> para no bloquear el hilo de la API.
    /// </summary>
    /// <param name="context">Contexto HTTP actual (opcional, para reglas de cabecera/pie).</param>
    /// <param name="logContent">Contenido del log a registrar.</param>
    public void WriteLog(HttpContext? context, string logContent)
    {
        try
        {
            var filePath = GetCurrentLogFile();
            var isNewFile = !File.Exists(filePath);

            StringBuilder logBuilder = new();

            // Cabecera automática solo en el primer write de ese archivo.
            if (isNewFile) logBuilder.AppendLine(LogFormatter.FormatBeginLog());

            // Contenido del log aportado por el llamador.
            logBuilder.AppendLine(logContent);

            // Pie automático si la respuesta ya inició (headers enviados).
            if (context is not null && context.Response.HasStarted)
                logBuilder.AppendLine(LogFormatter.FormatEndLog());

            var fullText = logBuilder.ToString();

            // Si el log supera ~128KB, escribir en background para no bloquear.
            var isLargeLog = fullText.Length > (128 * 1024);
            if (isLargeLog)
            {
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
            // El logging nunca debe interrumpir el flujo del request.
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
            var formatted = LogFormatter.FormatSingleLog(message).Indent(LogScope.CurrentLevel);
            LogHelper.SafeWriteLog(_logDirectory, GetCurrentLogFile(), formatted);
        }
        catch (Exception ex) { LogInternalError(ex); }
    }

    /// <summary>
    /// Registra un objeto en los logs con un nombre descriptivo.
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
    /// Registra un objeto en los logs sin necesidad de un nombre específico.
    /// Se utiliza el nombre del tipo del objeto si está disponible.
    /// </summary>
    public void AddObjLog(object logObject)
    {
        try
        {
            var objectName = logObject?.GetType()?.Name ?? "ObjetoDesconocido";
            var safeObject = logObject ?? new { }; // evita null en el serializador
            var formatted = LogFormatter.FormatObjectLog(objectName, safeObject).Indent(LogScope.CurrentLevel);

            LogHelper.SafeWriteLog(_logDirectory, GetCurrentLogFile(), formatted);
        }
        catch (Exception ex) { LogInternalError(ex); }
    }

    /// <summary>
    /// Registra excepciones en los logs (canal transversal para diagnósticos).
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
    /// Registra un log estructurado de éxito para una operación SQL usando un modelo preformateado.
    /// Mantiene compatibilidad: si hay <see cref="HttpContext"/>, usa la ruta derivada del contexto;
    /// si no lo hay, escribe directo en el archivo general.
    /// </summary>
    public void LogDatabaseSuccess(SqlLogModel model, HttpContext? context = null)
    {
        // Mantén el formateo que ya tienes (texto estructurado)
        var formatted = LogFormatter.FormatDbExecution(model);

        // Compatibilidad: si llega contexto, se deriva el path con GetPathFromContext; si no, se usa el actual.
        LogHelper.SaveStructuredLog(formatted, context);
    }

    /// <summary>
    /// Método para registrar comandos SQL que fallan (incluye metadatos de conexión y la sentencia).
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

            WriteLog(context, formatted); // escribe usando el archivo “actual”
            AddExceptionLog(ex);          // rastro transversal para correlación
        }
        catch (Exception errorAlLoguear)
        {
            LogInternalError(errorAlLoguear);
        }
    }

    // ===================== Bloques manuales =====================

    /// <summary>
    /// Inicia un bloque de log. Escribe una cabecera común y permite ir agregando filas.
    /// Al finalizar (Dispose), se escribe el pie del bloque.
    /// </summary>
    public ILogBlock StartLogBlock(string title, HttpContext? context = null)
    {
        // Asegura que compartimos el mismo archivo de la request actual.
        context ??= _httpContextAccessor.HttpContext;
        var filePath = GetCurrentLogFile();

        // Cabecera del bloque (formato consistente con el resto del log).
        var header = LogFormatter.BuildBlockHeader(title);
        LogHelper.SafeWriteLog(_logDirectory, filePath, header);

        return new LogBlock(this, filePath);
    }

    /// <summary>
    /// Implementación concreta de un bloque de log (cabecera + líneas + pie).
    /// Cada llamada a Add agrega contenido al mismo archivo que abrió el bloque;
    /// Dispose escribe el pie para cerrarlo.
    /// </summary>
    private sealed class LogBlock(LoggingService svc, string filePath) : ILogBlock
    {
        private readonly LoggingService _svc = svc;
        private readonly string _filePath = filePath;
        private int _ended; // 0: abierto, 1: cerrado (idempotencia)

        /// <summary>Agrega una línea de texto al bloque.</summary>
        public void Add(string line)
        {
            var formatted = line.Indent(LogScope.CurrentLevel) + Environment.NewLine;
            LogHelper.SafeWriteLog(_svc._logDirectory, _filePath, formatted);
        }

        /// <summary>Finaliza el bloque escribiendo el pie (idempotente).</summary>
        public void Dispose()
        {
            if (Interlocked.Exchange(ref _ended, 1) == 1) return; // evitar doble cierre
            var footer = LogFormatter.BuildBlockFooter();
            LogHelper.SafeWriteLog(_svc._logDirectory, _filePath, footer);
        }
    }

    // ===================== Utilidades privadas =====================

    /// <summary>
    /// Devuelve un nombre seguro para usar en rutas/archivos (quita caracteres inválidos).
    /// </summary>
    private static string Sanitize(string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return "Unknown";
        var invalid = Path.GetInvalidFileNameChars().Concat(Path.GetInvalidPathChars()).Distinct().ToArray();
        var cleaned = new string([.. name.Where(c => !invalid.Contains(c))]).Trim();
        return string.IsNullOrWhiteSpace(cleaned) ? "Unknown" : cleaned;
    }

    /// <summary>
    /// Obtiene un nombre de endpoint seguro desde el <see cref="HttpContext"/>.
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
    /// Carpeta de errores por fecha local: &lt;base&gt;/Errores/&lt;yyyy-MM-dd&gt;.
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
    /// Registra errores internos del propio servicio en la carpeta de errores.
    /// Nunca interrumpe la solicitud en curso.
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
            // Evita bucles de error del propio logger
        }
    }
}
