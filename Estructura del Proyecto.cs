Es este el metodo que debemos modificar
        /// <summary>
        /// Obtiene el archivo de log de la petición actual, garantizando que toda la información
        /// se guarde en el mismo archivo. Si no existe aún, se genera uno nuevo dentro de una carpeta
        /// con el nombre del controlador.
        /// </summary>
        public string GetCurrentLogFile()
        {
            try
            {
                var context = _httpContextAccessor.HttpContext;

                // Si ya existe un archivo de log en esta petición, reutilizarlo
                if (context is not null &&
                    context.Items.ContainsKey("LogFileName") &&
                    context.Items["LogFileName"] is string logFileName)
                {
                    return logFileName;
                }

                // Generar un nuevo nombre de archivo solo si no se ha creado antes
                if (context is not null && context.Items.ContainsKey("ExecutionId"))
                {
                    string executionId = context.Items["ExecutionId"]?.ToString() ?? Guid.NewGuid().ToString();
                    string endpoint = context.Request.Path.ToString().Replace("/", "_").Trim('_');
                    string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");

                    // Obtener el nombre del controlador a partir del endpoint actual
                    var endpointMetadata = context.GetEndpoint();
                    var controllerName = endpointMetadata?.Metadata
                        .OfType<Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor>()
                        .FirstOrDefault()?.ControllerName ?? "UnknownController";

                    // Crear subcarpeta del controlador si no existe
                    var controllerDirectory = Path.Combine(_logDirectory, controllerName);
                    Directory.CreateDirectory(controllerDirectory);

                    Console.WriteLine($"[DEBUG] Ruta carpeta controlador: {controllerDirectory}");

                    if (!Directory.Exists(controllerDirectory))
                    {
                        Directory.CreateDirectory(controllerDirectory);
                        Console.WriteLine("[DEBUG] Carpeta creada");
                    }
                    else
                    {
                        Console.WriteLine("[DEBUG] Carpeta ya existía");
                    }

                    // Generar nombre de archivo incluyendo subcarpeta del controlador
                    string newLogFileName = Path.Combine(controllerDirectory, $"{executionId}_{endpoint}_{timestamp}.txt");

                    context.Items["LogFileName"] = newLogFileName;
                    return newLogFileName;
                }
            }
            catch (Exception ex)
            {
                LogInternalError(ex); // Método interno para registrar errores del sistema de logging
            }

            return Path.Combine(_logDirectory, "GlobalManualLogs.txt");
        }
