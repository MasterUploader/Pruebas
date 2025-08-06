private static async Task<string> CaptureRequestInfoAsync(HttpContext context)
{
    context.Request.EnableBuffering(); // Permite leer el cuerpo de la petición sin afectar la ejecución

    using var reader = new StreamReader(context.Request.Body, Encoding.UTF8, leaveOpen: true);
    string body = await reader.ReadToEndAsync();
    context.Request.Body.Position = 0; // Restablece la posición para que el controlador pueda leerlo

    // ✅ Mantener el formateo y almacenamiento actual
    string formattedRequest = LogFormatter.FormatRequestInfo(context,
        method: context.Request.Method,
        path: context.Request.Path,
        queryParams: context.Request.QueryString.ToString(),
        body: body
    );

    // ✅ Agregar lógica adicional SIN modificar el comportamiento actual
    TryExtractLogFileNameFromBody(context, body);

    return formattedRequest;
}

private static void TryExtractLogFileNameFromBody(HttpContext context, string body)
{
    try
    {
        var jsonDoc = JsonDocument.Parse(body);

        foreach (var property in jsonDoc.RootElement.EnumerateObject())
        {
            // Aquí puedes personalizar el nombre del campo si quieres que sea más flexible
            if (property.Name.Equals("CodigoAgencia", StringComparison.OrdinalIgnoreCase))
            {
                var value = property.Value.GetString();
                if (!string.IsNullOrWhiteSpace(value))
                {
                    context.Items["LogFileNameCustom"] = $"id-{value}";
                    break;
                }
            }
        }
    }
    catch
    {
        // No interrumpas si falla la lectura
    }
}
