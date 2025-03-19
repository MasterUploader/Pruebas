using System;
using System.Collections.Concurrent;
using RestUtilities.Connections.Interfaces;

namespace RestUtilities.Connections.Managers
{
    /// <summary>
    /// Administra las conexiones a bases de datos y permite reutilizar instancias activas.
    /// </summary>
    public class DatabaseManager : IDisposable
    {
        private readonly IConnectionManager _connectionManager;
        private readonly ConcurrentDictionary<string, IDatabaseConnection> _databaseConnections = new();

        public DatabaseManager(IConnectionManager connectionManager)
        {
            _connectionManager = connectionManager;
        }

        /// <summary>
        /// Obtiene una conexión a base de datos, reutilizando si ya existe.
        /// </summary>
        public IDatabaseConnection GetDatabaseConnection(string connectionName)
        {
            return _databaseConnections.GetOrAdd(connectionName, _ =>
                _connectionManager.GetDatabaseConnection(connectionName));
        }

        /// <summary>
        /// Libera todas las conexiones a bases de datos activas.
        /// </summary>
        public void Dispose()
        {
            foreach (var connection in _databaseConnections.Values)
            {
                connection.Dispose();
            }
            _databaseConnections.Clear();
        }
    }
}
