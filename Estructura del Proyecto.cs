/// <summary>
/// Agrega la información del entorno al log, capturando detalles del sistema y la petición actual.
/// </summary>
public void AddEnvironmentLog()
{
    try
    {
        var context = _httpContextAccessor.HttpContext;
        var hostEnvironment = context?.RequestServices.GetService<IHostEnvironment>();

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
            distribution: "N/A"
        );

        LogHelper.WriteLogToFile(_logDirectory, GetCurrentLogFile(), formatted);
    }
    catch (Exception ex)
    {
        LogInternalError(ex);
    }
}
