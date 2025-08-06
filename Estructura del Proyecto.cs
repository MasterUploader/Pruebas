try
{
    // Deserializa a un objeto genérico (puedes usar tipos específicos si prefieres)
    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
    var jsonDoc = JsonDocument.Parse(body);

    foreach (var property in jsonDoc.RootElement.EnumerateObject())
    {
        // ⚠️ Esta parte se puede adaptar: aquí podrías buscar en un tipo fuerte si conoces el DTO
        var propName = property.Name;
        var propValue = property.Value.GetString();

        // Guarda el valor si el nombre coincide con una propiedad conocida con el atributo
        // En lugar de hacerlo así, lo ideal es usar reflexión sobre un tipo conocido
        if (!string.IsNullOrEmpty(propName) && !string.IsNullOrEmpty(propValue))
        {
            // Aquí simulas que esta propiedad tiene el atributo
            if (propName.Equals("CodigoAgencia", StringComparison.OrdinalIgnoreCase))
            {
                context.Items["LogFileNameCustom"] = $"id-{propValue}";
                break;
            }
        }
    }
}
catch
{
    // Silencioso: si falla el parsing no afecta
}

string customNamePart = "";
if (context.Items.TryGetValue("LogFileNameCustom", out var customValue) && customValue is string customStr)
{
    customNamePart = $"_{customStr}";
}
