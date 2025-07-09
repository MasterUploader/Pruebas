using System.Data.OleDb;
using System.Reflection;
using QueryBuilder.Attributes;
using QueryBuilder.Enums;

namespace RestUtilities.Connections.Helpers;

/// <summary>
/// Ayudante para agregar parámetros a comandos OleDb basados en modelos con metadatos SQL.
/// </summary>
public class FieldsQuery
{
    /// <summary>
    /// Agrega los parámetros a un comando a partir de un modelo que define sus columnas con el atributo <see cref="SqlColumnDefinitionAttribute"/>.
    /// </summary>
    /// <typeparam name="TModel">Tipo del modelo con metadatos de columnas.</typeparam>
    /// <param name="command">Comando OleDb donde se agregarán los parámetros.</param>
    /// <param name="model">Instancia del modelo con los datos.</param>
    public void AddParametersFromModel<TModel>(OleDbCommand command, TModel model)
    {
        var properties = typeof(TModel).GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (var prop in properties)
        {
            var attr = prop.GetCustomAttribute<SqlColumnDefinitionAttribute>();
            if (attr == null) continue;

            string columnName = attr.ColumnName;
            SqlDataType expectedType = attr.DataType;
            int maxLength = attr.Length;

            object? value = prop.GetValue(model);

            // Validación de longitud si es tipo carácter
            if (expectedType == SqlDataType.Char && value is string sValue)
            {
                if (sValue.Length > maxLength)
                    throw new ArgumentException($"El valor para la columna {columnName} excede la longitud máxima ({maxLength}).");

                // Asegura que el valor esté dentro del tamaño permitido
                value = sValue.PadRight(maxLength);
            }

            // Conversión según tipo SQL esperado
            OleDbType dbType = ConvertToOleDbType(expectedType);
            OleDbParameter parameter = new($"@{columnName}", dbType)
            {
                Value = value ?? DBNull.Value
            };

            command.Parameters.Add(parameter);
        }
    }

    /// <summary>
    /// Convierte un tipo de dato SQL personalizado a <see cref="OleDbType"/>.
    /// </summary>
    private OleDbType ConvertToOleDbType(SqlDataType sqlType)
    {
        return sqlType switch
        {
            SqlDataType.Char => OleDbType.Char,
            SqlDataType.VarChar => OleDbType.VarChar,
            SqlDataType.Numeric => OleDbType.Numeric,
            SqlDataType.Decimal => OleDbType.Decimal,
            SqlDataType.Integer => OleDbType.Integer,
            SqlDataType.SmallInt => OleDbType.SmallInt,
            SqlDataType.Date => OleDbType.Date,
            SqlDataType.Time => OleDbType.DBTime,
            SqlDataType.Timestamp => OleDbType.DBTimeStamp,
            SqlDataType.Double => OleDbType.Double,
            _ => throw new NotSupportedException($"Tipo SQL no soportado: {sqlType}")
        };
    }
}
