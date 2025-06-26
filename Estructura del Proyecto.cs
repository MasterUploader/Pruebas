using System.Data.Common;
using Microsoft.AspNetCore.Http;

namespace RestUtilities.Logging.Database;

/// <summary>
/// Interfaz para registrar logs de conexiones a bases de datos internas (AS400, Oracle, SQL, etc.).
/// Permite registrar información útil de ejecución, tiempo, errores y contexto.
/// </summary>
public interface IDatabaseLogger
{
    /// <summary>
    /// Registra la ejecución de un comando SQL, incluyendo el tiempo y el resultado.
    /// </summary>
    /// <param name="command">Instancia del comando ejecutado.</param>
    /// <param name="elapsedMilliseconds">Tiempo total de ejecución en milisegundos.</param>
    /// <param name="context">Contexto HTTP si está disponible (para capturar TraceId, headers, etc.).</param>
    /// <param name="customMessage">Mensaje adicional opcional a incluir en el log.</param>
    void LogCommandExecuted(DbCommand command, long elapsedMilliseconds, HttpContext? context = null, string? customMessage = null);

    /// <summary>
    /// Registra un error ocurrido durante la ejecución de un comando SQL.
    /// </summary>
    /// <param name="command">Comando SQL que provocó el error.</param>
    /// <param name="exception">Excepción lanzada.</param>
    /// <param name="context">Contexto HTTP si está disponible.</param>
    void LogCommandError(DbCommand command, Exception exception, HttpContext? context = null);
}



using Microsoft.AspNetCore.Http;
using RestUtilities.Logging.Database;
using RestUtilities.Logging.Helpers;
using System.Data.Common;
using System.Text;

namespace RestUtilities.Logging.Services;

/// <summary>
/// Implementación de <see cref="IDatabaseLogger"/> para registrar logs de comandos SQL en servicios internos como AS400, Oracle, SQL Server, etc.
/// </summary>
public class DatabaseLogger : IDatabaseLogger
{
    /// <summary>
    /// Instancia de servicio de log interno reutilizable.
    /// </summary>
    private readonly ILoggingService _loggingService;

    /// <summary>
    /// Constructor con inyección del servicio de logging.
    /// </summary>
    public DatabaseLogger(ILoggingService loggingService)
    {
        _loggingService = loggingService ?? throw new ArgumentNullException(nameof(loggingService));
    }

    /// <inheritdoc />
    public void LogCommandExecuted(DbCommand command, long elapsedMilliseconds, HttpContext? context = null, string? customMessage = null)
    {
        var sb = new StringBuilder();
        var traceId = context?.TraceIdentifier ?? Guid.NewGuid().ToString();

        sb.AppendLine("🗃️ [LOG DE CONSULTA A BASE DE DATOS]");
        sb.AppendLine($"🔁 TraceId: {traceId}");
        sb.AppendLine($"📡 Tipo de Servicio: {GetDatabaseType(command.Connection?.GetType()?.Name)}");
        sb.AppendLine($"🖥️ Servidor: {command.Connection?.DataSource ?? "Desconocido"}");
        sb.AppendLine($"🗂️ Base de Datos: {command.Connection?.Database ?? "N/A"}");
        sb.AppendLine($"⏱ Tiempo de ejecución: {elapsedMilliseconds} ms");

        if (!string.IsNullOrWhiteSpace(customMessage))
            sb.AppendLine($"📝 Información adicional: {customMessage}");

        sb.AppendLine($"📄 Comando SQL:");
        sb.AppendLine(command.CommandText.Trim());

        if (command.Parameters.Count > 0)
        {
            sb.AppendLine("📦 Parámetros:");
            foreach (DbParameter param in command.Parameters)
            {
                sb.AppendLine($"   • {param.ParameterName} = {param.Value} ({param.DbType})");
            }
        }

        _loggingService.WriteLog(context, sb.ToString());
    }

    /// <inheritdoc />
    public void LogCommandError(DbCommand command, Exception exception, HttpContext? context = null)
    {
        var sb = new StringBuilder();
        var traceId = context?.TraceIdentifier ?? Guid.NewGuid().ToString();

        sb.AppendLine("❌ [ERROR EN CONSULTA A BASE DE DATOS]");
        sb.AppendLine($"🔁 TraceId: {traceId}");
        sb.AppendLine($"📡 Tipo de Servicio: {GetDatabaseType(command.Connection?.GetType()?.Name)}");
        sb.AppendLine($"🖥️ Servidor: {command.Connection?.DataSource ?? "Desconocido"}");
        sb.AppendLine($"🗂️ Base de Datos: {command.Connection?.Database ?? "N/A"}");
        sb.AppendLine($"📄 Comando SQL fallido:");
        sb.AppendLine(command.CommandText.Trim());

        if (command.Parameters.Count > 0)
        {
            sb.AppendLine("📦 Parámetros:");
            foreach (DbParameter param in command.Parameters)
            {
                sb.AppendLine($"   • {param.ParameterName} = {param.Value} ({param.DbType})");
            }
        }

        sb.AppendLine($"🛑 Excepción: {exception.Message}");
        sb.AppendLine($"🔍 StackTrace: {exception.StackTrace}");

        _loggingService.WriteLog(context, sb.ToString());
    }

    /// <summary>
    /// Detecta el tipo de conexión (AS400, Oracle, etc.) a partir del nombre de clase de conexión.
    /// </summary>
    private static string GetDatabaseType(string? connectionTypeName)
    {
        if (string.IsNullOrWhiteSpace(connectionTypeName))
            return "Desconocido";

        return connectionTypeName.ToLowerInvariant() switch
        {
            var name when name.Contains("oledb") => "AS400 (OleDb)",
            var name when name.Contains("oracle") => "Oracle",
            var name when name.Contains("sql") => "SQL Server",
            var name when name.Contains("npgsql") => "PostgreSQL",
            var name when name.Contains("mysql") => "MySQL",
            _ => connectionTypeName
        };
    }
}
