Acá se encuentra, sugiereme cambios no hagas el cambio aun, porque hay funcionalidades que se pueden afectar:

using Connections.Abstractions;
using Logging.Abstractions;
using Logging.Decorators;
using Microsoft.AspNetCore.Http;
using QueryBuilder.Models;
using System.Data;
using System.Data.Common;
using System.Data.OleDb;
using System.Runtime.Versioning;

namespace Connections.Providers.Database;

/// <summary>
/// Proveedor de conexión a base de datos AS400 utilizando OleDb.
/// Esta implementación permite la ejecución de comandos SQL con o sin logging estructurado.
/// </summary>
/// <remarks>
/// Inicializa una nueva instancia de <see cref="AS400ConnectionProvider"/>.
/// </remarks>
/// <param name="connectionString">Cadena de conexión a AS400 en formato OleDb.</param>
/// <param name="loggingService">Servicio de logging estructurado (opcional).</param>
/// <param name="httpContextAccessor">Accessor del contexto HTTP (opcional).</param>
public sealed class AS400ConnectionProvider(
    string connectionString,
    ILoggingService? loggingService = null,
    IHttpContextAccessor? httpContextAccessor = null) : IDatabaseConnection, IDisposable
{
    [SupportedOSPlatform("windows")]
    private readonly OleDbConnection _oleDbConnection = new(connectionString);
    private readonly ILoggingService? _loggingService = loggingService;
    private readonly IHttpContextAccessor? _httpContextAccessor = httpContextAccessor;

    /// <inheritdoc />
    [SupportedOSPlatform("windows")]
    public void Open()
    {
        if (_oleDbConnection.State != ConnectionState.Open)
            _oleDbConnection.Open();
    }

    /// <inheritdoc />
    [SupportedOSPlatform("windows")]
    public void Close()
    {
        if (_oleDbConnection.State != ConnectionState.Closed)
            _oleDbConnection.Close();
    }

    /// <inheritdoc />
    [SupportedOSPlatform("windows")]
    public bool IsConnected => _oleDbConnection?.State == ConnectionState.Open;

    /// <inheritdoc />
    [SupportedOSPlatform("windows")]
    public DbCommand GetDbCommand(HttpContext? context = null)
    {
        var command = _oleDbConnection.CreateCommand();

        // Si el servicio de logging está disponible, devolvemos el comando decorado.
        if (_loggingService != null)
            return new LoggingDbCommandWrapper(command, _loggingService, _httpContextAccessor);

        // En caso contrario, devolvemos el comando básico.
        return command;
    }

    /// <summary>
    /// Crea un comando configurado con la consulta SQL generada por QueryBuilder y sus parámetros asociados.
    /// Si el CommandText aún no está definido, lo asigna automáticamente.
    /// Si el CommandType no fue configurado, se establece en Text.
    /// </summary>
    /// <param name="queryResult">Objeto que contiene el SQL generado y la lista de parámetros.</param>
    /// <param name="context">Contexto HTTP actual para trazabilidad opcional.</param>
    /// <returns>DbCommand listo para ejecución.</returns>
    [SupportedOSPlatform("windows")]
    public DbCommand GetDbCommand(QueryResult queryResult, HttpContext? context)
    {
        var command = GetDbCommand(context);

        // Asignación “opcional” para no interferir si ya fue configurado por el consumidor.
        if (string.IsNullOrWhiteSpace(command.CommandText))
            command.CommandText = queryResult.Sql;

        if (command.CommandType == CommandType.Text) // por defecto ya es Text; lo reafirmamos
            command.CommandType = CommandType.Text;

        // Limpiamos y reponemos los parámetros (posicionales) si vienen en QueryResult.
        command.Parameters.Clear();

        if (queryResult.Parameters is not null && queryResult.Parameters.Count > 0)
        {
            foreach (var paramValue in queryResult.Parameters)
            {
                var parameter = command.CreateParameter();
                parameter.Value = paramValue ?? DBNull.Value;
                command.Parameters.Add(parameter);
            }
        }

        return command;
    }

    /// <inheritdoc />
    [SupportedOSPlatform ("windows")]
    public void Dispose()
    {
        _oleDbConnection?.Dispose();
    }
}





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
using Connections.Abstractions;
using Connections.Helpers;
using Connections.Providers.Database;
using Connections.Services;
using Logging.Abstractions;
using Logging.Filters;
using Logging.Middleware;
using Logging.Services;
using Microsoft.OpenApi.Models;
using Logging.Handlers;
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
