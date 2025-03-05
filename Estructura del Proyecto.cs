/// <summary>
/// Captura la información del entorno del servidor y del cliente.
/// </summary>
private async Task<string> CaptureEnvironmentInfoAsync(HttpContext context)
{
    return LogFormatter.FormatEnvironmentInfoStart(
        application: context.RequestServices.GetService<IHostEnvironment>()?.ApplicationName ?? "Desconocido",
        env: context.RequestServices.GetService<IHostEnvironment>()?.EnvironmentName ?? "Desconocido",
        contentRoot: context.RequestServices.GetService<IHostEnvironment>()?.ContentRootPath ?? "Desconocido",
        executionId: context.TraceIdentifier ?? "Desconocido",
        clientIp: context.Connection.RemoteIpAddress?.ToString() ?? "Desconocido",
        userAgent: context.Request.Headers["User-Agent"].ToString() ?? "Desconocido",
        machineName: Environment.MachineName,
        os: Environment.OSVersion.ToString(),
        host: context.Request.Host.ToString() ?? "Desconocido",
        distribution: "N/A"
    );
}



public async Task InvokeAsync(HttpContext context)
{
    string executionId = Guid.NewGuid().ToString();
    context.Items["ExecutionId"] = executionId; // Guardamos el ID en Items para trazabilidad

    // 1️⃣ Capturar la información del entorno y escribirla en el log
    string environmentLog = await CaptureEnvironmentInfoAsync(context);
    _loggingService.WriteLog(context, environmentLog);

    // 2️⃣ Capturar la información del Request y escribirla en el log
    string requestLog = await CaptureRequestInfoAsync(context);
    _loggingService.WriteLog(context, requestLog);

    var originalBodyStream = context.Response.Body;
    using (var responseBody = new MemoryStream())
    {
        context.Response.Body = responseBody;

        await _next(context); // Llamar al siguiente middleware

        // 3️⃣ Capturar la respuesta HTTP y escribirla en el log
        string responseLog = await CaptureResponseInfoAsync(context);
        _loggingService.WriteLog(context, responseLog);

        // Restaurar el cuerpo de la respuesta
        responseBody.Seek(0, SeekOrigin.Begin);
        await responseBody.CopyToAsync(originalBodyStream);
    }

    // 4️⃣ Capturar las excepciones si ocurrieron
    if (context.Items.ContainsKey("Exception"))
    {
        Exception ex = context.Items["Exception"] as Exception;
        _loggingService.AddExceptionLog(ex);
    }
}




public void WriteLog(HttpContext context, string logContent)
{
    try
    {
        string filePath = GetCurrentLogFile();
        bool isNewFile = !File.Exists(filePath);

        var logBuilder = new StringBuilder();

        // Si es la primera vez que escribimos en este archivo, agregamos la cabecera
        if (isNewFile)
        {
            logBuilder.AppendLine(LogFormatter.FormatBeginLog());
        }

        // Agregamos el contenido del log
        logBuilder.AppendLine(logContent);

        // Si es la última entrada del log, agregamos el cierre
        if (context.Response.HasStarted)
        {
            logBuilder.AppendLine(LogFormatter.FormatEndLog());
        }

        // Guardamos el log en el archivo
        LogHelper.WriteLogToFile(_logDirectory, filePath, logBuilder.ToString());
    }
    catch (Exception ex)
    {
        LogInternalError(ex);
    }
}
