using API_1_TERCEROS_REMESADORAS.Services.BTSServices.AuthenticateService;
using API_1_TERCEROS_REMESADORAS.Services.BTSServices.ConfirmacionPago;
using API_1_TERCEROS_REMESADORAS.Services.BTSServices.ConfirmacionTransaccionDirectaService;
using API_1_TERCEROS_REMESADORAS.Services.BTSServices.ConsultaService;
using API_1_TERCEROS_REMESADORAS.Services.BTSServices.PagoService;
using API_1_TERCEROS_REMESADORAS.Services.BTSServices.RechazoPago;
using API_1_TERCEROS_REMESADORAS.Services.BTSServices.ReporteriaService;
using API_1_TERCEROS_REMESADORAS.Services.BTSServices.ReporteriaService.SEDP;
using API_1_TERCEROS_REMESADORAS.Services.BTSServices.ReversoServices;
using API_1_TERCEROS_REMESADORAS.Utilities;
using API_Terceros.Middleware;
using Connections.Helpers;
using Connections.Interfaces;
using Connections.Providers.Database;
using Connections.Services;
using Logging.Abstractions;
using Logging.Filters;
using Logging.Middleware;
using Logging.Services;
using Microsoft.OpenApi.Models;
using RestUtilities.Logging.Handlers;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

var connectionSettings = new ConnectionSettings(builder.Configuration);
ConnectionManagerHelper.ConnectionConfig = connectionSettings;

// Registrar ConnectionSettings para leer configuración dinámica
builder.Services.AddSingleton<ConnectionSettings>();

// Registra IHttpContextAccessor para acceder al HttpContext en el servicio de logging.
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<LoggingActionFilter>();

// Registra el servicio de logging. La implementación utilizará la configuración inyectada (IOptions<LoggingOptions>)
// y la información del entorno (IHostEnvironment).
builder.Services.AddSingleton<ILoggingService, LoggingService>();

// Registrar la conexión principal a AS400 usando OleDbCommand
// Registrar servicio de conexión con soporte de logging para AS400
builder.Services.AddScoped<IDatabaseConnection>(sp =>
{
    // Obtener configuración general
    var config = sp.GetRequiredService<IConfiguration>();
    var settings = new ConnectionSettings(config); // Tu clase para acceder a settings

    // Obtener cadena de conexión desencriptada para AS400
    var connStr = settings.GetAS400ConnectionString("AS400");

    // Instanciar proveedor interno
    return new AS400ConnectionProvider(
        connStr,
        sp.GetRequiredService<ILoggingService>()); // Constructor de AS400ConnectionProvider

    //// Retornar decorador con logging
    //return new LoggingDatabaseConnectionDecorator(
    //    innerProvider,
    //    sp.GetRequiredService<IHttpContextAccessor>(),
    //    sp.GetRequiredService<ILoggingService>());        
});
/*Conexiones*/

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
builder.Services.AddHttpClient<IReporteriaService, ReporteriaService>();
builder.Services.AddHttpClient<ISEDPService, SEDPService>();

builder.Services.AddScoped<IAuthenticateService, AuthenticateService>();
builder.Services.AddScoped<IConsultaService, ConsultaService>();
builder.Services.AddScoped<IPagoService, PagoService>();
builder.Services.AddScoped<IReversoService, ReversoService>();
builder.Services.AddScoped<IConfirmacionTransaccionDirecta, ConfirmacionTransaccionDirectaService>();
builder.Services.AddScoped<IConfirmacionPago, ConfirmacionPagoService>();
builder.Services.AddScoped<IRechazoPago, RechazoPagoService>();
builder.Services.AddScoped<IReporteriaService, ReporteriaService>();
builder.Services.AddScoped<ISEDPService, SEDPService>();

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
