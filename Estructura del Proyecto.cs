using API_1_TERCEROS_REMESADORAS.Services.BTSServices.AuthenticateService;
using API_1_TERCEROS_REMESADORAS.Services.BTSServices.ConfirmacionPago;
using API_1_TERCEROS_REMESADORAS.Services.BTSServices.ConfirmacionTransaccionDirectaService;
using API_1_TERCEROS_REMESADORAS.Services.BTSServices.ConsultaService;
using API_1_TERCEROS_REMESADORAS.Services.BTSServices.PagoService;
using API_1_TERCEROS_REMESADORAS.Services.BTSServices.RechazoPago;
using API_1_TERCEROS_REMESADORAS.Services.BTSServices.ReversoServices;
using API_1_TERCEROS_REMESADORAS.Utilities;
using API_Terceros.Middleware;
using Logging.Abstractions;
using Logging.Filters;
using Logging.Middleware;
using Logging.Services;
using Microsoft.OpenApi.Models;
using RestUtilities.Logging.Handlers;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

// Registra IHttpContextAccessor para acceder al HttpContext en el servicio de logging.
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<LoggingActionFilter>();

// Registra el servicio de logging. La implementación utilizará la configuración inyectada (IOptions<LoggingOptions>)
// y la información del entorno (IHostEnvironment).
builder.Services.AddSingleton<ILoggingService, LoggingService>();

// Configura LoggingOptions a partir de la sección "LoggingOptions" en el appsettings.json.
builder.Services.Configure<Logging.Configuration.LoggingOptions>(builder.Configuration.GetSection("LoggingOptions"));

//Login para HTTPClientHandler
builder.Services.AddTransient<HttpClientLoggingHandler>();

//Configuramos para que capture logs de salida del HTTP
builder.Services.AddHttpClient("BTS").AddHttpMessageHandler<HttpClientLoggingHandler>();


/*Configuración de la conexión*/
builder.Configuration
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile("ConnectionData.json", optional: false, reloadOnChange: true);

var configuration = builder.Configuration;

//Leer Ambiente
var enviroment = configuration["ApiSettings:Enviroment"] ?? "DEV";
//Leer node correspondiente desde ConnectionData.json
var connectionSection = configuration.GetSection(enviroment);
var connectionConfig = connectionSection.Get<ConnectionConfig>();

//Asignación de conexión Global
GlobalConnection.Current = connectionConfig!;

/*Configuración de la conexión*/

// Add services to the container.
builder.Services.AddHttpClient<IAuthenticateService, AuthenticateService>();
builder.Services.AddHttpClient<IConsultaService, ConsultaService>();
builder.Services.AddHttpClient<IPagoService, PagoService>();
builder.Services.AddHttpClient<IReversoService, ReversoService>();
builder.Services.AddHttpClient<IConfirmacionTransaccionDirecta, ConfirmacionTransaccionDirectaService>();
builder.Services.AddHttpClient<IConfirmacionPago, ConfirmacionPagoService>();
builder.Services.AddHttpClient<IRechazoPago, RechazoPagoService>();

builder.Services.AddScoped<IAuthenticateService, AuthenticateService>();
builder.Services.AddScoped<IConsultaService, ConsultaService>();
builder.Services.AddScoped<IPagoService, PagoService>();
builder.Services.AddScoped<IReversoService, ReversoService>();
builder.Services.AddScoped<IConfirmacionTransaccionDirecta, ConfirmacionTransaccionDirectaService>();
builder.Services.AddScoped<IConfirmacionPago, ConfirmacionPagoService>();
builder.Services.AddScoped<IRechazoPago, RechazoPagoService>();

// Add services to the container.
builder.Services.AddControllers(options =>
{
    options.Filters.Add<LoggingActionFilter>();

});

