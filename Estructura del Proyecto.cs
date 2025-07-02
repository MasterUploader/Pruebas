using System.Collections.Generic;
using System.Text;

namespace RestUtilities.QueryBuilder.Builders
{
    /// <summary>
    /// Constructor de consultas SQL del tipo UPDATE.
    /// Permite construir sentencias UPDATE con asignación de columnas y condiciones WHERE.
    /// </summary>
    public class UpdateQueryBuilder
    {
        /// <summary>Nombre de la tabla a actualizar.</summary>
        public string Table { get; set; }

        /// <summary>Columnas a actualizar con sus valores nuevos.</summary>
        public Dictionary<string, string> SetColumns { get; set; } = new();

        /// <summary>Condiciones para la cláusula WHERE.</summary>
        public List<string> WhereConditions { get; set; } = new();

        /// <summary>
        /// Construye la sentencia SQL UPDATE.
        /// </summary>
        /// <returns>Consulta SQL generada.</returns>
        public string Build()
        {
            var sb = new StringBuilder();
            sb.Append($"UPDATE {Table} SET ");

            var setParts = new List<string>();
            foreach (var kvp in SetColumns)
                setParts.Add($"{kvp.Key} = {kvp.Value}");

            sb.Append(string.Join(", ", setParts));

            if (WhereConditions.Count > 0)
                sb.Append(" WHERE ").Append(string.Join(" AND ", WhereConditions));

            return sb.ToString();
        }
    }
}


using System.Collections.Generic;
using System.Text;

namespace RestUtilities.QueryBuilder.Builders
{
    /// <summary>
    /// Constructor de consultas SQL del tipo DELETE.
    /// Permite construir sentencias DELETE con condiciones WHERE.
    /// </summary>
    public class DeleteQueryBuilder
    {
        /// <summary>Nombre de la tabla desde la que se eliminarán los registros.</summary>
        public string Table { get; set; }

        /// <summary>Condiciones WHERE que limitan la eliminación.</summary>
        public List<string> WhereConditions { get; set; } = new();

        /// <summary>
        /// Construye la sentencia SQL DELETE.
        /// </summary>
        /// <returns>Consulta SQL generada.</returns>
        public string Build()
        {
            var sb = new StringBuilder();
            sb.Append($"DELETE FROM {Table}");

            if (WhereConditions.Count > 0)
                sb.Append(" WHERE ").Append(string.Join(" AND ", WhereConditions));

            return sb.ToString();
        }
    }
}


using RestUtilities.QueryBuilder.Enums;
using System.Text;

namespace RestUtilities.QueryBuilder.Builders
{
    /// <summary>
    /// Constructor para sentencias JOIN SQL.
    /// Permite agregar JOINs con sus respectivos tipos y condiciones ON.
    /// </summary>
    public class JoinBuilder
    {
        /// <summary>Tabla secundaria que se desea unir.</summary>
        public string JoinTable { get; set; }

        /// <summary>Condición que relaciona ambas tablas.</summary>
        public string JoinCondition { get; set; }

        /// <summary>Tipo de JOIN (INNER, LEFT, etc.).</summary>
        public SqlJoinType JoinType { get; set; } = SqlJoinType.Inner;

        /// <summary>
        /// Construye el fragmento de JOIN SQL.
        /// </summary>
        /// <returns>Fragmento SQL del JOIN.</returns>
        public string Build()
        {
            var joinTypeStr = JoinType switch
            {
                SqlJoinType.Left => "LEFT JOIN",
                SqlJoinType.Right => "RIGHT JOIN",
                SqlJoinType.Full => "FULL JOIN",
                SqlJoinType.Self => "JOIN", // El SELF JOIN se especifica con alias
                _ => "INNER JOIN"
            };

            return new StringBuilder()
                .Append($"{joinTypeStr} {JoinTable} ON {JoinCondition}")
                .ToString();
        }
    }
}
