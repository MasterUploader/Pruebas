/// <summary>
/// Registra un log estructurado de éxito para una operación SQL.
/// Publica el bloque en Items["HttpClientLogsTimed"] con el INICIO UTC para que
/// el middleware lo inserte entre (4) y (5) en el mismo archivo del request.
/// </summary>
public void LogDatabaseSuccess(SqlLogModel model, HttpContext? context = null)
{
    try
    {
        var formatted = LogFormatter.FormatDbExecution(model); // bloque ya formateado

        if (context is not null)
        {
            // INICIO real en UTC (clave para el orden cronológico correcto)
            var startedUtc = model.StartTime.Kind == DateTimeKind.Utc
                ? model.StartTime
                : model.StartTime.ToUniversalTime();

            // Usamos la misma lista “timed” que el middleware ya consume y ordena
            if (!context.Items.ContainsKey("HttpClientLogsTimed")) context.Items["HttpClientLogsTimed"] = new List<object>();
            if (context.Items["HttpClientLogsTimed"] is List<object> timed)
                timed.Add(new { TsUtc = startedUtc, Content = formatted });
        }
        else
        {
            // Sin contexto HTTP: caemos a la escritura directa en el archivo “actual”
            WriteLog(context, formatted);
        }
    }
    catch (Exception loggingEx)
    {
        LogInternalError(loggingEx);
    }
}

/// <summary>
/// Registra un log de error para una ejecución SQL. Usa el INICIO propagado por el wrapper
/// (Items["__SqlStartedUtc"]) y, si no existe, usa ahora (UTC). Se publica en la lista “timed”
/// para mantener el orden entre (4) y (5). Además, deja el detalle de excepción transversal.
/// </summary>
public void LogDatabaseError(DbCommand command, Exception ex, HttpContext? context = null)
{
    try
    {
        var connectionInfo = LogHelper.ExtractDbConnectionInfo(command.Connection?.ConnectionString);
        var tabla = LogHelper.ExtractTableName(command.CommandText);

        var formatted = LogFormatter.FormatDbExecutionError(
            nombreBD: connectionInfo.Database,
            ip: connectionInfo.Ip,
            puerto: connectionInfo.Port,
            biblioteca: connectionInfo.Library,
            tabla: tabla,
            sentenciaSQL: command.CommandText,
            exception: ex,
            horaError: DateTime.Now
        );

        if (context is not null)
        {
            // Preferimos el INICIO real que puso el wrapper; si no, ahora (UTC)
            var startedUtc =
                context.Items.TryGetValue("__SqlStartedUtc", out var obj) && obj is DateTime dt
                    ? dt
                    : DateTime.UtcNow;

            if (!context.Items.ContainsKey("HttpClientLogsTimed")) context.Items["HttpClientLogsTimed"] = new List<object>();
            if (context.Items["HttpClientLogsTimed"] is List<object> timed)
                timed.Add(new { TsUtc = startedUtc, Content = formatted });
        }
        else
        {
            WriteLog(context, formatted); // fallback sin contexto
        }

        AddExceptionLog(ex); // detalle transversal
    }
    catch (Exception fail)
    {
        LogInternalError(fail);
    }
}
