using System;
using System.Threading.Tasks;
using System.Net.Http;

namespace RestUtilities.Connections.Interfaces
{
    /// <summary>
    /// Define los métodos estándar para conexiones a servicios externos REST o SOAP.
    /// </summary>
    public interface IExternalServiceConnection : IDisposable
    {
        /// <summary>
        /// Realiza una solicitud GET a un servicio externo.
        /// </summary>
        /// <param name="endpoint">URL del endpoint.</param>
        /// <returns>Respuesta en formato `HttpResponseMessage`.</returns>
        Task<HttpResponseMessage> GetAsync(string endpoint);

        /// <summary>
        /// Realiza una solicitud POST a un servicio externo.
        /// </summary>
        /// <param name="endpoint">URL del endpoint.</param>
        /// <param name="data">Datos en formato JSON.</param>
        /// <returns>Respuesta en formato `HttpResponseMessage`.</returns>
        Task<HttpResponseMessage> PostAsync(string endpoint, object data);

        /// <summary>
        /// Realiza una solicitud SOAP a un servicio externo.
        /// </summary>
        /// <param name="soapAction">Acción SOAP.</param>
        /// <param name="xmlBody">Cuerpo de la petición en XML.</param>
        /// <returns>Respuesta en formato `HttpResponseMessage`.</returns>
        Task<HttpResponseMessage> CallSoapServiceAsync(string soapAction, string xmlBody);
    }
}
