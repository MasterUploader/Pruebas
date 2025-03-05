
    /// <summary>
    /// Middleware para capturar logs de ejecución de controladores en la API.
    /// Captura información de Request, Response, Excepciones y Entorno.
    /// </summary>
    public class LoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILoggingService _loggingService;
        private readonly LogFormatter _logFormatter; // Instancia de formateador de logs

        /// <summary>
        /// Constructor del Middleware que recibe el servicio de logs inyectado.
        /// </summary>
        public LoggingMiddleware(RequestDelegate next, ILoggingService loggingService)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
            _loggingService = loggingService ?? throw new ArgumentNullException(nameof(loggingService));
            _logFormatter = new LogFormatter(); // Instancia del formateador
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
                string envLog = await CaptureEnvironmentInfoAsync(context);
                _loggingService.WriteLog(context, envLog);

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
        /// Captura la información del entorno del servidor y del cliente.
        /// </summary>
        private async Task<string> CaptureEnvironmentInfoAsync(HttpContext context)
        {
            return LogFormatter.FormatEnvironmentInfoStart(
                application: context.RequestServices.GetService<IHostEnvironment>()?.ApplicationName ?? "Desconocido",
                env: context.RequestServices.GetService<IHostEnvironment>()?.EnvironmentName ?? "Desconocido",
                contentRoot: context.RequestServices.GetService<IHostEnvironment>()?.ContentRootPath ?? "Desconocido",
                executionId: context.TraceIdentifier ?? "Desconocido",
                clientIp: context.Connection.RemoteIpAddress?.ToString() ?? "Desconocido",
                userAgent: context.Request.Headers["User-Agent"].ToString() ?? "Desconocido",
                machineName: Environment.MachineName,
                os: Environment.OSVersion.ToString(),
                host: context.Request.Host.ToString() ?? "Desconocido",
                distribution: "N/A"
            );
        }

        /// <summary>
        /// Captura la información de la solicitud HTTP antes de que sea procesada por los controladores.
        /// </summary>
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