builder.Services.AddControllers().AddNewtonsoftJson(options =>
{
    options.SerializerSettings.ContractResolver = new Newtonsoft.Json.Serialization.DefaultContractResolver();
});

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "API de Terceros Remesadoras",
        Version = "v1",
        Description = "API para las consultas de Remesadoras.",
        Contact = new OpenApiContact
        {
            Name = "Remesadoras",
            Email = "soporte@api.com",
            Url = new Uri("https://api.com")
        },
        License = new OpenApiLicense
        {
            Name = "License",
            Url = new Uri("https://api.com")
        }
    });

    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);

    options.IncludeXmlComments(xmlPath);
});

builder.Services.AddSwaggerGenNewtonsoftSupport();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment() || app.Environment.IsProduction())
{
    app.UseSwagger(c =>
    {
        c.RouteTemplate = "swagger/{documentName}/swagger.json";
    });

    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "API de Terceros Remesadoras");
    });
}
app.UseHeaderValidation();

app.UseHttpsRedirection();

app.UseAuthorization();

app.UseMiddleware<LoggingMiddleware>();

app.MapControllers();

app.Run();



using API_1_TERCEROS_REMESADORAS.Models.DTO.BTS.Autenticacion.Response;
using API_1_TERCEROS_REMESADORAS.Models.DTO.BTS.Common.Request;
using API_1_TERCEROS_REMESADORAS.Models.DTO.BTS.Common.Response;
using API_1_TERCEROS_REMESADORAS.Models.DTO.BTS.Consulta.Request;
using API_1_TERCEROS_REMESADORAS.Models.DTO.BTS.Consulta.Response;
using API_1_TERCEROS_REMESADORAS.Services.BTSServices.AuthenticateService;
using API_1_TERCEROS_REMESADORAS.Utilities;
using Logging.Abstractions;
using RestUtilities.Logging.Handlers;
using System.Text;
using System.Xml.Serialization;

namespace API_1_TERCEROS_REMESADORAS.Services.BTSServices.ConsultaService
{
    /// <summary>
    /// Clase de servicio para método de consulta de BTS.
    /// </summary>
    public class ConsultaService : IConsultaService
    {
        private readonly IAuthenticateService _authenticateService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IHttpClientFactory _httpClientFactory;

        /// <summary>
        /// Constructor de clase ConsultaService.
        /// </summary>
        /// <param name="authenticateService">Instancia de la Clase AuthenticateService.</param>
        /// <param name="httpContextAccessor">Instancia de IHttpContextAccesor</param>
        public ConsultaService(IAuthenticateService authenticateService, IHttpContextAccessor httpContextAccessor, IHttpClientFactory httpClientFactory )
        {
            _authenticateService = authenticateService;
            _httpContextAccessor = httpContextAccessor;
            _httpClientFactory = httpClientFactory;
        }

