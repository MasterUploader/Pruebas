/// <summary>
/// Clave de secuencia por-request para desempatar eventos con el mismo TsUtc.
/// </summary>
private const string TimedSeqKey = "__TimedSeq";

/// <summary>
/// Devuelve un número incremental por-request. Se almacena en Items[TimedSeqKey].
/// </summary>
private static long NextSeq(HttpContext ctx)
{
    // Como Items es por-request, no necesitamos sincronización pesada aquí.
    var curr = ctx.Items.TryGetValue(TimedSeqKey, out var obj) && obj is long c ? c : 0L;
    curr++;
    ctx.Items[TimedSeqKey] = curr;
    return curr;
}
