using System;
using System.Diagnostics;
using Castle.DynamicProxy;
using Logging.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

public class LogMethodExecutionInterceptor : IInterceptor
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public LogMethodExecutionInterceptor(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
    }

    public void Intercept(IInvocation invocation)
    {
        var httpContext = _httpContextAccessor.HttpContext;

        if (httpContext == null)
        {
            throw new InvalidOperationException("HttpContext is not available.");
        }

        var loggingService = httpContext.RequestServices.GetService<ILoggingService>();

        if (loggingService == null)
        {
            throw new InvalidOperationException("ILoggingService is not registered in DI.");
        }

        var methodName = invocation.Method.Name;
        var className = invocation.TargetType.Name;

        // Registrar inicio del método
        loggingService.AddSingleLog($"Inicio de ejecución del método {className}.{methodName}");

        var stopwatch = Stopwatch.StartNew();

        try
        {
            invocation.Proceed();
            stopwatch.Stop();

            // Registrar parámetros de salida
            var outputParams = invocation.ReturnValue;
            loggingService.AddOutputParameters(className, methodName, outputParams);

            loggingService.AddSingleLog($"Método {className}.{methodName} ejecutado en {stopwatch.ElapsedMilliseconds} ms");
        }
        catch (Exception ex)
        {
            loggingService.AddExceptionLog(ex);
            throw;
        }
    }
}
