private static void TryExtractLogFileNameFromBody(HttpContext context, string body)
{
    try
    {
        Console.WriteLine("[LOGGING] Iniciando extracción de LogFileName desde el body...");

        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        object? dto = JsonSerializer.Deserialize<object>(body, options);

        if (dto == null)
        {
            Console.WriteLine("[LOGGING] No se pudo deserializar el body.");
            return;
        }

        Console.WriteLine($"[LOGGING] Body deserializado exitosamente: Tipo={dto.GetType().Name}");

        if (TryFindLogFileNameValue(dto, out string? logName) && !string.IsNullOrWhiteSpace(logName))
        {
            context.Items["LogFileNameCustom"] = logName;
            Console.WriteLine($"[LOGGING] Se encontró LogFileNameCustom: {logName}");
        }
        else
        {
            Console.WriteLine("[LOGGING] No se encontró ninguna propiedad con el atributo [LogFileName].");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[LOGGING] Error al procesar el body: {ex.Message}");
    }
}

private static bool TryFindLogFileNameValue(object obj, out string? logName)
{
    logName = null;

    if (obj == null)
    {
        Console.WriteLine("[LOGGING] Objeto nulo recibido en TryFindLogFileNameValue.");
        return false;
    }

    var type = obj.GetType();
    Console.WriteLine($"[LOGGING] Analizando objeto de tipo: {type.Name}");

    if (obj is IEnumerable<object> enumerable)
    {
        Console.WriteLine("[LOGGING] Es una colección, iterando elementos...");
        foreach (var item in enumerable)
        {
            if (TryFindLogFileNameValue(item, out logName))
                return true;
        }
        return false;
    }

    foreach (var prop in type.GetProperties())
    {
        Console.WriteLine($"[LOGGING] Revisando propiedad: {prop.Name}");

        var attr = prop.GetCustomAttributes(typeof(LogFileNameAttribute), true)
                       .FirstOrDefault() as LogFileNameAttribute;
        var value = prop.GetValue(obj);

        if (attr != null)
        {
            Console.WriteLine($"[LOGGING] -> Se detectó el atributo [LogFileName(\"{attr.Label}\")] en la propiedad {prop.Name}");

            if (value is string strValue && !string.IsNullOrWhiteSpace(strValue))
            {
                logName = string.IsNullOrWhiteSpace(attr.Label)
                    ? strValue
                    : $"{attr.Label}-{strValue}";

                Console.WriteLine($"[LOGGING] -> Valor válido encontrado: {logName}");
                return true;
            }
            else
            {
                Console.WriteLine("[LOGGING] -> Valor nulo o vacío, ignorando...");
            }
        }

        // Buscar recursivamente en objetos complejos
        if (value != null && value.GetType().IsClass && value.GetType() != typeof(string))
        {
            Console.WriteLine($"[LOGGING] -> Propiedad {prop.Name} es un objeto complejo, analizando internamente...");
            if (TryFindLogFileNameValue(value, out logName))
                return true;
        }
    }

    return false;
}
