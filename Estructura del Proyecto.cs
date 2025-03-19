using System;
using System.Collections.Concurrent;
using RestUtilities.Connections.Interfaces;

namespace RestUtilities.Connections.Managers
{
    /// <summary>
    /// Administra conexiones a servicios gRPC y permite reutilizar instancias activas.
    /// </summary>
    public class GrpcManager : IDisposable
    {
        private readonly ConcurrentDictionary<string, IGrpcConnection> _grpcConnections = new();

        public IGrpcConnection GetGrpcConnection(string endpoint)
        {
            return _grpcConnections.GetOrAdd(endpoint, _ =>
            {
                return new GrpcConnectionProvider(endpoint);
            });
        }

        /// <summary>
        /// Libera todas las conexiones gRPC activas.
        /// </summary>
        public void Dispose()
        {
            foreach (var connection in _grpcConnections.Values)
            {
                connection.Dispose();
            }
            _grpcConnections.Clear();
        }
    }
}
