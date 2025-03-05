using Microsoft.AspNetCore.Http;
using System.Diagnostics;
using System.Text.Json;
using Logging.Abstractions;
using System.Text;

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
            // Iniciar medición del tiempo de ejecución
            var stopwatch = Stopwatch.StartNew();

            try
            {
                // Capturar información del entorno y request
                await CaptureEnvironmentInfoAsync(context);
                await CaptureRequestInfoAsync(context);

                // Guardar logs de inicio de ejecución en el archivo correspondiente
                _loggingService.SaveLogsToFile();

                // Continuar con la ejecución del siguiente middleware en la cadena
                await _next(context);

                // Capturar la respuesta HTTP después de procesar la solicitud
                await CaptureResponseInfoAsync(context);
            }
            catch (Exception ex)
            {
                // Capturar errores y guardarlos en el log
                _loggingService.AddExceptionLog(ex);
            }
            finally
            {
                // Detener el cronómetro y registrar el tiempo total de ejecución
                stopwatch.Stop();
                _loggingService.AddSingleLog($"[Tiempo Total de Ejecución]: {stopwatch.ElapsedMilliseconds} ms");

                // Guardar todos los logs en el archivo definitivo
                _loggingService.SaveLogsToFile();
            }
        }

        /// <summary>
        /// Captura información del entorno, como la máquina y el entorno de ejecución.
        /// </summary>
        private async Task CaptureEnvironmentInfoAsync(HttpContext context)
        {
            var envInfo = new
            {
                MachineName = Environment.MachineName,
                OS = Environment.OSVersion.ToString(),
                Framework = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription,
                Timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")
            };

            string formattedEnvInfo = JsonSerializer.Serialize(envInfo, new JsonSerializerOptions { WriteIndented = true });
            _loggingService.AddSingleLog($"[Environment Info]: {formattedEnvInfo}");
        }

        /// <summary>
        /// Captura información de la solicitud HTTP.
        /// </summary>
        private async Task CaptureRequestInfoAsync(HttpContext context)
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

            string formattedRequest = JsonSerializer.Serialize(requestInfo, new JsonSerializerOptions { WriteIndented = true });
            _loggingService.AddSingleLog($"[Request Info]: {formattedRequest}");
        }

        /// <summary>
        /// Captura información de la respuesta HTTP.
        /// </summary>
        private async Task CaptureResponseInfoAsync(HttpContext context)
        {
            var response = context.Response;
            var responseInfo = new
            {
                StatusCode = response.StatusCode,
                Headers = response.Headers.ToDictionary(h => h.Key, h => h.Value.ToString())
            };

            string formattedResponse = JsonSerializer.Serialize(responseInfo, new JsonSerializerOptions { WriteIndented = true });
            _loggingService.AddSingleLog($"[Response Info]: {formattedResponse}");
        }

        /// <summary>
        /// Lee el cuerpo de la solicitud HTTP sin afectar el flujo de la aplicación.
        /// </summary>
        private async Task<string> ReadRequestBodyAsync(HttpRequest request)
        {
            request.EnableBuffering();
            using var reader = new StreamReader(request.Body, Encoding.UTF8, leaveOpen: true);
            string body = await reader.ReadToEndAsync();
            request.Body.Position = 0; // Restablecer la posición del stream para que pueda ser leído nuevamente
            return body;
        }
    }
}
