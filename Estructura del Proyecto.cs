/// <summary>
/// Registra información de ejecución de una operación SQL en formato estructurado.
/// Este método genera una representación textual (a través del formateador)
/// y la persiste en el archivo de log asociado al ciclo de la petición actual,
/// garantizando coherencia con el resto de eventos registrados durante la misma solicitud.
/// </summary>
/// <param name="model">Datos de la ejecución (duración, SQL, conexiones, filas afectadas, etc.).</param>
/// <param name="context">
/// Contexto HTTP actual (opcional). Cuando está presente, permite resolver y reutilizar
/// el archivo de log asociado al request/endpoint en curso.
/// </param>
public void LogDatabaseSuccess(SqlLogModel model, HttpContext? context = null)
{
    try
    {
        // Construye el bloque en texto plano utilizando el formateador existente,
        // manteniendo el mismo esquema visual y de campos.
        var formatted = LogFormatter.FormatDbExecution(model);

        // Escribe en el MISMO archivo asociado al request/endpoint (si existe contexto),
        // preservando la coherencia del rastro completo sin crear archivos alternos.
        WriteLog(context, formatted);

        // Si además llevas métricas/CSV/telemetría separada, hazlo aquí sin duplicar
        // la escritura del bloque de texto (por ejemplo, llamando a un método explícito
        // de CSV que no genere otro .txt):
        // TryWriteCsv(model, context);
    }
    catch (Exception loggingEx)
    {
        // El logging nunca debe interrumpir el flujo de la aplicación;
        // registra internamente cualquier fallo de escritura/formateo.
        LogInternalError(loggingEx);
    }
}

/// <summary>
/// Registra información estructurada de una ejecución SQL fallida.
/// Incluye datos de conexión, sentencia y detalle de la excepción,
/// y persiste la salida en el archivo asociado al ciclo de la petición actual.
/// Opcionalmente, puede derivar un rastro de excepción general para análisis transversal.
/// </summary>
/// <param name="command">Comando que falló.</param>
/// <param name="ex">Excepción capturada.</param>
/// <param name="context">
/// Contexto HTTP actual (opcional). Cuando está presente, permite resolver y reutilizar
/// el archivo de log asociado al request/endpoint en curso.
/// </param>
public void LogDatabaseError(DbCommand command, Exception ex, HttpContext? context = null)
{
    try
    {
        // Extrae metadatos disponibles de la conexión para enriquecer el bloque.
        var connectionInfo = LogHelper.ExtractDbConnectionInfo(command.Connection?.ConnectionString);
        var tabla = LogHelper.ExtractTableName(command.CommandText);

        // Mantiene el mismo formato de error estructurado que ya utilizas.
        var formatted = LogFormatter.FormatDbExecutionError(
            nombreBD:   connectionInfo.Database,
            ip:         connectionInfo.Ip,
            puerto:     connectionInfo.Port,
            biblioteca: connectionInfo.Library,
            tabla:      tabla,
            sentenciaSQL: command.CommandText,
            exception:  ex,
            horaError:  DateTime.Now
        );

        // Escribe el bloque en el archivo activo del request/endpoint.
        WriteLog(context, formatted);

        // Además, conserva el rastro de excepción general si tu estrategia lo requiere
        // (por ejemplo, un canal paralelo de errores globales).
        AddExceptionLog(ex);
    }
    catch (Exception errorAlLoguear)
    {
        // Registro defensivo para fallos durante el propio proceso de logging.
        LogInternalError(errorAlLoguear);
    }
}
