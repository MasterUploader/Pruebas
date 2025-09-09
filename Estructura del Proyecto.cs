using API_1_TERCEROS_REMESADORAS.Services.BTSServices.AuthenticateService;
using API_1_TERCEROS_REMESADORAS.Services.BTSServices.ConfirmacionPago;
using API_1_TERCEROS_REMESADORAS.Services.BTSServices.ConfirmacionTransaccionDirectaService;
using API_1_TERCEROS_REMESADORAS.Services.BTSServices.ConsultaService;
using API_1_TERCEROS_REMESADORAS.Services.BTSServices.PagoService;
using API_1_TERCEROS_REMESADORAS.Services.BTSServices.RechazoPago;
using API_1_TERCEROS_REMESADORAS.Services.BTSServices.ReporteriaService;
using API_1_TERCEROS_REMESADORAS.Services.BTSServices.ReporteriaService.SEDP;
using API_1_TERCEROS_REMESADORAS.Services.BTSServices.ReversoServices;
using API_Terceros.Middleware;
using Connections.Abstractions;
using Connections.Helpers;
using Connections.Providers.Database;
using Connections.Services;
using Logging.Abstractions;
using Logging.Filters;
using Logging.Handlers;
using Logging.Middleware;
using Logging.Services;
using Microsoft.OpenApi.Models;
using System.Reflection;

/// <summary>
/// Punto de entrada principal de la API.
/// Configura DI, clientes HTTP con handler de logging, proveedor de BD con HttpContext,
/// opciones de logging, Swagger y el pipeline (incluye el middleware de logging).
/// </summary>
var builder = WebApplication.CreateBuilder(args);

/// <summary>
/// Carga de configuración de conexiones y exposición para componentes dependientes.
/// </summary>
ConnectionSettings connectionSettings = new(builder.Configuration);
ConnectionManagerHelper.ConnectionConfig = connectionSettings;

// Registrar ConnectionSettings como singleton (acceso a configuración dinámica en tiempo de ejecución).
builder.Services.AddSingleton<ConnectionSettings>();

/// <summary>
/// Acceso al HttpContext para propagar correlación y ordenar logs (HTTP/SQL) por inicio real.
/// </summary>
builder.Services.AddHttpContextAccessor();

/// <summary>
/// Filtro de logging por acción (captura metadatos del pipeline MVC).
/// </summary>
builder.Services.AddScoped<LoggingActionFilter>();

/// <summary>
/// Servicio central de logging (scope global por proceso; seguro y no intrusivo).
/// </summary>
builder.Services.AddSingleton<ILoggingService, LoggingService>();

/// <summary>
/// Proveedor de BD (AS400) con soporte de logging estructurado y contexto HTTP.
/// Se inyecta IHttpContextAccessor para que el wrapper SQL pueda:
///  - Sellar el instante de INICIO real (Items["__SqlStartedUtc"]).
///  - Encolar los bloques en la cola “timed” para ordenarse entre Request y Response.
/// </summary>
builder.Services.AddScoped<IDatabaseConnection>(sp =>
{
    IConfiguration config = sp.GetRequiredService<IConfiguration>();
    ConnectionSettings settings = new(config);
    string connStr = settings.GetAS400ConnectionString("AS400");

    return new AS400ConnectionProvider(
        connStr,
        sp.GetRequiredService<ILoggingService>(),
        sp.GetRequiredService<IHttpContextAccessor>());
});

/// <summary>
/// Mapeo de opciones de logging desde appsettings (rutas, switches .txt/.csv, etc.).
/// </summary>
builder.Services.Configure<Logging.Configuration.LoggingOptions>(builder.Configuration.GetSection("LoggingOptions"));

/// <summary>
/// Handler que intercepta TODAS las solicitudes/respuestas HTTP salientes para loguearlas.
/// </summary>
builder.Services.AddTransient<HttpClientLoggingHandler>();

