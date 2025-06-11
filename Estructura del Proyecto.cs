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

        /// <summary>
        /// Constructor de clase ConsultaService.
        /// </summary>
        /// <param name="authenticateService">Instancia de la Clase AuthenticateService.</param>
        /// <param name="httpContextAccessor">Instancia de IHttpContextAccesor</param>
        public ConsultaService(IAuthenticateService authenticateService, IHttpContextAccessor httpContextAccessor )
        {
            _authenticateService = authenticateService;
            _httpContextAccessor = httpContextAccessor;
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

                var loggingHandler = new HttpClientLoggingHandler(_httpContextAccessor)
                {
                    InnerHandler = handler
                };

                using var client = new HttpClient(loggingHandler);
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
