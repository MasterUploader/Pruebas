using Connections.Interfaces;
using Connections.Providers.Database.AS400;
using Logging.Abstractions;
using Microsoft.AspNetCore.Http;

var builder = WebApplication.CreateBuilder(args);

// Agregar configuración y servicios necesarios
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Registrar IHttpContextAccessor para acceso al contexto en servicios
builder.Services.AddHttpContextAccessor();

// Registrar servicio de Logging
builder.Services.AddScoped<ILoggingService, LoggingService>();

// Registrar servicio de conexión con soporte de logging para AS400
builder.Services.AddScoped<IDatabaseConnection>(sp =>
{
    // Obtener configuración general
    var config = sp.GetRequiredService<IConfiguration>();
    var settings = new ConnectionSettings(config); // Tu clase para acceder a settings

    // Obtener cadena de conexión desencriptada para AS400
    var connStr = settings.GetAS400ConnectionString("AS400");

    // Instanciar proveedor interno
    var innerProvider = new AS400ConnectionProvider(
        connStr,
        sp.GetRequiredService<ILoggingService>()); // Constructor de AS400ConnectionProvider

    // Retornar decorador con logging
    return new LoggingDatabaseConnectionDecorator(
        innerProvider,
        sp.GetRequiredService<ILoggingService>(),
        sp.GetRequiredService<IHttpContextAccessor>());
});

var app = builder.Build();

// Configuración de middlewares
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
