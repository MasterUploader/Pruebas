/// <summary>
/// Formatea la información de inicio del entorno donde se ejecuta la aplicación.
/// Incluye datos del sistema operativo, máquina, entorno, y cliente.
/// </summary>
public static string FormatEnvironmentInfoStart(
    string application, string env, string contentRoot, string executionId,
    string clientIp, string userAgent, string machineName, string os,
    string host, string distribution)
{
    var sb = new StringBuilder();

    sb.AppendLine("---------------------------Enviroment Info-------------------------");
    sb.AppendLine($"Inicio: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
    sb.AppendLine("-------------------------------------------------------------------");
    sb.AppendLine($"Application: {application}");
    sb.AppendLine($"Environment: {env}");
    sb.AppendLine($"ContentRoot: {contentRoot}");
    sb.AppendLine($"Execution ID: {executionId}");
    sb.AppendLine($"Client IP: {clientIp}");
    sb.AppendLine($"User Agent: {userAgent}");
    sb.AppendLine($"Machine Name: {machineName}");
    sb.AppendLine($"OS: {os}");
    sb.AppendLine($"Host: {host}");
    sb.AppendLine($"Distribución: {distribution}");
    sb.AppendLine("----------------------------Enviroment Info-------------------------");

    return sb.ToString();
}



/// <summary>
/// Formatea la sección de cierre de la información del entorno.
/// </summary>
public static string FormatEnvironmentInfoEnd()
{
    var sb = new StringBuilder();

    sb.AppendLine("---------------------------Enviroment Info-------------------------");
    sb.AppendLine($"Fin: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
    sb.AppendLine("-------------------------------------------------------------------");

    return sb.ToString();
}
