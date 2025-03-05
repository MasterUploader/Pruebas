using Microsoft.AspNetCore.Http;
using Logging.Abstractions;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Logging.Middleware
{
    /// <summary>
    /// Middleware para capturar logs de ejecución de controladores en la API.
    /// Captura información de Request, Response, Excepciones y Entorno.
    /// </summary>
    public class LoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILoggingService _loggingService;

        /// <summary>
        /// Constructor del Middleware que recibe el servicio de logs inyectado.
        /// </summary>
        public LoggingMiddleware(RequestDelegate next, ILoggingService loggingService)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
            _loggingService = loggingService ?? throw new ArgumentNullException(nameof(loggingService));
        }

        /// <summary>
        /// Método principal del Middleware que intercepta las solicitudes HTTP.
        /// </summary>
        public async Task InvokeAsync(HttpContext context)
        {
            var stopwatch = Stopwatch.StartNew(); // Iniciar medición de tiempo

            try
            {
                // 1️⃣ Asegurar que exista un ExecutionId único para la solicitud
                if (!context.Items.ContainsKey("ExecutionId"))
                {
                    context.Items["ExecutionId"] = Guid.NewGuid().ToString();
                }

                // 2️⃣ Capturar información del entorno y request
                _loggingService.WriteLog(context, _loggingService.FormatEnvironmentInfoStart());
                _loggingService.WriteLog(context, await CaptureRequestInfoAsync(context));

                // 3️⃣ Reemplazar el Stream original de respuesta para capturarla
                var originalBodyStream = context.Response.Body;
                using (var responseBody = new MemoryStream())
                {
                    context.Response.Body = responseBody;

                    // 4️⃣ Continuar con la ejecución del pipeline
                    await _next(context);

                    // 5️⃣ Capturar la respuesta y agregarla al log
                    _loggingService.WriteLog(context, await CaptureResponseInfoAsync(context));

                    // 6️⃣ Restaurar el stream original para que el API pueda responder correctamente
                    responseBody.Seek(0, SeekOrigin.Begin);
                    await responseBody.CopyToAsync(originalBodyStream);
                }

                // 7️⃣ Verificar si hubo alguna excepción en la ejecución y loguearla
                if (context.Items.ContainsKey("Exception"))
                {
                    Exception ex = context.Items["Exception"] as Exception;
                    _loggingService.AddExceptionLog(ex);
                }
            }
            catch (Exception ex)
            {
                // 8️⃣ Manejo de excepciones para evitar que el middleware interrumpa la API
                _loggingService.AddExceptionLog(ex);
            }
            finally
            {
                // 9️⃣ Detener el cronómetro y registrar el tiempo total de ejecución
                stopwatch.Stop();
                _loggingService.WriteLog(context, $"[Tiempo Total de Ejecución]: {stopwatch.ElapsedMilliseconds} ms");

                // 🔟 Guardar todos los logs en un solo archivo consolidado
                _loggingService.SaveLogsToFile();
            }
        }

        /// <summary>
        /// Captura información detallada de la solicitud HTTP.
        /// </summary>
        private async Task<string> CaptureRequestInfoAsync(HttpContext context)
        {
            var request = context.Request;
            var requestInfo = new
            {
                Method = request.Method,
                Path = request.Path,
                Query = request.QueryString.ToString(),
                Headers = request.Headers.ToDictionary(h => h.Key, h => h.Value.ToString()),
                Body = await ReadRequestBodyAsync(request)
            };

            return _loggingService.FormatRequestInfo(requestInfo);
        }

        /// <summary>
        /// Captura información detallada de la respuesta HTTP.
        /// </summary>
        private async Task<string> CaptureResponseInfoAsync(HttpContext context)
        {
            var response = context.Response;
            var responseInfo = new
            {
                StatusCode = response.StatusCode,
                Headers = response.Headers.ToDictionary(h => h.Key, h => h.Value.ToString())
            };

            return _loggingService.FormatResponseInfo(responseInfo);
        }

        /// <summary>
        /// Lee el cuerpo de la solicitud HTTP sin afectar el flujo de la aplicación.
        /// </summary>
        private async Task<string> ReadRequestBodyAsync(HttpRequest request)
        {
            request.EnableBuffering();
            using var reader = new StreamReader(request.Body, Encoding.UTF8, leaveOpen: true);
            string body = await reader.ReadToEndAsync();
            request.Body.Position = 0; // Restablecer el Stream para que pueda ser leído nuevamente
            return body;
        }
    }
}