/// <summary>
/// Cliente HTTP “nombrado” (BTS) que encadena el handler de logging.
/// Los servicios de negocio deben obtener este cliente vía IHttpClientFactory.CreateClient("BTS").
/// </summary>
builder.Services.AddHttpClient("BTS")
    .AddHttpMessageHandler<HttpClientLoggingHandler>();

/// <summary>
/// Carga de archivos de configuración base de la API.
/// </summary>
builder.Configuration
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile("ConnectionData.json", optional: false, reloadOnChange: true);

IConfiguration configuration = builder.Configuration;

/// <summary>
/// Resolución de nodo de conexión por ambiente y publicación global (para componentes legacy).
/// </summary>
string enviroment = configuration["ApiSettings:Enviroment"] ?? "DEV";
IConfigurationSection connectionSection = configuration.GetSection(enviroment);
ConnectionConfig? connectionConfig = connectionSection.Get<ConnectionConfig>();
GlobalConnection.Current = connectionConfig!; // Uso intencional: el archivo de conexiones debe existir

/// <summary>
/// Registro de servicios de dominio (Scoped). Estos servicios deben usar IHttpClientFactory
/// para obtener el cliente “BTS” y así garantizar que el handler de logging intercepte todo.
/// </summary>
builder.Services.AddScoped<IAuthenticateService, AuthenticateService>();
builder.Services.AddScoped<IConsultaService, ConsultaService>();
builder.Services.AddScoped<IPagoService, PagoService>();
builder.Services.AddScoped<IReversoService, ReversoService>();
builder.Services.AddScoped<IConfirmacionTransaccionDirecta, ConfirmacionTransaccionDirectaService>();
builder.Services.AddScoped<IConfirmacionPago, ConfirmacionPagoService>();
builder.Services.AddScoped<IRechazoPago, RechazoPagoService>();
builder.Services.AddScoped<IReporteriaService, ReporteriaService>();
builder.Services.AddScoped<ISEDPService, SEDPService>();

/// <summary>
/// MVC + filtro de logging por acción. Se agrega compatibilidad con Newtonsoft.Json.
/// </summary>
builder.Services.AddControllers(options =>
{
    // Inserta el filtro para anotar Controller/Action en el log (bloque fijo “Controlador”).
    options.Filters.Add<LoggingActionFilter>();
});

builder.Services.AddControllers().AddNewtonsoftJson(options =>
{
    // Resolver de contrato por defecto (útil para mantener nombres y formatos esperados).
    options.SerializerSettings.ContractResolver = new Newtonsoft.Json.Serialization.DefaultContractResolver();
});

/// <summary>
/// Swagger/OpenAPI con carga de comentarios XML para documentación enriquecida.
/// </summary>
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

    string xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    string xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    options.IncludeXmlComments(xmlPath);
});

builder.Services.AddSwaggerGenNewtonsoftSupport();

/// <summary>
/// Construcción de la aplicación y configuración del pipeline HTTP.
/// </summary>
var app = builder.Build();

if (app.Environment.IsDevelopment() || app.Environment.IsProduction())
{
    // Publicación de Swagger (JSON + UI) en entornos de desarrollo/producción.
    app.UseSwagger(c => c.RouteTemplate = "swagger/{documentName}/swagger.json");
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "API de Terceros Remesadoras"));
}

/// <summary>
/// Middleware de validación de cabeceras personalizado de tu solución (previo al logging).
/// </summary>
app.UseHeaderValidation();

/// <summary>
/// Redirección a HTTPS para reforzar transporte seguro.
/// </summary>
app.UseHttpsRedirection();

/// <summary>
/// Autorización (si existen políticas o atributos en controladores/acciones).
/// </summary>
app.UseAuthorization();

/// <summary>
/// Middleware de logging central.
/// Escribe bloques fijos en el orden 1..7 y mezcla los bloques dinámicos (HTTP/SQL)
/// entre (4) Request Info y (5) Response Info, ordenados por INICIO real (TsUtc) y desempate (Seq).
/// </summary>
app.UseMiddleware<LoggingMiddleware>();

app.MapControllers();

app.Run();
