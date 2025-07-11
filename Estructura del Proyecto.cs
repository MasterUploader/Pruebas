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
        /// se guarde en el mismo archivo. Si no existe aún, se genera uno nuevo dentro de una carpeta
        /// con el nombre del controlador.
        /// </summary>
        public string GetCurrentLogFile()
        {
            try
            {
                var context = _httpContextAccessor.HttpContext;

                // Si ya existe un archivo de log en esta petición, reutilizarlo
                if (context is not null &&
                    context.Items.ContainsKey("LogFileName") &&
                    context.Items["LogFileName"] is string logFileName)
                {
                    return logFileName;
                }

                // Generar un nuevo nombre de archivo solo si no se ha creado antes
                if (context is not null && context.Items.ContainsKey("ExecutionId"))
                {
                    string executionId = context.Items["ExecutionId"]?.ToString() ?? Guid.NewGuid().ToString();
                    string endpoint = context.Request.Path.ToString().Replace("/", "_").Trim('_');
                    string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");

                    // Obtener el nombre del controlador a partir del endpoint actual
                    var endpointMetadata = context.GetEndpoint();
                    var controllerName = endpointMetadata?.Metadata
                        .OfType<Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor>()
                        .FirstOrDefault()?.ControllerName ?? "UnknownController";

                    // Crear subcarpeta del controlador si no existe
                    var controllerDirectory = Path.Combine(_logDirectory, controllerName);
                    Directory.CreateDirectory(controllerDirectory);

                    // Generar nombre de archivo incluyendo subcarpeta del controlador
                    string newLogFileName = Path.Combine(controllerDirectory, $"{executionId}_{endpoint}_{timestamp}.txt");

                    context.Items["LogFileName"] = newLogFileName;
                    return newLogFileName;
                }
            }
            catch (Exception ex)
            {
                LogInternalError(ex); // Método interno para registrar errores del sistema de logging
            }

            return Path.Combine(_logDirectory, "GlobalManualLogs.txt");
        }

        /// <summary>
        /// Obtiene el archivo de log de la petición actual, garantizando que toda la información se guarde en el mismo archivo.
        /// </summary>
        public string GetCurrentLogFile2()
        {
            try
            {
                var context = _httpContextAccessor.HttpContext;

                // Si ya existe un archivo de log en esta petición, reutilizarlo
                if (context is not null && context.Items.ContainsKey("LogFileName") && context.Items["LogFileName"] is string logFileName)
                {
                    return logFileName;
                }

                // Generar un nuevo nombre de archivo solo si no se ha creado antes
                if (context is not null && context.Items.ContainsKey("ExecutionId"))
                {
                    string executionId = context.Items["ExecutionId"]?.ToString() ?? Guid.NewGuid().ToString();
                    string endpoint = context.Request?.Path.ToString().Replace("/", "_").Trim('/') ?? "UnknownEndpoint";
                    string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                    string newLogFileName = Path.Combine(_logDirectory, $"{executionId}_{endpoint}_{timestamp}.txt");
                    context.Items["LogFileName"] = newLogFileName;

                    return newLogFileName;
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
