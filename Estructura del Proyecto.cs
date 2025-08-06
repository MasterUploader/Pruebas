Esta modificación que sugieres:

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

Cambia bastante el estado actual del codigo

   /// <summary>
   /// Captura la información de la solicitud HTTP antes de que sea procesada por los controladores.
   /// </summary>
   private static async Task<string> CaptureRequestInfoAsync(HttpContext context)
   {
       context.Request.EnableBuffering(); // Permite leer el cuerpo de la petición sin afectar la ejecución

       using var reader = new StreamReader(context.Request.Body, Encoding.UTF8, leaveOpen: true);
       string body = await reader.ReadToEndAsync();
       context.Request.Body.Position = 0; // Restablece la posición para que el controlador pueda leerlo

       return LogFormatter.FormatRequestInfo(context,
           method: context.Request.Method,
           path: context.Request.Path,
           queryParams: context.Request.QueryString.ToString(),
           body: body
       );
   }

se pierde la logica del formateo y almacenamiento de LogFormatter.FormatRequestInfo, eso se debe de mantener, cualquier cambio debe ser un extra a las funciones de los metodos existentes.

