using System;
using System.Threading.Tasks;
using Grpc.Net.Client;
using RestUtilities.Connections.Interfaces;

namespace RestUtilities.Connections.Providers.Services
{
    /// <summary>
    /// Cliente para conexiones gRPC.
    /// </summary>
    public class GrpcConnectionProvider : IGrpcConnection
    {
        private readonly GrpcChannel _channel;

        public GrpcConnectionProvider(string baseUrl)
        {
            _channel = GrpcChannel.ForAddress(baseUrl);
        }

        public async Task<TResponse> CallGrpcServiceAsync<TRequest, TResponse>(string method, TRequest request)
        {
            // Implementación de gRPC con cliente dinámico
            throw new NotImplementedException("El método debe ser implementado según el contrato gRPC.");
        }

        public void Dispose()
        {
            _channel.ShutdownAsync().Wait();
        }
    }
}
