A este método:

  /// <summary>
  /// Captura la información del entorno del servidor y del cliente.
  /// </summary>
  private static async Task<string> CaptureEnvironmentInfoAsync(HttpContext context)
  {
      await Task.Delay(TimeSpan.FromMilliseconds(1));

      var request = context.Request;
      var connection = context.Connection;
      var hostEnvironment = context.RequestServices.GetService<IHostEnvironment>();

      // 1. Intentar obtener de un header HTTP
      var distributionFromHeader = context.Request.Headers["Distribucion"].FirstOrDefault();

      // 2. Intentar obtener de los claims del usuario (si existe autenticación JWT)
      var distributionFromClaim = context.User?.Claims?
          .FirstOrDefault(c => c.Type == "distribution")?.Value;

      // 3. Intentar extraer del subdominio (ejemplo: cliente1.api.com)
      var host = context.Request.Host.Host;
      var distributionFromSubdomain = !string.IsNullOrWhiteSpace(host) && host.Contains('.')
          ? host.Split('.')[0]
          : null;

      // 4. Seleccionar la primera fuente válida o asignar "N/A"
      var distribution = distributionFromHeader
                         ?? distributionFromClaim
                         ?? distributionFromSubdomain
                         ?? "N/A";

      // Preparar información extendida
      string application = hostEnvironment?.ApplicationName ?? "Desconocido";
      string env = hostEnvironment?.EnvironmentName ?? "Desconocido";
      string contentRoot = hostEnvironment?.ContentRootPath ?? "Desconocido";
      string executionId = context.TraceIdentifier ?? "Desconocido";
      string clientIp = connection?.RemoteIpAddress?.ToString() ?? "Desconocido";
      string userAgent = request.Headers.UserAgent.ToString() ?? "Desconocido";
      string machineName = Environment.MachineName;
      string os = Environment.OSVersion.ToString();
      host = request.Host.ToString() ?? "Desconocido";

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
              { "Referer", request.Headers.Referer.ToString() ?? "N/A" }
          };

      return LogFormatter.FormatEnvironmentInfo(
              application: application,
              env: env,
              contentRoot: contentRoot,
              executionId: executionId,
              clientIp: clientIp,
              userAgent: userAgent,
              machineName: machineName,
              os: os,
              host: host,
              distribution: distribution,
              extras: extras
      );
  }


Le quiero agregar otros valores utiles si existen, sino así dejalo.
