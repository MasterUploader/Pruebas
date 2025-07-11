"LoggingOptions": {
  "GenerateTxt": true,
  "GenerateCsv": true
    }


namespace RestUtilities.Logging.Models
{
    /// <summary>
    /// Configuración de opciones para el sistema de logging.
    /// </summary>
    public class LoggingOptions
    {
        /// <summary>
        /// Indica si se debe generar el archivo de log en formato .txt.
        /// </summary>
        public bool GenerateTxt { get; set; }

        /// <summary>
        /// Indica si se debe generar el archivo de log en formato .csv.
        /// </summary>
        public bool GenerateCsv { get; set; }
    }
}

using Microsoft.Extensions.Options;
using RestUtilities.Logging.Helpers;
using RestUtilities.Logging.Models;

public class LoggingService : ILoggingService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly LoggingOptions _loggingOptions;

    public LoggingService(
        IHttpContextAccessor httpContextAccessor,
        IOptions<LoggingOptions> loggingOptions)
    {
        _httpContextAccessor = httpContextAccessor;
        _loggingOptions = loggingOptions.Value;
    }

    public void WriteLog(string traceId, string endpoint, string logContent)
    {
        var logFormatted = LogFormatter.FormatLog(traceId, endpoint, logContent);

        if (_loggingOptions.GenerateTxt)
        {
            LogHelper.SaveLogAsTxt(traceId, logFormatted);
        }

        if (_loggingOptions.GenerateCsv)
        {
            LogHelper.WriteCsvLog(traceId, endpoint, logFormatted);
        }
    }
}

