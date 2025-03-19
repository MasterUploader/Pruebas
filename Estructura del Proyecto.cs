using System;
using System.Collections.Concurrent;
using RestUtilities.Connections.Interfaces;

namespace RestUtilities.Connections.Managers
{
    /// <summary>
    /// Administra conexiones a servicios externos REST y SOAP.
    /// </summary>
    public class ServiceManager : IDisposable
    {
        private readonly IConnectionManager _connectionManager;
        private readonly ConcurrentDictionary<string, IExternalServiceConnection> _serviceConnections = new();

        public ServiceManager(IConnectionManager connectionManager)
        {
            _connectionManager = connectionManager;
        }

        /// <summary>
        /// Obtiene una conexión a un servicio externo, reutilizando si ya existe.
        /// </summary>
        public IExternalServiceConnection GetServiceConnection(string serviceName)
        {
            return _serviceConnections.GetOrAdd(serviceName, _ =>
                _connectionManager.GetServiceConnection(serviceName));
        }

        /// <summary>
        /// Libera todas las conexiones a servicios externos activas.
        /// </summary>
        public void Dispose()
        {
            foreach (var connection in _serviceConnections.Values)
            {
                connection.Dispose();
            }
            _serviceConnections.Clear();
        }
    }
}
