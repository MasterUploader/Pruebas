Actualmente el codigo esta así para que lo tomes en cuenta:

using Microsoft.AspNetCore.Http;
using System.Text;
using System.Text.Json;
using System.Xml;

namespace Logging.Helpers;

/// <summary>
/// Proporciona métodos auxiliares para la gestión y almacenamiento de logs en archivos.
/// </summary>
public static class LogHelper
{
    /// <summary>
    /// Escribe un log en un archivo, asegurando que no interrumpa la ejecución si ocurre un error.
    /// </summary>
    /// <param name="logDirectory">Directorio donde se almacenará el archivo de log.</param>
    /// <param name="fileName">Nombre del archivo de log.</param>
    /// <param name="logContent">Contenido del log a escribir.</param>
    public static void WriteLogToFile(string logDirectory, string filePath, string logContent)
    {
        try
        {
            // Asegura que la carpeta del archivo exista
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            using (var fileStream = new FileStream(filePath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite))
            using (var writer = new StreamWriter(fileStream, Encoding.UTF8))
            {
                writer.Write(logContent);
            }
        }
        catch (Exception ex)
        {
            LogInternalError(logDirectory, ex);
        }
    }

    /// <summary>
    /// Escribe un log en un archivo, asegurando que no interrumpa la ejecución si ocurre un error.
    /// </summary>
    /// <param name="logDirectory">Directorio donde se almacenará el archivo de log.</param>
    /// <param name="fileName">Nombre del archivo de log.</param>
    /// <param name="logContent">Contenido del log a escribir.</param>
    public static void WriteLogToFile2(string logDirectory, string fileName, string logContent)
    {
        try
        {
            // Asegura que el directorio de logs exista
            if (!Directory.Exists(logDirectory))
            {
                Directory.CreateDirectory(logDirectory);
            }

            // Define la ruta completa del archivo de log
            string logFilePath = Path.Combine(logDirectory, fileName);

            //Usamos FileStream con FileShare para permitir accesos concurrentes
            using (var fileStream = new FileStream(logFilePath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite))
            using (var writer = new StreamWriter(fileStream, Encoding.UTF8))
            {
                writer.Write(logContent);
            }

        }
        catch (Exception ex)
        {
            // En caso de error, guarda un log interno para depuración
            LogInternalError(logDirectory, ex);
        }
    }

    /// <summary>
    /// Registra un error interno en un archivo separado ("InternalErrorLog.txt") sin afectar la API.
    /// </summary>
    /// <param name="logDirectory">Directorio donde se almacenará el archivo de errores internos.</param>
    /// <param name="ex">Excepción capturada.</param>
    private static void LogInternalError(string logDirectory, Exception ex)
    {
        try
        {
            // Define la ruta del archivo de errores internos
            string errorLogPath = Path.Combine(logDirectory, "InternalErrorLog.txt");

            // Mensaje de error con timestamp
            string errorMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Error en LogHelper: {ex}{Environment.NewLine}";

            // Guarda el error sin interrumpir la ejecución de la API
            File.AppendAllText(errorLogPath, errorMessage);
        }
        catch
        {
            // Evita bucles de error si la escritura en el log interno también falla
        }
    }

    /// <summary>
    /// Guarda una entrada de log en formato CSV (una línea por log con campos separados por coma).
    /// Utiliza el mismo nombre base del archivo .txt pero con extensión .csv.
    /// </summary>
    /// <param name="logDirectory">Directorio donde se almacenan los logs.</param>
    /// <param name="txtFilePath">Ruta del archivo .txt original (para extraer nombre base).</param>
    /// <param name="logContent">Contenido del log a registrar en CSV.</param>
    public static void SaveLogAsCsv(string logDirectory, string txtFilePath, string logContent)
    {
        try
        {
            // Obtener el nombre base sin extensión (ej. "Log_trace123_Controller_20250408_150000")
            var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(txtFilePath);
            // var csvFilePath = Path.Combine(logDirectory, fileNameWithoutExtension + ".csv");
            var csvFilePath = Path.Combine(Path.GetDirectoryName(txtFilePath) ?? logDirectory, fileNameWithoutExtension + ".csv");

            // Extraer los campos obligatorios para el CSV
            var traceId = fileNameWithoutExtension.Split('_').FirstOrDefault() ?? "Desconocido";
            var fecha = DateTime.Now.ToString("yyyy-MM-dd");
            var hora = DateTime.Now.ToString("HH:mm:ss");
            var apiName = AppDomain.CurrentDomain.FriendlyName;
            var endpoint = fileNameWithoutExtension.Contains("_") ? fileNameWithoutExtension.Split('_').Skip(1).FirstOrDefault() ?? "Desconocido" : "Desconocido";

            // Convertir el contenido del log en una sola línea
            string singleLineLog = ConvertLogToCsvLine(logContent);

            // Crear la línea CSV completa
            string csvLine = $"{traceId},{fecha},{hora},{apiName},{endpoint},\"{singleLineLog}\"";

            // Guardar en el archivo .csv
            WriteCsvLog(csvFilePath, csvLine);
        }
        catch
        {
            // Silenciar cualquier error para no afectar al API
        }
    }

