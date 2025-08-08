using System.Reflection;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Controllers;
using Logging.Attributes;

namespace Logging.Helpers
{
    /// <summary>
    /// Extrae valores para el nombre del log a partir de propiedades marcadas con <see cref="LogFileNameAttribute"/>.
    /// - Para cuerpos JSON (POST/PUT/PATCH): deserializa al tipo real del Action y recorre recursivamente.
    /// - Para GET (o cuando no hay body): instancia el tipo real y lo hidrata desde Query/Route (case-insensitive),
    ///   luego recorre recursivamente.
    /// Soporta DTOs complejos, genéricos y colecciones (se limita a los primeros 5 elementos).
    /// </summary>
    public static class StrongTypedLogFileNameExtractor
    {
        /// <summary>
        /// Ejecuta la extracción de valores marcados con <see cref="LogFileNameAttribute"/> para la request actual.
        /// </summary>
        /// <param name="context">HttpContext actual.</param>
        /// <param name="body">Cuerpo de la petición en texto (si existe).</param>
        /// <returns>Cadenas concatenadas por '_' con los valores encontrados (incluye Label del atributo), o null.</returns>
        public static string? Extract(HttpContext context, string? body)
        {
            var cad = context.GetEndpoint()?.Metadata.GetMetadata<ControllerActionDescriptor>();
            if (cad == null) return null;

            var parts = new List<string>();
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            bool isJsonBody = !string.IsNullOrWhiteSpace(body) &&
                              (context.Request.ContentType?.Contains("application/json", StringComparison.OrdinalIgnoreCase) ?? false);

            foreach (var p in cad.Parameters)
            {
                var paramType = p.ParameterType;
                if (paramType == null) continue;

                object? instance = null;

                // 1) Si hay JSON válido, deserializa al tipo REAL (soporta genéricos)
                if (isJsonBody)
                {
                    try { instance = JsonSerializer.Deserialize(body!, paramType, options); }
                    catch { /* ignorar y caer a GET/Query binding */ }
                }

                // 2) Si no hay body válido (GET / sin body), hidratar desde Query/Route
                if (instance == null)
                {
                    try
                    {
                        instance = Activator.CreateInstance(paramType);
                        if (instance != null)
                            BindFromQueryAndRoute(instance, context);
                    }
                    catch { instance = null; }
                }

                if (instance != null)
                    CollectLogNameParts(instance, parts);
            }

            return parts.Count > 0 ? string.Join("_", parts) : null;
        }

        /// <summary>
        /// Recorre recursivamente un objeto para agregar a <paramref name="parts"/> los valores de propiedades
        /// marcadas con <see cref="LogFileNameAttribute"/>.
        /// </summary>
        private static void CollectLogNameParts(object obj, List<string> parts, int depth = 0)
        {
            if (obj == null || depth > 8) return;

            var t = obj.GetType();

            // Evitar ciclos / tipos terminales
            if (IsTerminal(t)) return;

            // Colecciones: procesar algunos elementos
            if (typeof(System.Collections.IEnumerable).IsAssignableFrom(t) && t != typeof(string))
            {
                var enumer = (System.Collections.IEnumerable)obj;
                int i = 0;
                foreach (var item in enumer)
                {
                    if (item == null) continue;
                    CollectLogNameParts(item, parts, depth + 1);
                    if (++i >= 5) break;
                }
                return;
            }

            // 1) Propiedades con LogFileName
            foreach (var prop in t.GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                var attr = prop.GetCustomAttribute<LogFileNameAttribute>();
                if (attr == null) continue;

                var val = prop.GetValue(obj)?.ToString();
                if (!string.IsNullOrWhiteSpace(val))
                    parts.Add(!string.IsNullOrWhiteSpace(attr.Label) ? $"{attr.Label}-{val}" : val);
            }

            // 2) Propiedades complejas/anidadas
            foreach (var prop in t.GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                var value = prop.GetValue(obj);
                if (value == null) continue;

                var pt = prop.PropertyType;
                if (IsTerminal(pt)) continue;

                if (typeof(System.Collections.IEnumerable).IsAssignableFrom(pt) && pt != typeof(string))
                {
                    var enumer = (System.Collections.IEnumerable)value;
                    int i = 0;
                    foreach (var item in enumer)
                    {
                        if (item == null) continue;
                        CollectLogNameParts(item, parts, depth + 1);
                        if (++i >= 5) break;
                    }
                    continue;
                }

                CollectLogNameParts(value, parts, depth + 1);
            }
        }

