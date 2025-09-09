Este es el program.cs de otro API, actualizalo al igual que el anterior con los comentarios xml y comentarios en general

using Connections.Abstractions;
using Connections.Helpers;
using Connections.Providers.Database;
using Connections.Services;
using Logging.Abstractions;
using Logging.Filters;
using Logging.Middleware;
using Logging.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using MS_BAN_43_Embosado_Tarjetas_Debito.Services;
using MS_BAN_43_Embosado_Tarjetas_Debito.Services.AuthService;
using MS_BAN_43_Embosado_Tarjetas_Debito.Services.DetalleTarjetasImprimir;
using MS_BAN_43_Embosado_Tarjetas_Debito.Services.MachineInformationService;
using MS_BAN_43_Embosado_Tarjetas_Debito.Services.RegistraImpresion;
using MS_BAN_43_Embosado_Tarjetas_Debito.Services.SessionManagerService;
using MS_BAN_43_Embosado_Tarjetas_Debito.Services.ValidaImpresion;
using MS_BAN_43_Embosado_Tarjetas_Debito.Utils;
using Logging.Handlers;
using System.Reflection;
using System.Text;

var builder = WebApplication.CreateBuilder(args);
/*Conexiones */

var connectionSettings = new ConnectionSettings(builder.Configuration);
ConnectionManagerHelper.ConnectionConfig = connectionSettings;

// Registrar ConnectionSettings para leer configuración dinámica
builder.Services.AddSingleton<ConnectionSettings>();

// Registrar IHttpContextAccessor para acceso al contexto en servicios
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<LoggingActionFilter>();

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
    return new AS400ConnectionProvider(
        connStr,
        sp.GetRequiredService<ILoggingService>()); // Constructor de AS400ConnectionProvider
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

builder.Services.AddTransient<IMachineInfoService, MachineInformationService>();

//Login para HTTPClientHandler
builder.Services.AddTransient<HttpClientLoggingHandler>();

/*Configuración de la conexión*/
builder.Configuration
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile("ConnectionData.json", optional: false, reloadOnChange: true);

var configuration = builder.Configuration;

//Leer Ambiente
var enviroment = configuration["ApiSettings:Enviroment"] ?? "DEV";
////Leer node correspondiente desde ConnectionData.json
var connectionSection = configuration.GetSection(enviroment);
var connectionConfig = connectionSection.Get<ConnectionConfig>();

////Asignación de conexión Global
GlobalConnection.Current = connectionConfig!;

/*Configuración de la conexión*/

//Carga de Clave secreta
var jwtKeyService = new JwtKeyService();
var secretKey = await jwtKeyService.GetSecretKeyAsync();
builder.Configuration["JwtKey"] = secretKey;


builder.Services.AddHttpClient<IJwtService, JwtKeyService>();
builder.Services.AddHttpClient<IAuthService, AuthService>();
builder.Services.AddHttpClient<ISessionManagerService, SessionManagerService>();
builder.Services.AddHttpClient<IDetalleTarjetasImprimirServices, DetalleTarjetasImprimirServices>();
builder.Services.AddHttpClient<IRegistraImpresionService, RegistraImpresionService>();
builder.Services.AddHttpClient<IValidaImpresionService, ValidaImpresionService>();
//builder.Services.AddHttpClient<IHeartbeat,  HeartbeatService>();

builder.Services.AddScoped<IJwtService, JwtKeyService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ISessionManagerService, SessionManagerService>();
builder.Services.AddScoped<IDetalleTarjetasImprimirServices, DetalleTarjetasImprimirServices>();
builder.Services.AddScoped<IRegistraImpresionService, RegistraImpresionService>();
builder.Services.AddScoped<IValidaImpresionService, ValidaImpresionService>();
//builder.Services.AddScoped<IHeartbeat,  HeartbeatService>();

// Add services to the container.
builder.Services.AddControllers(options =>
{
    options.Filters.Add<LoggingActionFilter>();

});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "MS_BAN_43_Embosado_Tarjetas_Debito",
        Version = "v1",
        Description = "API para embosado de tarjetas de Debito",
        Contact = new OpenApiContact
        {
            Name = "Embosado Tarjetas Debito",
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

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
        ValidateIssuer = false,
        ValidateAudience = false,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };

});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngularOrigins", builder =>
    {
        builder.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
    });
});

var app = builder.Build();

app.UseCors("AllowAngularOrigins");

/*Configuración de Sunitp*/

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment() || app.Environment.IsProduction())
{
    app.UseSwagger(c =>
    {
        c.RouteTemplate = "swagger/{documentName}/swagger.json";
    });

    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "API para embosado de tarjetas de Debito.");
    });
}

app.UseHttpsRedirection();

app.UseAuthentication();

app.UseAuthorization();

app.UseMiddleware<LoggingMiddleware>();

app.MapControllers();

app.Run();
