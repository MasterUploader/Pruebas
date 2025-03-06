using Castle.DynamicProxy;
using Logging.Abstractions;
using Logging.Filters;
using Logging.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Scrutor;
using System;
using System.Linq;

var builder = WebApplication.CreateBuilder(args);

// Registrar IHttpContextAccessor
builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

// Registrar servicios de logging
builder.Services.AddSingleton<ILoggingService, LoggingService>();

// Registrar el generador de proxies de Castle
builder.Services.AddSingleton<IProxyGenerator, ProxyGenerator>();

// Registrar el interceptor
builder.Services.AddTransient<LogMethodExecutionInterceptor>();

// Escanear y registrar todas las clases del ensamblado, excluyendo:
// - Clases del sistema
// - Clases marcadas con [NonIntercepted]
builder.Services.Scan(scan => scan
    .FromAssemblyOf<LoggingService>()
    .AddClasses(classes => classes
        .Where(type =>
            type.Namespace != null &&
            !type.Namespace.StartsWith("System") &&
            !type.IsDefined(typeof(NonInterceptedAttribute), false)))
    .AsSelf()
    .WithTransientLifetime()
);

// Construir la aplicación
var app = builder.Build();

// Aplicar la interceptación después de construir la app
using (var scope = app.Services.CreateScope())
{
    var proxyGenerator = scope.ServiceProvider.GetRequiredService<IProxyGenerator>();
    var interceptor = scope.ServiceProvider.GetRequiredService<LogMethodExecutionInterceptor>();

    foreach (var serviceDescriptor in builder.Services)
    {
        if (serviceDescriptor.ServiceType.Namespace != null &&
            !serviceDescriptor.ServiceType.Namespace.StartsWith("System") &&
            !serviceDescriptor.ServiceType.IsDefined(typeof(NonInterceptedAttribute), false))
        {
            var implementation = scope.ServiceProvider.GetRequiredService(serviceDescriptor.ServiceType);
            var proxy = proxyGenerator.CreateClassProxyWithTarget(serviceDescriptor.ServiceType, implementation, interceptor);
            builder.Services.AddTransient(_ => proxy);
        }
    }
}

// Agregar el middleware de logging
app.UseMiddleware<LoggingMiddleware>();

// Ejecutar la aplicación
app.Run();
