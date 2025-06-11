/// <summary>
/// Devuelve el cuerpo formateado automáticamente como JSON o XML si es posible.
/// </summary>
/// <param name="body">El contenido de la respuesta.</param>
/// <param name="contentType">El tipo de contenido (Content-Type).</param>
public static string PrettyPrintAuto(string? body, string? contentType)
{
    if (string.IsNullOrWhiteSpace(body))
        return "[Sin contenido]";

    contentType = contentType?.ToLowerInvariant();

    try
    {
        if (contentType != null && contentType.Contains("json"))
            return PrettyPrintJson(body);

        if (contentType != null && (contentType.Contains("xml") || contentType.Contains("text/xml")))
            return PrettyPrintXml(body);

        return body;
    }
    catch
    {
        return body; // Si el formateo falla, devolver el cuerpo original
    }
}

responseBody: LogHelper.PrettyPrintAuto(responseBody, response.Content?.Headers?.ContentType?.MediaType)


    /// <summary>
/// Da formato legible a un string JSON.
/// Si no es un JSON válido, devuelve el texto original.
/// </summary>
/// <param name="json">Contenido en formato JSON.</param>
/// <returns>JSON formateado con sangrías o el texto original si falla el parseo.</returns>
public static string PrettyPrintJson(string json)
{
    if (string.IsNullOrWhiteSpace(json))
        return "[Sin contenido JSON]";

    try
    {
        using var jdoc = JsonDocument.Parse(json);
        var options = new JsonSerializerOptions
        {
            WriteIndented = true
        };

        return JsonSerializer.Serialize(jdoc.RootElement, options);
    }
    catch
    {
        return json; // Si no es JSON válido, devolverlo sin cambios
    }
}
