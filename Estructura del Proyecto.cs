using Logging.Attributes;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Controllers;
using System.Reflection;
using System.Text.Json;

namespace Logging.Helpers
{
    public static class LogFileNameExtractor
    {
        /// <summary>
        /// Extrae valores de propiedades marcadas con [LogFileName] desde el body JSON, query string o route values.
        /// Soporta propiedades anidadas.
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

                    // 1️⃣ Propiedades raíz
                    foreach (var prop in paramType.GetProperties())
                    {
                        var attr = prop.GetCustomAttribute<LogFileNameAttribute>();
                        if (attr != null)
                        {
                            Console.WriteLine($"[LOGGING] ✅ Atributo encontrado en propiedad raíz: {prop.Name}");
                            var value = TryGetValue(prop.Name, context, requestBody);
                            Console.WriteLine($"[LOGGING] Valor para {prop.Name}: '{value ?? "(null)"}'");
                            AddPart(parts, attr, value);
                        }

                        // 2️⃣ Propiedades anidadas
                        foreach (var subProp in prop.PropertyType.GetProperties())
                        {
                            var nestedAttr = subProp.GetCustomAttribute<LogFileNameAttribute>();
                            if (nestedAttr != null)
                            {
                                Console.WriteLine($"[LOGGING] ✅ Atributo encontrado en propiedad anidada: {prop.Name}.{subProp.Name}");
                                var value = TryGetValue(subProp.Name, context, requestBody);
                                Console.WriteLine($"[LOGGING] Valor para {prop.Name}.{subProp.Name}: '{value ?? "(null)"}'");
                                AddPart(parts, nestedAttr, value);
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

        /// <summary>
        /// Busca un valor por nombre de propiedad en body JSON (case-insensitive), query string y route values.
        /// </summary>
        private static string? TryGetValue(string key, HttpContext context, string? body)
        {
            // 1️⃣ Body JSON
            var valueFromBody = TryGetValueFromBody(body, context, key);
            if (!string.IsNullOrWhiteSpace(valueFromBody))
                return valueFromBody;

            // 2️⃣ Query string
            if (context.Request.Query.TryGetValue(key, out var queryValue) && !string.IsNullOrWhiteSpace(queryValue))
                return queryValue.ToString();

            // 3️⃣ Route values
            if (context.Request.RouteValues.TryGetValue(key, out var routeValue) && routeValue != null)
                return routeValue.ToString();

            return null;
        }

        /// <summary>
        /// Busca un valor en el JSON del body de forma case-insensitive.
        /// Soporta propiedades raíz y un nivel anidado.
        /// </summary>
        private static string? TryGetValueFromBody(string? body, HttpContext context, string key)
        {
            if (string.IsNullOrWhiteSpace(body)) return null;
            if (!(context.Request.ContentType?.Contains("application/json", StringComparison.OrdinalIgnoreCase) ?? false))
                return null;

            try
            {
                var json = JsonSerializer.Deserialize<JsonElement>(body);
                if (json.ValueKind != JsonValueKind.Object) return null;

                return TryGetValueCaseInsensitive(json, key);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[LOGGING] ⚠️ Error al leer JSON del body: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Busca en un JsonElement por nombre de propiedad sin importar mayúsculas/minúsculas.
        /// Soporta nivel raíz y un nivel anidado.
        /// </summary>
        private static string? TryGetValueCaseInsensitive(JsonElement obj, string key)
        {
            // Nivel raíz
            foreach (var p in obj.EnumerateObject())
                if (string.Equals(p.Name, key, StringComparison.OrdinalIgnoreCase))
                    return p.Value.ToString();

            // Un nivel anidado
            foreach (var p in obj.EnumerateObject())
            {
                if (p.Value.ValueKind == JsonValueKind.Object)
                {
                    foreach (var p2 in p.Value.EnumerateObject())
                        if (string.Equals(p2.Name, key, StringComparison.OrdinalIgnoreCase))
                            return p2.Value.ToString();
                }
            }

            return null;
        }
    }
}
