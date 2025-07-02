using System;
using System.Collections.Generic;

namespace RestUtilities.QueryBuilder.Compatibility
{
    /// <summary>
    /// Servicio que permite validar si una función, cláusula u operador SQL
    /// es compatible con un determinado motor de base de datos.
    /// </summary>
    public static class SqlCompatibilityService
    {
        /// <summary>
        /// Enum que representa los motores de bases de datos soportados.
        /// </summary>
        public enum SqlEngine
        {
            As400,
            SqlServer,
            Oracle,
            MySql,
            PostgreSql
        }

        /// <summary>
        /// Diccionario que mapea cada función SQL a los motores donde está soportada.
        /// </summary>
        private static readonly Dictionary<string, HashSet<SqlEngine>> _compatibilityMap = new(StringComparer.OrdinalIgnoreCase)
        {
            { "FETCH", new() { SqlEngine.As400, SqlEngine.SqlServer, SqlEngine.Oracle, SqlEngine.PostgreSql } },
            { "OFFSET", new() { SqlEngine.As400, SqlEngine.SqlServer, SqlEngine.Oracle, SqlEngine.PostgreSql } },
            { "ISNULL", new() { SqlEngine.SqlServer } },
            { "IFNULL", new() { SqlEngine.MySql } },
            { "NVL", new() { SqlEngine.Oracle } },
            { "COALESCE", new() { SqlEngine.As400, SqlEngine.SqlServer, SqlEngine.Oracle, SqlEngine.MySql } },
            { "TOP", new() { SqlEngine.SqlServer } },
            { "LIMIT", new() { SqlEngine.MySql, SqlEngine.PostgreSql } },
            { "ROWNUM", new() { SqlEngine.Oracle } },
            { "SELECT INTO", new() { SqlEngine.SqlServer, SqlEngine.As400 } },
            { "INSERT INTO SELECT", new() { SqlEngine.SqlServer, SqlEngine.Oracle, SqlEngine.As400 } },
            { "CASE", new() { SqlEngine.SqlServer, SqlEngine.Oracle, SqlEngine.As400, SqlEngine.MySql } },
            { "EXISTS", new() { SqlEngine.SqlServer, SqlEngine.Oracle, SqlEngine.As400 } },
            { "ALL", new() { SqlEngine.SqlServer, SqlEngine.Oracle } },
            { "ANY", new() { SqlEngine.SqlServer, SqlEngine.Oracle } },
        };

        /// <summary>
        /// Determina si una función o cláusula está soportada por un motor SQL.
        /// </summary>
        /// <param name="sqlFeature">Nombre de la función o cláusula (por ejemplo: "ISNULL", "LIMIT").</param>
        /// <param name="engine">Motor SQL objetivo.</param>
        /// <returns>Verdadero si está soportado, falso en caso contrario.</returns>
        public static bool IsCompatible(string sqlFeature, SqlEngine engine)
        {
            if (_compatibilityMap.TryGetValue(sqlFeature.ToUpperInvariant(), out var engines))
            {
                return engines.Contains(engine);
            }

            // Si no está registrado, asumimos que no es compatible
            return false;
        }

        /// <summary>
        /// Devuelve la lista de motores compatibles con una función SQL específica.
        /// </summary>
        /// <param name="sqlFeature">Nombre de la función o palabra clave.</param>
        /// <returns>Lista de motores compatibles.</returns>
        public static List<SqlEngine> GetCompatibleEngines(string sqlFeature)
        {
            if (_compatibilityMap.TryGetValue(sqlFeature.ToUpperInvariant(), out var engines))
            {
                return new List<SqlEngine>(engines);
            }

            return new List<SqlEngine>();
        }
    }
}


