/// <summary>
/// Obtiene el valor para LogCustomPart deserializando el body al tipo REAL del parámetro del Action
/// (si hay JSON) o hidratando el DTO desde Query/Route (para GET/sin body). Guarda el resultado
/// en <c>HttpContext.Items["LogCustomPart"]</c>.
/// </summary>
private static async Task ExtractLogCustomPartFromBody(HttpContext context)
{
    string? bodyString = null;

    // Si viene JSON, lo leemos (para POST/PUT/PATCH, etc.)
    if (context.Request.ContentType?.Contains("application/json", StringComparison.OrdinalIgnoreCase) == true)
    {
        context.Request.EnableBuffering();
        using var reader = new StreamReader(context.Request.Body, Encoding.UTF8, leaveOpen: true);
        bodyString = await reader.ReadToEndAsync();
        context.Request.Body.Position = 0;
    }

    try
    {
        // 👉 El extractor soporta tanto JSON (tipado) como GET (Query/Route) si bodyString es null o vacío
        var customPart = StrongTypedLogFileNameExtractor.Extract(context, bodyString);
        if (!string.IsNullOrWhiteSpace(customPart))
        {
            context.Items["LogCustomPart"] = customPart;
        }
    }
    catch
    {
        // No interrumpir el pipeline por fallos de extracción
    }
}
