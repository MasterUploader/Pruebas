using System;

namespace QueryBuilder.Attributes
{
    /// <summary>
    /// Atributo que define metadatos de una columna SQL para una propiedad.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class SqlColumnDefinitionAttribute : Attribute
    {
        /// <summary>
        /// Nombre de la columna en la base de datos.
        /// </summary>
        public string ColumnName { get; }

        /// <summary>
        /// Tipo de dato SQL de la columna (ej. CHAR, VARCHAR).
        /// </summary>
        public SqlDataType DataType { get; }

        /// <summary>
        /// Longitud de la columna (por ejemplo, 50 para CHAR(50)).
        /// </summary>
        public int Length { get; }

        /// <summary>
        /// Inicializa el atributo con nombre, tipo y longitud de columna.
        /// </summary>
        /// <param name="columnName">Nombre de la columna.</param>
        /// <param name="dataType">Tipo de dato SQL.</param>
        /// <param name="length">Longitud del campo.</param>
        public SqlColumnDefinitionAttribute(string columnName, SqlDataType dataType, int length)
        {
            ColumnName = columnName;
            DataType = dataType;
            Length = length;
        }
    }
}


namespace QueryBuilder
{
    /// <summary>
    /// Enum para representar tipos de datos SQL soportados por el atributo.
    /// </summary>
    public enum SqlDataType
    {
        Char,
        Varchar,
        Int,
        Decimal,
        Date,
        Time,
        Timestamp,
        Boolean
    }
}

using QueryBuilder.Attributes;
using System.Reflection;

namespace QueryBuilder.Helpers;

/// <summary>
/// Utilidades para obtener metadatos de columnas SQL definidas mediante atributos personalizados.
/// </summary>
public static class SqlModelMetadataHelper
{
    /// <summary>
    /// Obtiene los metadatos de todas las propiedades decoradas con <see cref="SqlColumnDefinitionAttribute"/> de un modelo.
    /// </summary>
    /// <param name="model">Instancia del modelo.</param>
    /// <returns>Diccionario con nombre de propiedad y su definición SQL.</returns>
    public static Dictionary<string, SqlColumnDefinitionAttribute> GetColumnDefinitions(object model)
    {
        var result = new Dictionary<string, SqlColumnDefinitionAttribute>();

        var properties = model.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
        foreach (var prop in properties)
        {
            var attr = prop.GetCustomAttribute<SqlColumnDefinitionAttribute>();
            if (attr != null)
                result.Add(prop.Name, attr);
        }

        return result;
    }

    /// <summary>
    /// Aplica padding automático a propiedades tipo CHAR/VARCHAR según la longitud definida en el atributo.
    /// </summary>
    /// <typeparam name="T">Tipo del modelo.</typeparam>
    /// <param name="model">Instancia del modelo.</param>
    /// <param name="padChar">Carácter de padding (por defecto espacio).</param>
    /// <param name="padRight">Indica si el padding debe ser al final (true) o al inicio (false).</param>
    public static void ApplyStringPadding<T>(T model, char padChar = ' ', bool padRight = true) where T : class
    {
        var props = model.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (var prop in props)
        {
            var attr = prop.GetCustomAttribute<SqlColumnDefinitionAttribute>();
            if (attr == null) continue;

            if ((attr.DataType == SqlDataType.Char || attr.DataType == SqlDataType.Varchar) &&
                prop.PropertyType == typeof(string))
            {
                var currentValue = (string?)prop.GetValue(model);
                if (currentValue == null) continue;

                string padded = padRight
                    ? currentValue.PadRight(attr.Length, padChar)
                    : currentValue.PadLeft(attr.Length, padChar);

                prop.SetValue(model, padded);
            }
        }
    }
}


using QueryBuilder.Attributes;
using System.Reflection;
using System.Text;

namespace QueryBuilder.Services;

/// <summary>
/// Generador dinámico de sentencias SQL INSERT a partir de atributos definidos en el modelo.
/// </summary>
public static class DynamicInsertGenerator
{
    /// <summary>
    /// Genera un INSERT INTO ... VALUES (...) para el modelo especificado.
    /// </summary>
    /// <typeparam name="T">Tipo del modelo.</typeparam>
    /// <param name="schemaAndTable">Nombre completo de tabla (por ejemplo: "BCAH96DTA.BTSACTA").</param>
    /// <param name="model">Instancia del modelo a insertar.</param>
    /// <param name="parameters">Lista de nombres de parámetros generados, en el mismo orden que las columnas.</param>
    /// <returns>SQL generado.</returns>
    public static string GenerateInsertSql<T>(string schemaAndTable, T model, out List<string> parameters) where T : class
    {
        var props = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);
        var columns = new List<string>();
        parameters = new List<string>();

