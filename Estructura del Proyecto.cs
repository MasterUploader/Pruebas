/// <summary>
/// Captura la información de la respuesta HTTP antes de enviarla al cliente.
/// </summary>
/// <param name="context">Contexto HTTP de la petición actual.</param>
private async Task<string> CaptureResponseInfoAsync(HttpContext context)
{
    try
    {
        // Clonar el stream de respuesta antes de procesarlo
        context.Response.Body.Seek(0, SeekOrigin.Begin);

        using var reader = new StreamReader(context.Response.Body, Encoding.UTF8, leaveOpen: true);
        string body = await reader.ReadToEndAsync();
        
        // Restablecer el stream para que el controlador pueda leerlo
        context.Response.Body.Seek(0, SeekOrigin.Begin);

        return LogFormatter.FormatResponseInfo(
            statusCode: context.Response.StatusCode.ToString(),
            headers: string.Join("; ", context.Response.Headers),
            body: body
        );
    }
    catch (Exception ex)
    {
        _loggingService.AddExceptionLog(ex);
        return "Error al capturar la respuesta HTTP.";
    }
}




public async Task InvokeAsync(HttpContext context)
{
    // Iniciar medición del tiempo de ejecución
    var stopwatch = Stopwatch.StartNew();

    try
    {
        // Capturar información del entorno y request
        await CaptureEnvironmentInfoAsync(context);
        await CaptureRequestInfoAsync(context);

        // Guardar logs de inicio de ejecución
        _loggingService.WriteLog(context, "Inicio de ejecución");

        // Clonar el body de la respuesta
        var originalBodyStream = context.Response.Body;
        using var responseBody = new MemoryStream();
        context.Response.Body = responseBody;

        // Continuar con la ejecución del siguiente middleware
        await _next(context);

        // Capturar la respuesta HTTP después de procesar la solicitud
        string responseLog = await CaptureResponseInfoAsync(context);
        _loggingService.WriteLog(context, responseLog);

        // Copiar el contenido de vuelta al stream original
        responseBody.Seek(0, SeekOrigin.Begin);
        await responseBody.CopyToAsync(originalBodyStream);
    }
    catch (Exception ex)
    {
        _loggingService.AddExceptionLog(ex);
    }
    finally
    {
        stopwatch.Stop();
        _loggingService.AddSingleLog($"Tiempo Total de Ejecución: {stopwatch.ElapsedMilliseconds} ms");
        _loggingService.WriteLog(context, "Fin de ejecución");
    }
}
