using Logging.Services;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using System;
using System.Linq;
using System.Text.Json;

namespace Logging.Filters
{
    /// <summary>
    /// Filtro de acción que captura la ejecución de métodos dentro de la API.
    /// </summary>
    public class LoggingActionFilter : IActionFilter
    {
        private readonly LoggingService _loggingService;

        public LoggingActionFilter(LoggingService loggingService)
        {
            _loggingService = loggingService;
        }

        public void OnActionExecuting(ActionExecutingContext context)
        {
            if (context.ActionDescriptor is ControllerActionDescriptor descriptor)
            {
                string methodName = descriptor.MethodInfo.Name;
                string parameters = JsonSerializer.Serialize(context.ActionArguments);
                _loggingService.AddMethodEntryLog(methodName, parameters);
            }
        }

        public void OnActionExecuted(ActionExecutedContext context)
        {
            if (context.ActionDescriptor is ControllerActionDescriptor descriptor)
            {
                string methodName = descriptor.MethodInfo.Name;
                string returnValue = context.Result != null ? JsonSerializer.Serialize(context.Result) : "Void";
                _loggingService.AddMethodExitLog(methodName, returnValue);
            }
        }
    }
}
