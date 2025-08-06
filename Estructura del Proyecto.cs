private async Task CaptureRequestInfoAsync(HttpContext context)
{
    try
    {
        var request = context.Request;

        // Ignorar si no es JSON o es una ruta conocida que no necesita análisis
        if (request.Method != HttpMethods.Post &&
            request.Method != HttpMethods.Put &&
            request.Method != HttpMethods.Patch)
        {
            Console.WriteLine("[LOGGING] Método no procesable para body: " + request.Method);
            return;
        }

        if (!request.ContentType?.Contains("application/json", StringComparison.OrdinalIgnoreCase) ?? true)
        {
            Console.WriteLine("[LOGGING] Content-Type no es JSON: " + request.ContentType);
            return;
        }

        if (request.Path.HasValue && request.Path.Value.Contains("swagger", StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine("[LOGGING] Ruta ignorada por ser swagger: " + request.Path);
            return;
        }

        request.EnableBuffering();

        using var reader = new StreamReader(request.Body, Encoding.UTF8, leaveOpen: true);
        var body = await reader.ReadToEndAsync();

        // Restablece el stream para que el controller pueda leerlo luego
        request.Body.Position = 0;

        Console.WriteLine("[LOGGING] Procesando body JSON...");
        LogFileNameResolver.TryExtractLogFileNameFromJson(context, body);
    }
    catch (Exception ex)
    {
        Console.WriteLine("[LOGGING] Error al procesar el body JSON: " + ex.Message);
    }
}
