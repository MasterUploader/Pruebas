using RestUtilities.QueryBuilder.Interfaces;

namespace RestUtilities.QueryBuilder.Engines
{
    /// <summary>
    /// Traductor de sentencias SQL específico para IBM AS400 (DB2 for i).
    /// Adapta ciertas funciones y cláusulas que varían respecto a SQL estándar.
    /// </summary>
    public class As400SqlTranslator : ISqlEngineTranslator
    {
        /// <inheritdoc />
        public string TranslateEngineSpecific(string query)
        {
            // Adaptaciones específicas para AS400:
            // - FETCH FIRST N ROWS ONLY no siempre está soportado, se puede usar RRN o subquery
            // - NVL() en lugar de COALESCE()
            // - CONCAT con ||
            // Aquí se podrían aplicar transformaciones condicionales
            return query
                .Replace("COALESCE", "NVL")
                .Replace("||", " CONCAT ")
                .Replace("FETCH NEXT", "FETCH FIRST"); // si aplica según versión
        }
    }
}

using RestUtilities.QueryBuilder.Interfaces;

namespace RestUtilities.QueryBuilder.Engines
{
    /// <summary>
    /// Traductor de sentencias SQL específico para Microsoft SQL Server.
    /// Adapta funciones como ISNULL, TOP, y FETCH según compatibilidad.
    /// </summary>
    public class SqlServerSqlTranslator : ISqlEngineTranslator
    {
        /// <inheritdoc />
        public string TranslateEngineSpecific(string query)
        {
            return query
                .Replace("COALESCE", "ISNULL") // SQL Server prefiere ISNULL
                .Replace("LIMIT", "")          // No se usa LIMIT
                .Replace("FETCH FIRST", "FETCH NEXT"); // si se usa OFFSET/FETCH
        }
    }
}

using RestUtilities.QueryBuilder.Interfaces;

namespace RestUtilities.QueryBuilder.Engines
{
    /// <summary>
    /// Traductor de sentencias SQL específico para Oracle Database.
    /// Incluye adaptaciones como NVL, ROWNUM y funciones propias de Oracle.
    /// </summary>
    public class OracleSqlTranslator : ISqlEngineTranslator
    {
        /// <inheritdoc />
        public string TranslateEngineSpecific(string query)
        {
            return query
                .Replace("COALESCE", "NVL")
                .Replace("LIMIT", "") // Oracle usa ROWNUM o FETCH
                .Replace("FETCH NEXT", "FETCH FIRST");
        }
    }
}

using RestUtilities.QueryBuilder.Interfaces;

namespace RestUtilities.QueryBuilder.Engines
{
    /// <summary>
    /// Traductor de sentencias SQL específico para PostgreSQL.
    /// Soporta LIMIT, OFFSET, COALESCE y funciones estándar.
    /// </summary>
    public class PostgreSqlTranslator : ISqlEngineTranslator
    {
        /// <inheritdoc />
        public string TranslateEngineSpecific(string query)
        {
            return query; // PostgreSQL usa muchas convenciones estándar
        }
    }
}
using RestUtilities.QueryBuilder.Interfaces;

namespace RestUtilities.QueryBuilder.Engines
{
    /// <summary>
    /// Traductor de sentencias SQL específico para MySQL.
    /// Incluye soporte para funciones como IFNULL, LIMIT y operadores estándar.
    /// </summary>
    public class MySqlTranslator : ISqlEngineTranslator
    {
        /// <inheritdoc />
        public string TranslateEngineSpecific(string query)
        {
            return query
                .Replace("COALESCE", "IFNULL");
        }
    }
}

using RestUtilities.QueryBuilder.Interfaces;

namespace RestUtilities.QueryBuilder.Engines
{
    /// <summary>
    /// Traductor de sentencias SQL específico para MySQL.
    /// Incluye soporte para funciones como IFNULL, LIMIT y operadores estándar.
    /// </summary>
    public class MySqlTranslator : ISqlEngineTranslator
    {
        /// <inheritdoc />
        public string TranslateEngineSpecific(string query)
        {
            return query
                .Replace("COALESCE", "IFNULL");
        }
    }
}

