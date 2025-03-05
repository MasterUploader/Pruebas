
    /// <summary>
    /// Middleware para capturar logs de ejecución de controladores en la API.
    /// Captura información de Request, Response, Excepciones y Entorno.
    /// </summary>
    public class LoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILoggingService _loggingService;
        private readonly LogFormatter _logFormatter; // Se usa directamente LogFormatter

        /// <summary>
        /// Constructor del Middleware que recibe el servicio de logs inyectado.
        /// </summary>
        public LoggingMiddleware(RequestDelegate next, ILoggingService loggingService)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
            _loggingService = loggingService ?? throw new ArgumentNullException(nameof(loggingService));
            _logFormatter = new LogFormatter(); // Se instancia directamente
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

                // 2️⃣ Capturar información del entorno y escribirlo en el log
                await CaptureEnvironmentInfoAsync(context);

                // 3️⃣ Capturar y escribir en el log la información de la solicitud HTTP
                string requestLog = await CaptureRequestInfoAsync(context);
                _loggingService.WriteLog(context, requestLog);

                // 4️⃣ Reemplazar el Stream original de respuesta para capturarla
                var originalBodyStream = context.Response.Body;
                using (var responseBody = new MemoryStream())
                {
                    context.Response.Body = responseBody;

                    // 5️⃣ Continuar con la ejecución del pipeline
                    await _next(context);

                    // 6️⃣ Capturar la respuesta y agregarla al log
                    string responseLog = await CaptureResponseInfoAsync(context);
                    _loggingService.WriteLog(context, responseLog);

                    // 7️⃣ Restaurar el stream original para que el API pueda responder correctamente
                    responseBody.Seek(0, SeekOrigin.Begin);
                    await responseBody.CopyToAsync(originalBodyStream);
                }

                // 8️⃣ Verificar si hubo alguna excepción en la ejecución y loguearla
                if (context.Items.ContainsKey("Exception"))
                {
                    Exception ex = context.Items["Exception"] as Exception;
                    _loggingService.AddExceptionLog(ex);
                }
            }
            catch (Exception ex)
            {
                // 9️⃣ Manejo de excepciones para evitar que el middleware interrumpa la API
                _loggingService.AddExceptionLog(ex);
            }
            finally
            {
                // 🔟 Detener el cronómetro y registrar el tiempo total de ejecución
                stopwatch.Stop();
                _loggingService.WriteLog(context, $"[Tiempo Total de Ejecución]: {stopwatch.ElapsedMilliseconds} ms");
            }
        }

        /// <summary>
        /// Captura información del entorno y lo escribe en el log.
        /// </summary>
        private async Task CaptureEnvironmentInfoAsync(HttpContext context)
        {
            string envLog = await _logFormatter.FormatEnvironmentInfoAsync();
            _loggingService.WriteLog(context, envLog);
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

            return _logFormatter.FormatRequestInfo(requestInfo);
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

            return _logFormatter.FormatResponseInfo(responseInfo);
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

