public string GetCurrentLogFile(HttpContext context)
{
    // 🔹 Validación inicial para regenerar si falta el custom part
    if (context.Items.TryGetValue("LogFileName", out var existingObj) && existingObj is string existingPath)
    {
        if (context.Items.TryGetValue("LogCustomPart", out var partObj) && partObj is string part && !string.IsNullOrWhiteSpace(part))
        {
            // Si el nombre de archivo actual no contiene el custom part, lo forzamos a regenerar
            if (!existingPath.Contains($"{part}", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine($"[LOGGING] Forzando regeneración del log file: no contiene '{part}' en '{existingPath}'");
                context.Items.Remove("LogFileName");
            }
        }
    }

    // 🔹 Si ya existe, lo devolvemos directamente
    if (context.Items.TryGetValue("LogFileName", out var logFile) && logFile is string path && !string.IsNullOrWhiteSpace(path))
    {
        return path;
    }

    // 📌 Aquí sigue tu lógica existente para construir el nombre base
    var executionId = context.Items.TryGetValue("ExecutionId", out var execObj) ? execObj?.ToString() : Guid.NewGuid().ToString();
    var endpointName = context.GetEndpoint()?.DisplayName?.Replace(".", "_").Replace(" ", "_") ?? "UnknownEndpoint";
    var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");

    // 🔹 Agregamos la parte personalizada si existe
    string customPart = "";
    if (context.Items.TryGetValue("LogCustomPart", out var partValue) && partValue is string partStr && !string.IsNullOrWhiteSpace(partStr))
    {
        customPart = $"_{partStr}";
        Console.WriteLine($"[LOGGING] Incluyendo LogCustomPart en nombre: {customPart}");
    }

    // 🔹 Nombre final del archivo
    var fileName = $"{executionId}_{endpointName}{customPart}_{timestamp}.txt";

    // 🔹 Guardamos en Items para reutilizar en la misma request
    context.Items["LogFileName"] = fileName;

    Console.WriteLine($"[LOGGING] Archivo de log generado: {fileName}");

    return fileName;
}
