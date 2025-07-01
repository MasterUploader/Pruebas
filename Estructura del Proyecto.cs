/// <summary>
/// Formatea un bloque de log para errores en ejecución SQL, incluyendo contexto y detalles de excepción.
/// </summary>
/// <param name="nombreBD">Nombre de la base de datos.</param>
/// <param name="ip">IP del servidor de base de datos.</param>
/// <param name="puerto">Puerto utilizado en la conexión.</param>
/// <param name="biblioteca">Biblioteca o esquema objetivo.</param>
/// <param name="tabla">Tabla afectada por la operación fallida.</param>
/// <param name="sentenciaSQL">Sentencia SQL que generó el error.</param>
/// <param name="exception">Excepción lanzada por el proveedor de datos.</param>
/// <param name="horaError">Hora en la que ocurrió el error.</param>
/// <returns>Texto formateado para almacenar como log de error estructurado.</returns>
public static string FormatDbExecutionError(
    string nombreBD,
    string ip,
    int puerto,
    string biblioteca,
    string tabla,
    string sentenciaSQL,
    Exception exception,
    DateTime horaError)
{
    var sb = new StringBuilder();

    sb.AppendLine("============= DB ERROR =============");
    sb.AppendLine($"Nombre BD: {nombreBD}");
    sb.AppendLine($"IP: {ip}");
    sb.AppendLine($"Puerto: {puerto}");
    sb.AppendLine($"Biblioteca: {biblioteca}");
    sb.AppendLine($"Tabla: {tabla}");
    sb.AppendLine($"Hora del error: {horaError:yyyy-MM-dd HH:mm:ss}");
    sb.AppendLine("Sentencia SQL:");
    sb.AppendLine(sentenciaSQL);
    sb.AppendLine();
    sb.AppendLine("Excepción:");
    sb.AppendLine(exception.Message);
    sb.AppendLine("StackTrace:");
    sb.AppendLine(exception.StackTrace ?? "Sin detalles de stack.");
    sb.AppendLine("============= END DB ERROR ===================");

    return sb.ToString();
}


public void LogDatabaseError(DbCommand command, Exception ex, HttpContext? context = null)
{
    try
    {
        var connectionInfo = LogHelper.ExtractDbConnectionInfo(command.Connection?.ConnectionString);
        var tabla = LogHelper.ExtractTableName(command.CommandText);

        var formatted = LogFormatter.FormatDbExecutionError(
            nombreBD: connectionInfo.Database,
            ip: connectionInfo.Ip,
            puerto: connectionInfo.Port,
            biblioteca: connectionInfo.Library,
            tabla: tabla,
            sentenciaSQL: command.CommandText,
            exception: ex,
            horaError: DateTime.Now
        );

        WriteLog(context, formatted);
        AddExceptionLog(ex); // También lo guardás como log general si usás esa ruta
    }
    catch (Exception errorAlLoguear)
    {
        LogInternalError(errorAlLoguear);
    }
}
