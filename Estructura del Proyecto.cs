using System;
using System.IO;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Logging.Extensions;
using Logging.Helpers;

namespace Logging.Services
{
    /// <summary>
    /// Servicio de logging que captura y almacena eventos en archivos de log.
    /// </summary>
    public class LoggingService
    {
        private readonly string _logDirectory;
        private readonly IHttpContextAccessor _httpContextAccessor;

        /// <summary>
        /// Constructor que inicializa el servicio de logging con la configuración de rutas.
        /// </summary>
        public LoggingService(IHttpContextAccessor httpContextAccessor, IHostEnvironment hostEnvironment, IOptions<Logging.Configuration.LoggingOptions> options)
        {
            _httpContextAccessor = httpContextAccessor;
            string baseLogDir = options.Value.BaseLogDirectory;
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
        /// Obtiene el archivo de log asociado a la petición actual.
        /// </summary>
        private string GetCurrentLogFile()
        {
            try
            {
                var context = _httpContextAccessor.HttpContext;
                if (context != null && context.Items.ContainsKey("ExecutionId"))
                {
                    string executionId = context.Items["ExecutionId"].ToString();
                    string endpoint = context.Request.Path.ToString().Replace("/", "_").Trim('_');
                    string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                    return Path.Combine(_logDirectory, $"{executionId}_{endpoint}_{timestamp}.txt");
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
        private void LogInternalError(Exception ex)
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
        /// Escribe un log en el archivo correspondiente.
        /// </summary>
        public void WriteLog(HttpContext context, string logContent)
        {
            try
            {
                LogHelper.WriteLogToFile(_logDirectory, GetCurrentLogFile(), logContent.Indent(LogScope.CurrentLevel));
            }
            catch (Exception ex)
            {
                LogInternalError(ex);
            }
        }

        /// <summary>
        /// Agrega un log manual de texto en el archivo de log actual.
        /// </summary>
        public void AddSingleLog(string logMessage)
        {
            try
            {
                string formatted = LogFormatter.FormatSingleLog(logMessage).Indent(LogScope.CurrentLevel);
                LogHelper.WriteLogToFile(_logDirectory, GetCurrentLogFile(), formatted);
            }
            catch (Exception ex)
            {
                LogInternalError(ex);
            }
        }

        /// <summary>
        /// Registra un objeto en los logs (JSON o XML).
        /// </summary>
        public void AddObjLog(string objectName, object logObject)
        {
            try
            {
                string formatted = LogFormatter.FormatObjectLog(objectName, logObject).Indent(LogScope.CurrentLevel);
                LogHelper.WriteLogToFile(_logDirectory, GetCurrentLogFile(), formatted);
            }
            catch (Exception ex)
            {
                LogInternalError(ex);
            }
        }

        /// <summary>
        /// Registra la información de una respuesta HTTP.
        /// </summary>
        public void AddResponseLog(string logMessage)
        {
            try
            {
                string formatted = LogFormatter.FormatResponseInfo("200", "application/json", logMessage).Indent(LogScope.CurrentLevel);
                LogHelper.WriteLogToFile(_logDirectory, GetCurrentLogFile(), formatted);
            }
            catch (Exception ex)
            {
                LogInternalError(ex);
            }
        }

        /// <summary>
        /// Registra la información del entorno del servidor.
        /// </summary>
        public void AddEnvironmentLog()
        {
            try
            {
                string formatted = LogFormatter.FormatEnvironmentInfoStart().Indent(LogScope.CurrentLevel);
                LogHelper.WriteLogToFile(_logDirectory, GetCurrentLogFile(), formatted);
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
                LogHelper.WriteLogToFile(_logDirectory, GetCurrentLogFile(), formatted);
            }
            catch (Exception e)
            {
                LogInternalError(e);
            }
        }

        /// <summary>
        /// Registra los parámetros de entrada de un método.
        /// </summary>
        public void AddMethodEntryLog(string methodName, string parameters)
        {
            try
            {
                LogScope.IncreaseIndentation();
                string formatted = LogFormatter.FormatMethodEntry(methodName, parameters).Indent(LogScope.CurrentLevel);
                LogHelper.WriteLogToFile(_logDirectory, GetCurrentLogFile(), formatted);
            }
            catch (Exception ex)
            {
                LogInternalError(ex);
            }
        }

        /// <summary>
        /// Registra los valores de salida de un método.
        /// </summary>
        public void AddMethodExitLog(string methodName, string returnValue)
        {
            try
            {
                string formatted = LogFormatter.FormatMethodExit(methodName, returnValue).Indent(LogScope.CurrentLevel);
                LogHelper.WriteLogToFile(_logDirectory, GetCurrentLogFile(), formatted);
                LogScope.DecreaseIndentation();
            }
            catch (Exception ex)
            {
                LogInternalError(ex);
            }
        }
    }
}