        foreach (var prop in props)
        {
            var attr = prop.GetCustomAttribute<SqlColumnDefinitionAttribute>();
            if (attr == null)
                continue;

            columns.Add(attr.ColumnName);
            parameters.Add($"@{attr.ColumnName}"); // nombre de parámetro OleDb
        }

        string columnList = string.Join(", ", columns);
        string paramList = string.Join(", ", parameters);

        return $"INSERT INTO {schemaAndTable} ({columnList}) VALUES ({paramList})";
    }
}

using QueryBuilder.Attributes;
using System.Reflection;
using System.Text;

namespace QueryBuilder.Services;

/// <summary>
/// Generador dinámico de sentencias SQL UPDATE a partir de modelos decorados con atributos de metadatos.
/// </summary>
public static class DynamicUpdateGenerator
{
    /// <summary>
    /// Genera una sentencia SQL UPDATE dinámica con los campos que tienen el atributo <see cref="SqlColumnDefinitionAttribute"/>.
    /// </summary>
    /// <typeparam name="T">Tipo del modelo.</typeparam>
    /// <param name="schemaAndTable">Nombre completo de la tabla (por ejemplo: "BCAH96DTA.BTSACTA").</param>
    /// <param name="model">Instancia del modelo que contiene los valores a actualizar.</param>
    /// <param name="whereClause">Cláusula WHERE (sin la palabra clave "WHERE").</param>
    /// <param name="parameters">Lista de nombres de parámetros generados para los valores SET.</param>
    /// <returns>Cadena SQL del UPDATE.</returns>
    public static string GenerateUpdateSql<T>(string schemaAndTable, T model, string whereClause, out List<string> parameters)
        where T : class
    {
        var props = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);
        var setClauses = new List<string>();
        parameters = new List<string>();

        foreach (var prop in props)
        {
            var attr = prop.GetCustomAttribute<SqlColumnDefinitionAttribute>();
            if (attr == null)
                continue;

            var paramName = $"@{attr.ColumnName}";
            setClauses.Add($"{attr.ColumnName} = {paramName}");
            parameters.Add(paramName);
        }

        var sql = new StringBuilder();
        sql.Append($"UPDATE {schemaAndTable} SET ");
        sql.Append(string.Join(", ", setClauses));

        if (!string.IsNullOrWhiteSpace(whereClause))
        {
            sql.Append(" WHERE ");
            sql.Append(whereClause);
        }

        return sql.ToString();
    }
}

using QueryBuilder.Attributes;
using System.Reflection;
using System.Text;

namespace QueryBuilder.Services;

/// <summary>
/// Generador dinámico de sentencias SQL SELECT a partir de modelos decorados con atributos de metadatos.
/// </summary>
public static class DynamicSelectGenerator
{
    /// <summary>
    /// Genera una sentencia SQL SELECT basada en los atributos <see cref="SqlColumnDefinitionAttribute"/>.
    /// </summary>
    /// <typeparam name="T">Tipo del modelo decorado con atributos.</typeparam>
    /// <param name="schemaAndTable">Nombre completo de la tabla (por ejemplo: "BCAH96DTA.BTSACTA").</param>
    /// <param name="whereConditions">Diccionario con condiciones WHERE: clave = nombre de columna, valor = nombre del parámetro.</param>
    /// <returns>Sentencia SQL SELECT generada dinámicamente.</returns>
    public static string GenerateSelectSql<T>(string schemaAndTable, Dictionary<string, string>? whereConditions = null)
        where T : class
    {
        var props = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);
        var columns = new List<string>();

        foreach (var prop in props)
        {
            var attr = prop.GetCustomAttribute<SqlColumnDefinitionAttribute>();
            if (attr != null)
                columns.Add(attr.ColumnName);
        }

        var sql = new StringBuilder();
        sql.Append($"SELECT {string.Join(", ", columns)} FROM {schemaAndTable}");

        if (whereConditions != null && whereConditions.Count > 0)
        {
            sql.Append(" WHERE ");
            sql.Append(string.Join(" AND ", whereConditions.Select(kvp => $"{kvp.Key} = @{kvp.Value}")));
        }

        return sql.ToString();
    }
}


