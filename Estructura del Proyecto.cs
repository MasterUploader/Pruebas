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

public void AddObjLog(object logObject)
{
    try
    {
        string objectName = logObject?.GetType()?.Name ?? "ObjetoDesconocido";
        object safeObject = logObject ?? new { };
        string formatted = LogFormatter.FormatObjectLog(objectName, safeObject).Indent(LogScope.CurrentLevel);
        LogHelper.SafeWriteLog(_logDirectory, GetCurrentLogFile(), formatted);
    }
    catch (Exception ex)
    {
        LogInternalError(ex);
    }
}

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
