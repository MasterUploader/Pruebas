namespace QueryBuilder
{
    /// <summary>
    /// Tipos de datos SQL compatibles.
    /// </summary>
    public enum SqlDataType
    {
        CHAR,
        VARCHAR,
        INTEGER,
        DECIMAL
    }
}

using System;

namespace QueryBuilder.Attributes
{
    /// <summary>
    /// Atributo que define los metadatos SQL de una propiedad del modelo.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class SqlColumnDefinitionAttribute : Attribute
    {
        /// <summary>Nombre de la columna SQL.</summary>
        public string ColumnName { get; }

        /// <summary>Tipo de dato SQL.</summary>
        public SqlDataType DataType { get; }

        /// <summary>Longitud del campo (para CHAR/VARCHAR).</summary>
        public int Length { get; }

        /// <summary>Precisión (para DECIMAL).</summary>
        public int Precision { get; }

        /// <summary>Escala (para DECIMAL).</summary>
        public int Scale { get; }

        /// <summary>
        /// Constructor para CHAR/VARCHAR/INTEGER.
        /// </summary>
        public SqlColumnDefinitionAttribute(string columnName, SqlDataType dataType, int length)
        {
            ColumnName = columnName;
            DataType = dataType;
            Length = length;
            Precision = 0;
            Scale = 0;
        }

        /// <summary>
        /// Constructor para DECIMAL con precisión y escala.
        /// </summary>
        public SqlColumnDefinitionAttribute(string columnName, SqlDataType dataType, int length, int precision, int scale)
        {
            ColumnName = columnName;
            DataType = dataType;
            Length = length;
            Precision = precision;
            Scale = scale;
        }
    }
}


using System;

namespace QueryBuilder.Validators
{
    /// <summary>
    /// Valida si los valores cumplen con las restricciones definidas en SqlColumnDefinitionAttribute.
    /// </summary>
    public static class SqlTypeValidator
    {
        public static bool IsValid(object value, SqlDataType type, int maxLength, int precision = 0, int scale = 0)
        {
            if (value == null) return false;

            switch (type)
            {
                case SqlDataType.CHAR:
                case SqlDataType.VARCHAR:
                    return value.ToString()?.Length <= maxLength;

                case SqlDataType.INTEGER:
                    return int.TryParse(value.ToString(), out _);

                case SqlDataType.DECIMAL:
                    if (decimal.TryParse(value.ToString(), out var dec))
                    {
                        var str = dec.ToString(System.Globalization.CultureInfo.InvariantCulture);
                        var totalDigits = str.Replace(".", "").Replace(",", "").Length;
                        var decimalPart = str.Contains('.') ? str.Split('.')[1].Length : 0;

                        return totalDigits <= precision && decimalPart <= scale;
                    }
                    return false;

                default:
                    return true;
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Reflection;
using QueryBuilder.Attributes;
using QueryBuilder.Validators;

namespace QueryBuilder.Utils
{
    /// <summary>
    /// Valida modelos que utilizan SqlColumnDefinitionAttribute antes de generar SQL.
    /// </summary>
    public static class QueryModelValidator
    {
        /// <summary>
        /// Evalúa si un modelo es válido para generar un query SQL.
        /// </summary>
        /// <param name="model">Instancia del modelo a validar.</param>
        /// <returns>Lista de errores encontrados. Vacía si es válido.</returns>
        public static List<string> ValidateModel(object model)
        {
            var errors = new List<string>();
            if (model == null) return errors;

            var props = model.GetType().GetProperties();

            foreach (var prop in props)
            {
                var attr = prop.GetCustomAttribute<SqlColumnDefinitionAttribute>();
                if (attr != null)
                {
                    var value = prop.GetValue(model);
                    if (value != null && !SqlTypeValidator.IsValid(value, attr.DataType, attr.Length, attr.Precision, attr.Scale))
                    {
                        errors.Add($"Campo '{prop.Name}' no cumple con las restricciones definidas: Tipo={attr.DataType}, Longitud={attr.Length}, Precisión={attr.Precision}, Escala={attr.Scale}");
                    }
                }
            }

            return errors;
        }
    }
}