        /// <summary>
        /// Método de autenticación con BTS.
        /// </summary>
        /// <returns>Retorna objeto ConsultaResponseData.</returns>
        public async Task<(ConsultaResponseData consultaResponseData, int statusCode)> ConsultaServiceRequestAsync(ConsultaBody consultaModel)
        {
            var respuesta = new ConsultaResponseData();
            try
            {
                //Realizamos la consulta para obtener el token ya que es requerido para cada petición.
                var (responseAutenticacion, statusCodeA) = await _authenticateService.AuthenticateServiceRequestAsync();
                string session = responseAutenticacion.Session_Id;

                if (!responseAutenticacion.OpCode.Equals("S010") && !responseAutenticacion.OpCode.Equals("S000"))
                {
                    respuesta.ProcessDt = responseAutenticacion.Proces_DT;
                    respuesta.ProcessTm = responseAutenticacion.Process_Tm;
                    respuesta.OpCode = responseAutenticacion.OpCode;
                    respuesta.ProcessMsg = responseAutenticacion.Process_Msg;

                    return (respuesta, statusCodeA);
                }

                //Extraemos los campos Estaticos para Cada Petición
                string user = GlobalConnection.Current.BTSUser;
                string password = GlobalConnection.Current.BTSPassword;
                string host = GlobalConnection.Current.Host;
                string domain = GlobalConnection.Current.Domain;

                var request = new GetRequestConsultaEnvelope<GetDataRequest<GetConsultaDataRequest>>
                {
                    Header = new GetHeader
                    {
                        Security = new GetSecurity
                        {
                            UserDomain = domain,
                            UserName = user,
                            UserPass = password,
                            SessionId = session

                        },
                        Addressing = new GetAddressing
                        {
                            From = "",
                            To = ""
                        }
                    },
                    Body = consultaModel
                };

                var xmlPost = XmlHelper.SerializeToXml(request);

                var content = new StringContent(xmlPost, Encoding.UTF8, "text/xml");

                HttpClientHandler handler = new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback = (sender, certificate, chain, SslPolicyErrors) => true

                };

                var client = _httpClientFactory.CreateClient("BTS");

                //using var client = new HttpClient(loggingHandler);
                var response = await client.PostAsync(host, content);

                int statusCode = (int)response.StatusCode;
                string responseXml = await response.Content.ReadAsStringAsync();

                var serializer = new XmlSerializer(typeof(GetResponseEnvelope<GetResponseBody<ExecTRResponseConsulta>>));
                using var reader = new StringReader(responseXml);
                GetResponseEnvelope<GetResponseBody<ExecTRResponseConsulta>> result = (GetResponseEnvelope<GetResponseBody<ExecTRResponseConsulta>>)serializer.Deserialize(reader)!;

                return (result.Body.ExectTRResponse.ExecTRResult.RESPONSE, statusCode);
            }
            catch (Exception ex)
            {
                respuesta.ProcessDt = DateTime.Now.ToString("yyyyMMdd");
                respuesta.ProcessTm = DateTime.Now.ToString("hhmmss");
                respuesta.OpCode = "S099";
                respuesta.ProcessMsg = ex.Message;

                return (respuesta, 500);
            }
        }
    }
}



using Logging.Abstractions;
using Logging.Helpers;
using Microsoft.AspNetCore.Http;
using System.Diagnostics;
using System.Text;

namespace RestUtilities.Logging.Handlers;

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
        if (context == null)
        {
            Console.WriteLine("⚠ HttpContext is null. Logging will not be captured.");
        }
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
                url: request.RequestUri?.ToString() ?? "Uri no definida",
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
                url: request.RequestUri?.ToString() ?? "Uri no definida",
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


using Logging.Abstractions;
using Logging.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace Logging.Middleware;


