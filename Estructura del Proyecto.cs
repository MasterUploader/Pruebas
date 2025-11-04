Así es mi program.cs

using Connections.Abstractions;
using Connections.Helpers;
using Connections.Providers.Database;
using Connections.Services;
using Logging.Abstractions;
using Logging.Filters;
using Logging.Handlers;
using Logging.Middleware;
using Logging.Services;
using Microsoft.AspNetCore.Mvc;
using Pagos_Davivienda_TNP.Filters;
using Pagos_Davivienda_TNP.Middleware;
using Pagos_Davivienda_TNP.Services;
using Pagos_Davivienda_TNP.Services.Interfaces;
using Pagos_Davivienda_TNP.Utils;

var builder = WebApplication.CreateBuilder(args);

// ===============================  Conexiones  ===============================
// Publica la configuración de conexiones y deja disponible un helper global
// usado por componentes existentes (compatibilidad con código legacy).
ConnectionSettings connectionSettings = new(builder.Configuration);
ConnectionManagerHelper.ConnectionConfig = connectionSettings;

// Registrar ConnectionSettings para lectura dinámica en tiempo de ejecución
builder.Services.AddSingleton<ConnectionSettings>();

// ===============================  Infra Logging  ===============================
// Accessor del HttpContext para:
//  - Propagar correlación (ExecutionId, etc.)
//  - Permitir que SQL/HTTP se encolen en la **misma cola “timed”** y se ordenen por INICIO real.
builder.Services.AddHttpContextAccessor();

// Filtro que anota Controller/Action y otros metadatos MVC en los bloques fijos del log.
builder.Services.AddScoped<LoggingActionFilter>();

// Servicio central de logging (singleton: configuración y E/S de archivos compartidos).
builder.Services.AddSingleton<ILoggingService, LoggingService>();

// Opciones de logging (rutas, switches .txt/.csv) mapeadas desde appsettings.
builder.Services.Configure<Logging.Configuration.LoggingOptions>(
    builder.Configuration.GetSection("LoggingOptions"));

// ===============================  BD Provider  ===============================
// Proveedor AS400 con soporte de logging estructurado y **HttpContext**.
// Se pasa IHttpContextAccessor para que el wrapper SQL:
//  - Selle Items["__SqlStartedUtc"] (ancla de orden por INICIO real)
//  - Encole el bloque en HttpClientLogsTimed (mezcla/orden junto con HTTP).
builder.Services.AddScoped<IDatabaseConnection>(sp =>
{
    IConfiguration config = sp.GetRequiredService<IConfiguration>();
    ConnectionSettings settings = new(config);
    string connStr = settings.GetAS400ConnectionString("AS400");

    return new AS400ConnectionProvider(
        connStr,
        sp.GetRequiredService<ILoggingService>(),
        sp.GetRequiredService<IHttpContextAccessor>()); // ? imprescindible para orden cronológico correcto
});

// ===============================  HTTP Saliente  ===============================
// Handler que intercepta TODAS las solicitudes/respuestas HTTP salientes para loguearlas
// en la cola “timed”. Los servicios deben usar IHttpClientFactory con el cliente **"Embosado"**.
builder.Services.AddTransient<HttpClientLoggingHandler>();

// Cliente HTTP **nombrado** con el handler de logging encadenado.
builder.Services.AddHttpClient("TNP").AddHttpMessageHandler<HttpClientLoggingHandler>();

// ===============================  Config general / archivos  ===============================
// Carga de archivos de configuración de la API.
builder.Configuration
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile("ConnectionData.json", optional: false, reloadOnChange: true);

ConfigurationManager configuration = builder.Configuration;

// Resolver nodo de conexión por ambiente y publicarlo globalmente (compatibilidad).
string enviroment = configuration["ApiSettings:Enviroment"] ?? "DEV";
IConfigurationSection connectionSection = configuration.GetSection(enviroment);
ConnectionConfig? connectionConfig = connectionSection.Get<ConnectionConfig>();
GlobalConnection.Current = connectionConfig!; // intencional: debe existir configuración válida






// ===============================  Configuración de la API  ===============================

// Controllers + Newtonsoft.Json (para respetar tus atributos)
builder.Services
    .AddControllers(o => o.Filters.Add<ModelStateToErrorResponseFilter>())
    .AddJsonOptions(o =>
    {
        o.JsonSerializerOptions.PropertyNamingPolicy = null; // respeta nombres exactos
        o.JsonSerializerOptions.DefaultIgnoreCondition =
            System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
    });

// Desactivar el filtro automático de ModelState para usar el nuestro
builder.Services.Configure<ApiBehaviorOptions>(o =>
{
    o.SuppressModelStateInvalidFilter = true;
});

// DI servicios
builder.Services.AddScoped<IPaymentAuthorizationService, PaymentAuthorizationService>();
builder.Services.AddScoped<IHealthService, HealthService>();

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "API de Pagos DaviviendaTNP",
        Version = "v1",
        Description = "API REST ligera para procesar autorizaciones de pago."
    });
});

var app = builder.Build();

// Middleware de excepciones  {error,status,timestamp}
//app.UseMiddleware<ExceptionHandlingMiddleware>()

app.UseRouting();

// HTTPS recomendado en prod (agrega UseHttpsRedirection si corresponde)
app.UseAuthorization();

// Middleware de logging central:
// - Escribe los 7 bloques fijos (Inicio/Env/Controlador/Request/Dinámicos/Errores/Fin).
// - Mezcla HTTP/SQL entre (4) Request y (5) Response, ordenados por INICIO real (TsUtc) y Seq.
app.UseMiddleware<LoggingMiddleware>();

app.MapControllers();

// Swagger en DEV/UAT
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "DaviviendaTNP v1");
});

await app.RunAsync();
