Tengo el log así:

---------------------------Inicio de Log-------------------------
Inicio: 2025-11-14 10:46:08
-------------------------------------------------------------------

---------------------------Enviroment Info-------------------------
Inicio: 2025-11-14 10:46:08
-------------------------------------------------------------------
Application: MS_BAN_56_ProcesamientoTransaccionesPOS
Environment: Development
ContentRoot: C:\Git\MS_BAN_56_ProcesamientoTransaccionesPOS\MS_BAN_56_ProcesamientoTransaccionesPOS\MS_BAN_56_ProcesamientoTransaccionesPOS
Execution ID: 0HNH3JL45L8QD:00000007
Client IP: ::1
User Agent: Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/138.0.0.0 Safari/537.36
Machine Name: HNCSTG015243WAP
OS: Microsoft Windows NT 10.0.20348.0
Host: localhost:7298
Distribución: N/A
  -- Extras del HttpContext --
    Scheme              : https
    Protocol            : HTTP/2
    Method              : POST
    Path                : /api/ProcesamientoTransaccionesPOS/Transacciones/GuardarTransacciones
    Query               : 
    ContentType         : application/json
    ContentLength       : 433
    ClientPort          : 54163
    LocalIp             : ::1
    LocalPort           : 7298
    ConnectionId        : 0HNH3JL45L8QD
    Referer             : https://localhost:7298/swagger/index.html
----------------------------------------------------------------------
---------------------------Enviroment Info-------------------------
Fin: 2025-11-14 10:46:08
-------------------------------------------------------------------

-------------------------------------------------------------------------------
Controlador: Transacciones
Action: GuardarTransacciones
Inicio: 2025-11-14 10:46:08
-------------------------------------------------------------------------------
-------------------------------------------------------------------------------

----------------------------------Request Info---------------------------------
Inicio: 2025-11-14 10:46:08
-------------------------------------------------------------------------------
Método: POST
URL: /api/ProcesamientoTransaccionesPOS/Transacciones/GuardarTransacciones
Cuerpo:

                              {
                                "numeroCuenta": "1240015443",
                                "montoDebitado": "224.60000610351562",
                                "montoAcreditado": "0.00",
                                "codigoComercio": "4000009",
                                "nombreComercio": "Banco Davivienda",
                                "terminal": "P0055638",
                                "descripcion": "PRWS-P0055638-250805-00",
                                "naturalezaContable": "D",
                                "numeroDeCorte": "1",
                                "idTransaccionUnico": "PRWS-P0055638-250805-00",
                                "estado": "APROBADA",
                                "descripcionEstado": "Operación aprobada por el emisor"
                              }
----------------------------------Request Info---------------------------------
Fin: 2025-11-14 10:46:08
-------------------------------------------------------------------------------

-----------------------------Exception Details---------------------------------
Inicio: 2025-11-14 10:46:15
-------------------------------------------------------------------------------
System.Data.OleDb.OleDbException (0x80004005): SQL0803: Se ha especificado valor de clave duplicada.
Causa . . . . . :   Existe un índice único o una restricción de unicidad *N en *N sobre una o más columnas de la tabla IPOSRE01G1 en BCAH96DTA. La operación no puede realizarse porque uno o más valores habrían producido una clave duplicada en el índice único o restricción de unicidad. Recuperación . .:   Cambie la sentencia de tal manera que no se produzcan claves duplicadas.  Para obtener información sobre qué filas contienen los valores de clave duplicada, vea los mensajes listados anteriormente en las anotaciones de trabajo (mandato DSPJOBLOG) o pulse F10 (Visualizar mensajes en anotaciones de trabajo) en esta pantalla.
   at System.Data.OleDb.OleDbCommand.ExecuteCommandTextErrorHandling(OleDbHResult hr)
   at System.Data.OleDb.OleDbCommand.ExecuteCommandTextForMultpleResults(tagDBPARAMS dbParams, Object& executeResult)
   at System.Data.OleDb.OleDbCommand.ExecuteCommandText(Object& executeResult)
   at System.Data.OleDb.OleDbCommand.ExecuteCommand(CommandBehavior behavior, Object& executeResult)
   at System.Data.OleDb.OleDbCommand.ExecuteReaderInternal(CommandBehavior behavior, String method)
   at System.Data.OleDb.OleDbCommand.ExecuteNonQuery()
   at Logging.Decorators.LoggingDbCommandWrapper.ExecuteNonQuery()
