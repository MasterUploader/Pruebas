/// <summary>
/// Captura la información de la solicitud HTTP antes de que sea procesada por los controladores.
/// </summary>
private static async Task<string> CaptureRequestInfoAsync(HttpContext context)
{
    Console.WriteLine("[LOGGING] CaptureRequestInfoAsync");

    context.Request.EnableBuffering(); // Permite leer el cuerpo de la petición sin afectar la ejecución

    string body = "";

    try
    {
        using var reader = new StreamReader(context.Request.Body, Encoding.UTF8, leaveOpen: true);
        body = await reader.ReadToEndAsync();
        context.Request.Body.Position = 0;

        if (!string.IsNullOrWhiteSpace(body))
        {
            LogFileNameExtractors.TryExtractLogFileNameFromBody(context, body);
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[LOGGING] Error al leer body: {ex.Message}");
        // Continúa sin cortar el flujo
    }

    return LogFormatter.FormatRequestInfo(
        context,
        method: context.Request.Method,
        path: context.Request.Path,
        queryParams: context.Request.QueryString.ToString(),
        body: body
    );
}

/// <summary>
/// Captura la información de la respuesta HTTP antes de enviarla al cliente.
/// </summary>
private static async Task<string> CaptureResponseInfoAsync(HttpContext context)
{
    try
    {
        if (context.Response.Body.CanSeek)
        {
            context.Response.Body.Seek(0, SeekOrigin.Begin);

            using var reader = new StreamReader(context.Response.Body, Encoding.UTF8, leaveOpen: true);
            string body = await reader.ReadToEndAsync();

            context.Response.Body.Seek(0, SeekOrigin.Begin);

            return LogFormatter.FormatResponseInfo(context, body);
        }
        else
        {
            Console.WriteLine("[LOGGING] El stream de respuesta no es seekable.");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[LOGGING] Error al leer la respuesta: {ex.Message}");
    }

    return LogFormatter.FormatResponseInfo(context, string.Empty);
}
