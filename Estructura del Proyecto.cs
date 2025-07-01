using Connections.Interfaces;
using Connections.Logging;
using Logging.Abstractions;
using Microsoft.AspNetCore.Http;
using System.Data.Common;

namespace Connections.Managers;

/// <summary>
/// Clase base reutilizable para conexiones de base de datos que requieren logging automático.
/// Proporciona funcionalidad estándar para apertura, cierre y monitoreo de comandos SQL.
/// </summary>
public abstract class LoggingDatabaseConnection : IDatabaseConnection
{
    protected readonly ILoggingService _loggingService;
    protected DbConnection _connection;

    /// <summary>
    /// Inicializa una nueva instancia de la clase <see cref="LoggingDatabaseConnection"/>.
    /// </summary>
    /// <param name="connection">Instancia de conexión a base de datos subyacente.</param>
    /// <param name="loggingService">Servicio de logging estructurado.</param>
    protected LoggingDatabaseConnection(DbConnection connection, ILoggingService loggingService)
    {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
        _loggingService = loggingService ?? throw new ArgumentNullException(nameof(loggingService));
    }

    /// <inheritdoc />
    public void Open()
    {
        if (_connection.State != System.Data.ConnectionState.Open)
            _connection.Open();
    }

    /// <inheritdoc />
    public void Close()
    {
        if (_connection.State != System.Data.ConnectionState.Closed)
            _connection.Close();
    }

    /// <inheritdoc />
    public bool IsConnected => _connection?.State == System.Data.ConnectionState.Open;

    /// <inheritdoc />
    public DbCommand GetDbCommand(HttpContext? context = null)
    {
        var command = _connection.CreateCommand();
        return new LoggingDbCommand(command, context, _loggingService);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _connection?.Dispose();
    }
}
