
    /// <summary>
    /// Middleware que captura y registra las solicitudes HTTP, respuestas y excepciones.
    /// </summary>
    public class LoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly LoggingService _loggingService;

        public LoggingMiddleware(RequestDelegate next, LoggingService loggingService)
        {
            _next = next;
            _loggingService = loggingService;
        }

        public async Task InvokeAsync(HttpContext context)
        {
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
    }

