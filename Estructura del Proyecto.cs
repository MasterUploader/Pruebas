/// <summary>
/// Obtiene Api y Endpoint priorizando el último segmento literal de la plantilla de ruta.
/// - Api = RouteValues["controller"] (o 1er segmento de URL)
/// - Endpoint = último literal de RoutePattern (o action, o 2º segmento de URL)
/// </summary>
private static (string ApiName, string EndpointName) GetRouteNamesPreferringTemplate(HttpContext context)
{
    // 1) API: controller si existe; si no, del path
    string? api = context.Request.RouteValues.TryGetValue("controller", out var c) ? c?.ToString() : null;

    // 2) Endpoint: intentar desde la plantilla de ruta (RoutePattern)
    string? endpointFromTemplate = null;

    if (context.GetEndpoint() is RouteEndpoint re && re.RoutePattern is not null)
    {
        // Tomar el ÚLTIMO segmento que contenga una parte literal y leer su Content
        var lastLiteralContent = ExtractLastLiteralSegment(re.RoutePattern);
        if (!string.IsNullOrWhiteSpace(lastLiteralContent))
            endpointFromTemplate = lastLiteralContent;
    }

    // 3) Fallbacks si no hubo plantilla o no hubo literal:
    if (string.IsNullOrWhiteSpace(endpointFromTemplate))
        endpointFromTemplate = context.Request.RouteValues.TryGetValue("action", out var a) ? a?.ToString() : null;

    if (string.IsNullOrWhiteSpace(api) || string.IsNullOrWhiteSpace(endpointFromTemplate))
    {
        var segments = (context.Request.Path.Value ?? string.Empty)
            .Trim('/')
            .Split('/', StringSplitOptions.RemoveEmptyEntries);

        api ??= segments.Length > 0 ? segments[0] : "UnknownApi";
        endpointFromTemplate ??= segments.Length > 1 ? segments[1] : "UnknownEndpoint";
    }

    return (api!, endpointFromTemplate!);
}

/// <summary>
/// Devuelve el contenido del último segmento literal de un <see cref="RoutePattern"/>.
/// Por ejemplo, para "/Bts/Consulta/{id}" devuelve "Consulta".
/// </summary>
private static string? ExtractLastLiteralSegment(RoutePattern pattern)
{
    // Recorre los segmentos y extrae la última parte literal con .Content
    for (int i = pattern.PathSegments.Count - 1; i >= 0; i--)
    {
        var seg = pattern.PathSegments[i];
        // Busca una parte literal en el segmento (puede haber varias partes por segmento)
        var literalPart = seg.Parts.OfType<RoutePatternLiteralPart>().LastOrDefault();
        if (literalPart is not null && !string.IsNullOrWhiteSpace(literalPart.Content))
            return literalPart.Content;
    }
    return null;
}
