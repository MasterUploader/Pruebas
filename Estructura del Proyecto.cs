/// <summary>
/// Escribe una línea de log en el archivo .txt y automáticamente en un .csv del mismo nombre base.
/// </summary>
/// <param name="context">Contexto HTTP para extraer el archivo actual.</param>
/// <param name="logContent">Contenido del log a registrar.</param>
public void WriteLog(HttpContext context, string logContent)
{
    try
    {
        string logFilePath = GetCurrentLogFile();

        // Escribir log en archivo .txt (formato detallado y multilinea)
        LogHelper.WriteLogToFile(_logDirectory, logFilePath, logContent.Indent(LogScope.CurrentLevel));

        // Guardar log también como CSV (en una sola línea, separado por comas)
        LogHelper.SaveLogAsCsv(_logDirectory, logFilePath, logContent);
    }
    catch (Exception ex)
    {
        LogInternalError(ex);
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
        var csvFilePath = Path.Combine(logDirectory, fileNameWithoutExtension + ".csv");

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
