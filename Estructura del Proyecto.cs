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
