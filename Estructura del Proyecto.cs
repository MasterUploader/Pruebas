using System;
using System.Diagnostics;
using Castle.DynamicProxy;
using Logging.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class LogMethodExecutionAttribute : Attribute, IInterceptor
{
    public void Intercept(IInvocation invocation)
    {
        var httpContextAccessor = invocation.InvocationTarget as IHttpContextAccessor;

        if (httpContextAccessor == null)
        {
            throw new InvalidOperationException("IHttpContextAccessor is not available.");
        }

        var httpContext = httpContextAccessor.HttpContext;
        var loggingService = httpContext?.RequestServices.GetService<ILoggingService>();

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
