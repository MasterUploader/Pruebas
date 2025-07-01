using Connections.Interfaces;
using Logging.Abstractions;
using Microsoft.AspNetCore.Http;
using System.Data.Common;

namespace Connections.Managers;

/// <summary>
/// Clase base reutilizable para conexiones de base de datos con soporte de logging estructurado.
/// Encapsula la lógica común para abrir, cerrar y obtener comandos SQL instrumentados con logs.
/// </summary>
public abstract class LoggingDatabaseConnection : IDatabaseConnection
{
    /// <summary>
    /// Instancia de la conexión subyacente a la base de datos.
    /// </summary>
    protected readonly DbConnection _connection;

    /// <summary>
    /// Servicio de logging utilizado para registrar los comandos SQL y sus métricas.
    /// </summary>
    protected readonly ILoggingService _loggingService;

    /// <summary>
    /// Inicializa una nueva instancia de <see cref="LoggingDatabaseConnection"/>.
    /// </summary>
    /// <param name="connection">Conexión de base de datos concreta (por ejemplo, OleDbConnection).</param>
    /// <param name="loggingService">Servicio de logging estructurado a utilizar.</param>
    protected LoggingDatabaseConnection(DbConnection connection, ILoggingService loggingService)
    {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
        _loggingService = loggingService ?? throw new ArgumentNullException(nameof(loggingService));
    }

    /// <summary>
    /// Abre la conexión si aún no está abierta.
    /// </summary>
    public void Open()
    {
        if (_connection.State != System.Data.ConnectionState.Open)
            _connection.Open();
    }

    /// <summary>
    /// Cierra la conexión si aún no está cerrada.
    /// </summary>
    public void Close()
    {
        if (_connection.State != System.Data.ConnectionState.Closed)
            _connection.Close();
    }

    /// <summary>
    /// Indica si la conexión está actualmente abierta.
    /// </summary>
    public bool IsConnected => _connection?.State == System.Data.ConnectionState.Open;

    /// <summary>
    /// Obtiene un comando de base de datos decorado con soporte de logging estructurado.
    /// </summary>
    /// <param name="context">Contexto HTTP opcional (puede usarse para incluir información de trazabilidad).</param>
    /// <returns>Instancia de <see cref="DbCommand"/> decorada.</returns>
    public DbCommand GetDbCommand(HttpContext? context = null)
    {
        var command = _connection.CreateCommand();
        return new LoggingDbCommandWrapper(command, _loggingService);
    }

    /// <summary>
    /// Libera los recursos utilizados por la conexión.
    /// </summary>
    public void Dispose()
    {
        _connection?.Dispose();
    }
}
