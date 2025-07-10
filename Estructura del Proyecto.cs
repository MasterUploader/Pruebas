/// <summary>
/// Obtiene un diccionario de pares columna/valor para una instancia de modelo.
/// </summary>
public static Dictionary<string, object> GetColumnValuePairs<T>(T instance)
{
    var result = new Dictionary<string, object>();
    var props = typeof(T).GetProperties();

    foreach (var prop in props)
    {
        if (prop.GetCustomAttribute<SqlIgnoreAttribute>() != null)
            continue;

        var nameAttr = prop.GetCustomAttribute<SqlColumnNameAttribute>();
        var columnName = nameAttr?.Name ?? prop.Name;
        var value = prop.GetValue(instance);
        result[columnName] = value ?? DBNull.Value;
    }

    return result;
}