    /// <summary>
    /// Escribe una línea en un archivo CSV. Si el archivo no existe, lo crea con cabecera.
    /// </summary>
    /// <param name="csvFilePath">Ruta del archivo CSV.</param>
    /// <param name="csvLine">Línea a escribir.</param>
    public static void WriteCsvLog(string csvFilePath, string csvLine)
    {
        try
        {
            var directory = Path.GetDirectoryName(csvFilePath);
            if (!string.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            bool fileExists = File.Exists(csvFilePath);
            using var writer = new StreamWriter(csvFilePath, append: true, encoding: Encoding.UTF8);

            if (!fileExists)
            {
                writer.WriteLine("TraceId,Fecha,Hora,ApiName,Endpoint,LogCompleto");
            }

            writer.WriteLine(csvLine);
        }
        catch
        {
            // Silenciar para no afectar la ejecución
        }
    }

    /// <summary>
    /// Escribe una línea en un archivo CSV. Si el archivo no existe, lo crea con cabecera.
    /// </summary>
    /// <param name="csvFilePath">Ruta del archivo CSV.</param>
    /// <param name="csvLine">Línea a escribir.</param>
    public static void WriteCsvLog2(string csvFilePath, string csvLine)
    {
        try
        {
            bool fileExists = File.Exists(csvFilePath);

            using var writer = new StreamWriter(csvFilePath, append: true, encoding: Encoding.UTF8);

            // Escribir cabecera si el archivo no existe
            if (!fileExists)
            {
                writer.WriteLine("TraceId,Fecha,Hora,ApiName,Endpoint,LogCompleto");
            }

            writer.WriteLine(csvLine);
        }
        catch
        {
            // Silenciar para no afectar la ejecución
        }
    }



    /// <summary>
    /// Convierte el contenido de un log multilinea a una sola línea, separando líneas con un símbolo (ej. '|').
    /// También escapa caracteres especiales para evitar errores en CSV.
    /// </summary>
    /// <param name="logContent">Contenido del log en texto plano.</param>
    /// <returns>Log transformado en una sola línea.</returns>
    private static string ConvertLogToCsvLine(string logContent)
    {
        if (string.IsNullOrWhiteSpace(logContent)) return "Sin contenido";

        return logContent
            .Replace("\r\n", "|")
            .Replace("\n", "|")
            .Replace("\r", "|")
            .Replace("\"", "'") // Escapar comillas dobles
            .Trim();
    }
    /// <summary>
    /// Devuelve el cuerpo formateado automáticamente como JSON o XML si es posible.
    /// </summary>
    /// <param name="body">El contenido de la respuesta.</param>
    /// <param name="contentType">El tipo de contenido (Content-Type).</param>
    public static string PrettyPrintAuto(string? body, string? contentType)
    {
        if (string.IsNullOrWhiteSpace(body))
            return "[Sin contenido]";

        contentType = contentType?.ToLowerInvariant();

        try
        {
            if (contentType != null && contentType.Contains("json"))
                return PrettyPrintJson(body);

            if (contentType != null && (contentType.Contains("xml") || contentType.Contains("text/xml")))
                return PrettyPrintXml(body);

            return body;
        }
        catch
        {
            return body; // Si el formateo falla, devolver el cuerpo original
        }
    }

    /// <summary>
    /// Da formato legible a un string JSON.
    /// Si no es un JSON válido, devuelve el texto original.
    /// </summary>
    /// <param name="json">Contenido en formato JSON.</param>
    /// <returns>JSON formateado con sangrías o el texto original si falla el parseo.</returns>
    private static string PrettyPrintJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return "[Sin contenido JSON]";

        try
        {
            using var jdoc = JsonDocument.Parse(json);
            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };

            return JsonSerializer.Serialize(jdoc.RootElement, options);
        }
        catch
        {
            return json; // Si no es JSON válido, devolverlo sin cambios
        }
    }

    /// <summary>
    /// Mejora la estructura del XML para que no quede en una sola linea.
    /// </summary>
    /// <param name="xml">Xml sin formatear.</param>
    /// <returns>Devuelve XML fromateado.</returns>
    private static string PrettyPrintXml(string xml)
    {
        try
        {
            var doc = new System.Xml.XmlDocument();
            doc.LoadXml(xml);

            var stringBuilder = new StringBuilder();
            var settings = new XmlWriterSettings
            {
                Indent = true,
                IndentChars = "  ",
                NewLineChars = "\n",
                NewLineHandling = NewLineHandling.Replace
            };

            using (var writer = XmlWriter.Create(stringBuilder, settings))
            {
                doc.Save(writer);
            }

            return stringBuilder.ToString();
        }
        catch
        {
            // Si el XML es inválido o viene mal, lo devolvemos como está
            return xml;
        }
    }

    /// <summary>
    /// Extrae los datos de IP, puerto, base de datos y biblioteca desde una cadena de conexión.
    /// </summary>
    public static DbConnectionInfo ExtractDbConnectionInfo(string? connectionString)
    {
        var info = new DbConnectionInfo();

        if (string.IsNullOrWhiteSpace(connectionString))
            return info;

        var parts = connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries);
        foreach (var part in parts)
        {
            var kv = part.Split('=', 2);
            if (kv.Length != 2) continue;

            var key = kv[0].Trim().ToLowerInvariant();
            var value = kv[1].Trim();

            switch (key)
            {
                case "data source":
                case "server":
                    if (value.Contains(":"))
                    {
                        var ipPort = value.Split(':');
                        info.Ip = ipPort[0];
                        int.TryParse(ipPort[1], out int port);
                        info.Port = port;
                    }
                    else
                    {
                        info.Ip = value;
                    }
                    break;

                case "port":
                    int.TryParse(value, out int parsedPort);
                    info.Port = parsedPort;
                    break;

                case "initial catalog":
                case "database":
                    info.Database = value;
                    break;

                case "default collection":
                case "library":
                    info.Library = value;
                    break;
            }
        }

        return info;
    }

    /// <summary>
    /// Extrae el nombre de la tabla desde una sentencia SQL básica (INSERT, UPDATE, DELETE, SELECT).
    /// </summary>
    /// <param name="sql">Sentencia SQL.</param>
    /// <returns>Nombre de la tabla o "Desconocida".</returns>
    public static string ExtractTableName(string sql)
    {
        if (string.IsNullOrWhiteSpace(sql)) return "Desconocida";

        string lowerSql = sql.ToLowerInvariant();

        var patterns = new[]
        {
        @"insert\s+into\s+([a-zA-Z0-9_\.]+)",
        @"update\s+([a-zA-Z0-9_\.]+)",
        @"delete\s+from\s+([a-zA-Z0-9_\.]+)",
        @"from\s+([a-zA-Z0-9_\.]+)"
    };

        foreach (var pattern in patterns)
        {
            var match = System.Text.RegularExpressions.Regex.Match(lowerSql, pattern);
            if (match.Success && match.Groups.Count > 1)
                return match.Groups[1].Value;
        }

        return "Desconocida";
    }

    /// <summary>
    /// Escribe contenido en un archivo de log `.txt`, eligiendo automáticamente
    /// entre escritura síncrona o asincrónica según el tamaño del contenido.
    /// Esto evita bloquear la ejecución de la API en logs grandes.
    /// </summary>
    /// <param name="directory">Directorio base donde se guardará el archivo.</param>
    /// <param name="filePath">Ruta completa del archivo de log .txt.</param>
    /// <param name="content">Contenido a escribir en el archivo.</param>
    /// <param name="forceAsyncThresholdBytes">
    /// Umbral en bytes a partir del cual se usará Task.Run para escritura asincrónica (por defecto 128 KB).
    /// </param>
    public static void SafeWriteLog(string directory, string filePath, string content, int forceAsyncThresholdBytes = 128 * 1024)
    {
        try
        {
            if (content.Length > forceAsyncThresholdBytes)
            {
                Task.Run(() => WriteLogToFile(directory, filePath, content));
            }
            else
            {
                WriteLogToFile(directory, filePath, content);
            }
        }
        catch
        {
            // Falla silenciosa para no interrumpir el flujo de ejecución principal
        }
    }


    /// <summary>
    /// Escribe contenido en el archivo de log `.csv`, eligiendo entre modo síncrono
    /// o asincrónico dependiendo del tamaño del contenido. Esta función es útil
    /// para garantizar rendimiento en logs muy extensos sin bloquear el hilo principal.
    /// </summary>
    /// <param name="directory">Directorio donde se guarda el archivo CSV.</param>
    /// <param name="logFilePath">Ruta base del archivo de log (de donde se deriva el nombre del .csv).</param>
    /// <param name="content">Contenido del log a escribir en una línea del archivo CSV.</param>
    /// <param name="forceAsyncThresholdBytes">
    /// Umbral en bytes a partir del cual se usará escritura asincrónica (por defecto 128 KB).
    /// </param>
    public static void SafeWriteCsv(string directory, string logFilePath, string content, int forceAsyncThresholdBytes = 128 * 1024)
    {
        try
        {
            if (content.Length > forceAsyncThresholdBytes)
            {
                Task.Run(() => SaveLogAsCsv(directory, logFilePath, content));
            }
            else
            {
                SaveLogAsCsv(directory, logFilePath, content);
            }
        }
        catch
        {
            // Silenciar errores de escritura en CSV para evitar interrupciones
        }
    }

    /// <summary>
    /// Guarda un log estructurado en un archivo de texto, utilizando el contexto HTTP si está disponible.
    /// </summary>
    /// <param name="formattedLog">El contenido del log ya formateado (por ejemplo, SQL estructurado, logs HTTP, etc.).</param>
    /// <param name="context">
    /// Opcional: contexto HTTP de la solicitud actual. Si se proporciona, se usará para nombrar el archivo de log con TraceId, endpoint, etc.
    /// </param>
    public static void SaveStructuredLog(string formattedLog, HttpContext? context)
    {
        try
        {
            // Obtener ruta del log dinámicamente
            var path = GetPathFromContext(context);

            // Asegurar que el directorio exista
            var directory = Path.GetDirectoryName(path);
            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory!);

            // Guardar el log estructurado
            File.AppendAllText(path, formattedLog + Environment.NewLine);
        }
        catch (Exception ex)
        {
            // Temporal: manejo silencioso en caso de error de escritura
            Console.WriteLine($"[LogHelper Error] {ex.Message}");
        }
    }

    /// <summary>
    /// Construye la ruta dinámica para guardar logs basada en el contexto HTTP.
    /// Si no hay contexto, se genera una ruta genérica con timestamp.
    /// </summary>
    /// <param name="context">Contexto HTTP actual (puede ser null).</param>
    /// <returns>Ruta absoluta del archivo de log.</returns>
    private static string GetPathFromContext(HttpContext? context)
    {
        var basePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");

        if (context != null)
        {
            var traceId = context.TraceIdentifier;
            var endpoint = context.Request.Path.Value?.Trim('/').Replace("/", "_") ?? "endpoint";
            var date = DateTime.UtcNow.ToString("yyyyMMdd");

            var filename = $"{traceId}_{endpoint}_{date}.txt";
            return Path.Combine(basePath, filename);
        }

        // Sin contexto: log general
        var genericName = $"GeneralLog_{DateTime.UtcNow:yyyyMMdd_HHmmssfff}.txt";
        return Path.Combine(basePath, genericName);
    }
}

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

                    Console.WriteLine($"[DEBUG] Ruta carpeta controlador: {controllerDirectory}");

                    if (!Directory.Exists(controllerDirectory))
                    {
                        Directory.CreateDirectory(controllerDirectory);
                        Console.WriteLine("[DEBUG] Carpeta creada");
                    }
                    else
                    {
                        Console.WriteLine("[DEBUG] Carpeta ya existía");
                    }

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
            _stopwatch = Stopwatch.StartNew(); // Iniciar medición de tiempo

            // 1️⃣ Asegurar que exista un ExecutionId único para la solicitud
            if (!context.Items.ContainsKey("ExecutionId"))
            {
                context.Items["ExecutionId"] = Guid.NewGuid().ToString();
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
        context.Request.EnableBuffering(); // Permite leer el cuerpo de la petición sin afectar la ejecución

        using var reader = new StreamReader(context.Request.Body, Encoding.UTF8, leaveOpen: true);
        string body = await reader.ReadToEndAsync();
        context.Request.Body.Position = 0; // Restablece la posición para que el controlador pueda leerlo

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

