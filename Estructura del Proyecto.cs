using System;
using System.Threading.Tasks;

namespace RestUtilities.Connections.Interfaces
{
    /// <summary>
    /// Define los métodos estándar para conexiones WebSocket.
    /// </summary>
    public interface IWebSocketConnection : IDisposable
    {
        /// <summary>
        /// Conecta con un servidor WebSocket.
        /// </summary>
        /// <param name="uri">URL del servidor WebSocket.</param>
        Task ConnectAsync(string uri);

        /// <summary>
        /// Envía un mensaje al servidor WebSocket.
        /// </summary>
        /// <param name="message">Mensaje a enviar.</param>
        Task SendMessageAsync(string message);

        /// <summary>
        /// Recibe un mensaje del servidor WebSocket.
        /// </summary>
        /// <returns>Mensaje recibido.</returns>
        Task<string> ReceiveMessageAsync();
    }
}
