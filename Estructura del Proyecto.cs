using Logging.Attributes;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Controllers;
using System.Reflection;
using System.Text.Json;

namespace Logging.Helpers;

public static class LogFileNameExtractor
{
    public static string? ExtractLogFileNameFromContext(HttpContext context, string? requestBody = null)
    {
        var parts = new List<string>();

        try
        {
            var endpoint = context.GetEndpoint();
            var actionDescriptor = endpoint?.Metadata.GetMetadata<ControllerActionDescriptor>();
            if (actionDescriptor == null)
                return null;

            var modelType = actionDescriptor.Parameters
                .FirstOrDefault(p => p.ParameterType.GetProperties()
                    .Any(x => x.GetCustomAttribute<LogFileNameAttribute>() != null))
                ?.ParameterType;

            if (modelType == null)
                return null;

            // 1️⃣ Del cuerpo (si está disponible)
            if (!string.IsNullOrWhiteSpace(requestBody) && context.Request.ContentType?.Contains("application/json") == true)
            {
                var json = JsonSerializer.Deserialize<JsonElement>(requestBody);

                foreach (var prop in modelType.GetProperties())
                {
                    var attr = prop.GetCustomAttribute<LogFileNameAttribute>();
                    if (attr != null && json.TryGetProperty(prop.Name, out var valueProp))
                    {
                        var value = valueProp.ToString();
                        if (!string.IsNullOrWhiteSpace(value))
                        {
                            parts.Add(!string.IsNullOrWhiteSpace(attr.Label)
                                ? $"{attr.Label}-{value}"
                                : value);
                        }
                    }
                }
            }

            // 2️⃣ Del query string
            foreach (var kvp in context.Request.Query)
            {
                var prop = modelType.GetProperty(kvp.Key, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
                var attr = prop?.GetCustomAttribute<LogFileNameAttribute>();
                if (attr != null)
                {
                    parts.Add(!string.IsNullOrWhiteSpace(attr.Label)
                        ? $"{attr.Label}-{kvp.Value}"
                        : kvp.Value.ToString());
                }
            }

            // 3️⃣ Del route values
            foreach (var kvp in context.Request.RouteValues)
            {
                var prop = modelType.GetProperty(kvp.Key, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
                var attr = prop?.GetCustomAttribute<LogFileNameAttribute>();
                if (attr != null)
                {
                    parts.Add(!string.IsNullOrWhiteSpace(attr.Label)
                        ? $"{attr.Label}-{kvp.Value}"
                        : kvp.Value?.ToString() ?? string.Empty);
                }
            }

            return parts.Count > 0 ? string.Join("_", parts) : null;
        }
        catch
        {
            return null;
        }
    }
}

// Extraer identificador para el nombre del log y guardarlo en context.Items
var customPart = LogFileNameExtractor.ExtractLogFileNameFromContext(context, body);
if (!string.IsNullOrWhiteSpace(customPart))
{
    context.Items["LogCustomPart"] = customPart;
}


string? customPart = null;
if (context.Items.TryGetValue("LogCustomPart", out var partObj) && partObj is string partValue)
{
    customPart = partValue;
}
