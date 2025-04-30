using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using RestUtilities.Connections.Interfaces;
using RestUtilities.Connections.Providers.Database;
using RestUtilities.Connections.Services;

var builder = WebApplication.CreateBuilder(args);

// Cargar configuración desde Connection.json
builder.Configuration.AddJsonFile("Connection.json", optional: false, reloadOnChange: true);

// Registrar ConnectionSettings como singleton (lee ambiente, encriptación, etc.)
builder.Services.AddSingleton<ConnectionSettings>();

// Registrar la conexión principal a AS400 usando OleDbCommand
builder.Services.AddScoped<IDatabaseConnection>(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var settings = new ConnectionSettings(config);

    // Obtener la cadena de conexión desencriptada desde el archivo
    var connStr = settings.GetRawConnectionString("AS400"); // o el nombre que estés usando

    // Crear instancia del proveedor híbrido (soporta OleDbCommand + DbContext)
    return new AS400ConnectionProvider(connStr);
});

// Otros servicios de tu API
builder.Services.AddScoped<ILoginService, LoginService>();

builder.Services.AddControllers();
var app = builder.Build();

app.MapControllers();
app.Run();
