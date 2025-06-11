protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
{
    var stopwatch = Stopwatch.StartNew();
    var context = _httpContextAccessor.HttpContext;
    string traceId = context?.TraceIdentifier ?? Guid.NewGuid().ToString();

    try
    {
        HttpResponseMessage response = await base.SendAsync(request, cancellationToken);
        stopwatch.Stop();

        string responseBody = response.Content != null
            ? await response.Content.ReadAsStringAsync()
            : "Sin contenido";

        // 🔹 Formato del log: incluye cuerpo de respuesta bien formateado
        string formatted = LogFormatter.FormatHttpClientRequestWithResponse(
            traceId: traceId,
            method: request.Method.Method,
            url: request.RequestUri?.ToString() ?? "URI no definida",
            statusCode: ((int)response.StatusCode).ToString(),
            elapsedMs: stopwatch.ElapsedMilliseconds,
            headers: request.Headers.ToString(),
            requestBody: request.Content != null ? await request.Content.ReadAsStringAsync() : null,
            responseBody: FormatXmlPretty(responseBody)
        );

        AppendHttpClientLogToContext(context, formatted);

        return response;
    }
    catch (Exception ex)
    {
        stopwatch.Stop();

        string errorLog = LogFormatter.FormatHttpClientError(
            traceId: traceId,
            method: request.Method.Method,
            url: request.RequestUri?.ToString() ?? "URI no definida",
            exception: ex
        );

        AppendHttpClientLogToContext(context, errorLog);
        throw;
    }
}
