/// <summary>
/// Genera automáticamente un archivo CSV complementario al archivo de log TXT.
/// El contenido se transforma en una sola línea, separando cada línea por '|'.
/// Si el archivo CSV no existe, se agrega el encabezado.
/// </summary>
private void GenerateCsvLog(string traceId, string logContent)
{
    try
    {
        var context = _httpContextAccessor.HttpContext;
        var hostEnvironment = context?.RequestServices.GetService<IHostEnvironment>();
        var request = context?.Request;

        string applicationName = hostEnvironment?.ApplicationName ?? "Desconocido";
        string endpoint = request?.Path.Value ?? "Desconocido";
        string fecha = DateTime.Now.ToString("yyyy-MM-dd");
        string hora = DateTime.Now.ToString("HH:mm:ss");

        // Reemplaza saltos de línea por separador '|' y comas por punto y coma para evitar conflictos
        string contenidoPlano = logContent.Replace(Environment.NewLine, "|").Replace(",", ";").Replace("\r", "").Replace("\n", "").Replace("|", "¦");

        var campos = new[]
        {
            traceId,
            fecha,
            hora,
            applicationName,
            endpoint,
            contenidoPlano
        };

        string lineaCsv = string.Join(",", campos);
        string csvFileName = Path.Combine(_logDirectory, $"Log_{traceId}.csv");

        var sb = new StringBuilder();

        // Si el archivo no existe, escribe encabezado
        if (!File.Exists(csvFileName))
        {
            sb.AppendLine("TraceId,Fecha,Hora,NombreApi,Endpoint,Log");
        }

        sb.AppendLine(lineaCsv);

        File.AppendAllText(csvFileName, sb.ToString(), Encoding.UTF8);
    }
    catch (Exception ex)
    {
        LogInternalError(ex);
    }
}



/// <summary>
/// Escribe un log completo para la petición actual en un archivo único.
/// El nombre del archivo se basa en el TraceIdentifier del HttpContext.
/// También genera automáticamente un archivo .csv con los mismos datos.
/// </summary>
public void WriteLog(HttpContext context, string logContent)
{
    try
    {
        string traceId = context.TraceIdentifier;
        var fileName = Path.Combine(_logDirectory, $"Log_{traceId}.txt");

        // Escribe en archivo TXT
        File.AppendAllText(fileName, logContent.Indent(LogScope.CurrentLevel) + Environment.NewLine + Environment.NewLine);

        // También escribe en archivo CSV
        GenerateCsvLog(traceId, logContent);
    }
    catch (Exception ex)
    {
        LogInternalError(ex);
    }
}
