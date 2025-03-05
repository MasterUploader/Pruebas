/// <summary>
/// Obtiene el archivo de log de la petición actual, garantizando que toda la información se guarde en el mismo archivo.
/// </summary>
private string GetCurrentLogFile()
{
    try
    {
        var context = _httpContextAccessor.HttpContext;

        // Si ya existe un archivo de log en esta petición, reutilizarlo
        if (context?.Items.ContainsKey("LogFileName") == true)
        {
            return context.Items["LogFileName"] as string;
        }

        // Generar un nuevo nombre de archivo solo si no se ha creado antes
        if (context != null && context.Items.ContainsKey("ExecutionId"))
        {
            string executionId = context.Items["ExecutionId"].ToString();
            string endpoint = context.Request.Path.ToString().Replace("/", "_").Trim('_');
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");

            string logFileName = Path.Combine(_logDirectory, $"{executionId}_{endpoint}_{timestamp}.txt");
            context.Items["LogFileName"] = logFileName; // Almacenar para reutilizar
            return logFileName;
        }
    }
    catch (Exception ex)
    {
        LogInternalError(ex);
    }

    return Path.Combine(_logDirectory, "GlobalManualLogs.txt");
}







public async Task InvokeAsync(HttpContext context)
{
    // Generar ExecutionId único por petición
    if (!context.Items.ContainsKey("ExecutionId"))
    {
        string executionId = Guid.NewGuid().ToString();
        context.Items["ExecutionId"] = executionId;
    }

    // Asegurar que se genera el archivo de log solo una vez
    _ = GetCurrentLogFile(); 

    // Capturar la información del entorno y escribirla en el log
    string environmentLog = await CaptureEnvironmentInfoAsync(context);
    _loggingService.WriteLog(context, environmentLog);

    // Capturar la información del Request y escribirla en el log
    string requestLog = await CaptureRequestInfoAsync(context);
    _loggingService.WriteLog(context, requestLog);

    var originalBodyStream = context.Response.Body;
    using (var responseBody = new MemoryStream())
    {
        context.Response.Body = responseBody;
        await _next(context); // Llamar al siguiente middleware

        // Capturar la respuesta HTTP y escribirla en el log
        string responseLog = await CaptureResponseInfoAsync(context);
        _loggingService.WriteLog(context, responseLog);

        responseBody.Seek(0, SeekOrigin.Begin);
        await responseBody.CopyToAsync(originalBodyStream);
    }

    // Capturar excepciones si ocurrieron
    if (context.Items.ContainsKey("Exception"))
    {
        Exception ex = context.Items["Exception"] as Exception;
        _loggingService.AddExceptionLog(ex);
    }
}







public override void OnActionExecuting(ActionExecutingContext context)
{
    var loggingService = context.HttpContext.RequestServices.GetRequiredService<ILoggingService>();

    // Capturar el nombre del método y los parámetros de entrada
    string methodName = $"{context.Controller.GetType().Name}.{context.ActionDescriptor.DisplayName}";
    string parameters = LogFormatter.FormatInputParameters(context.ActionArguments);

    loggingService.AddMethodEntryLog(methodName, parameters);
}

public override void OnActionExecuted(ActionExecutedContext context)
{
    var loggingService = context.HttpContext.RequestServices.GetRequiredService<ILoggingService>();

    // Capturar el nombre del método y los valores de salida
    string methodName = $"{context.Controller.GetType().Name}.{context.ActionDescriptor.DisplayName}";
    string outputParams = context.Result is ObjectResult objectResult
        ? JsonSerializer.Serialize(objectResult.Value, new JsonSerializerOptions { WriteIndented = true })
        : "Sin retorno";

    loggingService.AddMethodExitLog(methodName, outputParams);
}



