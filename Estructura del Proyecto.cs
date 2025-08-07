using Logging.Attributes;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Controllers;
using System.Reflection;
using System.Text.Json;

namespace Logging.Helpers;

public static class LogFileNameExtractor
{
    /// <summary>
    /// Extrae el valor o valores de propiedades marcadas con [LogFileName] desde el cuerpo,
    /// query string o route values. Soporta propiedades anidadas.
    /// </summary>
    public static string? ExtractLogFileNameFromContext(HttpContext context, string? requestBody = null)
    {
        var parts = new List<string>();

        try
        {
            var endpoint = context.GetEndpoint();
            var actionDescriptor = endpoint?.Metadata.GetMetadata<ControllerActionDescriptor>();
            if (actionDescriptor == null)
            {
                Console.WriteLine("[LOGGING] ❌ No se pudo obtener ControllerActionDescriptor.");
                return null;
            }

            Console.WriteLine($"[LOGGING] Controlador: {actionDescriptor.ControllerName}, Acción: {actionDescriptor.ActionName}");

            foreach (var param in actionDescriptor.Parameters)
            {
                Console.WriteLine($"[LOGGING] Parámetro: {param.Name}, Tipo: {param.ParameterType.Name}");
                var paramType = param.ParameterType;

                if (paramType == null)
                    continue;

                // 1️⃣ Propiedades directas
                foreach (var prop in paramType.GetProperties())
                {
                    var attr = prop.GetCustomAttribute<LogFileNameAttribute>();
                    if (attr != null)
                    {
                        Console.WriteLine($"[LOGGING] ✅ Atributo encontrado en propiedad raíz: {prop.Name}");
                        AddPart(parts, attr, TryGetValue(prop.Name, context, requestBody));
                    }

                    // 2️⃣ Propiedades anidadas
                    foreach (var subProp in prop.PropertyType.GetProperties())
                    {
                        var nestedAttr = subProp.GetCustomAttribute<LogFileNameAttribute>();
                        if (nestedAttr != null)
                        {
                            Console.WriteLine($"[LOGGING] ✅ Atributo encontrado en propiedad anidada: {prop.Name}.{subProp.Name}");
                            AddPart(parts, nestedAttr, TryGetValue(subProp.Name, context, requestBody));
                        }
                    }
                }
            }

            if (parts.Count > 0)
            {
                string result = string.Join("_", parts);
                Console.WriteLine($"[LOGGING] ✅ Resultado final del LogCustomPart: {result}");
                return result;
            }

            Console.WriteLine("[LOGGING] ❌ No se encontró ninguna propiedad con [LogFileName]");
            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[LOGGING] ❌ Error en LogFileNameExtractor: {ex}");
            return null;
        }
    }

    private static void AddPart(List<string> parts, LogFileNameAttribute attr, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            parts.Add(!string.IsNullOrWhiteSpace(attr.Label)
                ? $"{attr.Label}-{value}"
                : value);
        }
    }

    private static string? TryGetValue(string key, HttpContext context, string? body)
    {
        // 🔍 1. Desde el JSON del body (si está disponible)
        if (!string.IsNullOrWhiteSpace(body) &&
            context.Request.ContentType?.Contains("application/json", StringComparison.OrdinalIgnoreCase) == true)
        {
            try
            {
                var json = JsonSerializer.Deserialize<JsonElement>(body);
                if (json.ValueKind == JsonValueKind.Object)
                {
                    if (json.TryGetProperty(key, out var valueProp))
                        return valueProp.ToString();

                    // 🔁 Búsqueda en objetos anidados
                    foreach (var prop in json.EnumerateObject())
                    {
                        if (prop.Value.ValueKind == JsonValueKind.Object &&
                            prop.Value.TryGetProperty(key, out var nestedValue))
                        {
                            return nestedValue.ToString();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[LOGGING] ⚠️ Error al leer JSON del body: {ex.Message}");
            }
        }

        // 🔍 2. Desde query string
        if (context.Request.Query.TryGetValue(key, out var queryValue))
            return queryValue.ToString();

        // 🔍 3. Desde route values
        if (context.Request.RouteValues.TryGetValue(key, out var routeValue))
            return routeValue?.ToString();

        return null;
    }
}
