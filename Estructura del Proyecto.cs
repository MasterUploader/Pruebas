/// <summary>
/// Agrega la información del entorno al log, capturando detalles del sistema, petición actual y distribución.
/// </summary>
public void AddEnvironmentLog()
{
    try
    {
        var context = _httpContextAccessor.HttpContext;
        var hostEnvironment = context?.RequestServices.GetService<IHostEnvironment>();

        // 1. Intentar obtener de un header HTTP
        var distributionFromHeader = context?.Request.Headers["Distribucion"].FirstOrDefault();

        // 2. Intentar obtener de los claims del usuario (si existe autenticación JWT)
        var distributionFromClaim = context?.User?.Claims?
            .FirstOrDefault(c => c.Type == "distribution")?.Value;

        // 3. Intentar extraer del subdominio (ejemplo: cliente1.api.com)
        var host = context?.Request.Host.Host;
        var distributionFromSubdomain = !string.IsNullOrWhiteSpace(host) && host.Contains(".")
            ? host.Split('.')[0]
            : null;

        // 4. Seleccionar la primera fuente válida o asignar "N/A"
        var distribution = distributionFromHeader
                           ?? distributionFromClaim
                           ?? distributionFromSubdomain
                           ?? "N/A";

        // Llamar al formateador
        string formatted = LogFormatter.FormatEnvironmentInfoStart(
            application: hostEnvironment?.ApplicationName ?? "Desconocido",
            env: hostEnvironment?.EnvironmentName ?? "Desconocido",
            contentRoot: hostEnvironment?.ContentRootPath ?? "Desconocido",
            executionId: context?.TraceIdentifier ?? "Desconocido",
            clientIp: context?.Connection.RemoteIpAddress?.ToString() ?? "Desconocido",
            userAgent: context?.Request.Headers["User-Agent"].ToString() ?? "Desconocido",
            machineName: Environment.MachineName,
            os: Environment.OSVersion.ToString(),
            host: context?.Request.Host.ToString() ?? "Desconocido",
            distribution: distribution
        );

        // Guardar en archivo
        LogHelper.WriteLogToFile(_logDirectory, GetCurrentLogFile(), formatted);
    }
    catch (Exception ex)
    {
        LogInternalError(ex);
    }
}