/// <summary>
/// Middleware para capturar logs de ejecución de controladores en la API.
/// Captura información de Request, Response, Excepciones y Entorno.
/// </summary>
public class LoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILoggingService _loggingService;
    /// <summary>
    /// Nombre del controlador en ejecución.
    /// Se inicializa en el constructor y se actualiza en `OnActionExecuting()`.
    /// </summary>
    private string _controllerName { get; set; } = string.Empty;

    /// <summary>
    /// Nombre de la acción en ejecución.
    /// Se inicializa en el constructor y se actualiza en `OnActionExecuting()`.
    /// </summary>
    private string _actionName { get; set; } = string.Empty;

    /// <summary>
    /// Cronómetro utilizado para medir el tiempo de ejecución de la acción.
    /// Se inicializa cuando la acción comienza a ejecutarse.
    /// </summary>
    private Stopwatch _stopwatch = new();

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
        try
        {
            _stopwatch = Stopwatch.StartNew(); // Iniciar medición de tiempo

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

                // 5.5 Capturar logs del HttpClient si existen
                if (context.Items.TryGetValue("HttpClientLogs", out var clientLogsObj) && clientLogsObj is List<string> clientLogs)
                {
                    foreach (var log in clientLogs)
                    {
                        _loggingService.WriteLog(context, log);
                    }
                }

                // 6️⃣ Capturar la respuesta y agregarla al log
                string responseLog = await CaptureResponseInfoAsync(context);
                _loggingService.WriteLog(context, responseLog);

                // 7️⃣ Restaurar el stream original para que el API pueda responder correctamente
                responseBody.Seek(0, SeekOrigin.Begin);
                await responseBody.CopyToAsync(originalBodyStream);
            }

            // 8️⃣ Verificar si hubo alguna excepción en la ejecución y loguearla
            if (context.Items.ContainsKey("Exception") && context.Items["Exception"] is Exception ex)
            {
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
            _stopwatch.Stop();
            _loggingService.WriteLog(context, $"[Tiempo Total de Ejecución]: {_stopwatch.ElapsedMilliseconds} ms");
        }
    }

    /// <summary>
    /// Captura la información del entorno del servidor y del cliente.
    /// </summary>
    private static async Task<string> CaptureEnvironmentInfoAsync(HttpContext context)
    {
        await Task.Delay(TimeSpan.FromMilliseconds(1));

        var request = context.Request;
        var connection = context.Connection;
        var hostEnvironment = context.RequestServices.GetService<IHostEnvironment>();

        // 1. Intentar obtener de un header HTTP
        var distributionFromHeader = context.Request.Headers["Distribucion"].FirstOrDefault();

        // 2. Intentar obtener de los claims del usuario (si existe autenticación JWT)
        var distributionFromClaim = context.User?.Claims?
            .FirstOrDefault(c => c.Type == "distribution")?.Value;

        // 3. Intentar extraer del subdominio (ejemplo: cliente1.api.com)
        var host = context.Request.Host.Host;
        var distributionFromSubdomain = !string.IsNullOrWhiteSpace(host) && host.Contains('.')
            ? host.Split('.')[0]
            : null;

        // 4. Seleccionar la primera fuente válida o asignar "N/A"
        var distribution = distributionFromHeader
                           ?? distributionFromClaim
                           ?? distributionFromSubdomain
                           ?? "N/A";

        // Preparar información extendida
        string application = hostEnvironment?.ApplicationName ?? "Desconocido";
        string env = hostEnvironment?.EnvironmentName ?? "Desconocido";
        string contentRoot = hostEnvironment?.ContentRootPath ?? "Desconocido";
        string executionId = context.TraceIdentifier ?? "Desconocido";
        string clientIp = connection?.RemoteIpAddress?.ToString() ?? "Desconocido";
        string userAgent = request.Headers["User-Agent"].ToString() ?? "Desconocido";
        string machineName = Environment.MachineName;
        string os = Environment.OSVersion.ToString();
        host = request.Host.ToString() ?? "Desconocido";

        // Información adicional del contexto
        var extras = new Dictionary<string, string>
            {
                { "Scheme", request.Scheme },
                { "Protocol", request.Protocol },
                { "Method", request.Method },
                { "Path", request.Path },
                { "Query", request.QueryString.ToString() },
                { "ContentType", request.ContentType ?? "N/A" },
                { "ContentLength", request.ContentLength?.ToString() ?? "N/A" },
                { "ClientPort", connection?.RemotePort.ToString() ?? "Desconocido" },
                { "LocalIp", connection?.LocalIpAddress?.ToString() ?? "Desconocido" },
                { "LocalPort", connection?.LocalPort.ToString() ?? "Desconocido" },
                { "ConnectionId", connection?.Id ?? "Desconocido" },
                { "Referer", request.Headers.Referer.ToString() ?? "N/A" }
            };

        return LogFormatter.FormatEnvironmentInfo(
                application: application,
                env: env,
                contentRoot: contentRoot,
                executionId: executionId,
                clientIp: clientIp,
                userAgent: userAgent,
                machineName: machineName,
                os: os,
                host: host,
                distribution: distribution,
                extras: extras // Se requiere que FormatEnvironmentInfoStart lo soporte
        );
    }

    /// <summary>
    /// Captura la información de la solicitud HTTP antes de que sea procesada por los controladores.
    /// </summary>
    private static async Task<string> CaptureRequestInfoAsync(HttpContext context)
    {
        context.Request.EnableBuffering(); // Permite leer el cuerpo de la petición sin afectar la ejecución

        using var reader = new StreamReader(context.Request.Body, Encoding.UTF8, leaveOpen: true);
        string body = await reader.ReadToEndAsync();
        context.Request.Body.Position = 0; // Restablece la posición para que el controlador pueda leerlo

        return LogFormatter.FormatRequestInfo(context,
            method: context.Request.Method,
            path: context.Request.Path,
            queryParams: context.Request.QueryString.ToString(),
            body: body
        );
    }

    /// <summary>
    /// Captura la información de la respuesta HTTP antes de enviarla al cliente.
    /// </summary>
    private static async Task<string> CaptureResponseInfoAsync(HttpContext context)
    {
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(context.Response.Body, Encoding.UTF8, leaveOpen: true);
        string body = await reader.ReadToEndAsync();
        context.Response.Body.Seek(0, SeekOrigin.Begin);

        string formattedResponse;

        // Usar el objeto guardado en context.Items si existe
        if (context.Items.ContainsKey("ResponseObject"))
        {
            var responseObject = context.Items["ResponseObject"];
            formattedResponse = LogFormatter.FormatResponseInfo(context,
                statusCode: context.Response.StatusCode.ToString(),
                headers: string.Join("; ", context.Response.Headers),
                body: responseObject != null
                    ? JsonSerializer.Serialize(responseObject, new JsonSerializerOptions { WriteIndented = true })
                    : "null"
            );
        }
        else
        {
            // Si no se interceptó el ObjectResult, usar el cuerpo normal
            formattedResponse = LogFormatter.FormatResponseInfo(context,
                statusCode: context.Response.StatusCode.ToString(),
                headers: string.Join("; ", context.Response.Headers),
                body: body
            );
        }

        return formattedResponse;
    }
}




