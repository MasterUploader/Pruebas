Console.WriteLine($"[LOGGING] Controlador: {actionDescriptor.ControllerName}, Acción: {actionDescriptor.ActionName}");
foreach (var param in actionDescriptor.Parameters)
{
    Console.WriteLine($"[LOGGING] Parámetro: {param.Name}, Tipo: {param.ParameterType.Name}");
}

if (modelType == null)
    Console.WriteLine("[LOGGING] ❌ No se encontró ningún modelo con propiedades [LogFileName]");
