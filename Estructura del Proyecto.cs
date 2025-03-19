using System;
using System.Threading.Tasks;

namespace RestUtilities.Connections.Interfaces
{
    /// <summary>
    /// Define los métodos estándar para conexiones gRPC.
    /// </summary>
    public interface IGrpcConnection : IDisposable
    {
        /// <summary>
        /// Realiza una llamada a un servicio gRPC.
        /// </summary>
        /// <typeparam name="TRequest">Tipo de la solicitud.</typeparam>
        /// <typeparam name="TResponse">Tipo de la respuesta.</typeparam>
        /// <param name="method">Método gRPC.</param>
        /// <param name="request">Solicitud a enviar.</param>
        /// <returns>Respuesta gRPC.</returns>
        Task<TResponse> CallGrpcServiceAsync<TRequest, TResponse>(string method, TRequest request);
    }
}
