using System;
using System.Collections.Generic;
using RestUtilities.Connections.Interfaces;

namespace RestUtilities.Connections.Providers.Database
{
    /// <summary>
    /// Fábrica para la creación de conexiones a bases de datos según el tipo configurado.
    /// </summary>
    public class DatabaseConnectionFactory
    {
        private readonly Dictionary<string, Func<string, IDatabaseConnection>> _providers;

        public DatabaseConnectionFactory()
        {
            _providers = new Dictionary<string, Func<string, IDatabaseConnection>>
            {
                { "AS400", connectionString => new AS400ConnectionProvider(connectionString) },
                { "MSSQL", connectionString => new MSSQLConnectionProvider(connectionString) },
                { "Oracle", connectionString => new OracleConnectionProvider(connectionString) }
                // Se pueden agregar más motores de BD aquí
            };
        }

        /// <summary>
        /// Crea una conexión a la base de datos según el tipo configurado.
        /// </summary>
        /// <param name="dbType">Tipo de base de datos (ejemplo: "MSSQL", "AS400").</param>
        /// <param name="connectionString">Cadena de conexión.</param>
        /// <returns>Instancia de `IDatabaseConnection`.</returns>
        public IDatabaseConnection CreateConnection(string dbType, string connectionString)
        {
            if (_providers.TryGetValue(dbType, out var provider))
            {
                return provider(connectionString);
            }

            throw new ArgumentException($"No se encontró un proveedor para el tipo de base de datos: {dbType}");
        }
    }
}
