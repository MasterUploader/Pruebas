namespace RestUtilities.QueryBuilder.Models
{
    /// <summary>
    /// Representa la definición de un parámetro SQL.
    /// Incluye el nombre del parámetro, su tipo de datos y su valor.
    /// </summary>
    public class SqlParameterDefinition
    {
        /// <summary>
        /// Nombre del parámetro, incluyendo el prefijo @ si es necesario.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Valor que se asignará al parámetro.
        /// </summary>
        public object Value { get; set; }

        /// <summary>
        /// Tipo de datos esperado por el motor SQL (por ejemplo, VARCHAR, INT).
        /// </summary>
        public string DataType { get; set; }

        /// <summary>
        /// Tamaño o precisión del dato, si aplica (por ejemplo, 20 o 10,2).
        /// </summary>
        public string Size { get; set; }
    }
}


using System.Collections.Generic;

namespace RestUtilities.QueryBuilder.Models
{
    /// <summary>
    /// Representa una tabla SQL y su definición estructural.
    /// Incluye el nombre, alias, columnas y posibles restricciones.
    /// </summary>
    public class TableDefinition
    {
        /// <summary>
        /// Nombre de la tabla tal como aparece en la base de datos.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Alias opcional para usar en la consulta SQL.
        /// </summary>
        public string Alias { get; set; }

        /// <summary>
        /// Lista de columnas que componen la tabla.
        /// </summary>
        public List<ColumnDefinition> Columns { get; set; } = new();
    }
}

namespace RestUtilities.QueryBuilder.Models
{
    /// <summary>
    /// Representa una columna de una tabla SQL.
    /// Incluye nombre, tipo, tamaño y si es clave primaria o permite nulos.
    /// </summary>
    public class ColumnDefinition
    {
        /// <summary>
        /// Nombre de la columna.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Tipo de dato SQL (por ejemplo: VARCHAR, INT, DECIMAL).
        /// </summary>
        public string DataType { get; set; }

        /// <summary>
        /// Tamaño o precisión del campo, si aplica.
        /// </summary>
        public string Size { get; set; }

        /// <summary>
        /// Indica si la columna permite valores nulos.
        /// </summary>
        public bool IsNullable { get; set; }

        /// <summary>
        /// Indica si la columna es clave primaria.
        /// </summary>
        public bool IsPrimaryKey { get; set; }
    }
}

using System.Collections.Generic;

namespace RestUtilities.QueryBuilder.Models
{
    /// <summary>
    /// Representa los metadatos generales asociados a una consulta SQL generada.
    /// Permite almacenar el SQL, los parámetros y otra información útil.
    /// </summary>
    public class QueryMetadata
    {
        /// <summary>
        /// Consulta SQL final generada.
        /// </summary>
        public string Sql { get; set; }

        /// <summary>
        /// Lista de parámetros que deben asignarse antes de ejecutar la consulta.
        /// </summary>
        public List<SqlParameterDefinition> Parameters { get; set; } = new();

        /// <summary>
        /// Información adicional para depuración o ejecución (por ejemplo, tiempo estimado).
        /// </summary>
        public Dictionary<string, string> Metadata { get; set; } = new();
    }
}
