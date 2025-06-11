using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Logging.Abstractions;
using Microsoft.AspNetCore.Http;

namespace RestUtilities.Logging.Handlers
{
    /// <summary>
    /// Interceptor para registrar automáticamente logs de solicitudes HTTP realizadas por HttpClient.
    /// </summary>
    public class HttpClientLoggingHandler : DelegatingHandler
    {
        private readonly ILoggingService _loggingService;
        private readonly IHttpContextAccessor _httpContextAccessor;

        /// <summary>
        /// Inicializa el interceptor con servicios de logging e información de contexto HTTP.
        /// </summary>
        public HttpClientLoggingHandler(
            ILoggingService loggingService,
            IHttpContextAccessor httpContextAccessor)
        {
            _loggingService = loggingService ?? throw new ArgumentNullException(nameof(loggingService));
            _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        }

        /// <summary>
        /// Intercepta la ejecución de llamadas HTTP y registra la solicitud y respuesta.
        /// </summary>
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var stopwatch = Stopwatch.StartNew();

            // Leer contenido del request si existe
            string? requestBody = null;
            if (request.Content != null)
                requestBody = await request.Content.ReadAsStringAsync(cancellationToken);

            var headers = request.Headers.ToDictionary(h => h.Key, h => string.Join(", ", h.Value));
            HttpResponseMessage? response = null;
            string? responseBody = null;

            try
            {
                // Ejecutar la llamada
                response = await base.SendAsync(request, cancellationToken);

                // Leer contenido del response si existe
                if (response.Content != null)
                    responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

                stopwatch.Stop();

                // Usar el formateador centralizado
                string formattedLog = LogFormatter.FormatHttpClientLog(
                    method: request.Method.Method,
                    url: request.RequestUri?.ToString() ?? "Desconocido",
                    headers: headers,
                    requestBody: requestBody,
                    statusCode: response.StatusCode,
                    responseBody: responseBody,
                    durationMs: stopwatch.ElapsedMilliseconds
                );

                // Registrar log completo usando ILoggingService
                _loggingService.Write(_httpContextAccessor.HttpContext!, formattedLog);

                return response;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                string errorLog = LogFormatter.FormatHttpClientError(
                    method: request.Method.Method,
                    url: request.RequestUri?.ToString() ?? "Desconocido",
                    exceptionMessage: ex.Message,
                    durationMs: stopwatch.ElapsedMilliseconds
                );

                _loggingService.Write(_httpContextAccessor.HttpContext!, errorLog);

                throw;
            }
        }
    }
}