using Logging.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using System.Net;
using System.Text;
using System.Text.Json;

namespace Logging.Helpers;

/// <summary>
/// Clase estática encargada de formatear los logs con la estructura pre definida.
/// </summary>
public static class LogFormatter
{
    /// <summary>
    /// Formato de Log para FormatBeginLog.
    /// </summary>
    /// <returns>Un string con el formato de Log para FormatBeginLog.</returns>
    public static string FormatBeginLog()
    {
        var sb = new StringBuilder();

        sb.AppendLine("---------------------------Inicio de Log-------------------------");
        sb.AppendLine($"Inicio: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine("-------------------------------------------------------------------");

        return sb.ToString();
    }

    /// <summary>
    /// Formato de Log para FormatEndLog.
    /// </summary>
    /// <returns>Un string con el formato de Log para FormatEndLog.</returns>
    public static string FormatEndLog()
    {
        var sb = new StringBuilder();

        sb.AppendLine("---------------------------Fin de Log-------------------------");
        sb.AppendLine($"Final: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine("-------------------------------------------------------------------");

        return sb.ToString();

    }

    /// <summary>
    /// Formatea la información del entorno, incluyendo datos adicionales si están disponibles.
    /// </summary>
    /// <param name="application">Nombre de la aplicación.</param>
    /// <param name="env">Nombre del entorno (Development, Production, etc.).</param>
    /// <param name="contentRoot">Ruta raíz del contenido.</param>
    /// <param name="executionId">Identificador único de la ejecución.</param>
    /// <param name="clientIp">Dirección IP del cliente.</param>
    /// <param name="userAgent">Agente de usuario del cliente.</param>
    /// <param name="machineName">Nombre de la máquina donde corre la aplicación.</param>
    /// <param name="os">Sistema operativo del servidor.</param>
    /// <param name="host">Host del request recibido.</param>
    /// <param name="distribution">Distribución personalizada u origen (opcional).</param>
    /// <param name="extras">Diccionario con información adicional opcional.</param>
    /// <returns>Texto formateado con la información del entorno.</returns>
    public static string FormatEnvironmentInfo(
        string application, string env, string contentRoot, string executionId,
        string clientIp, string userAgent, string machineName, string os,
        string host, string distribution, Dictionary<string, string>? extras = null)
    {
        var sb = new StringBuilder();

        sb.AppendLine("---------------------------Enviroment Info-------------------------");
        sb.AppendLine($"Inicio: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine("-------------------------------------------------------------------");
        sb.AppendLine($"Application: {application}");
        sb.AppendLine($"Environment: {env}");
        sb.AppendLine($"ContentRoot: {contentRoot}");
        sb.AppendLine($"Execution ID: {executionId}");
        sb.AppendLine($"Client IP: {clientIp}");
        sb.AppendLine($"User Agent: {userAgent}");
        sb.AppendLine($"Machine Name: {machineName}");
        sb.AppendLine($"OS: {os}");
        sb.AppendLine($"Host: {host}");
        sb.AppendLine($"Distribución: {distribution}");

        if (extras is not null && extras.Any())
        {
            sb.AppendLine("  -- Extras del HttpContext --");
            foreach (var kvp in extras)
            {
                sb.AppendLine($"    {kvp.Key,-20}: {kvp.Value}");
            }
        }

        sb.AppendLine(new string('-', 70));
        sb.AppendLine("---------------------------Enviroment Info-------------------------");
        sb.AppendLine($"Fin: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine("-------------------------------------------------------------------");

        return sb.ToString();
    }

    /// <summary>
    /// Formatea los parámetros de entrada de un método antes de guardarlos en el log.
    /// </summary>
    public static string FormatInputParameters(IDictionary<string, object> parameters)
    {
        var sb = new StringBuilder();

        sb.AppendLine("-----------------------Parámetros de Entrada-----------------------------------");

        if (parameters == null || parameters.Count == 0)
        {
            sb.AppendLine("Sin parámetros de entrada.");
        }
        else
        {
            foreach (var param in parameters)
            {
                if (param.Value == null)
                {
                    sb.AppendLine($"{param.Key} = null");
                }
                else if (param.Value.GetType().IsPrimitive || param.Value is string)
                {
                    sb.AppendLine($"{param.Key} = {param.Value}");
                }
                else
                {
                    string json = JsonSerializer.Serialize(param.Value, new JsonSerializerOptions { WriteIndented = true });
                    sb.AppendLine($"Objeto {param.Key} =\n{json}");
                }
            }
        }

        sb.AppendLine("-----------------------Parámetros de Entrada-----------------------------------");

        return sb.ToString();
    }

    /// <summary>
    /// Formato de Log para Request.
    /// </summary>
    /// <param name="context">Contexto HTTP de la petición.</param>
    /// <param name="method">Endpoint.</param>
    /// <param name="path">Ruta del Endpoint.</param>
    /// <param name="queryParams">Parametros del Query.</param>
    /// <param name="body">Cuerpo de la petición.</param>
    /// <returns>uString con el Log Formateado.</returns>
    public static string FormatRequestInfo(HttpContext context, string method, string path, string queryParams, string body)
    {
        string formattedJson = string.IsNullOrWhiteSpace(body) ? "  (Sin cuerpo en la solicitud)" : StringExtensions.FormatJson(body, 30); // Aplica indentación controlada
        var routeData = context.GetRouteData();
        string controllerName = routeData?.Values["controller"]?.ToString() ?? "Desconocido";
        string actionName = routeData?.Values["action"]?.ToString() ?? "Desconocido";

        var sb = new StringBuilder();

        sb.AppendLine(FormatControllerBegin(controllerName, actionName));
        sb.AppendLine("----------------------------------Request Info---------------------------------");
        sb.AppendLine($"Inicio: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine("-------------------------------------------------------------------------------");
        sb.AppendLine($"Método: {method}");
        sb.AppendLine($"URL: {path}{queryParams}");
        sb.AppendLine($"Cuerpo:");
        sb.AppendLine($"{formattedJson}");
        sb.AppendLine("----------------------------------Request Info---------------------------------");
        sb.AppendLine($"Fin: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine("-------------------------------------------------------------------------------");

        return sb.ToString();
    }

    /// <summary>
    /// Formato de la información de Respuesta.
    /// </summary>
    /// <param name="context">Contexto HTTP de la petición.</param>
    /// <param name="statusCode">Codigo de Estádo de la respuesta.</param>
    /// <param name="headers">Cabeceras de la respuesta.</param>
    /// <param name="body">Cuerpo de la Respuesta.</param>
    /// <returns>String con el Log Formateado.</returns>
    public static string FormatResponseInfo(HttpContext context, string statusCode, string headers, string body)
    {
        string formattedJson = string.IsNullOrWhiteSpace(body) ? "        (Sin cuerpo en la respuesta)" : StringExtensions.FormatJson(body, 30); // Aplica indentación controlada
        var routeData = context.GetRouteData();
        string controllerName = routeData?.Values["controller"]?.ToString() ?? "Desconocido";
        string actionName = routeData?.Values["action"]?.ToString() ?? "Desconocido";

        var sb = new StringBuilder();

        sb.AppendLine("----------------------------------Response Info---------------------------------");
        sb.AppendLine($"Inicio: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine("-------------------------------------------------------------------------------");
        sb.AppendLine($"Código Estado: {statusCode}");
        sb.AppendLine($"Cuerpo:");
        sb.AppendLine($"{formattedJson}");
        sb.AppendLine("----------------------------------Response Info---------------------------------");
        sb.AppendLine($"Fin: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine("-------------------------------------------------------------------------------");
        sb.AppendLine(FormatControllerEnd(controllerName, actionName));

        return sb.ToString();
    }

    /// <summary>
    /// Método que formatea el Inicio del Log del Controlador.
    /// </summary>
    /// <param name="controllerName">Nombre del controlador.</param>
    /// <param name="actionName">Tipo de Acción.</param>
    /// <returns>String con el log formateado.</returns>
    private static string FormatControllerBegin(string controllerName, string actionName)
    {
        var sb = new StringBuilder();

        sb.AppendLine("-------------------------------------------------------------------------------");
        sb.AppendLine($"Controlador: {controllerName}");
        sb.AppendLine($"Action: {actionName}");
        sb.AppendLine($"Inicio: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine("-------------------------------------------------------------------------------");
        sb.AppendLine("-------------------------------------------------------------------------------");

        return sb.ToString();
    }

    /// <summary>
    /// Método que formatea el fin del Log del Controlador.
    /// </summary>
    /// <param name="controllerName">Nombre del Controlador.</param>
    /// <param name="actionName">Tipo de Acción.</param>
    /// <returns>String con el log formateado.</returns>
    private static string FormatControllerEnd(string controllerName, string actionName)
    {
        var sb = new StringBuilder();

        sb.AppendLine("-------------------------------------------------------------------------------");
        sb.AppendLine($"Controlador: {controllerName}");
        sb.AppendLine($"Action: {actionName}");
        sb.AppendLine($"Fin: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine("-------------------------------------------------------------------------------");

        return sb.ToString();
    }

    /// <summary>
    /// Método que formatea la estructura de inicio un método.
    /// </summary>
    /// <param name="methodName">Nombre del Método.</param>
    /// <param name="parameters">Parametros del metodo.</param>
    /// <returns>String con el log formateado.</returns>
    public static string FormatMethodEntry(string methodName, string parameters)
    {
        var sb = new StringBuilder();

        sb.AppendLine("-------------------------------------------------------------------------------");
        sb.AppendLine($"Método: {methodName}");
        sb.AppendLine($"Inicio: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine("-------------------------------------------------------------------------------");
        sb.AppendLine("Parámetros de Entrada:");
        sb.AppendLine($"{parameters}");

        return sb.ToString();

    }

    /// <summary>
    /// Método que formatea la estructura de salida de un método.
    /// </summary>
    /// <param name="methodName">Nombre del Método.</param>
    /// <param name="returnValue">Valores de Retorno.</param>
    /// <returns>String con el Log Formateado.</returns>
    public static string FormatMethodExit(string methodName, string returnValue)
    {
        var sb = new StringBuilder();

        sb.AppendLine("-------------------------------------------------------------------------------");
        sb.AppendLine($"Método: {methodName}");
        sb.AppendLine($"Fin: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine("-------------------------------------------------------------------------------");

        return sb.ToString();

    }

    /// <summary>
    /// Método de formatea un Log Simple.
    /// </summary>
    /// <param name="message">Cuerpo del texto del Log.</param>
    /// <returns>String con el Log formateado.</returns>
    public static string FormatSingleLog(string message)
    {
        var sb = new StringBuilder();

        sb.AppendLine("----------------------------------Single Log-----------------------------------");
        sb.AppendLine($"Inicio: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine("-------------------------------------------------------------------------------");
        sb.AppendLine($"{message}");
        sb.AppendLine("----------------------------------Single Log-----------------------------------");
        sb.AppendLine($"Fin: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine("-------------------------------------------------------------------------------");

        return sb.ToString();
    }

    /// <summary>
    /// Método que formatea el Log de un Objeto
    /// </summary>
    /// <param name="objectName">Nombre del Objeto.</param>
    /// <param name="obj">Objeto.</param>
    /// <returns>String con el log formateado.</returns>
    public static string FormatObjectLog(string objectName, object obj)
    {
        var sb = new StringBuilder();

        sb.AppendLine($"---------------------- Object -> {objectName}---------------------------------");
        sb.AppendLine($"Inicio: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine("-------------------------------------------------------------------------------");
        sb.AppendLine($"{StringExtensions.ConvertObjectToString(obj)}");
        sb.AppendLine($"---------------------- Object -> {objectName}---------------------------------");
        sb.AppendLine($"Fin: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine("-------------------------------------------------------------------------------");

        return sb.ToString();
    }

    /// <summary>
    /// Método que formatea el Log de una Excepción.
    /// </summary>
    /// <param name="exceptionMessage">Mensaje de la Excepción.</param>
    /// <returns>String con el log formateado.</returns>
    public static string FormatExceptionDetails(string exceptionMessage)
    {
        var sb = new StringBuilder();

        sb.AppendLine("-----------------------------Exception Details---------------------------------");
        sb.AppendLine($"Inicio: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine("-------------------------------------------------------------------------------");
        sb.AppendLine($"{exceptionMessage}");
        sb.AppendLine("-----------------------------Exception Details---------------------------------");
        sb.AppendLine($"Fin: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine("-------------------------------------------------------------------------------");

        return sb.ToString();

    }

    /// <summary>
    /// Formatea la información de una solicitud HTTP externa realizada mediante HttpClient.
    /// </summary>
    public static string FormatHttpClientRequest(
        string traceId,
        string method,
        string url,
        string statusCode,
        long elapsedMs,
        string headers,
        string? body)
    {
        var builder = new StringBuilder();

        builder.AppendLine();
        builder.AppendLine("========== INICIO HTTP CLIENT ==========");
        builder.AppendLine($"TraceId       : {traceId}");
        builder.AppendLine($"Método        : {method}");
        builder.AppendLine($"URL           : {url}");
        builder.AppendLine($"Código Status : {statusCode}");
        builder.AppendLine($"Duración (ms) : {elapsedMs}");
        builder.AppendLine("Encabezados   :");
        builder.AppendLine(headers.Trim());

        if (!string.IsNullOrWhiteSpace(body))
        {
            builder.AppendLine("Cuerpo:");
            builder.AppendLine(body.Trim());
        }

        builder.AppendLine("=========== FIN HTTP CLIENT ============");
        return builder.ToString();
    }

    /// <summary>
    /// Formatea la información de error ocurrida durante una solicitud con HttpClient.
    /// </summary>
    public static string FormatHttpClientError(
        string traceId,
        string method,
        string url,
        Exception exception)
    {
        var builder = new StringBuilder();

        builder.AppendLine();
        builder.AppendLine("======= ERROR HTTP CLIENT =======");
        builder.AppendLine($"TraceId     : {traceId}");
        builder.AppendLine($"Método      : {method}");
        builder.AppendLine($"URL         : {url}");
        builder.AppendLine($"Excepción   : {exception.Message}");
        builder.AppendLine($"StackTrace  : {exception.StackTrace}");
        builder.AppendLine("=================================");

        return builder.ToString();
    }
}




