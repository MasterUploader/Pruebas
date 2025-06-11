/// <summary>
/// Formatea la información del entorno, incluyendo datos adicionales si están disponibles.
/// </summary>
/// <param name="application">Nombre de la aplicación.</param>
/// <param name="env">Nombre del entorno (Development, Production, etc.).</param>
/// <param name="contentRoot">Ruta raíz del contenido.</param>
/// <param name="executionId">Identificador único de la ejecución.</param>
/// <param name="clientIp">Dirección IP del cliente.</param>
/// <param name="userAgent">Agente de usuario del cliente.</param>
/// <param name="machineName">Nombre de la máquina donde corre la aplicación.</param>
/// <param name="os">Sistema operativo del servidor.</param>
/// <param name="host">Host del request recibido.</param>
/// <param name="distribution">Distribución personalizada u origen (opcional).</param>
/// <param name="extras">Diccionario con información adicional opcional.</param>
/// <returns>Texto formateado con la información del entorno.</returns>
public static string FormatEnvironmentInfoStart(
    string application,
    string env,
    string contentRoot,
    string executionId,
    string clientIp,
    string userAgent,
    string machineName,
    string os,
    string host,
    string distribution,
    Dictionary<string, string>? extras = null)
{
    var sb = new StringBuilder();

    sb.AppendLine("[Inicio de Log]");
    sb.AppendLine($"  Timestamp.............: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}");
    sb.AppendLine($"  Aplicación............: {application}");
    sb.AppendLine($"  Entorno...............: {env}");
    sb.AppendLine($"  Content Root..........: {contentRoot}");
    sb.AppendLine($"  Execution ID..........: {executionId}");
    sb.AppendLine($"  IP Cliente............: {clientIp}");
    sb.AppendLine($"  User Agent............: {userAgent}");
    sb.AppendLine($"  Host..................: {host}");
    sb.AppendLine($"  Máquina...............: {machineName}");
    sb.AppendLine($"  Sistema Operativo.....: {os}");
    sb.AppendLine($"  Distribución..........: {distribution}");

    if (extras is not null && extras.Any())
    {
        sb.AppendLine("  -- Extras del HttpContext --");
        foreach (var kvp in extras)
        {
            sb.AppendLine($"    {kvp.Key,-20}: {kvp.Value}");
        }
    }

    sb.AppendLine(new string('-', 70));
    return sb.ToString();
}
