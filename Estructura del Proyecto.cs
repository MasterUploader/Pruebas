using System;

namespace RestUtilities.QueryBuilder.Attributes
{
    /// <summary>
    /// Atributo personalizado que permite definir el nombre de columna SQL que se usará
    /// en lugar del nombre de propiedad del modelo C#.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class SqlColumnNameAttribute : Attribute
    {
        /// <summary>
        /// Nombre de columna a usar en la sentencia SQL.
        /// </summary>
        public string ColumnName { get; }

        /// <summary>
        /// Inicializa una nueva instancia del atributo con el nombre de columna especificado.
        /// </summary>
        /// <param name="columnName">Nombre de columna SQL.</param>
        public SqlColumnNameAttribute(string columnName)
        {
            ColumnName = columnName;
        }
    }
}
