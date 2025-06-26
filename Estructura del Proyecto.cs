using Microsoft.AspNetCore.Http;
using System.Data.Common;
using RestUtilities.Logging.Helpers;
using RestUtilities.Logging.Formatters;

public partial class LoggingService : ILoggingService
{
    // ✅ Método para registrar comandos SQL exitosos
    public void LogDatabaseSuccess(DbCommand command, long elapsedMs, HttpContext? context = null, string? customMessage = null)
    {
        // Formatear log con todos los detalles del comando y duración
        var message = LogFormatter.FormatDatabaseSuccess(
            command: command,
            elapsedMs: elapsedMs,
            context: context,
            customMessage: customMessage
        );

        // Guardar en el archivo general de logs (sin crear uno independiente)
        WriteLog(context, message);
    }

    // ✅ Método para registrar comandos SQL fallidos
    public void LogDatabaseError(DbCommand command, Exception ex, HttpContext? context = null)
    {
        // Formatear log del error con información detallada
        var message = LogFormatter.FormatDatabaseError(
            command: command,
            exception: ex,
            context: context
        );

        // Guardar el error en el mismo archivo de log principal
        WriteLog(context, message);

        // Registrar excepción para visibilidad en SingleLog (si se desea mantener consistencia con errores críticos)
        AddExceptionLog(ex);
    }
}
