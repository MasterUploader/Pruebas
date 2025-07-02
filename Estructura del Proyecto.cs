using System;
using System.Collections.Generic;

namespace RestUtilities.QueryBuilder.Validators
{
    /// <summary>
    /// Valida los tipos de datos y sus restricciones (longitud, precisión, etc.) para una columna SQL.
    /// </summary>
    public static class SqlTypeValidator
    {
        /// <summary>
        /// Verifica si un valor cumple con las restricciones de tipo, tamaño y precisión de una columna SQL.
        /// </summary>
        /// <param name="value">Valor a validar.</param>
        /// <param name="expectedType">Tipo de dato esperado (por ejemplo: VARCHAR, NUMERIC, etc.).</param>
        /// <param name="length">Tamaño máximo permitido.</param>
        /// <param name="precision">Número de decimales, si aplica.</param>
        /// <returns>Verdadero si el valor es compatible, falso en caso contrario.</returns>
        public static bool IsValid(object value, string expectedType, int length = 0, int precision = 0)
        {
            if (value == null) return true;

            switch (expectedType.ToUpper())
            {
                case "CHAR":
                case "VARCHAR":
                    return value is string str && (length == 0 || str.Length <= length);

                case "NUMERIC":
                case "DECIMAL":
                    if (decimal.TryParse(value.ToString(), out decimal dec))
                    {
                        var parts = dec.ToString().Split('.');
                        var intPart = parts[0];
                        var decPart = parts.Length > 1 ? parts[1] : "";
                        return intPart.Length + decPart.Length <= length && decPart.Length <= precision;
                    }
                    return false;

                case "INT":
                case "INTEGER":
                    return int.TryParse(value.ToString(), out _);

                case "SMALLINT":
                    return short.TryParse(value.ToString(), out _);

                case "BIGINT":
                    return long.TryParse(value.ToString(), out _);

                case "DATE":
                    return DateTime.TryParse(value.ToString(), out _);

                default:
                    return true;
            }
        }
    }
}

using RestUtilities.QueryBuilder.Attributes;
using RestUtilities.QueryBuilder.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace RestUtilities.QueryBuilder.Validators
{
    /// <summary>
    /// Valida los modelos definidos por el usuario antes de construir una sentencia SQL.
    /// </summary>
    public static class QueryModelValidator
    {
        /// <summary>
        /// Evalúa si un modelo es válido para generar un query SQL.
        /// </summary>
        /// <param name="model">Instancia del modelo a validar.</param>
        /// <returns>Lista de errores detectados. Si está vacía, el modelo es válido.</returns>
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
                    if (!SqlTypeValidator.IsValid(value, attr.Type, attr.Length, attr.Precision))
                    {
                        errors.Add($"Campo '{prop.Name}' no cumple con las restricciones definidas: Tipo={attr.Type}, Longitud={attr.Length}, Precisión={attr.Precision}");
                    }
                }
            }

            return errors;
        }
    }
}

