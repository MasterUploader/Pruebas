/// <summary>
/// Formatea los parámetros de entrada de un método antes de guardarlos en el log.
/// </summary>
public static string FormatInputParameters(IDictionary<string, object> parameters)
{
    var sb = new StringBuilder();

    sb.AppendLine("-----------------------Parámetros de Entrada-----------------------------------");
    
    if (parameters == null || parameters.Count == 0)
    {
        sb.AppendLine("Sin parámetros de entrada.");
    }
    else
    {
        foreach (var param in parameters)
        {
            if (param.Value == null)
            {
                sb.AppendLine($"{param.Key} = null");
            }
            else if (param.Value.GetType().IsPrimitive || param.Value is string)
            {
                sb.AppendLine($"{param.Key} = {param.Value}");
            }
            else
            {
                string json = JsonSerializer.Serialize(param.Value, new JsonSerializerOptions { WriteIndented = true });
                sb.AppendLine($"Objeto {param.Key} =\n{json}");
            }
        }
    }

    sb.AppendLine("-----------------------Parámetros de Entrada-----------------------------------");

    return sb.ToString();
}




public override void OnActionExecuting(ActionExecutingContext context)
{
    var loggingService = context.HttpContext.RequestServices.GetRequiredService<ILoggingService>();

    // Capturar el nombre del método y los parámetros de entrada
    string methodName = $"{context.Controller.GetType().Name}.{context.ActionDescriptor.DisplayName}";
    string parameters = LogFormatter.FormatInputParameters(context.ActionArguments);

    // Registrar la entrada del método
    loggingService.AddMethodEntryLog(methodName, parameters);
}
