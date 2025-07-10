namespace QueryBuilder.Models;

/// <summary>
/// Representa el contexto de traducción de una consulta SQL, utilizado por los traductores como AS400QueryTranslator.
/// </summary>
public class QueryTranslationContext
{
    public string TableName { get; set; } = string.Empty;
    public List<string> SelectColumns { get; set; } = new();
    public string? WhereClause { get; set; }
    public string? OrderByClause { get; set; }

    // Para INSERT
    public List<string> InsertColumns { get; set; } = new();
    public List<object?> ParameterValues { get; set; } = new();

    // Para UPDATE
    public List<string> UpdateColumns { get; set; } = new();

    // Paginación
    public int? Offset { get; set; }
    public int? Limit { get; set; }

    // Solo para extracción de metadatos
    public bool MetadataOnly { get; set; } = false;
}


using QueryBuilder.Enums;

namespace QueryBuilder.Models;

/// <summary>
/// Representa metadatos de un parámetro SQL (nombre, tipo, longitud, valor actual).
/// </summary>
public class SqlParameterMetadata
{
    public string Name { get; set; } = string.Empty;
    public SqlDataType DataType { get; set; }
    public int? Length { get; set; }
    public object? Value { get; set; }
}


using QueryBuilder.Attributes;
using System;
using System.Linq;

namespace QueryBuilder.Utils;

/// <summary>
/// Utilidad para obtener el nombre completo de una tabla a partir del modelo.
/// </summary>
public static class SqlMetadataHelper
{
    public static string GetFullTableName<T>()
    {
        var type = typeof(T);
        var tableAttr = type.GetCustomAttributes(typeof(SqlTableAttribute), true)
                            .FirstOrDefault() as SqlTableAttribute;

        if (tableAttr == null)
        {
            throw new InvalidOperationException($"Missing [SqlTable] attribute on {type.Name}");
        }

        return !string.IsNullOrEmpty(tableAttr.Schema)
            ? $"{tableAttr.Schema}.{tableAttr.TableName}"
            : tableAttr.TableName;
    }
}

