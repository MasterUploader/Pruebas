using Connections.Interfaces;
using Logging.Abstractions;
using Logging.Decorators;
using Microsoft.AspNetCore.Http;
using QueryBuilder.Models;
using System.Data.Common;
using System.Data.OleDb;

namespace Connections.Providers.Database;

/// <summary>
/// Proveedor de conexión a base de datos AS400 utilizando OleDb.
/// Esta implementación permite la ejecución de comandos SQL con o sin logging estructurado.
/// </summary>
public partial class AS400ConnectionProvider : IDatabaseConnection, IDisposable
{
    private readonly OleDbConnection _oleDbConnection;
    private readonly ILoggingService? _loggingService;
    private readonly IHttpContextAccessor? _httpContextAccessor;

    /// <summary>
    /// Inicializa una nueva instancia de <see cref="AS400ConnectionProvider"/>.
    /// </summary>
    /// <param name="connectionString">Cadena de conexión a AS400 en formato OleDb.</param>
    /// <param name="loggingService">Servicio de logging estructurado (opcional).</param>
    /// <param name="httpContextAccessor">Accessor del contexto HTTP (opcional).</param>
    public AS400ConnectionProvider(
        string connectionString,
        ILoggingService? loggingService = null,
        IHttpContextAccessor? httpContextAccessor = null)
    {
        _oleDbConnection = new OleDbConnection(connectionString);
        _loggingService = loggingService;
        _httpContextAccessor = httpContextAccessor;
    }

    /// <inheritdoc />
    public void Open()
    {
        if (_oleDbConnection.State != System.Data.ConnectionState.Open)
            _oleDbConnection.Open();
    }

    /// <inheritdoc />
    public void Close()
    {
        if (_oleDbConnection.State != System.Data.ConnectionState.Closed)
            _oleDbConnection.Close();
    }

    /// <inheritdoc />
    public bool IsConnected => _oleDbConnection?.State == System.Data.ConnectionState.Open;

    /// <inheritdoc />
    public DbCommand GetDbCommand(HttpContext? context = null)
    {
        var command = _oleDbConnection.CreateCommand();

        // Si el servicio de logging está disponible, devolvemos el comando decorado
        if (_loggingService != null)
        {
            return new LoggingDbCommandWrapper(command, _loggingService, _httpContextAccessor);
        }

        // En caso contrario, devolvemos el comando básico
        return command;
    }

    /// <inheritdoc />
    public DbCommand GetDbCommand(QueryResult queryResult, HttpContext context)
    {
        var command = GetDbCommand(context);
        command.CommandText = queryResult.Sql;
        return command;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _oleDbConnection?.Dispose();
    }
}



using Logging.Abstractions;
using Microsoft.AspNetCore.Http;
using System.Data.Common;

namespace Logging.Decorators;

/// <summary>
/// Decorador para <see cref="DbCommand"/> que agrega funcionalidad de logging estructurado.
/// Puede operar con o sin un <see cref="HttpContext"/>.
/// </summary>
public class LoggingDbCommandWrapper : DbCommand
{
    private readonly DbCommand _innerCommand;
    private readonly ILoggingService _loggingService;
    private readonly IHttpContextAccessor? _httpContextAccessor;

    /// <summary>
    /// Inicializa una nueva instancia de <see cref="LoggingDbCommandWrapper"/>.
    /// </summary>
    /// <param name="innerCommand">Comando interno a decorar.</param>
    /// <param name="loggingService">Servicio de logging estructurado.</param>
    /// <param name="httpContextAccessor">Accessor al contexto HTTP (opcional).</param>
    public LoggingDbCommandWrapper(
        DbCommand innerCommand,
        ILoggingService loggingService,
        IHttpContextAccessor? httpContextAccessor = null)
    {
        _innerCommand = innerCommand;
        _loggingService = loggingService;
        _httpContextAccessor = httpContextAccessor;
    }

    /// <inheritdoc />
    public override int ExecuteNonQuery()
    {
        try
        {
            var result = _innerCommand.ExecuteNonQuery();
            _loggingService.LogDatabaseSuccess(_innerCommand, _httpContextAccessor?.HttpContext, result);
            return result;
        }
        catch (Exception ex)
        {
            _loggingService.LogDatabaseError(_innerCommand, ex, _httpContextAccessor?.HttpContext);
            throw;
        }
    }

    // Otros métodos delegados directamente al comando interno ↓

    public override string CommandText { get => _innerCommand.CommandText; set => _innerCommand.CommandText = value; }
    public override int CommandTimeout { get => _innerCommand.CommandTimeout; set => _innerCommand.CommandTimeout = value; }
    public override System.Data.CommandType CommandType { get => _innerCommand.CommandType; set => _innerCommand.CommandType = value; }
    public override bool DesignTimeVisible { get => _innerCommand.DesignTimeVisible; set => _innerCommand.DesignTimeVisible = value; }
    public override UpdateRowSource UpdatedRowSource { get => _innerCommand.UpdatedRowSource; set => _innerCommand.UpdatedRowSource = value; }
    protected override DbConnection DbConnection { get => _innerCommand.Connection; set => _innerCommand.Connection = value; }
    protected override DbParameterCollection DbParameterCollection => _innerCommand.Parameters;
    protected override DbTransaction DbTransaction { get => _innerCommand.Transaction; set => _innerCommand.Transaction = value; }
    public override void Cancel() => _innerCommand.Cancel();
    protected override DbParameter CreateDbParameter() => _innerCommand.CreateParameter();
    protected override DbDataReader ExecuteDbDataReader(System.Data.CommandBehavior behavior) => _innerCommand.ExecuteReader(behavior);
    public override object ExecuteScalar() => _innerCommand.ExecuteScalar();
    public override void Prepare() => _innerCommand.Prepare();
}
