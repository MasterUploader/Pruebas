private void AppendHttpClientLogToContext(HttpContext context, string logEntry)
{
    const string key = "HttpClientLogs";

    if (!context.Items.ContainsKey(key))
        context.Items[key] = new List<string>();

    if (context.Items[key] is List<string> logs)
        logs.Add(logEntry);
}
