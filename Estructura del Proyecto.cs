
    /// <summary>
    /// Middleware encargado de capturar información de las solicitudes HTTP y respuestas.
    /// </summary>
    public class LoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly LoggingService _loggingService;

        /// <summary>
        /// Constructor del middleware de logging.
        /// </summary>
        /// <param name="next">Delegado para la siguiente capa del middleware.</param>
        /// <param name="loggingService">Servicio de logging inyectado.</param>
        public LoggingMiddleware(RequestDelegate next, LoggingService loggingService)
        {
            _next = next;
            _loggingService = loggingService;
        }

        /// <summary>
        /// Método principal del middleware que se ejecuta en cada solicitud HTTP.
        /// </summary>
        /// <param name="context">Contexto HTTP actual.</param>
        public async Task InvokeAsync(HttpContext context)
        {
            // Lista para almacenar logs de la solicitud actual.
            var logs = new List<string>();
            context.Items["RequestLogs"] = logs;

            logs.Add(LogFormatter.FormatBeginLog());
            logs.Add(LogFormatter.FormatRequestInfo(context.Request.Method, context.Request.Path, context.Request.QueryString.ToString(), await ReadRequestBody(context)));

            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                logs.Add(LogFormatter.FormatExceptionDetails(ex.ToString()));
            }

            logs.Add(LogFormatter.FormatResponseInfo(context.Response.StatusCode.ToString(), context.Response.Headers.ToString(), await ReadResponseBody(context)));
            logs.Add(LogFormatter.FormatEndLog());

            _loggingService.FlushLogs();
        }

        /// <summary>
        /// Captura el cuerpo de la solicitud HTTP.
        /// </summary>
        private async Task<string> ReadRequestBody(HttpContext context)
        {
            context.Request.EnableBuffering();
            using var reader = new StreamReader(context.Request.Body, leaveOpen: true);
            string body = await reader.ReadToEndAsync();
            context.Request.Body.Position = 0;
            return body;
        }

        /// <summary>
        /// Captura el cuerpo de la respuesta HTTP (pendiente de implementación).
        /// </summary>
        private async Task<string> ReadResponseBody(HttpContext context)
        {
            return "Response captured"; // Implementación futura
        }
    }

