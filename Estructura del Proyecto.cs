using CAUAdministracion.Data;
using Microsoft.EntityFrameworkCore;
using RestUtilities.Connections;
using RestUtilities.Connections.Interfaces;
using RestUtilities.Connections.Providers.Database;

var builder = WebApplication.CreateBuilder(args);

// Cargar configuración dinámica desde Connection.json
builder.Configuration.AddJsonFile("Connection.json", optional: false, reloadOnChange: true);

// Registrar clase que gestiona ambientes y conexiones
builder.Services.AddSingleton<ConnectionSettings>();

// Registrar tu propio As400DbContext con la cadena de conexión del ambiente actual
builder.Services.AddDbContext<As400DbContext>(options =>
{
    var config = new ConnectionSettings(builder.Configuration);
    var connectionString = config.GetAS400ConnectionString("AS400");

    options.UseDb2(connectionString, o => o.SetServerInfo(IBMDBServerType.AS400));
});

// Adaptar el DbContext a IDatabaseConnection
builder.Services.AddScoped<IDatabaseConnection>(sp =>
{
    var ctx = sp.GetRequiredService<As400DbContext>();
    return new ExternalDbContextConnectionProvider<As400DbContext>(ctx);
});

// Servicios propios de tu API
builder.Services.AddControllersWithViews();
builder.Services.AddScoped<ILoginService, LoginService>();
builder.Services.AddSession();

builder.Services.AddAuthentication("Cookies")
    .AddCookie("Cookies", options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
    });

var app = builder.Build();

app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();
app.MapDefaultControllerRoute();

app.Run();
