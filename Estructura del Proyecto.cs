using System.Collections.Generic;

namespace Utils;

/// <summary>
/// Extensiones para obtener valores de diccionarios de forma segura,
/// compatibles con targets donde no existe GetValueOrDefault.
/// </summary>
public static class DictionaryExtensions
{
    /// <summary>
    /// Devuelve el valor asociado a <paramref name="key"/> o <c>null</c> si no existe.
    /// </summary>
    public static object? GetValueOrDefault(this IDictionary<string, object> dict, string key)
        => dict != null && dict.TryGetValue(key, out var value) ? value : null;

    /// <summary>
    /// Devuelve el valor asociado a <paramref name="key"/> convertido a <typeparamref name="T"/>,
    /// o el <paramref name="defaultValue"/> si no existe o no es del tipo esperado.
    /// </summary>
    public static T? GetValueOrDefault<T>(this IDictionary<string, object> dict, string key, T? defaultValue = default)
    {
        if (dict != null && dict.TryGetValue(key, out var value) && value is T t) return t;
        return defaultValue;
    }
}