-----------------------------Exception Details---------------------------------
Fin: 2025-11-14 10:46:15
-------------------------------------------------------------------------------
====================== INICIO LOG DE EJECUCIÓN SQL ======================
Fecha y Hora      : 2025-11-14 10:46:15.050
Duración          : 106.2955 ms
Base de Datos     : DVHNDEV
IP                : 166.178.81.19
Puerto            : 0
Esquema           : bcah96dta
Tabla             : bcah96dta.iposre01g1
Veces Ejecutado   : 1
Filas Afectadas   : 0
SQL:
-- Reserva inicial anti-duplicados via LF IPOSRE01G1 (1 + PRWS-P0055638-250805-00)
INSERT INTO BCAH96DTA.IPOSRE01G1 (GUID, FECHAPOST, HORAPOST, NUMCUENTA, MTODEBITO, MTOACREDI, CODCOMERC, NOMCOMERC, TERMINAL, DESCRIPC, NATCONTA, NUMCORTE, IDTRANUNI, ESTADO, DESCESTADO, CODERROR, DESCERROR) VALUES ('FE1495EA-CEEB-4393-8B10-DE6211270ECD', '20251114', '104609', '1240015443', '224.60000610351562', '0.00', '4000009', 'Banco Davivienda', 'P0055638', 'PRWS-P0055638-250805-00', 'D', '1', 'PRWS-P0055638-250805-00', 'P', 'En proceso', '99999', 'Reserva inicial')
====================== FIN LOG DE EJECUCIÓN SQL ======================

============= DB ERROR =============
Nombre BD: Desconocida
IP: 166.178.81.19
Puerto: 0
Biblioteca: Desconocida
Tabla: bcah96dta.iposre01g1
Hora del error: 2025-11-14 10:46:15
Sentencia SQL:
-- Reserva inicial anti-duplicados via LF IPOSRE01G1 (1 + PRWS-P0055638-250805-00)
INSERT INTO BCAH96DTA.IPOSRE01G1 (GUID, FECHAPOST, HORAPOST, NUMCUENTA, MTODEBITO, MTOACREDI, CODCOMERC, NOMCOMERC, TERMINAL, DESCRIPC, NATCONTA, NUMCORTE, IDTRANUNI, ESTADO, DESCESTADO, CODERROR, DESCERROR) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)

Excepción:
SQL0803: Se ha especificado valor de clave duplicada.
Causa . . . . . :   Existe un índice único o una restricción de unicidad *N en *N sobre una o más columnas de la tabla IPOSRE01G1 en BCAH96DTA. La operación no puede realizarse porque uno o más valores habrían producido una clave duplicada en el índice único o restricción de unicidad. Recuperación . .:   Cambie la sentencia de tal manera que no se produzcan claves duplicadas.  Para obtener información sobre qué filas contienen los valores de clave duplicada, vea los mensajes listados anteriormente en las anotaciones de trabajo (mandato DSPJOBLOG) o pulse F10 (Visualizar mensajes en anotaciones de trabajo) en esta pantalla.
StackTrace:
   at System.Data.OleDb.OleDbCommand.ExecuteCommandTextErrorHandling(OleDbHResult hr)
   at System.Data.OleDb.OleDbCommand.ExecuteCommandTextForMultpleResults(tagDBPARAMS dbParams, Object& executeResult)
   at System.Data.OleDb.OleDbCommand.ExecuteCommandText(Object& executeResult)
   at System.Data.OleDb.OleDbCommand.ExecuteCommand(CommandBehavior behavior, Object& executeResult)
   at System.Data.OleDb.OleDbCommand.ExecuteReaderInternal(CommandBehavior behavior, String method)
   at System.Data.OleDb.OleDbCommand.ExecuteNonQuery()
   at Logging.Decorators.LoggingDbCommandWrapper.ExecuteNonQuery()
============= END DB ERROR ===================

----------------------------------Response Info---------------------------------
Inicio: 2025-11-14 10:46:15
-------------------------------------------------------------------------------
Código Estado: 200
Headers: [Content-Type, application/json; charset=utf-8]; [Content-Length, 120]
Cuerpo:

                              {
                                "codigoError": "20001",
                                "descripcionError": "Transaccion ya registrada (Numero Corte = 1  y ID=PRWS-P0055638-250805-00)."
                              }
----------------------------------Response Info---------------------------------
Fin: 2025-11-14 10:46:15
-------------------------------------------------------------------------------
-------------------------------------------------------------------------------
Controlador: Transacciones
Action: GuardarTransacciones
Fin: 2025-11-14 10:46:15
-------------------------------------------------------------------------------


[Tiempo Total de Ejecución]: 6831 ms
---------------------------Fin de Log-------------------------
Final: 2025-11-14 10:46:15
-------------------------------------------------------------------


    Me dice Que Biblioteca y BD desconocida, no lo esta obteniendo bien, pero veo que si aparece más abajo en el log.


Y El codigo así:

using Microsoft.AspNetCore.Http;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Xml;

namespace Logging.Helpers;

/// <summary>
/// Proporciona métodos auxiliares para la gestión y almacenamiento de logs en archivos.
/// </summary>
public static partial class LogHelper
{
    private static readonly JsonSerializerOptions s_writeOptions = new()
    {
        WriteIndented = true
    };

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

