public void LogDatabaseSuccess(DbCommand command, long elapsedMs, HttpContext? context = null, string? customMessage = null)
{
    try
    {
        var connectionInfo = LogHelper.ExtractDbConnectionInfo(command.Connection?.ConnectionString);

        var formatted = LogFormatter.FormatDbExecution(
            nombreBD: connectionInfo.Database,
            ip: connectionInfo.Ip,
            puerto: connectionInfo.Port,
            sentenciasSQL: new List<string> { command.CommandText },
            cantidadEjecuciones: 1,
            resultado: customMessage ?? "Éxito",
            horaInicio: DateTime.Now.AddMilliseconds(-elapsedMs),
            duracion: TimeSpan.FromMilliseconds(elapsedMs)
        );

        WriteLog(context, formatted);
    }
    catch (Exception ex)
    {
        LogInternalError(ex);
    }
}


public class DbConnectionInfo
{
    public string Ip { get; set; } = "Desconocido";
    public int Port { get; set; } = 0;
    public string Database { get; set; } = "Desconocida";
    public string Library { get; set; } = "Desconocida";
}

public static class LogHelper
{
    /// <summary>
    /// Extrae los datos de IP, puerto, base de datos y biblioteca desde una cadena de conexión.
    /// </summary>
    public static DbConnectionInfo ExtractDbConnectionInfo(string? connectionString)
    {
        var info = new DbConnectionInfo();

        if (string.IsNullOrWhiteSpace(connectionString))
            return info;

        var parts = connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries);
        foreach (var part in parts)
        {
            var kv = part.Split('=', 2);
            if (kv.Length != 2) continue;

            var key = kv[0].Trim().ToLowerInvariant();
            var value = kv[1].Trim();

            switch (key)
            {
                case "data source":
                case "server":
                    if (value.Contains(":"))
                    {
                        var ipPort = value.Split(':');
                        info.Ip = ipPort[0];
                        int.TryParse(ipPort[1], out int port);
                        info.Port = port;
                    }
                    else
                    {
                        info.Ip = value;
                    }
                    break;

                case "port":
                    int.TryParse(value, out int parsedPort);
                    info.Port = parsedPort;
                    break;

                case "initial catalog":
                case "database":
                    info.Database = value;
                    break;

                case "default collection":
                case "library":
                    info.Library = value;
                    break;
            }
        }

        return info;
    }
}