        /// <summary>
        /// Hidrata un objeto con valores de QueryString y RouteValues (case-insensitive) para tipos primitivos/strings/Guid/DateTime/enum
        /// y para propiedades complejas (crea instancias y continúa recursivamente). También soporta colecciones simples separadas por coma.
        /// </summary>
        private static void BindFromQueryAndRoute(object instance, HttpContext context, int depth = 0)
        {
            if (instance == null || depth > 8) return;
            var t = instance.GetType();

            foreach (var prop in t.GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                var pt = prop.PropertyType;

                // 1) Intentar obtener un valor plano por nombre de propiedad
                var raw = TryGetQueryOrRouteValue(context, prop.Name);
                if (raw != null)
                {
                    try
                    {
                        object? converted;

                        // Colecciones simples: "a,b,c"
                        if (pt != typeof(string) &&
                            typeof(System.Collections.IEnumerable).IsAssignableFrom(pt) &&
                            pt.IsGenericType)
                        {
                            var itemType = pt.GetGenericArguments()[0];
                            var listType = typeof(List<>).MakeGenericType(itemType);
                            var list = (System.Collections.IList?)Activator.CreateInstance(listType);
                            foreach (var token in raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                            {
                                var item = ChangeType(token, itemType);
                                if (item != null) list?.Add(item);
                            }
                            converted = list;
                        }
                        else
                        {
                            converted = ChangeType(raw, pt);
                        }

                        if (converted != null) prop.SetValue(instance, converted);
                    }
                    catch { /* ignorar conversión fallida */ }

                    continue;
                }

                // 2) Si no hay valor directo y la propiedad es compleja, crear instancia y hacer binding recursivo
                if (!IsTerminal(pt))
                {
                    try
                    {
                        var child = prop.GetValue(instance);
                        if (child == null)
                        {
                            child = Activator.CreateInstance(pt);
                            if (child == null) continue;
                            prop.SetValue(instance, child);
                        }
                        BindFromQueryAndRoute(child, context, depth + 1);
                    }
                    catch { /* ignorar */ }
                }
            }
        }

        /// <summary>
        /// Devuelve un valor de Query o Route por clave (case-insensitive).
        /// </summary>
        private static string? TryGetQueryOrRouteValue(HttpContext context, string key)
        {
            // Query
            foreach (var kv in context.Request.Query)
                if (string.Equals(kv.Key, key, StringComparison.OrdinalIgnoreCase))
                    return kv.Value.ToString();

            // Route
            foreach (var kv in context.Request.RouteValues)
                if (string.Equals(kv.Key, key, StringComparison.OrdinalIgnoreCase))
                    return kv.Value?.ToString();

            return null;
        }

        /// <summary>
        /// Convierte string a un tipo destino común (incluye Guid, DateTime, enum).
        /// </summary>
        private static object? ChangeType(string value, Type targetType)
        {
            try
            {
                var u = Nullable.GetUnderlyingType(targetType);
                if (u != null)
                {
                    if (string.IsNullOrWhiteSpace(value)) return null;
                    return ChangeType(value, u);
                }

                if (targetType == typeof(string)) return value;
                if (targetType == typeof(Guid)) return Guid.Parse(value);
                if (targetType == typeof(DateTime)) return DateTime.Parse(value);
                if (targetType == typeof(DateTimeOffset)) return DateTimeOffset.Parse(value);
                if (targetType.IsEnum) return Enum.Parse(targetType, value, ignoreCase: true);

                return Convert.ChangeType(value, targetType);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Indica si un tipo se considera "terminal" (no se navega recursivamente).
        /// </summary>
        private static bool IsTerminal(Type t)
        {
            if (t.IsPrimitive) return true;
            if (t == typeof(string) || t == typeof(decimal)) return true;
            if (t == typeof(DateTime) || t == typeof(DateTimeOffset) || t == typeof(Guid)) return true;
            if (t.IsEnum) return true;
            return false;
        }
    }
}
var preCustom = StrongTypedLogFileNameExtractor.Extract(context, preBody);
if (!string.IsNullOrWhiteSpace(preCustom))
    context.Items["LogCustomPart"] = preCustom;
