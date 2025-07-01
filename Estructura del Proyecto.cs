using Connections.Interfaces;
using Logging.Abstractions;
using Microsoft.AspNetCore.Http;
using System.Data.Common;
using System.Data.OleDb;

namespace Connections.Providers.Database;

/// <summary>
/// Proveedor de conexión a base de datos AS400 utilizando OleDb.
/// Esta implementación está optimizada para ejecución directa de comandos SQL y logging estructurado.
/// </summary>
public partial class AS400ConnectionProvider : IDatabaseConnection
{
    private readonly OleDbConnection _oleDbConnection;
    private readonly ILoggingService _loggingService;

    /// <summary>
    /// Inicializa una nueva instancia de <see cref="AS400ConnectionProvider"/>.
    /// </summary>
    /// <param name="connectionString">Cadena de conexión a AS400 en formato OleDb.</param>
    /// <param name="loggingService">Servicio de logging estructurado.</param>
    public AS400ConnectionProvider(string connectionString, ILoggingService loggingService)
    {
        _oleDbConnection = new OleDbConnection(connectionString);
        _loggingService = loggingService;
    }

    /// <summary>
    /// Abre la conexión si no está ya abierta.
    /// </summary>
    public void Open()
    {
        if (_oleDbConnection.State != System.Data.ConnectionState.Open)
            _oleDbConnection.Open();
    }

    /// <summary>
    /// Cierra la conexión si está abierta.
    /// </summary>
    public void Close()
    {
        if (_oleDbConnection.State != System.Data.ConnectionState.Closed)
            _oleDbConnection.Close();
    }

    /// <summary>
    /// Indica si la conexión está actualmente abierta.
    /// </summary>
    public bool IsConnected => _oleDbConnection?.State == System.Data.ConnectionState.Open;

    /// <summary>
    /// Obtiene un <see cref="DbCommand"/> decorado con soporte de logging estructurado.
    /// </summary>
    /// <param name="context">Contexto HTTP opcional para trazabilidad adicional.</param>
    /// <returns>Comando decorado con logging.</returns>
    public DbCommand GetDbCommand(HttpContext? context = null)
    {
        var command = _oleDbConnection.CreateCommand();
        return new LoggingDbCommandWrapper(command, _loggingService, context); // ← El decorador correcto
    }

    /// <summary>
    /// Libera los recursos de la conexión.
    /// </summary>
    public void Dispose()
    {
        _oleDbConnection?.Dispose();
    }
}
