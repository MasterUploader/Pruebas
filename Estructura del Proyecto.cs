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
        LogHelper.WriteLogToFile(_logDirectory, GetCurrentLogFile(), formatted);
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
        string objectName = logObject?.GetType().Name ?? "ObjetoDesconocido";

        // Convertir objeto a JSON o XML según el formato
        string formatted = LogFormatter.FormatObjectLog(objectName, logObject).Indent(LogScope.CurrentLevel);

        // Guardar el log en archivo
        LogHelper.WriteLogToFile(_logDirectory, GetCurrentLogFile(), formatted);
    }
    catch (Exception ex)
    {
        LogInternalError(ex);
    }
}
