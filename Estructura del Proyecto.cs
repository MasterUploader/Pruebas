using Connections.Managers;
using Logging.Abstractions;
using System.Data;
using System.Data.Common;
using System.Data.OleDb;

namespace Connections.Providers.Database;

/// <summary>
/// Proveedor de conexión para AS400 usando únicamente OleDbCommand.
/// No utiliza DbContext ni Entity Framework.
/// </summary>
public class AS400ConnectionProvider : LoggingDatabaseConnection
{
    private readonly string _connectionString;
    private OleDbConnection _oleDbConnection;

    /// <summary>
    /// Inicializa una nueva instancia de <see cref="As400ConnectionProvider"/>.
    /// </summary>
    /// <param name="connectionString">Cadena de conexión a AS400.</param>
    /// <param name="loggingService">Servicio de logging para registrar consultas.</param>
    public As400ConnectionProvider(string connectionString, ILoggingService loggingService)
        : base(new OleDbConnection(connectionString), loggingService)
    {
    }

    /// <summary>
    /// Abre la conexión OleDb si aún no está abierta.
    /// </summary>
    public void Open()
    {
        if (_oleDbConnection == null)
            _oleDbConnection = new OleDbConnection(_connectionString);

        if (_oleDbConnection.State != ConnectionState.Open)
            _oleDbConnection.Open();
    }

    /// <summary>
    /// Cierra y limpia la conexión si está activa.
    /// </summary>
    public void Close()
    {
        if (_oleDbConnection?.State == ConnectionState.Open)
            _oleDbConnection.Close();
    }

    /// <summary>
    /// Verifica si la conexión está actualmente abierta y operativa.
    /// </summary>
    public bool IsConnected()
    {
        return _oleDbConnection?.State == ConnectionState.Open;
    }

    /// <summary>
    /// Retorna un OleDbCommand para ejecutar SQL directamente en AS400.
    /// </summary>
    public DbCommand GetDbCommand()
    {
        if (_oleDbConnection == null)
            _oleDbConnection = new OleDbConnection(_connectionString);

        if (_oleDbConnection.State != ConnectionState.Open)
            _oleDbConnection.Open();

        return _oleDbConnection.CreateCommand();
    }

    /// <summary>
    /// Libera la conexión OleDb.
    /// </summary>
    public void Dispose()
    {
        Close();
        _oleDbConnection?.Dispose();
    }
}
