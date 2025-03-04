using Logging.Services;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Text.Json;

namespace Logging.Filters
{
    /// <summary>
    /// Atributo para registrar automáticamente la ejecución de métodos en los logs.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class LogMethodExecutionAttribute : ActionFilterAttribute
    {
        private LoggingService? _loggingService;

        /// <summary>
        /// Se ejecuta antes de que el método sea llamado, registrando sus parámetros de entrada.
        /// </summary>
        /// <param name="context">Contexto de ejecución de la acción.</param>
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            // Obtiene la instancia del servicio de logging.
            _loggingService = context.HttpContext.RequestServices.GetService<LoggingService>();

            if (_loggingService != null)
            {
                string methodName = context.ActionDescriptor.DisplayName;

                // Serializa los parámetros de entrada.
                string parameters = context.ActionArguments.Count > 0
                    ? JsonSerializer.Serialize(context.ActionArguments, new JsonSerializerOptions { WriteIndented = true })
                    : "Sin parámetros";

                // Registra la entrada del método.
                _loggingService.AddMethodEntryLog(methodName, parameters);
            }
        }

        /// <summary>
        /// Se ejecuta después de que el método haya finalizado, registrando su valor de retorno.
        /// </summary>
        /// <param name="context">Contexto de ejecución de la acción.</param>
        public override void OnActionExecuted(ActionExecutedContext context)
        {
            if (_loggingService != null)
            {
                string methodName = context.ActionDescriptor.DisplayName;

                // Determina el valor de retorno del método.
                string returnValue = context.Result != null
                    ? JsonSerializer.Serialize(context.Result, new JsonSerializerOptions { WriteIndented = true })
                    : "Sin valor de retorno";

                // Registra la salida del método.
                _loggingService.AddMethodExitLog(methodName, returnValue);
            }
        }
    }
}
