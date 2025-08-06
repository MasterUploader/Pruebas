private static void TryExtractLogFileNameFromBody(HttpContext context, string body)
{
    try
    {
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        // Deserializa como tipo dinámico (objeto genérico)
        object? dto = JsonSerializer.Deserialize<object>(body, options);
        if (dto == null) return;

        // Recorrer recursivamente todas las propiedades para encontrar la que tenga [LogFileName]
        if (TryFindLogFileNameValue(dto, out string? logName) && !string.IsNullOrWhiteSpace(logName))
        {
            context.Items["LogFileNameCustom"] = logName;
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[LOGGING] Error al procesar body: {ex.Message}");
    }
}

private static bool TryFindLogFileNameValue(object obj, out string? logName)
{
    logName = null;

    if (obj == null)
        return false;

    Type type = obj.GetType();

    // Si es una lista o array, recorrer elementos
    if (obj is IEnumerable<object> enumerable)
    {
        foreach (var item in enumerable)
        {
            if (TryFindLogFileNameValue(item, out logName))
                return true;
        }
        return false;
    }

    // Recorre propiedades del objeto
    foreach (var prop in type.GetProperties())
    {
        var attr = prop.GetCustomAttributes(typeof(LogFileNameAttribute), true).FirstOrDefault() as LogFileNameAttribute;
        var value = prop.GetValue(obj);

        // Si la propiedad tiene el atributo, y el valor es string
        if (attr != null && value is string strValue && !string.IsNullOrWhiteSpace(strValue))
        {
            logName = string.IsNullOrWhiteSpace(attr.Label)
                ? strValue
                : $"{attr.Label}-{strValue}";
            return true;
        }

        // Si la propiedad es compleja (objeto anidado), buscar recursivamente
        if (value != null && value.GetType().IsClass && value.GetType() != typeof(string))
        {
            if (TryFindLogFileNameValue(value, out logName))
                return true;
        }
    }

    return false;
}
