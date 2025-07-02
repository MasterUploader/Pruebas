using RestUtilities.QueryBuilder.Models;

namespace RestUtilities.QueryBuilder.Translators
{
    /// <summary>
    /// Define un contrato para traducir un objeto de construcción de consulta a una sentencia SQL final.
    /// </summary>
    public interface IQueryTranslator
    {
        /// <summary>
        /// Traduce el contexto de consulta a SQL final compatible con el motor correspondiente.
        /// </summary>
        /// <param name="context">Contexto con la consulta construida.</param>
        /// <returns>Cadena SQL final traducida.</returns>
        string Translate(QueryTranslationContext context);
    }
}


using RestUtilities.QueryBuilder.Models;
using System.Text;

namespace RestUtilities.QueryBuilder.Translators
{
    /// <summary>
    /// Traductor de consultas específico para AS400 (DB2).
    /// </summary>
    public class As400QueryTranslator : IQueryTranslator
    {
        /// <inheritdoc />
        public string Translate(QueryTranslationContext context)
        {
            var sb = new StringBuilder();

            // Ejemplo: Traducción básica
            sb.Append("SELECT ");
            sb.Append(string.Join(", ", context.SelectColumns));
            sb.Append(" FROM ");
            sb.Append(context.TableName);

            if (!string.IsNullOrWhiteSpace(context.WhereClause))
            {
                sb.Append(" WHERE ");
                sb.Append(context.WhereClause);
            }

            if (!string.IsNullOrWhiteSpace(context.OrderByClause))
            {
                sb.Append(" ORDER BY ");
                sb.Append(context.OrderByClause);
            }

            if (context.Offset.HasValue && context.Limit.HasValue)
            {
                sb.Append($" OFFSET {context.Offset.Value} ROWS FETCH NEXT {context.Limit.Value} ROWS ONLY");
            }

            return sb.ToString();
        }
    }
}

using RestUtilities.QueryBuilder.Models;
using System.Text;

namespace RestUtilities.QueryBuilder.Translators
{
    /// <summary>
    /// Traductor de consultas para bases de datos Oracle.
    /// </summary>
    public class OracleQueryTranslator : IQueryTranslator
    {
        /// <inheritdoc />
        public string Translate(QueryTranslationContext context)
        {
            var sb = new StringBuilder();

            sb.Append("SELECT ");
            sb.Append(string.Join(", ", context.SelectColumns));
            sb.Append(" FROM ");
            sb.Append(context.TableName);

            if (!string.IsNullOrWhiteSpace(context.WhereClause))
            {
                sb.Append(" WHERE ");
                sb.Append(context.WhereClause);
            }

            if (!string.IsNullOrWhiteSpace(context.OrderByClause))
            {
                sb.Append(" ORDER BY ");
                sb.Append(context.OrderByClause);
            }

            if (context.Offset.HasValue && context.Limit.HasValue)
            {
                sb.Append($" OFFSET {context.Offset.Value} ROWS FETCH NEXT {context.Limit.Value} ROWS ONLY");
            }

            return sb.ToString();
        }
    }
}

using RestUtilities.QueryBuilder.Models;
using System.Text;

namespace RestUtilities.QueryBuilder.Translators
{
    /// <summary>
    /// Traductor de consultas para SQL Server.
    /// </summary>
    public class SqlServerQueryTranslator : IQueryTranslator
    {
        /// <inheritdoc />
        public string Translate(QueryTranslationContext context)
        {
            var sb = new StringBuilder();

            sb.Append("SELECT ");
            sb.Append(string.Join(", ", context.SelectColumns));
            sb.Append(" FROM ");
            sb.Append(context.TableName);

            if (!string.IsNullOrWhiteSpace(context.WhereClause))
            {
                sb.Append(" WHERE ");
                sb.Append(context.WhereClause);
            }

            if (!string.IsNullOrWhiteSpace(context.OrderByClause))
            {
                sb.Append(" ORDER BY ");
                sb.Append(context.OrderByClause);
            }

            if (context.Offset.HasValue && context.Limit.HasValue)
            {
                sb.Append($" OFFSET {context.Offset.Value} ROWS FETCH NEXT {context.Limit.Value} ROWS ONLY");
            }

            return sb.ToString();
        }
    }
}

using System.Collections.Generic;

namespace RestUtilities.QueryBuilder.Models
{
    /// <summary>
    /// Representa el contexto que contiene los elementos necesarios para construir una consulta SQL.
    /// </summary>
    public class QueryTranslationContext
    {
        /// <summary>
        /// Nombre de la tabla sobre la que se ejecutará la consulta.
        /// </summary>
        public string TableName { get; set; }

        /// <summary>
        /// Lista de columnas a seleccionar.
        /// </summary>
        public List<string> SelectColumns { get; set; } = new();

        /// <summary>
        /// Cláusula WHERE generada dinámicamente.
        /// </summary>
        public string? WhereClause { get; set; }

        /// <summary>
        /// Cláusula ORDER BY.
        /// </summary>
        public string? OrderByClause { get; set; }

        /// <summary>
        /// Número de filas a omitir (para paginación).
        /// </summary>
        public int? Offset { get; set; }

        /// <summary>
        /// Número de filas a recuperar después del OFFSET.
        /// </summary>
        public int? Limit { get; set; }
    }
}
