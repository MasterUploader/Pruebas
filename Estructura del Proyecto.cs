using System;
using System.Collections.Concurrent;
using RestUtilities.Connections.Interfaces;

namespace RestUtilities.Connections.Managers
{
    /// <summary>
    /// Administra todas las conexiones disponibles en la aplicación.
    /// </summary>
    public class ConnectionManager : IConnectionManager
    {
        private readonly IServiceConnectionFactory _serviceConnectionFactory;
        private readonly ConcurrentDictionary<string, object> _activeConnections = new();

        public ConnectionManager(IServiceConnectionFactory serviceConnectionFactory)
        {
            _serviceConnectionFactory = serviceConnectionFactory;
        }

        /// <summary>
        /// Obtiene una conexión de base de datos según el nombre configurado.
        /// </summary>
        public IDatabaseConnection GetDatabaseConnection(string connectionName)
        {
            return _activeConnections.GetOrAdd(connectionName, _ =>
                _serviceConnectionFactory.CreateConnection<IDatabaseConnection>()) as IDatabaseConnection;
        }

        /// <summary>
        /// Obtiene una conexión a un servicio externo según el nombre configurado.
        /// </summary>
        public IExternalServiceConnection GetServiceConnection(string serviceName)
        {
            return _activeConnections.GetOrAdd(serviceName, _ =>
                _serviceConnectionFactory.CreateConnection<IExternalServiceConnection>()) as IExternalServiceConnection;
        }

        /// <summary>
        /// Libera todas las conexiones activas.
        /// </summary>
        public void Dispose()
        {
            foreach (var connection in _activeConnections.Values)
            {
                if (connection is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }
            _activeConnections.Clear();
        }
    }
}
