using Microsoft.AspNetCore.Http;
using RestUtilities.Connections.Interfaces;
using RestUtilities.Logging.Execution;
using System.Data.Common;

namespace RestUtilities.Logging.Decorators;

/// <summary>
/// Decorador que intercepta las llamadas a la conexión de base de datos para registrar automáticamente los comandos ejecutados.
/// </summary>
public class LoggingDatabaseConnectionDecorator : IDatabaseConnection
{
    private readonly IDatabaseConnection _innerConnection;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly QueryExecutionLogger _queryLogger;

    /// <summary>
    /// Constructor del decorador que recibe la conexión original, el contexto HTTP y el logger especializado.
    /// </summary>
    public LoggingDatabaseConnectionDecorator(
        IDatabaseConnection innerConnection,
        IHttpContextAccessor httpContextAccessor,
        QueryExecutionLogger queryLogger)
    {
        _innerConnection = innerConnection;
        _httpContextAccessor = httpContextAccessor;
        _queryLogger = queryLogger;
    }

    /// <inheritdoc />
    public void Open() => _innerConnection.Open();

    /// <inheritdoc />
    public void Close() => _innerConnection.Close();

    /// <inheritdoc />
    public DbCommand GetDbCommand()
    {
        var originalCommand = _innerConnection.GetDbCommand();

        // Envolver el DbCommand con uno que loguee automáticamente al ejecutarse
        return new LoggingDbCommand(originalCommand, _queryLogger, _httpContextAccessor.HttpContext);
    }

    /// <inheritdoc />
    public DbConnection? GetDbConnection() => _innerConnection.GetDbConnection();

    /// <inheritdoc />
    public bool IsConnected() => _innerConnection.IsConnected();

    /// <inheritdoc />
    public void Dispose() => _innerConnection.Dispose();
}
