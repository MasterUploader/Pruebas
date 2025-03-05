using System;
using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using Castle.DynamicProxy;
using Logging.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace Logging.Filters
{
    /// <summary>
    /// Atributo para interceptar la ejecución de métodos y registrar logs automáticamente.
    /// Se usa con Castle DynamicProxy para capturar cualquier método decorado con este atributo.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class LogMethodExecutionAttribute : Attribute { }

    /// <summary>
    /// Interceptor que captura métodos decorados con [LogMethodExecution] y registra su ejecución en el log.
    /// </summary>
    public class LogMethodExecutionInterceptor : IInterceptor
    {
        private readonly ILoggingService _loggingService;

        public LogMethodExecutionInterceptor(ILoggingService loggingService)
        {
            _loggingService = loggingService ?? throw new ArgumentNullException(nameof(loggingService));
        }

        public void Intercept(IInvocation invocation)
        {
            var methodName = $"{invocation.TargetType.Name}.{invocation.Method.Name}";
            var stopwatch = Stopwatch.StartNew();

            try
            {
                _loggingService.AddSingleLog($"[Inicio de Método]: {methodName}");

                // Capturar parámetros de entrada
                var inputParams = JsonSerializer.Serialize(invocation.Arguments, new JsonSerializerOptions { WriteIndented = true });
                _loggingService.AddInputParameters(inputParams);

                // Ejecutar el método real
                invocation.Proceed();

                if (invocation.Method.ReturnType == typeof(Task))
                {
                    // Si el método devuelve Task, necesitamos capturar el resultado asincrónamente
                    var task = (Task)invocation.ReturnValue;
                    task.ContinueWith(t =>
                    {
                        if (t.IsFaulted)
                        {
                            _loggingService.AddExceptionLog(t.Exception);
                        }
                        else
                        {
                            _loggingService.AddSingleLog($"[Fin de Método]: {methodName} en {stopwatch.ElapsedMilliseconds} ms");
                        }
                        _loggingService.WriteLog();
                    });
                }
                else
                {
                    // Capturar parámetros de salida si no es una tarea asincrónica
                    string outputParams = invocation.ReturnValue != null
                        ? JsonSerializer.Serialize(invocation.ReturnValue, new JsonSerializerOptions { WriteIndented = true })
                        : "Sin retorno (void)";

                    _loggingService.AddOutputParameters(outputParams);
                    _loggingService.AddSingleLog($"[Fin de Método]: {methodName} en {stopwatch.ElapsedMilliseconds} ms");
                    _loggingService.WriteLog();
                }
            }
            catch (Exception ex)
            {
                _loggingService.AddExceptionLog(ex);
                throw;
            }
        }
    }
}
