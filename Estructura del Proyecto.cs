/// <summary>
/// Formatea la información de una solicitud HTTP externa realizada mediante HttpClient.
/// </summary>
public static string FormatHttpClientRequest(
    string traceId,
    string method,
    string url,
    string statusCode,
    long elapsedMs,
    string headers,
    string? body)
{
    var builder = new StringBuilder();

    builder.AppendLine();
    builder.AppendLine("========== INICIO HTTP CLIENT ==========");
    builder.AppendLine($"TraceId       : {traceId}");
    builder.AppendLine($"Método        : {method}");
    builder.AppendLine($"URL           : {url}");
    builder.AppendLine($"Código Status : {statusCode}");
    builder.AppendLine($"Duración (ms) : {elapsedMs}");
    builder.AppendLine("Encabezados   :");
    builder.AppendLine(headers.Trim());

    if (!string.IsNullOrWhiteSpace(body))
    {
        builder.AppendLine("Cuerpo:");
        builder.AppendLine(body.Trim());
    }

    builder.AppendLine("=========== FIN HTTP CLIENT ============");
    return builder.ToString();
}

/// <summary>
/// Formatea la información de error ocurrida durante una solicitud con HttpClient.
/// </summary>
public static string FormatHttpClientError(
    string traceId,
    string method,
    string url,
    Exception exception)
{
    var builder = new StringBuilder();

    builder.AppendLine();
    builder.AppendLine("======= ERROR HTTP CLIENT =======");
    builder.AppendLine($"TraceId     : {traceId}");
    builder.AppendLine($"Método      : {method}");
    builder.AppendLine($"URL         : {url}");
    builder.AppendLine($"Excepción   : {exception.Message}");
    builder.AppendLine($"StackTrace  : {exception.StackTrace}");
    builder.AppendLine("=================================");

    return builder.ToString();
}
