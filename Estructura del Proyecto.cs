// 5.5 Capturar logs del HttpClient si existen
if (context.Items.TryGetValue("HttpClientLogs", out var clientLogsObj) && clientLogsObj is List<string> clientLogs)
{
    foreach (var log in clientLogs)
    {
        _loggingService.WriteLog(context, log);
    }
}