            using var fileStream = new FileStream(filePath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
            using var writer = new StreamWriter(fileStream, Encoding.UTF8);
            writer.Write(logContent);
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
            using var fileStream = new FileStream(logFilePath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
            using var writer = new StreamWriter(fileStream, Encoding.UTF8);
            writer.Write(logContent);

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
            var csvFilePath = Path.Combine(Path.GetDirectoryName(txtFilePath) ?? logDirectory, fileNameWithoutExtension + ".csv");

            // Extraer los campos obligatorios para el CSV
            var traceId = fileNameWithoutExtension.Split('_').FirstOrDefault() ?? "Desconocido";
            var fecha = DateTime.Now.ToString("yyyy-MM-dd");
            var hora = DateTime.Now.ToString("HH:mm:ss");
            var apiName = AppDomain.CurrentDomain.FriendlyName;
            var endpoint = fileNameWithoutExtension.Contains('_') ? fileNameWithoutExtension.Split('_').Skip(1).FirstOrDefault() ?? "Desconocido" : "Desconocido";

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
            var options = s_writeOptions;

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
                    if (value.Contains(':'))
                    {
                        var ipPort = value.Split(':');          // ip:puerto
                        info.Ip = ipPort[0];                    // IP siempre disponible

                        // ✔ Solo asigna puerto si el parseo fue exitoso
                        if (ipPort.Length > 1 &&
                            int.TryParse(ipPort[1].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var port))
                        {
                            info.Port = port;                   // Puerto válido detectado
                        }
                        // else: se mantiene el valor actual (por defecto 0) para evitar datos incorrectos
                    }
                    else
                    {
                        info.Ip = value;                        // Solo IP, sin puerto
                    }
                    break;

                case "port":
                    // ✔ Solo asigna si el valor es un entero válido
                    if (int.TryParse(value.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedPort))
                    {
                        info.Port = parsedPort;
                    }
                    // else: ignora y conserva el valor actual (0 u otro previamente establecido)
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
    /// Registra un log de SQL exitoso y lo encola con el INICIO real para ordenar cronológicamente
    /// entre (4) Request Info y (5) Response Info.
    /// </summary>
    public void LogDatabaseSuccess(SqlLogModel model, HttpContext? context = null)
    {
        try
        {
            var formatted = LogFormatter.FormatDbExecution(model); // respeta tu formato visual

            if (context is not null)
            {
                // 1) Preferir el INICIO real propagado por el wrapper
                DateTime? fromItems = context.Items.TryGetValue("__SqlStartedUtc", out var o) && o is DateTime dt ? dt : null;

                // 2) Si no existe, usar el StartTime del modelo (cuando lo cargues correctamente)
                DateTime? fromModel;
                if (model.StartTime.Kind == DateTimeKind.Utc)
                {
                    fromModel = model.StartTime != default ? (model.StartTime) : null;
                }
                else
                {
                    fromModel = model.StartTime != default ? (model.StartTime.ToUniversalTime()) : null;
                }

                // 3) Último recurso: ahora (no ideal, pero nunca dejamos null)
                var startedUtc = fromItems ?? fromModel ?? DateTime.UtcNow;

                if (!context.Items.ContainsKey("HttpClientLogsTimed")) context.Items["HttpClientLogsTimed"] = new List<object>();
                if (context.Items["HttpClientLogsTimed"] is List<object> timed)
                    timed.Add(new { TsUtc = startedUtc, Seq = NextSeq(context), Content = formatted });
            }
            else
            {
                // Sin contexto: escribir directo para no perder el evento
                WriteLog(context, formatted);
            }
        }
        catch (Exception loggingEx)
        {
            LogInternalError(loggingEx);
        }
    }



    /// <summary>
    /// Registra un log de SQL con error y lo encola con el INICIO real para mantener el orden cronológico.
    /// </summary>
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
                horaError: DateTime.Now
            );

            if (context is not null)
            {
                // Preferimos el INICIO real que puso el wrapper; si no, ahora (UTC).
                var startedUtc = context.Items.TryGetValue("__SqlStartedUtc", out var o) && o is DateTime dt ? dt : DateTime.UtcNow;

                if (!context.Items.ContainsKey("HttpClientLogsTimed")) context.Items["HttpClientLogsTimed"] = new List<object>();
                if (context.Items["HttpClientLogsTimed"] is List<object> timed)
                    timed.Add(new { TsUtc = startedUtc, Seq = NextSeq(context), Content = formatted });
            }
            else
            {
                WriteLog(context, formatted); // fallback sin contexto
            }

            AddExceptionLog(ex); // rastro transversal
        }
        catch (Exception fail)
        {
            LogInternalError(fail);
        }
    }

    // ===================== Bloques manuales =====================

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
    private sealed class LogBlock(LoggingService svc, string filePath, string title) : ILogBlock
    {
        private readonly LoggingService _svc = svc;
        private readonly string _filePath = filePath;
        private readonly string _title = title;
        private int _ended; // 0 no, 1 sí (para idempotencia)

        /// <inheritdoc />
        public void Add(string message, bool includeTimestamp = false)
        {
            // cada "Add" es una fila en el mismo archivo, dentro del bloque
            var line = includeTimestamp
                ? $"[{DateTime.Now:HH:mm:ss}]•{message}"
                : $"• {message}";
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

    #endregion


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
    /// Clave de secuencia por-request para desempatar eventos con el mismo TsUtc.
    /// </summary>
    private const string TimedSeqKey = "__TimedSeq";

    /// <summary>
    /// Devuelve un número incremental por-request. Se almacena en Items[TimedSeqKey].
    /// </summary>
    private static long NextSeq(HttpContext ctx)
    {
        // Como Items es por-request, no necesitamos sincronización pesada aquí.
        var curr = ctx.Items.TryGetValue(TimedSeqKey, out var obj) && obj is long c ? c : 0L;
        curr++;
        ctx.Items[TimedSeqKey] = curr;
        return curr;
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

using Logging.Extensions;
using Logging.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using System.Data.Common;
using System.Text;
using System.Text.Json;

namespace Logging.Helpers;

/// <summary>
/// Clase estática encargada de formatear los logs con la estructura pre definida.
/// </summary>
public static class LogFormatter
{
    private static readonly JsonSerializerOptions s_writeOptions = new()
    {
        WriteIndented = true
    };

    /// <summary>
    /// Formato de Log para FormatBeginLog.
    /// </summary>
    /// <returns>Un string con el formato de Log para FormatBeginLog.</returns>
    public static string FormatBeginLog()
    {
        var sb = new StringBuilder();

        sb.AppendLine("---------------------------Inicio de Log-------------------------");
        sb.AppendLine($"Inicio: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine("-------------------------------------------------------------------");

        return sb.ToString();
    }

    /// <summary>
    /// Formato de Log para FormatEndLog.
    /// </summary>
    /// <returns>Un string con el formato de Log para FormatEndLog.</returns>
    public static string FormatEndLog()
    {
        var sb = new StringBuilder();

        sb.AppendLine("---------------------------Fin de Log-------------------------");
        sb.AppendLine($"Final: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine("-------------------------------------------------------------------");

        return sb.ToString();

    }

    /// <summary>
    /// Formatea la información del entorno, incluyendo datos adicionales si están disponibles.
    /// </summary>
    /// <param name="application">Nombre de la aplicación.</param>
    /// <param name="env">Nombre del entorno (Development, Production, etc.).</param>
    /// <param name="contentRoot">Ruta raíz del contenido.</param>
    /// <param name="executionId">Identificador único de la ejecución.</param>
    /// <param name="clientIp">Dirección IP del cliente.</param>
    /// <param name="userAgent">Agente de usuario del cliente.</param>
    /// <param name="machineName">Nombre de la máquina donde corre la aplicación.</param>
    /// <param name="os">Sistema operativo del servidor.</param>
    /// <param name="host">Host del request recibido.</param>
    /// <param name="distribution">Distribución personalizada u origen (opcional).</param>
    /// <param name="extras">Diccionario con información adicional opcional.</param>
    /// <returns>Texto formateado con la información del entorno.</returns>
    public static string FormatEnvironmentInfo(
        string application, string env, string contentRoot, string executionId,
        string clientIp, string userAgent, string machineName, string os,
        string host, string distribution, Dictionary<string, string>? extras = null)
    {
        var sb = new StringBuilder();

        sb.AppendLine("---------------------------Enviroment Info-------------------------");
        sb.AppendLine($"Inicio: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine("-------------------------------------------------------------------");
        sb.AppendLine($"Application: {application}");
        sb.AppendLine($"Environment: {env}");
        sb.AppendLine($"ContentRoot: {contentRoot}");
        sb.AppendLine($"Execution ID: {executionId}");
        sb.AppendLine($"Client IP: {clientIp}");
        sb.AppendLine($"User Agent: {userAgent}");
        sb.AppendLine($"Machine Name: {machineName}");
        sb.AppendLine($"OS: {os}");
        sb.AppendLine($"Host: {host}");
        sb.AppendLine($"Distribución: {distribution}");

        if (extras is not null && extras.Count != 0)
        {
            sb.AppendLine("  -- Extras del HttpContext --");
            foreach (var kvp in extras)
            {
                sb.AppendLine($"    {kvp.Key,-20}: {kvp.Value}");
            }
        }

        sb.AppendLine(new string('-', 70));
        sb.AppendLine("---------------------------Enviroment Info-------------------------");
        sb.AppendLine($"Fin: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine("-------------------------------------------------------------------");

        return sb.ToString();
    }

    /// <summary>
    /// Formatea los parámetros de entrada de un método antes de guardarlos en el log.
    /// </summary>
    public static string FormatInputParameters(IDictionary<string, object> parameters)
    {
        var sb = new StringBuilder();

        sb.AppendLine("-----------------------Parámetros de Entrada-----------------------------------");

        if (parameters == null || parameters.Count == 0)
        {
            sb.AppendLine("Sin parámetros de entrada.");
        }
        else
        {
            foreach (var param in parameters)
            {
                if (param.Value == null)
                {
                    sb.AppendLine($"{param.Key} = null");
                }
                else if (param.Value.GetType().IsPrimitive || param.Value is string)
                {
                    sb.AppendLine($"{param.Key} = {param.Value}");
                }
                else
                {
                    string json = JsonSerializer.Serialize(param.Value, s_writeOptions);
                    sb.AppendLine($"Objeto {param.Key} =\n{json}");
                }
            }
        }

        sb.AppendLine("-----------------------Parámetros de Entrada-----------------------------------");

        return sb.ToString();
    }

    /// <summary>
    /// Formato de Log para Request.
    /// </summary>
    /// <param name="context">Contexto HTTP de la petición.</param>
    /// <param name="method">Endpoint.</param>
    /// <param name="path">Ruta del Endpoint.</param>
    /// <param name="queryParams">Parametros del Query.</param>
    /// <param name="body">Cuerpo de la petición.</param>
    /// <returns>uString con el Log Formateado.</returns>
    public static string FormatRequestInfo(HttpContext context, string method, string path, string queryParams, string body)
    {
        string formattedJson = string.IsNullOrWhiteSpace(body) ? "  (Sin cuerpo en la solicitud)" : StringExtensions.FormatJson(body, 30); // Aplica indentación controlada
        var routeData = context.GetRouteData();
        string controllerName = routeData?.Values["controller"]?.ToString() ?? "Desconocido";
        string actionName = routeData?.Values["action"]?.ToString() ?? "Desconocido";

        var sb = new StringBuilder();

        sb.AppendLine(FormatControllerBegin(controllerName, actionName));
        sb.AppendLine("----------------------------------Request Info---------------------------------");
        sb.AppendLine($"Inicio: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine("-------------------------------------------------------------------------------");
        sb.AppendLine($"Método: {method}");
        sb.AppendLine($"URL: {path}{queryParams}");
        sb.AppendLine($"Cuerpo:");
        sb.AppendLine($"{formattedJson}");
        sb.AppendLine("----------------------------------Request Info---------------------------------");
        sb.AppendLine($"Fin: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine("-------------------------------------------------------------------------------");

        return sb.ToString();
    }

    /// <summary>
    /// Formato de la información de Respuesta.
    /// </summary>
    /// <param name="context">Contexto HTTP de la petición.</param>
    /// <param name="statusCode">Codigo de Estádo de la respuesta.</param>
    /// <param name="headers">Cabeceras de la respuesta.</param>
    /// <param name="body">Cuerpo de la Respuesta.</param>
    /// <returns>String con el Log Formateado.</returns>
    public static string FormatResponseInfo(HttpContext context, string statusCode, string headers, string body)
    {
        string formattedJson = string.IsNullOrWhiteSpace(body) ? "        (Sin cuerpo en la respuesta)" : StringExtensions.FormatJson(body, 30); // Aplica indentación controlada
        var routeData = context.GetRouteData();
        string controllerName = routeData?.Values["controller"]?.ToString() ?? "Desconocido";
        string actionName = routeData?.Values["action"]?.ToString() ?? "Desconocido";

        var sb = new StringBuilder();

        sb.AppendLine("----------------------------------Response Info---------------------------------");
        sb.AppendLine($"Inicio: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine("-------------------------------------------------------------------------------");
        sb.AppendLine($"Código Estado: {statusCode}");
        sb.AppendLine($"Headers: {headers}");
        sb.AppendLine($"Cuerpo:");
        sb.AppendLine($"{formattedJson}");
        sb.AppendLine("----------------------------------Response Info---------------------------------");
        sb.AppendLine($"Fin: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine("-------------------------------------------------------------------------------");
        sb.AppendLine(FormatControllerEnd(controllerName, actionName));

        return sb.ToString();
    }

    /// <summary>
    /// Método que formatea el Inicio del Log del Controlador.
    /// </summary>
    /// <param name="controllerName">Nombre del controlador.</param>
    /// <param name="actionName">Tipo de Acción.</param>
    /// <returns>String con el log formateado.</returns>
    private static string FormatControllerBegin(string controllerName, string actionName)
    {
        var sb = new StringBuilder();

        sb.AppendLine("-------------------------------------------------------------------------------");
        sb.AppendLine($"Controlador: {controllerName}");
        sb.AppendLine($"Action: {actionName}");
        sb.AppendLine($"Inicio: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine("-------------------------------------------------------------------------------");
        sb.AppendLine("-------------------------------------------------------------------------------");

        return sb.ToString();
    }

    /// <summary>
    /// Método que formatea el fin del Log del Controlador.
    /// </summary>
    /// <param name="controllerName">Nombre del Controlador.</param>
    /// <param name="actionName">Tipo de Acción.</param>
    /// <returns>String con el log formateado.</returns>
    private static string FormatControllerEnd(string controllerName, string actionName)
    {
        var sb = new StringBuilder();

        sb.AppendLine("-------------------------------------------------------------------------------");
        sb.AppendLine($"Controlador: {controllerName}");
        sb.AppendLine($"Action: {actionName}");
        sb.AppendLine($"Fin: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine("-------------------------------------------------------------------------------");

        return sb.ToString();
    }

    /// <summary>
    /// Método que formatea la estructura de inicio un método.
    /// </summary>
    /// <param name="methodName">Nombre del Método.</param>
    /// <param name="parameters">Parametros del metodo.</param>
    /// <returns>String con el log formateado.</returns>
    public static string FormatMethodEntry(string methodName, string parameters)
    {
        var sb = new StringBuilder();

        sb.AppendLine("-------------------------------------------------------------------------------");
        sb.AppendLine($"Método: {methodName}");
        sb.AppendLine($"Inicio: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine("-------------------------------------------------------------------------------");
        sb.AppendLine("Parámetros de Entrada:");
        sb.AppendLine($"{parameters}");

        return sb.ToString();

    }

    /// <summary>
    /// Método que formatea la estructura de salida de un método.
    /// </summary>
    /// <param name="methodName">Nombre del Método.</param>
    /// <param name="returnValue">Valores de Retorno.</param>
    /// <returns>String con el Log Formateado.</returns>
    public static string FormatMethodExit(string methodName, string returnValue)
    {
        var sb = new StringBuilder();

        sb.AppendLine("-------------------------------------------------------------------------------");
        sb.AppendLine($"Método: {methodName}");
        sb.AppendLine($"Valores de Retorno: {returnValue}");
        sb.AppendLine($"Fin: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine("-------------------------------------------------------------------------------");

        return sb.ToString();

    }

    /// <summary>
    /// Método de formatea un Log Simple.
    /// </summary>
    /// <param name="message">Cuerpo del texto del Log.</param>
    /// <returns>String con el Log formateado.</returns>
    public static string FormatSingleLog(string message)
    {
        var sb = new StringBuilder();

        sb.AppendLine("----------------------------------Single Log-----------------------------------");
        sb.AppendLine($"Inicio: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine("-------------------------------------------------------------------------------");
        sb.AppendLine($"{message}");
        sb.AppendLine("----------------------------------Single Log-----------------------------------");
        sb.AppendLine($"Fin: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine("-------------------------------------------------------------------------------");

        return sb.ToString();
    }

    /// <summary>
    /// Método que formatea el Log de un Objeto
    /// </summary>
    /// <param name="objectName">Nombre del Objeto.</param>
    /// <param name="obj">Objeto.</param>
    /// <returns>String con el log formateado.</returns>
    public static string FormatObjectLog(string objectName, object obj)
    {
        var sb = new StringBuilder();

        sb.AppendLine($"---------------------- Object -> {objectName}---------------------------------");
        sb.AppendLine($"Inicio: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine("-------------------------------------------------------------------------------");
        sb.AppendLine($"{StringExtensions.ConvertObjectToString(obj)}");
        sb.AppendLine($"---------------------- Object -> {objectName}---------------------------------");
        sb.AppendLine($"Fin: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine("-------------------------------------------------------------------------------");

        return sb.ToString();
    }

    /// <summary>
    /// Método que formatea el Log de una Excepción.
    /// </summary>
    /// <param name="exceptionMessage">Mensaje de la Excepción.</param>
    /// <returns>String con el log formateado.</returns>
    public static string FormatExceptionDetails(string exceptionMessage)
    {
        var sb = new StringBuilder();

        sb.AppendLine("-----------------------------Exception Details---------------------------------");
        sb.AppendLine($"Inicio: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine("-------------------------------------------------------------------------------");
        sb.AppendLine($"{exceptionMessage}");
        sb.AppendLine("-----------------------------Exception Details---------------------------------");
        sb.AppendLine($"Fin: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine("-------------------------------------------------------------------------------");

        return sb.ToString();

    }

    /// <summary>
    /// Formatea la información de una solicitud HTTP externa realizada mediante HttpClient.
    /// </summary>
    public static string FormatHttpClientRequest(
    string traceId,
    string method,
    string url,
    string statusCode,
    long elapsedMs,
    string headers,
    string? body,
    string? responseBody // <-- nuevo
)
    {
        var builder = new StringBuilder();
        builder.AppendLine("============= INICIO HTTP CLIENT =============");
        builder.AppendLine($"TraceId       : {traceId}");
        builder.AppendLine($"Método        : {method}");
        builder.AppendLine($"URL           : {url}");
        builder.AppendLine($"Código Status : {statusCode}");
        builder.AppendLine($"Duración (ms) : {elapsedMs}");
        builder.AppendLine($"Encabezados   :\n{headers}");

        if (!string.IsNullOrWhiteSpace(body))
        {
            builder.AppendLine("Cuerpo:");
            builder.AppendLine(body);
        }

        if (!string.IsNullOrWhiteSpace(responseBody))
        {
            builder.AppendLine("Respuesta:");
            builder.AppendLine(responseBody);
        }

        builder.AppendLine("============= FIN HTTP CLIENT =============");
        return builder.ToString();
    }

    /// <summary>
    /// Formatea la información de error ocurrida durante una solicitud con HttpClient.
    /// </summary>
    public static string FormatHttpClientError(
        string traceId,
        string method,
        string url,
        Exception exception)
    {
        var builder = new StringBuilder();

        builder.AppendLine();
        builder.AppendLine("======= ERROR HTTP CLIENT =======");
        builder.AppendLine($"TraceId     : {traceId}");
        builder.AppendLine($"Método      : {method}");
        builder.AppendLine($"URL         : {url}");
        builder.AppendLine($"Excepción   : {exception.Message}");
        builder.AppendLine($"StackTrace  : {exception.StackTrace}");
        builder.AppendLine("=================================");

        return builder.ToString();
    }

    /// <summary>
    /// Formatea un log detallado de una ejecución de base de datos exitosa.
    /// Incluye motor, servidor, base de datos, comando SQL y parámetros.
    /// </summary>
    /// <param name="command">Comando ejecutado (DbCommand).</param>
    /// <param name="elapsedMs">Milisegundos que tomó la ejecución.</param>
    /// <param name="context">Contexto HTTP opcional para enlazar trazabilidad.</param>
    /// <param name="customMessage">Mensaje adicional que puede incluir el log.</param>
    /// <returns>Cadena formateada para log de éxito en base de datos.</returns>
    public static string FormatDatabaseSuccess(DbCommand command, long elapsedMs, HttpContext? context = null, string? customMessage = null)
    {
        var sb = new StringBuilder();

        sb.AppendLine("📘 [Base de Datos] Consulta ejecutada exitosamente:");
        sb.AppendLine($"→ Motor: {command.Connection?.GetType().Name ?? "Desconocido"}");
        sb.AppendLine($"→ Servidor: {command.Connection?.DataSource ?? "Desconocido"}");
        sb.AppendLine($"→ Base de Datos: {command.Connection?.Database ?? "Desconocido"}");
        sb.AppendLine($"→ Tipo de Comando: {command.CommandType}");
        sb.AppendLine($"→ Texto SQL: {command.CommandText}");

        if (command.Parameters.Count > 0)
        {
            sb.AppendLine("→ Parámetros:");
            foreach (DbParameter param in command.Parameters)
            {
                sb.AppendLine($"   • {param.ParameterName} = {param.Value} ({param.DbType})");
            }
        }

        sb.AppendLine($"→ Tiempo de ejecución: {elapsedMs} ms");

        if (!string.IsNullOrWhiteSpace(customMessage))
        {
            sb.AppendLine($"→ Mensaje adicional: {customMessage}");
        }

        if (context != null)
        {
            sb.AppendLine($"→ TraceId: {context.TraceIdentifier}");
        }

        return sb.ToString();
    }

    /// <summary>
    /// Formatea un log detallado de un error durante la ejecución de una consulta a base de datos.
    /// Incluye información del motor, SQL ejecutado y excepción.
    /// </summary>
    /// <param name="command">Comando que produjo el error.</param>
    /// <param name="exception">Excepción generada.</param>
    /// <param name="context">Contexto HTTP opcional.</param>
    /// <returns>Cadena formateada para log de error en base de datos.</returns>
    public static string FormatDatabaseError(DbCommand command, Exception exception, HttpContext? context = null)
    {
        var sb = new StringBuilder();

        sb.AppendLine("❌ [Base de Datos] Error en la ejecución de una consulta:");
        sb.AppendLine($"→ Motor: {command.Connection?.GetType().Name ?? "Desconocido"}");
        sb.AppendLine($"→ Servidor: {command.Connection?.DataSource ?? "Desconocido"}");
        sb.AppendLine($"→ Base de Datos: {command.Connection?.Database ?? "Desconocido"}");
        sb.AppendLine($"→ Tipo de Comando: {command.CommandType}");
        sb.AppendLine($"→ Texto SQL: {command.CommandText}");

        if (command.Parameters.Count > 0)
        {
            sb.AppendLine("→ Parámetros:");
            foreach (DbParameter param in command.Parameters)
            {
                sb.AppendLine($"   • {param.ParameterName} = {param.Value} ({param.DbType})");
            }
        }

        sb.AppendLine($"→ Excepción: {exception.GetType().Name} - {exception.Message}");

        if (context != null)
        {
            sb.AppendLine($"→ TraceId: {context.TraceIdentifier}");
        }

        return sb.ToString();
    }

    /// <summary>
    /// Da formato al log estructurado de una ejecución SQL para fines de almacenamiento en log de texto plano.
    /// </summary>
    /// <param name="model">Modelo de log SQL estructurado.</param>
    /// <returns>Cadena con formato estándar para logging de SQL.</returns>
    public static string FormatDbExecution(SqlLogModel model)
    {
        var sb = new StringBuilder();
        sb.AppendLine("====================== INICIO LOG DE EJECUCIÓN SQL ======================");
        sb.AppendLine($"Fecha y Hora      : {model.StartTime:yyyy-MM-dd HH:mm:ss.fff}");
        sb.AppendLine($"Duración          : {model.Duration.TotalMilliseconds} ms");
        sb.AppendLine($"Base de Datos     : {model.DatabaseName}");
        sb.AppendLine($"IP                : {model.Ip}");
        sb.AppendLine($"Puerto            : {model.Port}");
        sb.AppendLine($"Esquema           : {model.Schema}");
        sb.AppendLine($"Tabla             : {model.TableName}");
        sb.AppendLine($"Veces Ejecutado   : {model.ExecutionCount}");
        sb.AppendLine($"Filas Afectadas   : {model.TotalAffectedRows}");
        sb.AppendLine("SQL:");
        sb.AppendLine(model.Sql);
        sb.AppendLine("====================== FIN LOG DE EJECUCIÓN SQL ======================");

        return sb.ToString();
    }

    /// <summary>
    /// Formatea un bloque de log para errores en ejecución SQL, incluyendo contexto y detalles de excepción.
    /// </summary>
    /// <param name="nombreBD">Nombre de la base de datos.</param>
    /// <param name="ip">IP del servidor de base de datos.</param>
    /// <param name="puerto">Puerto utilizado en la conexión.</param>
    /// <param name="biblioteca">Biblioteca o esquema objetivo.</param>
    /// <param name="tabla">Tabla afectada por la operación fallida.</param>
    /// <param name="sentenciaSQL">Sentencia SQL que generó el error.</param>
    /// <param name="exception">Excepción lanzada por el proveedor de datos.</param>
    /// <param name="horaError">Hora en la que ocurrió el error.</param>
    /// <returns>Texto formateado para almacenar como log de error estructurado.</returns>
    public static string FormatDbExecutionError(
        string nombreBD,
        string ip,
        int puerto,
        string biblioteca,
        string tabla,
        string sentenciaSQL,
        Exception exception,
        DateTime horaError)
    {
        var sb = new StringBuilder();

        sb.AppendLine("============= DB ERROR =============");
        sb.AppendLine($"Nombre BD: {nombreBD}");
        sb.AppendLine($"IP: {ip}");
        sb.AppendLine($"Puerto: {puerto}");
        sb.AppendLine($"Biblioteca: {biblioteca}");
        sb.AppendLine($"Tabla: {tabla}");
        sb.AppendLine($"Hora del error: {horaError:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine("Sentencia SQL:");
        sb.AppendLine(sentenciaSQL);
        sb.AppendLine();
        sb.AppendLine("Excepción:");
        sb.AppendLine(exception.Message);
        sb.AppendLine("StackTrace:");
        sb.AppendLine(exception.StackTrace ?? "Sin detalles de stack.");
        sb.AppendLine("============= END DB ERROR ===================");

        return sb.ToString();
    }

    /// <summary>
    /// Construye el texto de cabecera para un bloque de log.
    /// </summary>
    public static string BuildBlockHeader(string title)
    {
        var now = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        var sb = new StringBuilder();
        sb.AppendLine($"======================== [BEGIN BLOCK] ========================");
        sb.AppendLine($"Título     : {title}");
        sb.AppendLine($"Inicio     : {now}");
        sb.AppendLine($"===============================================================");
        sb.AppendLine("");
        return sb.ToString();
    }

    /// <summary>
    /// Construye el texto de cierre para un bloque de log.
    /// </summary>
    public static string BuildBlockFooter()
    {
        var now = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        var sb = new StringBuilder();
        sb.AppendLine($"===============================================================");
        sb.AppendLine($"Fin        : {now}");
        sb.AppendLine($"========================= [END BLOCK] =========================");
        sb.AppendLine("");
        return sb.ToString();
    }
}






