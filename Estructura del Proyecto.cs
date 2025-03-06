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
    .FromAssemblyOf<LoggingService>() // Se escanea desde el ensamblado actual
    .AddClasses(classes => classes
        .Where(type => 
            type.Namespace != null &&  // Evita clases sin namespace
            !type.Namespace.StartsWith("System") && // Excluye clases del sistema
            !type.IsDefined(typeof(NonInterceptedAttribute), false))) // Excluye clases con [NonIntercepted]
    .AsSelf()
    .WithTransientLifetime()
    .OnActivated(context =>
    {
        var proxyGenerator = context.GetRequiredService<IProxyGenerator>();
        var interceptor = context.GetRequiredService<LogMethodExecutionInterceptor>();

        // Reemplaza la instancia original con su proxy
        var instanceType = context.Instance.GetType();
        var proxy = proxyGenerator.CreateClassProxyWithTarget(instanceType, context.Instance, interceptor);
        context.ReplaceInstance(proxy);
    })
);

var app = builder.Build();
app.UseMiddleware<LoggingMiddleware>();

app.Run();
