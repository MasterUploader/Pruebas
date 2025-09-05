/// <summary>
/// Genera una secuencia incremental por-request para desempatar eventos con el mismo TsUtc.
/// Se guarda en Items["__TimedSeq"] como long.
/// </summary>
private static long NextSeq(HttpContext ctx)
{
    if (!ctx.Items.TryGetValue("__TimedSeq", out var obj) || obj is not long curr) curr = 0;
    curr++;
    ctx.Items["__TimedSeq"] = curr;
    return curr;
}


/// <summary>
/// Registra un log de SQL exitoso y lo encola con el INICIO real para ordenar cronológicamente
/// entre (4) Request Info y (5) Response Info.
/// </summary>
public void LogDatabaseSuccess(SqlLogModel model, HttpContext? context = null)
{
    try
    {
        var formatted = LogFormatter.FormatDbExecution(model); // respeta tu formato visual

        if (context is not null)
        {
            // 1) Preferir el INICIO real propagado por el wrapper
            DateTime? fromItems = context.Items.TryGetValue("__SqlStartedUtc", out var o) && o is DateTime dt ? dt : null;

            // 2) Si no existe, usar el StartTime del modelo (cuando lo cargues correctamente)
            DateTime? fromModel = model.StartTime != default ? (model.StartTime.Kind == DateTimeKind.Utc ? model.StartTime : model.StartTime.ToUniversalTime()) : null;

            // 3) Último recurso: ahora (no ideal, pero nunca dejamos null)
            var startedUtc = fromItems ?? fromModel ?? DateTime.UtcNow;

            if (!context.Items.ContainsKey("HttpClientLogsTimed")) context.Items["HttpClientLogsTimed"] = new List<object>();
            if (context.Items["HttpClientLogsTimed"] is List<object> timed)
                timed.Add(new { TsUtc = startedUtc, Seq = NextSeq(context), Content = formatted });
        }
        else
        {
            // Sin contexto: escribir directo para no perder el evento
            WriteLog(context, formatted);
        }
    }
    catch (Exception loggingEx)
    {
        LogInternalError(loggingEx);
    }
}



/// <summary>
/// Registra un log de SQL con error y lo encola con el INICIO real para mantener el orden cronológico.
/// </summary>
public void LogDatabaseError(DbCommand command, Exception ex, HttpContext? context = null)
{
    try
    {
        var info  = LogHelper.ExtractDbConnectionInfo(command.Connection?.ConnectionString);
        var tabla = LogHelper.ExtractTableName(command.CommandText);

        var formatted = LogFormatter.FormatDbExecutionError(
            nombreBD: info.Database,
            ip: info.Ip,
            puerto: info.Port,
            biblioteca: info.Library,
            tabla: tabla,
            sentenciaSQL: command.CommandText,
            exception: ex,
            horaError: DateTime.Now
        );

        if (context is not null)
        {
            // Preferimos el INICIO real que puso el wrapper; si no, ahora (UTC).
            var startedUtc = context.Items.TryGetValue("__SqlStartedUtc", out var o) && o is DateTime dt ? dt : DateTime.UtcNow;

            if (!context.Items.ContainsKey("HttpClientLogsTimed")) context.Items["HttpClientLogsTimed"] = new List<object>();
            if (context.Items["HttpClientLogsTimed"] is List<object> timed)
                timed.Add(new { TsUtc = startedUtc, Seq = NextSeq(context), Content = formatted });
        }
        else
        {
            WriteLog(context, formatted); // fallback sin contexto
        }

        AddExceptionLog(ex); // rastro transversal
    }
    catch (Exception fail)
    {
        LogInternalError(fail);
    }
}
var ordered = timedList
    .Select(o =>
    {
        var t  = o.GetType();
        var ts = t.GetProperty("TsUtc")?.GetValue(o);
        var sq = t.GetProperty("Seq")?.GetValue(o);
        var tx = t.GetProperty("Content")?.GetValue(o);

        DateTime tsUtc = ts is DateTime d ? d : DateTime.UtcNow; // fallback
        long seq = sq is long l ? l : long.MaxValue;             // legacy sin Seq va al final en empates
        string content = tx as string ?? string.Empty;

        return new { Ts = tsUtc, Seq = seq, Tx = content };
    })
    .OrderBy(x => x.Ts)    // primero por inicio real
    .ThenBy(x => x.Seq)    // luego por secuencia estable
    .ToList();

foreach (var e in ordered)
    _loggingService.WriteLog(context, e.Tx);

context.Items.Remove("HttpClientLogsTimed");


var startedLocal = DateTime.Now;
var startedUtc   = startedLocal.ToUniversalTime();
var ctx          = _httpContextAccessor?.HttpContext;
if (ctx is not null) ctx.Items["__SqlStartedUtc"] = startedUtc;





