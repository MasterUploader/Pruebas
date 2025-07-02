using System;

namespace RestUtilities.QueryBuilder.Attributes
{
    /// <summary>
    /// Atributo utilizado para excluir una propiedad de una clase
    /// durante la generación automática de sentencias SQL.
    /// 
    /// Este atributo indica que la propiedad no debe incluirse en cláusulas
    /// como SELECT, INSERT o UPDATE al construir dinámicamente una consulta.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class SqlIgnoreAttribute : Attribute
    {
        /// <summary>
        /// Inicializa una nueva instancia del atributo SqlIgnoreAttribute.
        /// </summary>
        public SqlIgnoreAttribute()
        {
        }
    }
}


using System;

namespace RestUtilities.QueryBuilder.Attributes
{
    /// <summary>
    /// Atributo utilizado para marcar una propiedad como clave primaria en una tabla SQL.
    /// Esto permite identificar la columna como parte de la llave al generar WHERE en UPDATE o DELETE.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class SqlKeyAttribute : Attribute
    {
        /// <summary>
        /// Inicializa una nueva instancia del atributo SqlKeyAttribute.
        /// </summary>
        public SqlKeyAttribute()
        {
        }
    }
}

using System;

namespace RestUtilities.QueryBuilder.Attributes
{
    /// <summary>
    /// Atributo utilizado para indicar explícitamente el nombre de la tabla
    /// asociada a una clase cuando no coincide con el nombre de la clase en C#.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class SqlTableAttribute : Attribute
    {
        /// <summary>
        /// Nombre de la tabla en la base de datos.
        /// </summary>
        public string TableName { get; }

        /// <summary>
        /// Inicializa una nueva instancia del atributo SqlTableAttribute.
        /// </summary>
        /// <param name="tableName">Nombre de la tabla.</param>
        public SqlTableAttribute(string tableName)
        {
            TableName = tableName;
        }
    }
}
