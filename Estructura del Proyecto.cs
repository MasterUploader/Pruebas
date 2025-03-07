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
