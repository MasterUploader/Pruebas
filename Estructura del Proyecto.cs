using Connections.Helpers;
using Connections.Interfaces;
using Connections.Providers.Database;
using Connections.Services;
using Logging.Abstractions;
using Logging.Decorators;
using Logging.Filters;
using Logging.Middleware;
using Logging.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using MS_BAN_38_UTH_RECAUDACION_PAGOS.Repository;
using MS_BAN_38_UTH_RECAUDACION_PAGOS.Repository.IRepository.Autenticacion;
using MS_BAN_38_UTH_RECAUDACION_PAGOS.ServiceReference.IServiceReference;
using MS_BAN_38_UTH_RECAUDACION_PAGOS.ServiceReference.REST_UTH.Companies.Companies_Services;
using MS_BAN_38_UTH_RECAUDACION_PAGOS.ServiceReference.REST_UTH.Payments.Payments_Services;
using MS_BAN_38_UTH_RECAUDACION_PAGOS.ServiceReference.REST_UTH.Receivables.Receivables_Services;
using MS_BAN_38_UTH_RECAUDACION_PAGOS.Utils;
using RestUtilities.Logging.Handlers;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

/*Conexiones */
// Cargar configuración desde Connection.json por ambiente (DEV, UAT, PROD)
builder.Configuration.AddJsonFile("ConnectionData.json", optional: false, reloadOnChange: true);

var connectionSettings = new ConnectionSettings(builder.Configuration);
ConnectionManagerHelper.ConnectionConfig = connectionSettings;

// Registrar ConnectionSettings para leer configuración dinámica
builder.Services.AddSingleton<ConnectionSettings>();

// Registrar IHttpContextAccessor para acceso al contexto en servicios
builder.Services.AddHttpContextAccessor();

// Registrar servicio de Logging
builder.Services.AddScoped<ILoggingService, LoggingService>();

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
    var innerProvider = new As400ConnectionProvider(
        connStr,
        sp.GetRequiredService<ILoggingService>()); // Constructor de AS400ConnectionProvider

    // Retornar decorador con logging
    return new LoggingDatabaseConnectionDecorator(
        innerProvider,
        sp.GetRequiredService<IHttpContextAccessor>(),
        sp.GetRequiredService<ILoggingService>());        
});
/*Conexiones*/



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
builder.Services.AddHttpClient("GINIH").AddHttpMessageHandler<HttpClientLoggingHandler>();


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
builder.Services.AddHttpClient<ILoginRepository, LoginRepository>();
builder.Services.AddHttpClient<ICompaniesServices, CompaniesServices>();
builder.Services.AddHttpClient<IPaymentsServices, PaymentsServices>();
builder.Services.AddHttpClient<IReceivablesServices, ReceivablesServices>();

builder.Services.AddScoped<ILoginRepository, LoginRepository>();
builder.Services.AddScoped<ICompaniesServices, CompaniesServices>();
builder.Services.AddScoped<IPaymentsServices, PaymentsServices>();
builder.Services.AddScoped<IReceivablesServices, ReceivablesServices>();


// Add services to the container.
builder.Services.AddControllers(options =>
{
    options.Filters.Add<LoggingActionFilter>();   

});

//builder.Services.AddControllers().AddNewtonsoftJson(options =>
//{
//    options.SerializerSettings.ContractResolver = new Newtonsoft.Json.Serialization.DefaultContractResolver();
//});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "MS_BAN_38_UTH_RECAUDACION_PAGOS",
        Version = "v1",
        Description = "API para gestión de pagos mediante Ginih.",
        Contact = new OpenApiContact
        {
            Name = "Ginih",
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

//builder.Services.AddSwaggerGenNewtonsoftSupport();


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
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "API para gestión de pagos mediante Ginih.");
    });
}

//app.UseHeaderValidation();

app.UseHttpsRedirection();

app.UseAuthorization();

app.UseMiddleware<LoggingMiddleware>();

app.MapControllers();

app.Run();
