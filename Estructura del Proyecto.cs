public async Task InvokeAsync(HttpContext context)
{
    try
    {
        if (!context.Items.ContainsKey("ExecutionId"))
        {
            string executionId = Guid.NewGuid().ToString();
            context.Items["ExecutionId"] = executionId;
        }

        _ = _loggingService.GetCurrentLogFile();

        string environmentLog = await CaptureEnvironmentInfoAsync(context);
        _loggingService.WriteLog(context, environmentLog);

        string requestLog = await CaptureRequestInfoAsync(context);
        _loggingService.WriteLog(context, requestLog);

        var originalBodyStream = context.Response.Body;
        using (var responseBody = new MemoryStream())
        {
            context.Response.Body = responseBody;

            await _next(context); // Llamar al siguiente middleware

            string responseLog = await CaptureResponseInfoAsync(context);
            _loggingService.WriteLog(context, responseLog);

            responseBody.Seek(0, SeekOrigin.Begin);
            await responseBody.CopyToAsync(originalBodyStream);
        }

        if (context.Items.ContainsKey("Exception"))
        {
            Exception ex = context.Items["Exception"] as Exception;
            _loggingService.AddExceptionLog(ex);
        }
    }
    catch (Exception ex)
    {
        _loggingService.AddExceptionLog(ex);
    }
}
