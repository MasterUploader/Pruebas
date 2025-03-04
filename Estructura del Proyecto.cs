using Logging.Abstractions;
using Logging.Formatters;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace Logging.Services
{
    /// <summary>
    /// Servicio responsable de gestionar los logs de la API.
    /// </summary>
    public class LoggingService : ILoggingService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ConcurrentQueue<string> _logQueue = new();
        private readonly object _lock = new();
        private readonly string _logDirectory;

        public LoggingService(IHttpContextAccessor httpContextAccessor, IHostEnvironment hostEnvironment, IOptions<Logging.Configuration.LoggingOptions> options)
        {
            _httpContextAccessor = httpContextAccessor;

            string baseLogDir = options.Value.BaseLogDirectory ?? @"C:\Logs";
            string apiName = !string.IsNullOrWhiteSpace(hostEnvironment.ApplicationName) ? hostEnvironment.ApplicationName : "Desconocido";

            _logDirectory = Path.Combine(baseLogDir, apiName);

            try
            {
                if (!Directory.Exists(_logDirectory))
                {
                    Directory.CreateDirectory(_logDirectory);
                }
            }
            catch (Exception ex)
            {
                LogInternalError(ex);
            }
        }

        private string GetCurrentLogFile()
        {
            var context = _httpContextAccessor.HttpContext;
            if (context == null) return Path.Combine(_logDirectory, "GlobalLog.txt");

            string traceId = context.TraceIdentifier ?? Guid.NewGuid().ToString();
            string methodName = context.Items["CurrentMethod"] as string ?? "UnknownMethod";
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");

            return Path.Combine(_logDirectory, $"{traceId}_{methodName}_{timestamp}.txt");
        }

        private void AddBufferedLog(string message)
        {
            _logQueue.Enqueue($"[{DateTime.Now:HH:mm:ss}] {message}");
        }

        public void AddSingleLog(string message) => AddBufferedLog(LogFormatter.FormatSingleLog(message));

        public void AddObjLog(string objectName, object obj)
        {
            string formatted = LogFormatter.FormatObjectLog(objectName, obj);
            AddBufferedLog(formatted);
        }

        public void AddMethodEntryLog(string methodName, string parameters)
        {
            _httpContextAccessor.HttpContext.Items["CurrentMethod"] = methodName;
            AddBufferedLog(LogFormatter.FormatInputParameters($"Método: {methodName}\n{parameters}"));
        }

        public void AddMethodExitLog(string methodName, string returnValue)
        {
            AddBufferedLog(LogFormatter.FormatOutputParameters($"Método: {methodName}\n{returnValue}"));
        }

        public void AddEnvironmentLog()
        {
            var context = _httpContextAccessor.HttpContext;
            string logMessage = LogFormatter.FormatEnvironmentInfo(
                context?.Request.Host.Value ?? "Desconocido",
                Environment.MachineName,
                Environment.OSVersion.ToString(),
                context?.Connection.RemoteIpAddress?.ToString() ?? "Desconocido"
            );
            AddBufferedLog(logMessage);
        }

        public void AddExceptionLog(Exception ex)
        {
            AddBufferedLog(LogFormatter.FormatExceptionDetails(ex.ToString()));
        }

        public void FlushLogs()
        {
            lock (_lock)
            {
                if (_logQueue.IsEmpty) return;

                var logsToWrite = new List<string>();
                while (_logQueue.TryDequeue(out var logEntry))
                {
                    logsToWrite.Add(logEntry);
                }

                File.AppendAllLines(GetCurrentLogFile(), logsToWrite);
            }
        }

        private void LogInternalError(Exception ex)
        {
            try
            {
                string errorLogPath = Path.Combine(_logDirectory, "InternalErrorLog.txt");
                string errorMessage = $"[{DateTime.Now}] ERROR en LoggingService: {ex.Message}\n{ex.StackTrace}\n";
                File.AppendAllText(errorLogPath, errorMessage);
            }
            catch
            {
                // Si falla la escritura en el log interno, ignoramos el error.
            }
        }
    }
}
