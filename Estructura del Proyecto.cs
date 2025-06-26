using Microsoft.AspNetCore.Http;
using System.Data.Common;
using System.Text;
using RestUtilities.Logging.Helpers;

public static partial class LogFormatter
{
    /// <summary>
    /// Formatea un log detallado de una ejecución de base de datos exitosa.
    /// Incluye motor, servidor, base de datos, comando SQL y parámetros.
    /// </summary>
    /// <param name="command">Comando ejecutado (DbCommand).</param>
    /// <param name="elapsedMs">Milisegundos que tomó la ejecución.</param>
    /// <param name="context">Contexto HTTP opcional para enlazar trazabilidad.</param>
    /// <param name="customMessage">Mensaje adicional que puede incluir el log.</param>
    /// <returns>Cadena formateada para log de éxito en base de datos.</returns>
    public static string FormatDatabaseSuccess(DbCommand command, long elapsedMs, HttpContext? context = null, string? customMessage = null)
    {
        var sb = new StringBuilder();

        sb.AppendLine("📘 [Base de Datos] Consulta ejecutada exitosamente:");
        sb.AppendLine($"→ Motor: {command.Connection?.GetType().Name ?? "Desconocido"}");
        sb.AppendLine($"→ Servidor: {command.Connection?.DataSource ?? "Desconocido"}");
        sb.AppendLine($"→ Base de Datos: {command.Connection?.Database ?? "Desconocido"}");
        sb.AppendLine($"→ Tipo de Comando: {command.CommandType}");
        sb.AppendLine($"→ Texto SQL: {command.CommandText}");

        if (command.Parameters.Count > 0)
        {
            sb.AppendLine("→ Parámetros:");
            foreach (DbParameter param in command.Parameters)
            {
                sb.AppendLine($"   • {param.ParameterName} = {param.Value} ({param.DbType})");
            }
        }

        sb.AppendLine($"→ Tiempo de ejecución: {elapsedMs} ms");

        if (!string.IsNullOrWhiteSpace(customMessage))
        {
            sb.AppendLine($"→ Mensaje adicional: {customMessage}");
        }

        if (context != null)
        {
            sb.AppendLine($"→ TraceId: {context.TraceIdentifier}");
        }

        return sb.ToString();
    }

    /// <summary>
    /// Formatea un log detallado de un error durante la ejecución de una consulta a base de datos.
    /// Incluye información del motor, SQL ejecutado y excepción.
    /// </summary>
    /// <param name="command">Comando que produjo el error.</param>
    /// <param name="exception">Excepción generada.</param>
    /// <param name="context">Contexto HTTP opcional.</param>
    /// <returns>Cadena formateada para log de error en base de datos.</returns>
    public static string FormatDatabaseError(DbCommand command, Exception exception, HttpContext? context = null)
    {
        var sb = new StringBuilder();

        sb.AppendLine("❌ [Base de Datos] Error en la ejecución de una consulta:");
        sb.AppendLine($"→ Motor: {command.Connection?.GetType().Name ?? "Desconocido"}");
        sb.AppendLine($"→ Servidor: {command.Connection?.DataSource ?? "Desconocido"}");
        sb.AppendLine($"→ Base de Datos: {command.Connection?.Database ?? "Desconocido"}");
        sb.AppendLine($"→ Tipo de Comando: {command.CommandType}");
        sb.AppendLine($"→ Texto SQL: {command.CommandText}");

        if (command.Parameters.Count > 0)
        {
            sb.AppendLine("→ Parámetros:");
            foreach (DbParameter param in command.Parameters)
            {
                sb.AppendLine($"   • {param.ParameterName} = {param.Value} ({param.DbType})");
            }
        }

        sb.AppendLine($"→ Excepción: {exception.GetType().Name} - {exception.Message}");

        if (context != null)
        {
            sb.AppendLine($"→ TraceId: {context.TraceIdentifier}");
        }

        return sb.ToString();
    }
}
