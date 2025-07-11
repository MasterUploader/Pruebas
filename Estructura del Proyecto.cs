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

var csvFilePath = Path.Combine(Path.GetDirectoryName(txtFilePath) ?? logDirectory, fileNameWithoutExtension + ".csv");




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
