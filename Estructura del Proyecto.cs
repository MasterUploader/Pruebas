// Ordena primero por TsUtc (instante real de inicio) y luego por Seq (desempate estable).
var ordered = timedList
    .Select(o =>
    {
        var t  = o.GetType();
        var ts = t.GetProperty("TsUtc")?.GetValue(o);
        var sq = t.GetProperty("Seq")?.GetValue(o);
        var tx = t.GetProperty("Content")?.GetValue(o);

        DateTime tsUtc = ts is DateTime d ? d : DateTime.UtcNow;    // fallback seguro
        long seq = sq is long l ? l : long.MaxValue;                 // si no trae Seq (p.ej. legacy), va al final en empates
        string content = tx as string ?? string.Empty;

        return new { Ts = tsUtc, Seq = seq, Tx = content };
    })
    .OrderBy(x => x.Ts)
    .ThenBy(x => x.Seq)
    .ToList();

foreach (var e in ordered)
    _loggingService.WriteLog(context, e.Tx);

// Limpia para evitar duplicados
context.Items.Remove("HttpClientLogsTimed");




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




// En LogDatabaseSuccess(...)
if (context is not null)
{
    var startedUtc = model.StartTime.Kind == DateTimeKind.Utc
        ? model.StartTime
        : model.StartTime.ToUniversalTime();

    if (!context.Items.ContainsKey("HttpClientLogsTimed")) context.Items["HttpClientLogsTimed"] = new List<object>();
    if (context.Items["HttpClientLogsTimed"] is List<object> timed)
        timed.Add(new { TsUtc = startedUtc, Seq = NextSeq(context), Content = formatted });
}
else
{
    WriteLog(context, formatted);
}



// En LogDatabaseError(...)
if (context is not null)
{
    var startedUtc =
        context.Items.TryGetValue("__SqlStartedUtc", out var obj) && obj is DateTime dt
            ? dt
            : DateTime.UtcNow;

    if (!context.Items.ContainsKey("HttpClientLogsTimed")) context.Items["HttpClientLogsTimed"] = new List<object>();
    if (context.Items["HttpClientLogsTimed"] is List<object> timed)
        timed.Add(new { TsUtc = startedUtc, Seq = NextSeq(context), Content = formatted });
}
else
{
    WriteLog(context, formatted);
}


