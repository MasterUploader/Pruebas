using System.Data.Common;
using Microsoft.AspNetCore.Http;

namespace RestUtilities.Connections.Interfaces;

/// <summary>
/// Define las operaciones básicas para una conexión de base de datos compatible con múltiples motores.
/// </summary>
public interface IDatabaseConnection : IDisposable
{
    /// <summary>
    /// Abre la conexión a la base de datos.
    /// </summary>
    void Open();

    /// <summary>
    /// Cierra la conexión actual a la base de datos.
    /// </summary>
    void Close();

    /// <summary>
    /// Indica si la conexión está activa.
    /// </summary>
    bool IsConnected { get; }

    /// <summary>
    /// Crea un nuevo comando de base de datos, decorado con soporte de logging si está habilitado.
    /// </summary>
    /// <param name="context">Contexto HTTP opcional para registrar logs por petición.</param>
    /// <returns>Comando decorado con logging.</returns>
    DbCommand GetDbCommand(HttpContext? context = null);
}



using System;
using System.Data.Common;
using Microsoft.AspNetCore.Http;
using RestUtilities.Connections.Interfaces;
using RestUtilities.Logging.Abstractions;
using RestUtilities.Logging.Database;

namespace RestUtilities.Connections.Managers.Base;

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
