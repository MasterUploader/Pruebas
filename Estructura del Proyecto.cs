/// <summary>
/// Registra un log estructurado de éxito para una operación SQL usando un modelo preformateado.
/// </summary>
/// <param name="model">Modelo con los datos del comando SQL ejecutado.</param>
/// <param name="context">Contexto HTTP para trazabilidad opcional.</param>
void LogDatabaseSuccess(SqlLogModel model, HttpContext? context = null);


/// <inheritdoc />
public void LogDatabaseSuccess(SqlLogModel model, HttpContext? context = null)
{
    var traceId = context?.TraceIdentifier ?? Guid.NewGuid().ToString();

    // Usa el formateador que ya tienes para texto plano, si lo deseas
    var formatted = LogFormatter.FormatDbExecution(model);

    SaveStructuredLog(
        traceId: traceId,
        timestamp: model.StartTime,
        service: "Database",
        database: model.DatabaseName ?? "Desconocida",
        ip: model.Ip ?? "Desconocida",
        port: model.Port ?? 0,
        message: formatted
    );
}
