public async Task InvokeAsync(HttpContext context)
{
    try
    {
        var pathLower = context.Request.Path.Value?.ToLowerInvariant() ?? "";
        if (pathLower.StartsWith("/swagger") || pathLower.Contains("favicon") || pathLower.Contains("health"))
        {
            await _next(context);
            return;
        }

        _stopwatch = Stopwatch.StartNew();

        if (!context.Items.ContainsKey("ExecutionId"))
            context.Items["ExecutionId"] = Guid.NewGuid().ToString();

        // ✅ 1) PRE-EXTRACCIÓN (antes de cualquier WriteLog)
        string? preBody = null;
        try
        {
            if (!(string.Equals(context.Request.Method, "GET", StringComparison.OrdinalIgnoreCase) && 
                  (context.Request.ContentLength ?? 0) == 0))
            {
                context.Request.EnableBuffering();
                using var r = new StreamReader(context.Request.Body, Encoding.UTF8, leaveOpen: true);
                preBody = await r.ReadToEndAsync();
                context.Request.Body.Position = 0;
            }

            var preCustom = LogFileNameExtractor.ExtractLogFileNameFromContext(context, preBody);
            Console.WriteLine($"[LOGGING] (PRE) LogCustomPart: '{preCustom ?? "(null)"}'");
            if (!string.IsNullOrWhiteSpace(preCustom))
                context.Items["LogCustomPart"] = preCustom;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[LOGGING] (PRE) Error extrayendo LogCustomPart: {ex.Message}");
        }

        // ✅ 2) Ahora sí, escribir logs (ya existe LogCustomPart si aplica)
        var envLog = await CaptureEnvironmentInfoAsync(context);
        _loggingService.WriteLog(context, envLog);

        var requestLog = await CaptureRequestInfoAsync(context); // <- quita la extracción aquí
        _loggingService.WriteLog(context, requestLog);

        var originalBodyStream = context.Response.Body;
        using var responseBody = new MemoryStream();
        context.Response.Body = responseBody;

        await _next(context);

        if (context.Items.TryGetValue("HttpClientLogs", out var clientLogsObj) && clientLogsObj is List<string> clientLogs)
            foreach (var log in clientLogs) _loggingService.WriteLog(context, log);

        var responseLog = await CaptureResponseInfoAsync(context);
        _loggingService.WriteLog(context, responseLog);

        responseBody.Seek(0, SeekOrigin.Begin);
        await responseBody.CopyToAsync(originalBodyStream);
    }
    catch (Exception ex)
    {
        _loggingService.AddExceptionLog(ex);
    }
    finally
    {
        _stopwatch.Stop();
        _logging_service.WriteLog(context, $"[Tiempo Total de Ejecución]: {_stopwatch.ElapsedMilliseconds} ms");
    }
}

public string GetCurrentLogFile()
{
    try
    {
        var context = _http_contextAccessor.HttpContext;
        if (context is not null)
        {
            if (context.Items.TryGetValue("LogFileName", out var existing) && existing is string existingPath)
            {
                Console.WriteLine($"[LOGGING] GetCurrentLogFile() reuse: {existingPath}");
                return existingPath;
            }

            string rawPath = context.Request.Path.Value?.Trim('/') ?? "Unknown/Unknown";
            string endpoint = rawPath.Split('/').LastOrDefault() ?? "UnknownEndpoint";
            var cad = context.GetEndpoint()?.Metadata
                .OfType<Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor>()
                .FirstOrDefault();
            string controllerName = cad?.ControllerName ?? "UnknownController";

            string fecha = DateTime.UtcNow.ToString("yyyy-MM-dd");
            string timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
            string executionId = context.Items["ExecutionId"]?.ToString() ?? Guid.NewGuid().ToString();

            string? customPart = null;
            if (context.Items.TryGetValue("LogCustomPart", out var partObj) && partObj is string partValue && !string.IsNullOrWhiteSpace(partValue))
                customPart = partValue;

            Console.WriteLine($"[LOGGING] GetCurrentLogFile() customPart='{customPart ?? "(null)"}'");

            string finalDirectory = Path.Combine(_logDirectory, controllerName, endpoint, fecha);
            Directory.CreateDirectory(finalDirectory);

            string fileName = $"{executionId}_{endpoint}{(string.IsNullOrWhiteSpace(customPart) ? "" : "_" + customPart)}_{timestamp}.txt";
            string fullPath = Path.Combine(finalDirectory, fileName);

            Console.WriteLine($"[LOGGING] GetCurrentLogFile() path='{fullPath}'");
            context.Items["LogFileName"] = fullPath;
            return fullPath;
        }
    }
    catch (Exception ex)
    {
        LogInternalError(ex);
    }
    return Path.Combine(_logDirectory, "GlobalManualLogs.txt");
}


// Al inicio de GetCurrentLogFile()
if (context.Items.TryGetValue("LogFileName", out var existingObj) && existingObj is string existingPath)
{
    if (context.Items.TryGetValue("LogCustomPart", out var partObj) && partObj is string part && !string.IsNullOrWhiteSpace(part))
    {
        // Si el path anterior no contiene el custom, regenerar y reemplazar
        if (!existingPath.Contains($"_{part}_", StringComparison.OrdinalIgnoreCase))
        {
            context.Items.Remove("LogFileName"); // fuerza regeneración más abajo
        }
    }
}
