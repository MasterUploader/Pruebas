using Microsoft.AspNetCore.Http;
using System.Data.Common;

namespace RestUtilities.Logging;

/// <summary>
/// Contrato para la escritura estructurada de logs.
/// Soporta logs para controladores, excepciones, peticiones externas, y ahora también comandos de bases de datos.
/// </summary>
public interface ILoggingService
{
    // Métodos existentes...
    void WriteLog(HttpContext? context, string message);
    void AddExceptionLog(Exception ex);

    // 📌 NUEVOS MÉTODOS PARA LOG DE BASES DE DATOS

    /// <summary>
    /// Registra un log de éxito para un comando SQL ejecutado correctamente.
    /// </summary>
    /// <param name="command">El comando ejecutado (SELECT, INSERT, etc.).</param>
    /// <param name="elapsedMs">El tiempo de ejecución en milisegundos.</param>
    /// <param name="context">Contexto HTTP actual para extraer TraceId, usuario, etc. (opcional).</param>
    /// <param name="customMessage">Mensaje adicional personalizado (opcional).</param>
    void LogDatabaseSuccess(DbCommand command, long elapsedMs, HttpContext? context = null, string? customMessage = null);

    /// <summary>
    /// Registra un log de error para un comando SQL que lanzó una excepción.
    /// </summary>
    /// <param name="command">El comando ejecutado que causó el error.</param>
    /// <param name="ex">La excepción lanzada.</param>
    /// <param name="context">Contexto HTTP actual para extraer información adicional (opcional).</param>
    void LogDatabaseError(DbCommand command, Exception ex, HttpContext? context = null);
}
