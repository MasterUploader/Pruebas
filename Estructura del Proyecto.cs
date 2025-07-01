public void LogDatabaseSuccess(
    DbCommand command,
    long elapsedMs,
    HttpContext? context = null,
    int? totalAffectedRows = null,
    int? executionCount = null)
{
    try
    {
        var connectionInfo = LogHelper.ExtractDbConnectionInfo(command.Connection?.ConnectionString);
        var tabla = LogHelper.ExtractTableName(command.CommandText);
        var formatted = LogFormatter.FormatDbExecution(
            sql: command.CommandText,
            database: connectionInfo.Database,
            table: tabla,
            ip: connectionInfo.Ip,
            port: connectionInfo.Port,
            totalAffectedRows: totalAffectedRows ?? -1,
            executionCount: executionCount ?? -1,
            startTime: DateTime.Now.AddMilliseconds(-elapsedMs),
            durationMs: elapsedMs
        );

        WriteLog(context, formatted);
    }
    catch (Exception ex)
    {
        LogInternalError(ex);
    }
}


public static string FormatDbExecution(
    string sql,
    string database,
    string table,
    string ip,
    string port,
    int totalAffectedRows,
    int executionCount,
    DateTime startTime,
    long durationMs)
{
    var sb = new StringBuilder();
    sb.AppendLine("============= DB EXECUTION =============");
    sb.AppendLine($"Nombre BD: {database}");
    sb.AppendLine($"IP: {ip}");
    sb.AppendLine($"Puerto: {port}");
    sb.AppendLine($"Biblioteca: {database}");
    sb.AppendLine($"Tabla: {table}");
    sb.AppendLine("SQL:");
    sb.AppendLine(sql);
    sb.AppendLine();

    if (executionCount >= 0)
        sb.AppendLine($"Cantidad de ejecuciones: {executionCount}");

    if (totalAffectedRows >= 0)
        sb.AppendLine($"Resultado: Filas afectadas: {totalAffectedRows}");

    sb.AppendLine($"Hora de inicio: {startTime:yyyy-MM-dd HH:mm:ss}");
    sb.AppendLine($"Duración: {durationMs} ms");
    sb.AppendLine("============= END DB ===================");

    return sb.ToString();
}

int result = _inner.ExecuteNonQuery();
sw.Stop();

_loggingService?.LogDatabaseSuccess(
    _inner,
    sw.ElapsedMilliseconds,
    context: null,
    totalAffectedRows: result,
    executionCount: 1
);
