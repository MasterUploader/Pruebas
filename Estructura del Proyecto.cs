/// <summary>
/// Escribe un log en el archivo correspondiente de la petición actual (.txt) y automáticamente en un .csv del mismo nombre base.
/// Se asegura de que la API no se bloquee si ocurre un error en el proceso de escritura.
/// </summary>
public void WriteLog(HttpContext context, string logContent)
{
    try
    {
        string filePath = GetCurrentLogFile();
        bool isNewFile = !File.Exists(filePath);

        var logBuilder = new StringBuilder();

        // Si es la primera vez que escribimos en este archivo, agregamos la cabecera
        if (isNewFile)
        {
            logBuilder.AppendLine(LogFormatter.FormatBeginLog());
        }

        // Agregamos el contenido del log
        logBuilder.AppendLine(logContent);

        // Si es la última entrada del log, agregamos el cierre
        if (context.Response.HasStarted)
        {
            logBuilder.AppendLine(LogFormatter.FormatEndLog());
        }

        // Escritura en el archivo .txt sin bloquear la API
        Task.Run(() => LogHelper.WriteLogToFile(_logDirectory, filePath, logBuilder.ToString()));

        // Escritura automática en el archivo .csv (una sola línea, mismo nombre base)
        Task.Run(() => LogHelper.SaveLogAsCsv(_logDirectory, filePath, logContent));
    }
    catch (Exception ex)
    {
        LogInternalError(ex);
    }
}
