using QueryBuilder.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace QueryBuilder.Helpers;

public static class SqlMetadataHelper
{
    /// <summary>
    /// Obtiene el nombre completo de la tabla para el tipo de modelo especificado,
    /// utilizando el atributo [SqlTable] o el nombre de la clase si no está definido.
    /// </summary>
    public static string GetFullTableName<T>()
    {
        var type = typeof(T);
        var tableAttr = type.GetCustomAttribute<SqlTableAttribute>();
        return tableAttr != null
            ? $"{tableAttr.Schema}.{tableAttr.TableName}"
            : type.Name;
    }

    /// <summary>
    /// Obtiene los nombres de columnas SQL que corresponden al modelo,
    /// excluyendo las propiedades marcadas con [SqlIgnore].
    /// </summary>
    /// <typeparam name="T">Tipo del modelo.</typeparam>
    /// <returns>Lista de nombres de columnas SQL.</returns>
    public static List<string> GetSqlColumns<T>()
    {
        var type = typeof(T);
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        var columns = new List<string>();

        foreach (var prop in properties)
        {
            // Ignorar si tiene [SqlIgnore]
            if (prop.GetCustomAttribute<SqlIgnoreAttribute>() != null)
                continue;

            // Usar el nombre definido en [SqlColumnName] si existe
            var nameAttr = prop.GetCustomAttribute<SqlColumnNameAttribute>();
            if (nameAttr != null)
            {
                columns.Add(nameAttr.Name);
                continue;
            }

            // Usar el nombre de la propiedad como nombre de columna por defecto
            columns.Add(prop.Name);
        }

        return columns;
    }
}
