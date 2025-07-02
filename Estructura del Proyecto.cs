using System;

namespace RestUtilities.QueryBuilder.Attributes
{
    /// <summary>
    /// Atributo personalizado que define el tipo, longitud y precisión de una columna SQL
    /// asociada a una propiedad de una clase modelo.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class SqlColumnDefinitionAttribute : Attribute
    {
        /// <summary>
        /// Tipo de dato SQL (por ejemplo: VARCHAR, NUMERIC, DATE).
        /// </summary>
        public string Type { get; }

        /// <summary>
        /// Longitud máxima del campo, si aplica (por ejemplo: 20 para VARCHAR(20)).
        /// </summary>
        public int Length { get; }

        /// <summary>
        /// Precisión o número de decimales permitidos (solo aplica a tipos numéricos).
        /// </summary>
        public int Precision { get; }

        /// <summary>
        /// Inicializa una nueva instancia del atributo para definir columnas SQL.
        /// </summary>
        /// <param name="type">Tipo de dato SQL.</param>
        /// <param name="length">Longitud máxima (por defecto 0 si no aplica).</param>
        /// <param name="precision">Precisión decimal (por defecto 0 si no aplica).</param>
        public SqlColumnDefinitionAttribute(string type, int length = 0, int precision = 0)
        {
            Type = type;
            Length = length;
            Precision = precision;
        }
    }
}
