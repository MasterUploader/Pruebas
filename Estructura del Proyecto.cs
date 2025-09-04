Ya tengo una Clase LogHelper, y esto es lo que tengo actualmene de código, de lo nuevo que tu creaste no altera o es que se reemplaza?

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
