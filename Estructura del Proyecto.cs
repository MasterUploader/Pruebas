using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using RestUtilities.Connections.Interfaces;

namespace RestUtilities.Connections.Providers.Services
{
    /// <summary>
    /// Cliente para conexiones a servicios RESTful.
    /// </summary>
    public class RestServiceClient : IExternalServiceConnection
    {
        private readonly HttpClient _httpClient;

        public RestServiceClient(string baseUrl)
        {
            _httpClient = new HttpClient { BaseAddress = new Uri(baseUrl) };
        }

        public async Task<HttpResponseMessage> GetAsync(string endpoint)
        {
            return await _httpClient.GetAsync(endpoint);
        }

        public async Task<HttpResponseMessage> PostAsync(string endpoint, object data)
        {
            var jsonContent = JsonSerializer.Serialize(data);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
            return await _httpClient.PostAsync(endpoint, content);
        }

        public void Dispose()
        {
            _httpClient.Dispose();
        }
    }
}
