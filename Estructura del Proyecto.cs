using Microsoft.AspNetCore.Mvc.Filters;
using System.Diagnostics;
using Logging.Abstractions;

namespace Logging.Filters
{
    /// <summary>
    /// Filtro de acción que se ejecuta antes y después de la ejecución de una acción en un controlador.
    /// Captura información como parámetros de entrada, salida, tiempos de ejecución y errores.
    /// </summary>
    public class LoggingActionFilter : IAsyncActionFilter
    {
        private readonly ILoggingService _loggingService;
        private Stopwatch _stopwatch; // Para medir el tiempo de ejecución de la acción

        /// <summary>
        /// Constructor: Recibe `ILoggingService` a través de inyección de dependencias.
        /// </summary>
        public LoggingActionFilter(ILoggingService loggingService)
        {
            _loggingService = loggingService ?? throw new ArgumentNullException(nameof(loggingService));
        }

        /// <summary>
        /// Se ejecuta antes y después de la acción del controlador.
        /// Registra los parámetros de entrada y la información de inicio.
        /// </summary>
        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            try
            {
                _stopwatch = Stopwatch.StartNew(); // Iniciar medición del tiempo de ejecución
                
                // Capturar el nombre del controlador y acción
                string controllerName = context.ActionDescriptor.RouteValues["controller"] ?? "Desconocido";
                string actionName = context.ActionDescriptor.RouteValues["action"] ?? "Desconocido";
                string methodName = $"{controllerName}.{actionName}";

                // Capturar los parámetros de entrada
                string inputParams = context.ActionArguments.Any()
                    ? string.Join(Environment.NewLine, context.ActionArguments.Select(arg => $"{arg.Key}: {arg.Value}"))
                    : "Sin parámetros de entrada";

                // Escribir en el log la información inicial
                _loggingService.AddSingleLog($"[Inicio de Acción] {methodName} | Parámetros de entrada: {inputParams}");

                // Continuar con la ejecución del siguiente middleware
                var executedContext = await next();

                // Capturar los datos de salida después de la ejecución
                await OnActionExecutedAsync(executedContext);
            }
            catch (Exception ex)
            {
                _loggingService.AddExceptionLog(ex);
            }
        }

        /// <summary>
        /// Se ejecuta después de que la acción del controlador ha terminado.
        /// Registra los parámetros de salida y el tiempo de ejecución.
        /// </summary>
        private async Task OnActionExecutedAsync(ActionExecutedContext context)
        {
            try
            {
                _stopwatch.Stop(); // Detener la medición del tiempo

                // Capturar el nombre del controlador y acción
                string controllerName = context.ActionDescriptor.RouteValues["controller"] ?? "Desconocido";
                string actionName = context.ActionDescriptor.RouteValues["action"] ?? "Desconocido";
                string methodName = $"{controllerName}.{actionName}";

                // Capturar la respuesta del método
                string outputParams = "Sin datos de salida";
                if (context.Result is ObjectResult objectResult && objectResult.Value != null)
                {
                    outputParams = System.Text.Json.JsonSerializer.Serialize(objectResult.Value, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                }

                // Guardar los datos en el log
                _loggingService.AddSingleLog($"[Fin de Acción] {methodName} | Parámetros de salida: {outputParams}");
                _loggingService.AddSingleLog($"[Tiempo de ejecución] {methodName} | { _stopwatch.ElapsedMilliseconds} ms");
            }
            catch (Exception ex)
            {
                _loggingService.AddExceptionLog(ex);
            }
        }
    }
}
