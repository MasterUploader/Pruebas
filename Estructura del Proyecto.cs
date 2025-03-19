using System;
using System.Collections.Concurrent;
using RestUtilities.Connections.Interfaces;

namespace RestUtilities.Connections.Managers
{
    /// <summary>
    /// Administra conexiones WebSocket y permite reutilizar sesiones activas.
    /// </summary>
    public class WebSocketManager : IDisposable
    {
        private readonly ConcurrentDictionary<string, IWebSocketConnection> _webSocketConnections = new();

        public IWebSocketConnection GetWebSocketConnection(string uri)
        {
            return _webSocketConnections.GetOrAdd(uri, _ =>
            {
                var connection = new WebSocketConnectionProvider();
                connection.ConnectAsync(uri).GetAwaiter().GetResult();
                return connection;
            });
        }

        /// <summary>
        /// Libera todas las conexiones WebSocket activas.
        /// </summary>
        public void Dispose()
        {
            foreach (var connection in _webSocketConnections.Values)
            {
                connection.Dispose();
            }
            _webSocketConnections.Clear();
        }
    }
}
