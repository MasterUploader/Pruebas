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

/// <summary>
/// ===============================  Conexiones  ===============================
/// Publica la configuración de conexiones y deja disponible un helper global
/// usado por componentes existentes (compatibilidad con código legacy).
/// </summary>
ConnectionSettings connectionSettings = new(builder.Configuration);
ConnectionManagerHelper.ConnectionConfig = connectionSettings;

// Registrar ConnectionSettings para lectura dinámica en tiempo de ejecución
builder.Services.AddSingleton<ConnectionSettings>();

/// <summary>
/// ===============================  Infra Logging  ===============================
/// Accessor del HttpContext para:
///  - Propagar correlación (ExecutionId, etc.)
///  - Permitir que SQL/HTTP se encolen en la **misma cola “timed”** y se ordenen por INICIO real.
/// </summary>
builder.Services.AddHttpContextAccessor();

// Filtro que anota Controller/Action y otros metadatos MVC en los bloques fijos del log.
builder.Services.AddScoped<LoggingActionFilter>();

// Servicio central de logging (singleton: configuración y E/S de archivos compartidos).
builder.Services.AddSingleton<ILoggingService, LoggingService>();

// Opciones de logging (rutas, switches .txt/.csv) mapeadas desde appsettings.
builder.Services.Configure<Logging.Configuration.LoggingOptions>(
    builder.Configuration.GetSection("LoggingOptions"));

/// <summary>
/// ===============================  BD Provider  ===============================
/// Proveedor AS400 con soporte de logging estructurado y **HttpContext**.
/// Se pasa IHttpContextAccessor para que el wrapper SQL:
///  - Selle Items["__SqlStartedUtc"] (ancla de orden por INICIO real)
///  - Encole el bloque en HttpClientLogsTimed (mezcla/orden junto con HTTP).
/// </summary>
builder.Services.AddScoped<IDatabaseConnection>(sp =>
{
    IConfiguration config = sp.GetRequiredService<IConfiguration>();
    ConnectionSettings settings = new(config);
    string connStr = settings.GetAS400ConnectionString("AS400");

    return new AS400ConnectionProvider(
        connStr,
        sp.GetRequiredService<ILoggingService>(),
        sp.GetRequiredService<IHttpContextAccessor>()); // ← imprescindible para orden cronológico correcto
});

/// <summary>
/// ===============================  HTTP Saliente  ===============================
/// Handler que intercepta TODAS las solicitudes/respuestas HTTP salientes para loguearlas
/// en la cola “timed”. Los servicios deben usar IHttpClientFactory con el cliente **"Embosado"**.
/// </summary>
builder.Services.AddTransient<HttpClientLoggingHandler>();

// Cliente HTTP **nombrado** con el handler de logging encadenado.
builder.Services.AddHttpClient("Embosado")
    .AddHttpMessageHandler<HttpClientLoggingHandler>();

/// <summary>
/// ===============================  Config general / archivos  ===============================
/// Carga de archivos de configuración de la API.
/// </summary>
builder.Configuration
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile("ConnectionData.json", optional: false, reloadOnChange: true);

IConfiguration configuration = builder.Configuration;

// Resolver nodo de conexión por ambiente y publicarlo globalmente (compatibilidad).
string enviroment = configuration["ApiSettings:Enviroment"] ?? "DEV";
IConfigurationSection connectionSection = configuration.GetSection(enviroment);
ConnectionConfig? connectionConfig = connectionSection.Get<ConnectionConfig>();
GlobalConnection.Current = connectionConfig!; // intencional: debe existir configuración válida

/// <summary>
/// ===============================  JWT / Seguridad  ===============================
/// Carga de clave secreta dinámica y configuración de autenticación JWT.
/// </summary>
var jwtKeyService = new JwtKeyService();                // servicio utilitario existente (sin DI)
var secretKey = await jwtKeyService.GetSecretKeyAsync(); // obtiene llave (p.ej., de bóveda/archivo)
builder.Configuration["JwtKey"] = secretKey;

// Autenticación JWT (validación de firma y expiración; sin issuer/audience estrictos).
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new()
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });

/// <summary>
/// ===============================  Servicios de dominio  ===============================
/// Se registran como Scoped (no typed clients). Cada servicio debe obtener el HttpClient
/// via IHttpClientFactory.CreateClient("Embosado") para que el handler capture los logs HTTP.
/// </summary>
builder.Services.AddTransient<IMachineInfoService, MachineInformationService>();
builder.Services.AddScoped<IJwtService, JwtKeyService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ISessionManagerService, SessionManagerService>();
builder.Services.AddScoped<IDetalleTarjetasImprimirServices, DetalleTarjetasImprimirServices>();
builder.Services.AddScoped<IRegistraImpresionService, RegistraImpresionService>();
builder.Services.AddScoped<IValidaImpresionService, ValidaImpresionService>();
// Si en el futuro conviertes alguno a typed client, su clase debe exponer ctor(HttpClient, ...).

/// <summary>
/// ===============================  MVC / Swagger / CORS  ===============================
/// Inserta el filtro de logging y expone documentación con comentarios XML.
/// </summary>
builder.Services.AddControllers(options =>
{
    // Inserta filtro para capturar Controller/Action en los bloques fijos del log.
    options.Filters.Add<LoggingActionFilter>();
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "MS_BAN_43_Embosado_Tarjetas_Debito",
        Version = "v1",
        Description = "API para embosado de tarjetas de Débito",
        Contact = new OpenApiContact
        {
            Name = "Embosado Tarjetas Débito",
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

// Política CORS (ej. front Angular); abre orígenes/métodos/headers.
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngularOrigins", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

/// <summary>
/// ===============================  Pipeline HTTP  ===============================
/// Activa CORS, seguridad, middleware de logging y Swagger en Dev/Prod.
/// </summary>
var app = builder.Build();

app.UseCors("AllowAngularOrigins");

if (app.Environment.IsDevelopment() || app.Environment.IsProduction())
{
    app.UseSwagger(c => c.RouteTemplate = "swagger/{documentName}/swagger.json");
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "API para embosado de tarjetas de Débito"));
}

app.UseHttpsRedirection();

app.UseAuthentication(); // valida JWT en endpoints protegidos
app.UseAuthorization();  // aplica políticas/atributos

// Middleware de logging central:
// - Escribe los 7 bloques fijos (Inicio/Env/Controlador/Request/Dinámicos/Errores/Fin).
// - Mezcla HTTP/SQL entre (4) Request y (5) Response, ordenados por INICIO real (TsUtc) y Seq.
app.UseMiddleware<LoggingMiddleware>();

app.MapControllers();

app.Run();
