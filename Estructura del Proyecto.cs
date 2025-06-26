using Microsoft.AspNetCore.Http;
using RestUtilities.Logging.Abstractions;
using RestUtilities.Logging.Helpers;
using System;
using System.Data.Common;
using System.Diagnostics;

namespace RestUtilities.Queries.Helpers;

/// <summary>
/// Clase auxiliar para loguear ejecuciones de comandos SQL en múltiples motores.
/// Permite registrar logs de éxito y error reutilizando la lógica de LoggingMiddleware.
/// </summary>
public static class QueryExecutionLogger
{
    /// <summary>
    /// Ejecuta una acción sobre un comando SQL y registra automáticamente el log (éxito o error).
    /// </summary>
    /// <param name="command">Comando SQL a ejecutar.</param>
    /// <param name="action">Acción sin retorno que ejecuta la consulta (por ejemplo ExecuteNonQuery).</param>
    /// <param name="loggingService">Instancia del servicio de logging inyectado.</param>
    /// <param name="context">Contexto HTTP para trazabilidad (opcional).</param>
    /// <param name="customMessage">Mensaje adicional para el log (opcional).</param>
    public static void ExecuteAndLog(
        DbCommand command,
        Action<DbCommand> action,
        ILoggingService loggingService,
        HttpContext? context = null,
        string? customMessage = null)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            action(command);
            stopwatch.Stop();

            string log = LogFormatter.FormatDatabaseSuccess(command, stopwatch.ElapsedMilliseconds, context, customMessage);
            loggingService.WriteLog(context, log);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            string log = LogFormatter.FormatDatabaseError(command, ex, context);
            loggingService.AddExceptionLog(ex);
            loggingService.WriteLog(context, log);

            throw; // Re-lanza la excepción para no alterar el comportamiento original
        }
    }

    /// <summary>
    /// Ejecuta una función con retorno sobre un comando SQL y registra automáticamente el log (éxito o error).
    /// </summary>
    /// <typeparam name="T">Tipo del valor retornado.</typeparam>
    /// <param name="command">Comando SQL a ejecutar.</param>
    /// <param name="func">Función que ejecuta la consulta (por ejemplo ExecuteScalar o ExecuteReader).</param>
    /// <param name="loggingService">Instancia del servicio de logging inyectado.</param>
    /// <param name="context">Contexto HTTP para trazabilidad (opcional).</param>
    /// <param name="customMessage">Mensaje adicional para el log (opcional).</param>
    /// <returns>Resultado de la función ejecutada.</returns>
    public static T ExecuteAndLog<T>(
        DbCommand command,
        Func<DbCommand, T> func,
        ILoggingService loggingService,
        HttpContext? context = null,
        string? customMessage = null)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            T result = func(command);
            stopwatch.Stop();

            string log = LogFormatter.FormatDatabaseSuccess(command, stopwatch.ElapsedMilliseconds, context, customMessage);
            loggingService.WriteLog(context, log);

            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            string log = LogFormatter.FormatDatabaseError(command, ex, context);
            loggingService.AddExceptionLog(ex);
            loggingService.WriteLog(context, log);

            throw;
        }
    }
}
