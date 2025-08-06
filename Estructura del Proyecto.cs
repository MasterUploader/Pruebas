Este es el codigo que me generaste

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
                        string logName = $"id-{value}";
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


El error esta en el if, porque espera un nombre de propiedad predefinido, pero ese nombre la debe obtener de la propiedad de LogFileNameAttribute
