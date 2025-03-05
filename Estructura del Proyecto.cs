private async Task<string> CaptureRequestInfoAsync(HttpContext context)
{
    try
    {
        var sb = new StringBuilder();

        sb.AppendLine("---------------------------Request Info---------------------------");
        sb.AppendLine($"Inicio: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine("-------------------------------------------------------------------");
        sb.AppendLine($"Método: {context.Request.Method}");
        sb.AppendLine($"Ruta: {context.Request.Path}");

        if (context.Request.QueryString.HasValue)
        {
            sb.AppendLine($"Query Params: {context.Request.QueryString}");
        }

        // Leer el cuerpo de la solicitud
        context.Request.EnableBuffering();
        using (var reader = new StreamReader(context.Request.Body, Encoding.UTF8, leaveOpen: true))
        {
            string body = await reader.ReadToEndAsync();
            context.Request.Body.Position = 0;
            sb.AppendLine($"Cuerpo:\n{body}");
        }

        sb.AppendLine("---------------------------Request Info---------------------------");
        sb.AppendLine($"Fin: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine("-------------------------------------------------------------------");

        return sb.ToString();
    }
    catch (Exception ex)
    {
        LogInternalError(ex);
        return "Error capturando Request Info";
    }
}

private async Task<string> CaptureResponseInfoAsync(HttpContext context)
{
    try
    {
        var sb = new StringBuilder();

        sb.AppendLine("---------------------------Response Info---------------------------");
        sb.AppendLine($"Inicio: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine("-------------------------------------------------------------------");
        sb.AppendLine($"Código de Estado: {context.Response.StatusCode}");

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        using (var reader = new StreamReader(context.Response.Body, Encoding.UTF8, leaveOpen: true))
        {
            string responseBody = await reader.ReadToEndAsync();
            context.Response.Body.Seek(0, SeekOrigin.Begin);
            sb.AppendLine($"Cuerpo:\n{responseBody}");
        }

        sb.AppendLine("---------------------------Response Info---------------------------");
        sb.AppendLine($"Fin: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine("-------------------------------------------------------------------");

        return sb.ToString();
    }
    catch (Exception ex)
    {
        LogInternalError(ex);
        return "Error capturando Response Info";
    }
}
