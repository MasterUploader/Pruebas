using RestUtilities.Connections;
using RestUtilities.Connections.Interfaces;
using RestUtilities.Connections.Providers.Database;
using RestUtilities.Logging;

var builder = WebApplication.CreateBuilder(args);

// Cargar configuración desde Connection.json por ambiente (DEV, UAT, PROD)
builder.Configuration.AddJsonFile("Connection.json", optional: false, reloadOnChange: true);

// Registrar ConnectionSettings para leer configuración dinámica
builder.Services.AddSingleton<ConnectionSettings>();

// Registrar tu sistema de logging (si lo estás usando)
builder.Services.AddSingleton<ILogger, Logger>();

// Registrar proveedor de conexión a AS400 usando la configuración del ambiente actual
builder.Services.AddSingleton<IDatabaseConnection>(sp =>
{
    var settings = sp.GetRequiredService<ConnectionSettings>();
    var logger = sp.GetRequiredService<ILogger>();

    string connectionString = settings.GetConnectionString("AS400");

    logger.LogInformation($"[AS400] Usando cadena de conexión: {connectionString}");

    return new AS400ConnectionProvider(connectionString);
});

builder.Services.AddControllers();

var app = builder.Build();

app.MapControllers();

app.Run();
