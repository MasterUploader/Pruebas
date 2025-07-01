using Connections.Interfaces;
using Microsoft.AspNetCore.Http;
using System.Data.Common;
using Logging.Abstractions;
using Connections.Logging;

namespace Logging.Decorators;

/// <summary>
/// Decorador para <see cref="IDatabaseConnection"/> que añade soporte de logging estructurado
/// para las ejecuciones SQL, sin modificar la implementación interna de conexión.
/// </summary>
public class LoggingDatabaseConnectionDecorator : IDatabaseConnection
{
    private readonly IDatabaseConnection _innerConnection;
    private readonly IHttpContextAccessor _contextAccessor;
    private readonly ILoggingService _logger;

    /// <summary>
    /// Inicializa una nueva instancia del decorador con soporte de logging.
    /// </summary>
    /// <param name="innerConnection">Conexión real que será envuelta.</param>
    /// <param name="contextAccessor">Acceso al contexto HTTP para trazabilidad.</param>
    /// <param name="logger">Servicio de logging estructurado.</param>
    public LoggingDatabaseConnectionDecorator(
        IDatabaseConnection innerConnection,
        IHttpContextAccessor contextAccessor,
        ILoggingService logger)
    {
        _innerConnection = innerConnection;
        _contextAccessor = contextAccessor;
        _logger = logger;
    }

    /// <summary>
    /// Abre la conexión real.
    /// </summary>
    public void Open() => _innerConnection.Open();

    /// <summary>
    /// Cierra la conexión real.
    /// </summary>
    public void Close() => _innerConnection.Close();

    /// <summary>
    /// Verifica si la conexión está activa.
    /// </summary>
    /// <returns>True si está conectada; False en caso contrario.</returns>
    public bool IsConnected() => _innerConnection.IsConnected();

    /// <summary>
    /// Libera los recursos de la conexión decorada.
    /// </summary>
    public void Dispose() => _innerConnection.Dispose();

    /// <summary>
    /// Retorna un <see cref="DbCommand"/> decorado que registra logs estructurados
    /// al momento de ejecutar comandos SQL.
    /// </summary>
    /// <param name="context">Contexto HTTP actual.</param>
    /// <returns>Instancia decorada de <see cref="DbCommand"/> con logging.</returns>
    public DbCommand GetDbCommand(HttpContext context)
    {
        var originalCommand = _innerConnection.GetDbCommand(context);

        // ✅ Usamos el decorador correcto que invoca LogDatabaseSuccess → FormatDbExecution
        return new LoggingDbCommandWrapper(originalCommand, _logger);
    }
}
