public static string FormatHttpClientRequest(
    string traceId,
    string method,
    string url,
    string statusCode,
    long elapsedMs,
    string headers,
    string? body,
    string? responseBody // <-- nuevo
)
{
    var builder = new StringBuilder();
    builder.AppendLine("============= INICIO HTTP CLIENT =============");
    builder.AppendLine($"TraceId       : {traceId}");
    builder.AppendLine($"Método        : {method}");
    builder.AppendLine($"URL           : {url}");
    builder.AppendLine($"Código Status : {statusCode}");
    builder.AppendLine($"Duración (ms) : {elapsedMs}");
    builder.AppendLine($"Encabezados   :\n{headers}");

    if (!string.IsNullOrWhiteSpace(body))
    {
        builder.AppendLine("Cuerpo:");
        builder.AppendLine(body);
    }

    if (!string.IsNullOrWhiteSpace(responseBody))
    {
        builder.AppendLine("Respuesta:");
        builder.AppendLine(responseBody);
    }

    builder.AppendLine("============= FIN HTTP CLIENT =============");
    return builder.ToString();
}

string responseBody = await response.Content.ReadAsStringAsync();

string formatted = LogFormatter.FormatHttpClientRequest(
    traceId: traceId,
    method: request.Method.Method,
    url: request.RequestUri?.ToString() ?? "Uri no definida",
    statusCode: ((int)response.StatusCode).ToString(),
    elapsedMs: stopwatch.ElapsedMilliseconds,
    headers: request.Headers.ToString(),
    body: request.Content != null ? await request.Content.ReadAsStringAsync() : null,
    responseBody: responseBody // <-- nuevo
);
