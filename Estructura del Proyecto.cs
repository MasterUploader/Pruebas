/// <summary>
/// Formatea la información de una solicitud HTTP realizada mediante HttpClient,
/// incluyendo método, URL, cabeceras, cuerpo de la solicitud, código de respuesta, respuesta y duración.
/// </summary>
public static string FormatHttpClientLog(
    string method,
    string url,
    IDictionary<string, string> headers,
    string? requestBody,
    HttpStatusCode statusCode,
    string? responseBody,
    long durationMs)
{
    var sb = new StringBuilder();

    sb.AppendLine("[HttpClient Request]");
    sb.AppendLine($"  Método         : {method}");
    sb.AppendLine($"  URL            : {url}");
    sb.AppendLine($"  Headers        :");

    foreach (var header in headers)
    {
        sb.AppendLine($"    - {header.Key}: {header.Value}");
    }

    sb.AppendLine($"  Cuerpo Enviado : {(string.IsNullOrWhiteSpace(requestBody) ? "[vacío]" : requestBody)}");

    sb.AppendLine();
    sb.AppendLine("[HttpClient Response]");
    sb.AppendLine($"  Código Estado  : {(int)statusCode} {statusCode}");
    sb.AppendLine($"  Cuerpo Recibido: {(string.IsNullOrWhiteSpace(responseBody) ? "[vacío]" : responseBody)}");
    sb.AppendLine($"  Duración       : {durationMs} ms");
    sb.AppendLine(new string('-', 80));

    return sb.ToString();
}
