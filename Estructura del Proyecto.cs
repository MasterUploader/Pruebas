Este es el codigo original:

﻿using Logging.Abstractions;
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

    #region Métodos Privados

    /// <summary>
    /// Devuelve un nombre seguro para usar en rutas/archivos.
    /// </summary>
    private static string Sanitize(string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return "Unknown";
        var invalid = Path.GetInvalidFileNameChars().Concat(Path.GetInvalidPathChars()).Distinct().ToArray();
        var cleaned = new string(name.Where(c => !invalid.Contains(c)).ToArray()).Trim();
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
        context ??= _httpContextAccessor.HttpContext;
        var filePath = GetCurrentLogFile(); // asegura que compartimos el mismo archivo de la request

        // Cabecera del bloque
        var header = LogFormatter.BuildBlockHeader(title);
        LogHelper.SafeWriteLog(_logDirectory, filePath, header);

        return new LogBlock(this, filePath, title);
    }

    /// <summary>
    /// Implementación concreta de un bloque de log.
    /// </summary>
    private sealed class LogBlock : ILogBlock
    {
        private readonly LoggingService _svc;
        private readonly string _filePath;
        private readonly string _title;
        private int _ended; // 0 no, 1 sí (para idempotencia)

        public LogBlock(LoggingService svc, string filePath, string title)
        {
            _svc = svc;
            _filePath = filePath;
            _title = title;
        }

        /// <inheritdoc />
        public void Add(string message, bool includeTimestamp = false)
        {
            // cada "Add" es una fila en el mismo archivo, dentro del bloque
            var line = includeTimestamp 
                ? $"[{DateTime.Now:HH:mm:ss}]•{message}" 
                :  $"• {message}";
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



namespace Logging.Abstractions;

/// <summary>
/// Contrato para un bloque de log que agrupa múltiples filas con un encabezado y un pie comunes.
/// </summary>
public interface ILogBlock : IDisposable
{
    /// <summary>Agrega una fila de texto al bloque.</summary>
    void Add(string message, bool includeTimestamp = false);

    /// <summary>Agrega una fila logueando un objeto formateado.</summary>
    void AddObj(string name, object obj);

    /// <summary>Agrega una fila con detalle de excepción.</summary>
    void AddException(Exception ex);

    /// <summary>Finaliza el bloque (escribe el pie). Idempotente.</summary>
    void End();
}



using Logging.Models;
using Microsoft.AspNetCore.Http;
using System.Data.Common;

namespace Logging.Abstractions
{
    /// <summary>
    /// Define la interfaz para el servicio de logging.
    /// </summary>
    public interface ILoggingService
    {
        /// <summary>
        /// MEtodo que guarda logs
        /// </summary>
        /// <param name="context"></param>
        /// <param name="logContent"></param>
        void WriteLog(HttpContext? context, string logContent);
        /// <summary>
        /// Agrega un log de texto simple.
        /// </summary>
        void AddSingleLog(string message);

        /// <summary>
        /// Registra un objeto en los logs con un nombre descriptivo.
        /// </summary>
        /// <param name="objectName">Nombre descriptivo del objeto.</param>
        /// <param name="logObject">Objeto a registrar.</param>
        void AddObjLog(string objectName, object logObject);

        /// <summary>
        /// Registra un objeto en los logs sin necesidad de un nombre específico.
        /// Se intentará capturar automáticamente el tipo de objeto.
        /// </summary>
        /// <param name="logObject">Objeto a Registrar</param>
        void AddObjLog(object logObject);

        /// <summary>
        /// Registra una excepción en los logs.
        /// </summary>
        void AddExceptionLog(Exception ex);

        /// <summary>
        /// Registra un log estructurado de éxito para una operación SQL usando un modelo preformateado.
        /// </summary>
        /// <param name="model">Modelo con los datos del comando SQL ejecutado.</param>
        /// <param name="context">Contexto HTTP para trazabilidad opcional.</param>
        void LogDatabaseSuccess(SqlLogModel model, HttpContext? context = null);

        /// <summary>
        /// Registra un log de error para un comando SQL que lanzó una excepción.
        /// </summary>
        /// <param name="command">El comando ejecutado que causó el error.</param>
        /// <param name="ex">La excepción lanzada.</param>
        /// <param name="context">Contexto HTTP actual para extraer información adicional (opcional).</param>
        void LogDatabaseError(DbCommand command, Exception ex, HttpContext? context = null);

        /// <summary>
        /// Inicia un bloque de log. Escribe una cabecera común y permite ir agregando filas
        /// con <see cref="ILogBlock.Add(string)"/>. Al finalizar, llamar <see cref="ILogBlock.End()"/>
        /// o disponer el objeto (using) para escribir el cierre del bloque.
        /// </summary>
        /// <param name="title">Título o nombre del bloque (ej. "Proceso de conciliación").</param>
        /// <param name="context">Contexto HTTP (opcional). Si es null, se usa el contexto actual si existe.</param>
        /// <returns>Instancia del bloque para agregar filas.</returns>
        public ILogBlock StartLogBlock(string title, HttpContext? context = null);
    }
}


Considera todas las funciones y metodos que tenia originalmente para aplicar las mejoras, porque el codigo que me entregas elimina varia opciones que ya disponia.
