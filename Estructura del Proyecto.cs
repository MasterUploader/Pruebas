using System.Text.Json;
using System.Text.Json.Serialization;

namespace RestUtilities.Common.Helpers;

/// <summary>
/// Helper estático para serialización, deserialización y formateo de JSON.
/// Permite configuraciones reutilizables como pretty print, camelCase, y opciones personalizadas.
/// </summary>
public static class JsonHelper
{
    /// <summary>
    /// Serializa un objeto a una cadena JSON.
    /// </summary>
    /// <param name="obj">Objeto a serializar.</param>
    /// <param name="prettyPrint">Indica si debe aplicarse formato indentado (pretty print).</param>
    /// <param name="useCamelCase">Indica si se debe usar camelCase en los nombres de propiedad.</param>
    /// <param name="ignoreNull">Indica si se deben ignorar propiedades nulas.</param>
    public static string ToJson(
        object obj,
        bool prettyPrint = false,
        bool useCamelCase = true,
        bool ignoreNull = true)
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = prettyPrint,
            PropertyNamingPolicy = useCamelCase ? JsonNamingPolicy.CamelCase : null,
            DefaultIgnoreCondition = ignoreNull ? JsonIgnoreCondition.WhenWritingNull : JsonIgnoreCondition.Never
        };

        return JsonSerializer.Serialize(obj, options);
    }

    /// <summary>
    /// Deserializa una cadena JSON a un objeto del tipo especificado.
    /// </summary>
    /// <typeparam name="T">Tipo destino.</typeparam>
    /// <param name="json">Cadena JSON.</param>
    /// <param name="useCamelCase">Indica si se espera camelCase en el JSON.</param>
    public static T? FromJson<T>(string json, bool useCamelCase = true)
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = useCamelCase ? JsonNamingPolicy.CamelCase : null
        };

        return JsonSerializer.Deserialize<T>(json, options);
    }

    /// <summary>
    /// Pretty print para una cadena JSON, con opciones personalizadas.
    /// </summary>
    /// <param name="json">Cadena JSON de entrada.</param>
    /// <param name="options">Opciones personalizadas para la salida.</param>
    public static string PrettyPrint(string json, JsonSerializerOptions? options = null)
    {
        using var doc = JsonDocument.Parse(json);
        return JsonSerializer.Serialize(doc.RootElement, options ?? new JsonSerializerOptions { WriteIndented = true });
    }

    /// <summary>
    /// Serializa un objeto con opciones personalizadas.
    /// </summary>
    public static string Serialize(object obj, JsonSerializerOptions? options = null)
    {
        return JsonSerializer.Serialize(obj, options ?? new JsonSerializerOptions());
    }

    /// <summary>
    /// Deserializa una cadena JSON con opciones personalizadas.
    /// </summary>
    public static T? Deserialize<T>(string json, JsonSerializerOptions? options = null)
    {
        return JsonSerializer.Deserialize<T>(json, options ?? new JsonSerializerOptions());
    }
}
