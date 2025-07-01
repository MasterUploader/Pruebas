/// <summary>
/// Formatea un bloque estructurado de ejecución SQL para los archivos de log.
/// Incluye detalles como IP, puerto, base de datos, biblioteca, tabla afectada,
/// las sentencias ejecutadas, cantidad de ejecuciones, resultado, hora y duración.
/// </summary>
/// <param name="nombreBD">Nombre de la base de datos objetivo.</param>
/// <param name="ip">Dirección IP o nombre del host del servidor de base de datos.</param>
/// <param name="puerto">Puerto utilizado para la conexión a la base de datos.</param>
/// <param name="biblioteca">Biblioteca o esquema asociado (si aplica).</param>
/// <param name="tabla">Nombre de la tabla afectada por la operación (si es detectable).</param>
/// <param name="sentenciasSQL">Lista de sentencias SQL ejecutadas en la operación.</param>
/// <param name="cantidadEjecuciones">Número de veces que se ejecutaron las sentencias.</param>
/// <param name="resultado">Resultado de la operación (por ejemplo: filas afectadas).</param>
/// <param name="horaInicio">Hora exacta en que comenzó la ejecución.</param>
/// <param name="duracion">Duración total de la operación como <see cref="TimeSpan"/>.</param>
/// <returns>Texto formateado para incluir en el log estructurado.</returns>
public static string FormatDbExecution(
    string nombreBD,
    string ip,
    int puerto,
    string biblioteca,
    string tabla,
    List<string> sentenciasSQL,
    int cantidadEjecuciones,
    object resultado,
    DateTime horaInicio,
    TimeSpan duracion)
{
    var sb = new StringBuilder();

    sb.AppendLine("============= DB EXECUTION =============");
    sb.AppendLine($"Nombre BD: {nombreBD}");
    sb.AppendLine($"IP: {ip}");
    sb.AppendLine($"Puerto: {puerto}");
    sb.AppendLine($"Biblioteca: {biblioteca}");
    sb.AppendLine($"Tabla: {tabla}");
    sb.AppendLine("SQL:");

    foreach (var sentencia in sentenciasSQL)
    {
        sb.AppendLine(sentencia);
    }

    sb.AppendLine();
    sb.AppendLine($"Cantidad de ejecuciones: {cantidadEjecuciones}");
    sb.AppendLine($"Resultado: {resultado}");
    sb.AppendLine($"Hora de inicio: {horaInicio:yyyy-MM-dd HH:mm:ss}");
    sb.AppendLine($"Duración: {duracion.TotalMilliseconds} ms");
    sb.AppendLine("============= END DB ===================");

    return sb.ToString();
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




public void LogDatabaseSuccess(DbCommand command, long elapsedMs, HttpContext? context = null, string? customMessage = null)
{
    try
    {
        var connectionInfo = LogHelper.ExtractDbConnectionInfo(command.Connection?.ConnectionString);
        var tabla = LogHelper.ExtractTableName(command.CommandText);

        var formatted = LogFormatter.FormatDbExecution(
            nombreBD: connectionInfo.Database,
            ip: connectionInfo.Ip,
            puerto: connectionInfo.Port,
            biblioteca: connectionInfo.Library,
            tabla: tabla,
            sentenciasSQL: new List<string> { command.CommandText },
            cantidadEjecuciones: 1,
            resultado: customMessage ?? "Éxito",
            horaInicio: DateTime.Now.AddMilliseconds(-elapsedMs),
            duracion: TimeSpan.FromMilliseconds(elapsedMs)
        );

        WriteLog(context, formatted);
    }
    catch (Exception ex)
    {
        LogInternalError(ex);
    }
}
