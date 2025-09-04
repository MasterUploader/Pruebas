// ANTES (provoca CS0051)
public static object? GetOrCreateBuffer(HttpContext? ctx, string? filePath, out RequestLogBuffer? buffer)

// DESPUÉS (método privado; ya no expone el tipo interno)
private static RequestLogBuffer? GetOrCreateBuffer(HttpContext? ctx, string? filePath)
{
    if (ctx is null) return null;

    // Determina ruta del archivo final (usa tu lógica existente si no la pasan).
    var path = string.IsNullOrWhiteSpace(filePath) ? GetPathFromContext(ctx) : filePath;

    if (ctx.Items.TryGetValue(BufferKey, out var existing) && existing is RequestLogBuffer ok)
        return ok;

    var created = new RequestLogBuffer(path!);
    ctx.Items[BufferKey] = created;
    return created;
}



public static void SetEnvironment(HttpContext ctx, string? filePath, string text)
    => GetOrCreateBuffer(ctx, filePath)?.SetEnvironment(text);

public static void SetController(HttpContext ctx, string? filePath, string text)
    => GetOrCreateBuffer(ctx, filePath)?.SetController(text);

public static void SetRequest(HttpContext ctx, string? filePath, string text)
    => GetOrCreateBuffer(ctx, filePath)?.SetRequest(text);

public static void SetResponse(HttpContext ctx, string? filePath, string text)
    => GetOrCreateBuffer(ctx, filePath)?.SetResponse(text);

public static void AddError(HttpContext ctx, string? filePath, string text)
    => GetOrCreateBuffer(ctx, filePath)?.AddError(text);

public static void AppendDynamic(HttpContext? ctx, string? filePath, DynamicLogKind kind, string text)
    => GetOrCreateBuffer(ctx!, filePath)?.AppendDynamic(kind, text);

public static void Flush(HttpContext ctx, string? filePath)
{
    var buf = GetOrCreateBuffer(ctx, filePath);
    if (buf is null) return;

    var core = buf.BuildCore();
    var path = buf.FilePath;
    var dir = Path.GetDirectoryName(path) ?? AppDomain.CurrentDomain.BaseDirectory;

    // Reutiliza tu writer existente
    WriteLogToFile(dir, path, core);
    // Opcional: CSV
    // SaveLogAsCsv(dir, path, core);
}
