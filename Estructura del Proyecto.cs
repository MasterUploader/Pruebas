using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Logging.Services;
using Microsoft.AspNetCore.Http;

namespace Logging.Middleware
{
    /// <summary>
    /// Middleware de logging que captura todas las solicitudes HTTP y sus respuestas.
    /// </summary>
    public class LoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly LoggingService _loggingService;

        /// <summary>
        /// Constructor que inyecta el siguiente middleware en la canalización y el servicio de logging.
        /// </summary>
        /// <param name="next">Delegado del middleware siguiente en la canalización.</param>
        /// <param name="loggingService">Instancia del servicio de logging.</param>
        public LoggingMiddleware(RequestDelegate next, LoggingService loggingService)
        {
            _next = next;
            _loggingService = loggingService;
        }

        /// <summary>
        /// Método principal que captura la solicitud y la respuesta HTTP.
        /// </summary>
        /// <param name="context">Contexto HTTP de la petición actual.</param>
        public async Task InvokeAsync(HttpContext context)
        {
            string executionId = Guid.NewGuid().ToString(); // Identificador único de la ejecución
            context.Items["ExecutionId"] = executionId; // Guarda el ID en el contexto de la petición

            // Capturar la información de la solicitud antes de enviarla al controlador
            string requestLog = await CaptureRequestInfoAsync(context);
            _loggingService.WriteLog(context, requestLog);

            // Capturar la respuesta después de que el controlador la procese
            var originalBodyStream = context.Response.Body;
            using (var responseBody = new MemoryStream())
            {
                context.Response.Body = responseBody;

                // Continuar con el siguiente middleware en la cadena de ejecución
                await _next(context);

                // Capturar y registrar la respuesta HTTP
                string responseLog = await CaptureResponseInfoAsync(context);
                _loggingService.WriteLog(context, responseLog);

                // Copiar la respuesta original al cuerpo de la respuesta para que llegue al cliente
                responseBody.Seek(0, SeekOrigin.Begin);
                await responseBody.CopyToAsync(originalBodyStream);
            }
        }

        /// <summary>
        /// Captura la información de la solicitud HTTP antes de que sea procesada por los controladores.
        /// </summary>
        /// <param name="context">Contexto HTTP de la petición actual.</param>
        private async Task<string> CaptureRequestInfoAsync(HttpContext context)
        {
            context.Request.EnableBuffering(); // Permite leer el cuerpo de la petición sin afectar la ejecución

            using var reader = new StreamReader(context.Request.Body, Encoding.UTF8, leaveOpen: true);
            string body = await reader.ReadToEndAsync();
            context.Request.Body.Position = 0; // Restablece la posición para que el controlador pueda leerlo

            return LogFormatter.FormatRequestInfo(
                method: context.Request.Method,
                path: context.Request.Path,
                queryParams: context.Request.QueryString.ToString(),
                body: body
            );
        }

        /// <summary>
        /// Captura la información de la respuesta HTTP antes de enviarla al cliente.
        /// </summary>
        /// <param name="context">Contexto HTTP de la petición actual.</param>
        private async Task<string> CaptureResponseInfoAsync(HttpContext context)
        {
            context.Response.Body.Seek(0, SeekOrigin.Begin);
            using var reader = new StreamReader(context.Response.Body, Encoding.UTF8, leaveOpen: true);
            string body = await reader.ReadToEndAsync();
            context.Response.Body.Seek(0, SeekOrigin.Begin);

            return LogFormatter.FormatResponseInfo(
                statusCode: context.Response.StatusCode.ToString(),
                headers: string.Join("; ", context.Response.Headers),
                body: body
            );
        }
    }
}
