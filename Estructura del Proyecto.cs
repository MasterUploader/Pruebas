using System.Reflection;
using System.Text.Json;
using Microsoft.AspNetCore.Http;

namespace Logging.Helpers
{
    /// <summary>
    /// Clase auxiliar para detectar automáticamente propiedades marcadas con el atributo
    /// [LogFileName] dentro del body JSON, sin conocer el tipo DTO en tiempo de compilación.
    /// </summary>
    public static class LogFileNameExtractor
    {
        /// <summary>
        /// Cache de propiedades que contienen el atributo [LogFileName], por nombre JSON (en minúsculas).
        /// </summary>
        private static readonly Dictionary<string, (string Label, PropertyInfo Prop)> _loggableProperties = new();
        private static bool _cacheBuilt = false;

        /// <summary>
        /// Intenta extraer el valor de una propiedad marcada con [LogFileName] desde el cuerpo JSON.
        /// Si se encuentra, lo almacena en context.Items["LogFileNameCustom"] para que pueda usarse en el nombre del log.
        /// </summary>
        /// <param name="context">Contexto HTTP actual</param>
        /// <param name="body">Cuerpo de la petición como texto</param>
        public static void TryExtractLogFileNameFromBody(HttpContext context, string body)
        {
            try
            {
                Console.WriteLine("[LOGGING] Iniciando extracción genérica de LogFileName...");

                if (!_cacheBuilt)
                {
                    BuildLoggablePropertyCache();
                    _cacheBuilt = true;
                    Console.WriteLine($"[LOGGING] Cache cargada con {_loggableProperties.Count} propiedades.");
                }

                var jsonDoc = JsonDocument.Parse(body);

                if (TryScanJson(jsonDoc.RootElement, out var logValue))
                {
                    context.Items["LogFileNameCustom"] = logValue;
                    Console.WriteLine($"[LOGGING] Valor log extraído: {logValue}");
                }
                else
                {
                    Console.WriteLine("[LOGGING] No se encontró ninguna coincidencia de LogFileName.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[LOGGING] Error al procesar el body JSON: {ex.Message}");
            }
        }

        /// <summary>
        /// Construye el cache con todas las propiedades en todos los tipos cargados
        /// que están decoradas con [LogFileName].
        /// </summary>
        private static void BuildLoggablePropertyCache()
        {
            var allTypes = AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => !a.IsDynamic && !a.FullName!.StartsWith("System"))
                .SelectMany(a => a.GetTypes())
                .Where(t => t.IsClass && !t.IsAbstract && !t.FullName!.StartsWith("Microsoft"));

            foreach (var type in allTypes)
            {
                foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
                {
                    var attr = prop.GetCustomAttributes(typeof(LogFileNameAttribute), true)
                                   .FirstOrDefault() as LogFileNameAttribute;

                    if (attr != null)
                    {
                        string jsonName = prop.Name;

                        // Detectar si se usa [JsonPropertyName] o [JsonProperty]
                        var jsonAttr = prop.GetCustomAttributes().FirstOrDefault(a =>
                            a.GetType().Name is "JsonPropertyNameAttribute" or "JsonPropertyAttribute");

                        if (jsonAttr != null)
                        {
                            var nameProp = jsonAttr.GetType().GetProperty("Name");
                            jsonName = nameProp?.GetValue(jsonAttr)?.ToString() ?? jsonName;
                        }

                        string key = jsonName.ToLowerInvariant();
                        if (!_loggableProperties.ContainsKey(key))
                        {
                            _loggableProperties[key] = (attr.Label ?? "", prop);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Escanea recursivamente un JsonElement para buscar coincidencias con propiedades marcadas con [LogFileName].
        /// </summary>
        private static bool TryScanJson(JsonElement element, out string? logValue)
        {
            logValue = null;

            if (element.ValueKind == JsonValueKind.Object)
            {
                foreach (var prop in element.EnumerateObject())
                {
                    string propName = prop.Name.ToLowerInvariant();

                    if (_loggableProperties.TryGetValue(propName, out var info))
                    {
                        var val = prop.Value.GetString();
                        if (!string.IsNullOrWhiteSpace(val))
                        {
                            logValue = string.IsNullOrWhiteSpace(info.Label) ? val : $"{info.Label}-{val}";
                            return true;
                        }
                    }

                    if (TryScanJson(prop.Value, out logValue))
                        return true;
                }
            }
            else if (element.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in element.EnumerateArray())
                {
                    if (TryScanJson(item, out logValue))
                        return true;
                }
            }

            return false;
        }
    }
}

LogFileNameExtractor.TryExtractLogFileNameFromBody(context, body);
