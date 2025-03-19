using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using RestUtilities.Connections.Interfaces;

namespace RestUtilities.Connections.Providers.Services
{
    /// <summary>
    /// Cliente para conexiones a servicios SOAP.
    /// </summary>
    public class SoapServiceClient : IExternalServiceConnection
    {
        private readonly HttpClient _httpClient;

        public SoapServiceClient(string baseUrl)
        {
            _httpClient = new HttpClient { BaseAddress = new Uri(baseUrl) };
        }

        public async Task<HttpResponseMessage> CallSoapServiceAsync(string soapAction, string xmlBody)
        {
            var content = new StringContent(xmlBody, Encoding.UTF8, "text/xml");
            content.Headers.Add("SOAPAction", soapAction);
            return await _httpClient.PostAsync("", content);
        }

        public void Dispose()
        {
            _httpClient.Dispose();
        }
    }
}
