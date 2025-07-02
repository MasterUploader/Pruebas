using System;
using System.Data;

namespace RestUtilities.QueryBuilder.Attributes
{
    /// <summary>
    /// Atributo utilizado para mapear una propiedad de una clase con una columna SQL.
    /// Permite definir el nombre exacto de la columna en la base de datos,
    /// el tipo de dato SQL y su longitud o precisión.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class SqlColumnAttribute : Attribute
    {
        /// <summary>
        /// Nombre exacto de la columna en la base de datos.
        /// </summary>
        public string ColumnName { get; }

        /// <summary>
        /// Tipo de dato SQL que se debe utilizar (compatible con OleDbType).
        /// </summary>
        public OleDbType SqlType { get; }

        /// <summary>
        /// Longitud máxima permitida del campo (opcional, aplicable a tipos CHAR, VARCHAR, etc.).
        /// </summary>
        public int? Length { get; }

        /// <summary>
        /// Número de decimales permitidos (opcional, para tipos numéricos con precisión).
        /// </summary>
        public int? Scale { get; }

        /// <summary>
        /// Inicializa una nueva instancia del atributo SqlColumnAttribute.
        /// </summary>
        /// <param name="columnName">Nombre de la columna SQL.</param>
        /// <param name="sqlType">Tipo de dato SQL (OleDbType).</param>
        /// <param name="length">Longitud máxima permitida (opcional).</param>
        /// <param name="scale">Número de decimales (opcional).</param>
        public SqlColumnAttribute(string columnName, OleDbType sqlType, int? length = null, int? scale = null)
        {
            ColumnName = columnName;
            SqlType = sqlType;
            Length = length;
            Scale = scale;
        }
    }
}
