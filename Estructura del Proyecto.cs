/// <summary>
/// Construye la ruta dinámica para guardar logs basada en el contexto HTTP.
/// Estructura: Logs/ApiName/Controlador/Endpoint/AAAA-MM-DD/Archivo.txt
/// </summary>
/// <param name="context">Contexto HTTP actual (puede ser null).</param>
/// <returns>Ruta absoluta del archivo de log.</returns>
private static string GetPathFromContext(HttpContext? context)
{
    string basePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");

    string apiName = AppDomain.CurrentDomain.FriendlyName;
    string controller = "UnknownController";
    string endpoint = "UnknownEndpoint";
    string fecha = DateTime.UtcNow.ToString("yyyy-MM-dd");
    string traceId = context?.TraceIdentifier ?? Guid.NewGuid().ToString();

    if (context != null)
    {
        var path = context.Request.Path.Value?.Trim('/'); // Ej: "Agencia/Actualizar"

        if (!string.IsNullOrEmpty(path))
        {
            var parts = path.Split('/');
            if (parts.Length > 0) controller = parts[0];
            if (parts.Length > 1) endpoint = parts[1];
        }
    }

    // Carpeta: Logs/ApiName/Controlador/Endpoint/AAAA-MM-DD/
    string directoryPath = Path.Combine(basePath, apiName, controller, endpoint, fecha);

    string fileName = $"{traceId}_{fecha}.txt";
    return Path.Combine(directoryPath, fileName);
}
