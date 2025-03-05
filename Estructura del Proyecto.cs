public void Intercept(IInvocation invocation)
{
    var context = _httpContextAccessor.HttpContext;
    if (context == null)
    {
        invocation.Proceed();
        return;
    }

    string methodName = $"{invocation.TargetType.Name}.{invocation.Method.Name}";
    var stopwatch = Stopwatch.StartNew();

    try
    {
        _loggingService.AddSingleLog($"[Inicio de Método]: {methodName}");
        _loggingService.WriteLog(context, $"[Inicio de Método]: {methodName}");

        // Capturar parámetros de entrada
        var inputParams = JsonSerializer.Serialize(invocation.Arguments, new JsonSerializerOptions { WriteIndented = true });
        _loggingService.AddInputParameters(methodName, inputParams, context);

        // Ejecutar el método real
        invocation.Proceed();

        if (invocation.Method.ReturnType == typeof(Task))
        {
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
                    _loggingService.WriteLog(context, $"[Fin de Método]: {methodName} en {stopwatch.ElapsedMilliseconds} ms");
                }
            });
        }
        else
        {
            // Capturar parámetros de salida si no es una tarea asincrónica
            string outputParams = invocation.ReturnValue != null
                ? JsonSerializer.Serialize(invocation.ReturnValue, new JsonSerializerOptions { WriteIndented = true })
                : "Sin retorno (void)";

            _loggingService.AddOutputParameters(methodName, outputParams, context);
            _loggingService.AddSingleLog($"[Fin de Método]: {methodName} en {stopwatch.ElapsedMilliseconds} ms");
            _loggingService.WriteLog(context, $"[Fin de Método]: {methodName} en {stopwatch.ElapsedMilliseconds} ms");
        }
    }
    catch (Exception ex)
    {
        _loggingService.AddExceptionLog(ex);
        throw;
    }
}
