using System;
using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Logging.Helpers;
using Logging.Formatters;

namespace Logging.Handlers
{
    /// <summary>
    /// Handler personalizado para interceptar y registrar llamadas HTTP salientes realizadas mediante HttpClient.
    /// Este log se integrará automáticamente con el archivo de log del Middleware.
    /// </summary>
    public class HttpClientLoggingHandler : DelegatingHandler
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public HttpClientLoggingHandler(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        }

        /// <summary>
        /// Intercepta la solicitud y la respuesta del HttpClient, y guarda su información en HttpContext.Items.
        /// </summary>
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var stopwatch = Stopwatch.StartNew();
            var context = _httpContextAccessor.HttpContext;

            // Prevenir ejecución fuera de un contexto HTTP válido (por ejemplo en tareas en background)
            if (context == null)
                return await base.SendAsync(request, cancellationToken);

            string traceId = context.TraceIdentifier ?? Guid.NewGuid().ToString();

            try
            {
                // Realizar la solicitud
                HttpResponseMessage response = await base.SendAsync(request, cancellationToken);
                stopwatch.Stop();

                // Formatear el log
                string formatted = LogFormatter.FormatHttpClientRequest(
                    traceId: traceId,
                    method: request.Method.Method,
                    url: request.RequestUri.ToString(),
                    statusCode: ((int)response.StatusCode).ToString(),
                    elapsedMs: stopwatch.ElapsedMilliseconds,
                    headers: request.Headers.ToString(),
                    body: request.Content != null ? await request.Content.ReadAsStringAsync() : null
                );

                // Guardar en el contexto para que lo consuma el LoggingMiddleware
                AppendHttpClientLogToContext(context, formatted);

                return response;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                // En caso de excepción, guardar log de error
                string errorLog = LogFormatter.FormatHttpClientError(
                    traceId: traceId,
                    method: request.Method.Method,
                    url: request.RequestUri.ToString(),
                    exception: ex
                );

                AppendHttpClientLogToContext(context, errorLog);

                throw;
            }
        }

        /// <summary>
        /// Agrega el log de HttpClient a la lista en HttpContext.Items, para que luego sea procesado por el Middleware.
        /// </summary>
        private void AppendHttpClientLogToContext(HttpContext context, string logEntry)
        {
            const string key = "HttpClientLogs";

            if (!context.Items.ContainsKey(key))
                context.Items[key] = new StringBuilder();

            if (context.Items[key] is StringBuilder sb)
                sb.AppendLine(logEntry);
        }
    }
}
