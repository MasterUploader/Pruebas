public async Task OnActionExecutedAsync(ActionExecutedContext context)
{
    // Si la respuesta es un ObjectResult, extrae el valor y guárdalo en HttpContext.Items
    if (context.Result is ObjectResult objectResult)
    {
        context.HttpContext.Items["ResponseObject"] = objectResult.Value;
    }

    await Task.CompletedTask; // No es necesario ejecutar más lógica aquí
}



private async Task<string> CaptureResponseInfoAsync(HttpContext context)
{
    context.Response.Body.Seek(0, SeekOrigin.Begin);
    using var reader = new StreamReader(context.Response.Body, Encoding.UTF8, leaveOpen: true);
    string body = await reader.ReadToEndAsync();
    context.Response.Body.Seek(0, SeekOrigin.Begin);

    string formattedResponse;

    // Usar el objeto guardado en context.Items si existe
    if (context.Items.ContainsKey("ResponseObject"))
    {
        var responseObject = context.Items["ResponseObject"];
        formattedResponse = LogFormatter.FormatResponseInfo(
            statusCode: context.Response.StatusCode.ToString(),
            headers: string.Join("; ", context.Response.Headers),
            body: responseObject != null 
                ? JsonSerializer.Serialize(responseObject, new JsonSerializerOptions { WriteIndented = true }) 
                : "null"
        );
    }
    else
    {
        // Si no se interceptó el ObjectResult, usar el cuerpo normal
        formattedResponse = LogFormatter.FormatResponseInfo(
            statusCode: context.Response.StatusCode.ToString(),
            headers: string.Join("; ", context.Response.Headers),
            body: body
        );
    }

    return formattedResponse;
}
