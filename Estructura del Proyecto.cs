/// <summary>
/// Captura la información del entorno del servidor y del cliente, incluyendo detalles extendidos del HttpContext.
/// </summary>
private async Task<string> CaptureEnvironmentInfoAsync(HttpContext context)
{
    var request = context.Request;
    var connection = context.Connection;
    var hostEnvironment = context.RequestServices.GetService<IHostEnvironment>();

    // Preparar información extendida
    string application = hostEnvironment?.ApplicationName ?? "Desconocido";
    string env = hostEnvironment?.EnvironmentName ?? "Desconocido";
    string contentRoot = hostEnvironment?.ContentRootPath ?? "Desconocido";
    string executionId = context.TraceIdentifier ?? "Desconocido";
    string clientIp = connection?.RemoteIpAddress?.ToString() ?? "Desconocido";
    string userAgent = request.Headers["User-Agent"].ToString() ?? "Desconocido";
    string machineName = Environment.MachineName;
    string os = Environment.OSVersion.ToString();
    string host = request.Host.ToString() ?? "Desconocido";

    // Información adicional del contexto
    var extras = new Dictionary<string, string>
    {
        { "Scheme", request.Scheme },
        { "Protocol", request.Protocol },
        { "Method", request.Method },
        { "Path", request.Path },
        { "Query", request.QueryString.ToString() },
        { "ContentType", request.ContentType ?? "N/A" },
        { "ContentLength", request.ContentLength?.ToString() ?? "N/A" },
        { "ClientPort", connection?.RemotePort.ToString() ?? "Desconocido" },
        { "LocalIp", connection?.LocalIpAddress?.ToString() ?? "Desconocido" },
        { "LocalPort", connection?.LocalPort.ToString() ?? "Desconocido" },
        { "ConnectionId", connection?.Id ?? "Desconocido" },
        { "Referer", request.Headers["Referer"].ToString() ?? "N/A" }
    };

    // Usar el formateador existente
    return LogFormatter.FormatEnvironmentInfoStart(
        application: application,
        env: env,
        contentRoot: contentRoot,
        executionId: executionId,
        clientIp: clientIp,
        userAgent: userAgent,
        machineName: machineName,
        os: os,
        host: host,
        distribution: "N/A",
        extras: extras // Se requiere que FormatEnvironmentInfoStart lo soporte
    );
}
