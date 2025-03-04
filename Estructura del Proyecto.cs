using Logging.Services;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Text.Json;

namespace Logging.Filters
{
    /// <summary>
    /// Filtro de acción que captura la ejecución de métodos dentro de los controladores.
    /// </summary>
    public class LoggingActionFilter : IActionFilter
    {
        private readonly LoggingService _loggingService;

        /// <summary>
        /// Constructor que inyecta el servicio de logging.
        /// </summary>
        /// <param name="loggingService">Instancia del servicio de logging.</param>
        public LoggingActionFilter(LoggingService loggingService)
        {
            _loggingService = loggingService;
        }

        /// <summary>
        /// Se ejecuta antes de que el método del controlador sea llamado.
        /// Registra la entrada del método y los parámetros de entrada.
        /// </summary>
        /// <param name="context">Contexto de la ejecución de la acción.</param>
        public void OnActionExecuting(ActionExecutingContext context)
        {
            // Verifica si la acción pertenece a un controlador para capturar métodos internos.
            if (context.ActionDescriptor is ControllerActionDescriptor descriptor)
            {
                string methodName = descriptor.MethodInfo.Name;
                
                // Serializa los parámetros de entrada del método.
                string parameters = context.ActionArguments.Count > 0 
                    ? JsonSerializer.Serialize(context.ActionArguments, new JsonSerializerOptions { WriteIndented = true }) 
                    : "Sin parámetros";

                // Registra el inicio del método en los logs.
                _loggingService.AddMethodEntryLog(methodName, parameters);
            }
        }

        /// <summary>
        /// Se ejecuta después de que el método del controlador haya terminado su ejecución.
        /// Registra el valor de retorno del método.
        /// </summary>
        /// <param name="context">Contexto de la ejecución de la acción.</param>
        public void OnActionExecuted(ActionExecutedContext context)
        {
            // Verifica si la acción pertenece a un controlador.
            if (context.ActionDescriptor is ControllerActionDescriptor descriptor)
            {
                string methodName = descriptor.MethodInfo.Name;

                // Determina el valor de retorno del método.
                string returnValue = context.Result != null 
                    ? JsonSerializer.Serialize(context.Result, new JsonSerializerOptions { WriteIndented = true }) 
                    : "Sin valor de retorno";

                // Registra la salida del método en los logs.
                _loggingService.AddMethodExitLog(methodName, returnValue);
            }
        }
    }
}
