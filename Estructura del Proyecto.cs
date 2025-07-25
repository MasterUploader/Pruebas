using Logging.Abstractions;
using Logging.Decorators;
using Microsoft.AspNetCore.Http;
using System.Data.Common;

namespace RestUtilities.Connections.Providers;

/// <summary>
/// Proveedor de conexión para bases de datos AS400.
/// Devuelve comandos decorados con logging solo si el servicio de logging está disponible.
/// </summary>
public class AS400ConnectionProvider : IDatabaseConnection
{
    private readonly DbConnection _connection;
    private readonly ILoggingService? _loggingService;
    private readonly IHttpContextAccessor? _httpContextAccessor;

    /// <summary>
    /// Inicializa una nueva instancia de <see cref="AS400ConnectionProvider"/>.
    /// </summary>
    /// <param name="connection">Conexión de base de datos (AS400).</param>
    /// <param name="loggingService">Servicio de logging estructurado (opcional).</param>
    /// <param name="httpContextAccessor">Accessor para el contexto HTTP (opcional).</param>
    public AS400ConnectionProvider(
        DbConnection connection,
        ILoggingService? loggingService = null,
        IHttpContextAccessor? httpContextAccessor = null)
    {
        _connection = connection;
        _loggingService = loggingService;
        _httpContextAccessor = httpContextAccessor;
    }

    /// <summary>
    /// Devuelve un comando listo para ejecutar sentencias SQL.
    /// Si el servicio de logging está disponible, se retorna un decorador con soporte de trazabilidad.
    /// </summary>
    /// <param name="context">Contexto HTTP actual, útil para trazabilidad del log.</param>
    /// <returns>Instancia de <see cref="DbCommand"/> (con o sin decorador).</returns>
    public DbCommand GetDbCommand(HttpContext context)
    {
        var command = _connection.CreateCommand();

        if (_loggingService != null)
        {
            return new LoggingDbCommandWrapper(command, _loggingService, _httpContextAccessor);
        }

        return command;
    }

    /// <summary>
    /// Abre la conexión si aún no está abierta.
    /// </summary>
    public void Open()
    {
        if (_connection.State != System.Data.ConnectionState.Open)
        {
            _connection.Open();
        }
    }

    /// <summary>
    /// Cierra la conexión si está abierta.
    /// </summary>
    public void Close()
    {
        if (_connection.State != System.Data.ConnectionState.Closed)
        {
            _connection.Close();
        }
    }

    /// <summary>
    /// Libera recursos de la conexión.
    /// </summary>
    public void Dispose()
    {
        _connection.Dispose();
    }
}
