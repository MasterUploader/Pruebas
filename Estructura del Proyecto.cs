public string GetCurrentLogFile()
{
    try
    {
        var context = _httpContextAccessor.HttpContext;

        if (context is not null && context.Items.ContainsKey("LogFileName") && context.Items["LogFileName"] is string logFileName)
        {
            return logFileName;
        }

        if (context is not null && context.Items.ContainsKey("ExecutionId"))
        {
            string executionId = context.Items["ExecutionId"]?.ToString() ?? Guid.NewGuid().ToString();
            string endpoint = context.Request?.Path.ToString().Replace("/", "_").Trim('/') ?? "UnknownEndpoint";
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");

            string newLogFileName = Path.Combine(_logDirectory, $"{executionId}_{endpoint}_{timestamp}.txt");
            context.Items["LogFileName"] = newLogFileName;
            return newLogFileName;
        }
    }
    catch (Exception ex)
    {
        LogInternalError(ex);
    }

    return Path.Combine(_logDirectory, "GlobalManualLogs.txt");
}
