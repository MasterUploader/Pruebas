public static void WriteLogToFile(string baseDirectory, string filePath, string content)
{
    var directory = Path.GetDirectoryName(filePath);
    if (!string.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory))
    {
        Directory.CreateDirectory(directory);
    }

    File.AppendAllText(filePath, content);
}
