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
                LogHelper.WriteLogToFile(_logDirectory, filePath, fullText);
                LogHelper.SaveLogAsCsv(_logDirectory, filePath, logContent);
            });
        }
        else
        {
            // Escritura directa en orden (preserva el flujo)
            LogHelper.WriteLogToFile(_logDirectory, filePath, fullText);
            LogHelper.SaveLogAsCsv(_logDirectory, filePath, logContent);
        }
    }
    catch (Exception ex)
    {
        LogInternalError(ex);
    }
}
