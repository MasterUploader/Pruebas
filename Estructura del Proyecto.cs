using Logging.Abstractions;
using Microsoft.AspNetCore.Http;
using System.Data.Common;
using System.Diagnostics;

namespace RestUtilities.Connections.Logging;

/// <summary>
/// Decorador para interceptar y registrar información sobre comandos SQL ejecutados por un proveedor de conexión.
/// Captura datos como texto del comando, parámetros, tiempo de ejecución y errores.
/// </summary>
public class LoggingDbCommand : DbCommand
{
    private readonly DbCommand _innerCommand;
    private readonly HttpContext? _context;
    private readonly ILoggingService? _loggingService;

    /// <summary>
    /// Inicializa una nueva instancia de <see cref="LoggingDbCommand"/>.
    /// </summary>
    /// <param name="innerCommand">El comando original a ser decorado.</param>
    /// <param name="context">El contexto HTTP actual (si está disponible).</param>
    /// <param name="loggingService">Servicio de logging utilizado para guardar la información.</param>
    public LoggingDbCommand(DbCommand innerCommand, HttpContext? context, ILoggingService? loggingService)
    {
        _innerCommand = innerCommand ?? throw new ArgumentNullException(nameof(innerCommand));
        _context = context;
        _loggingService = loggingService;
    }

    public override string CommandText
    {
        get => _innerCommand.CommandText;
        set => _innerCommand.CommandText = value;
    }

    public override int ExecuteNonQuery()
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            var result = _innerCommand.ExecuteNonQuery();
            stopwatch.Stop();
            LogSuccess(stopwatch.ElapsedMilliseconds);
            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            LogError(ex, stopwatch.ElapsedMilliseconds);
            throw;
        }
    }

    public override async Task<int> ExecuteNonQueryAsync(CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            var result = await _innerCommand.ExecuteNonQueryAsync(cancellationToken);
            stopwatch.Stop();
            LogSuccess(stopwatch.ElapsedMilliseconds);
            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            LogError(ex, stopwatch.ElapsedMilliseconds);
            throw;
        }
    }

    public override object ExecuteScalar()
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            var result = _innerCommand.ExecuteScalar();
            stopwatch.Stop();
            LogSuccess(stopwatch.ElapsedMilliseconds);
            return result!;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            LogError(ex, stopwatch.ElapsedMilliseconds);
            throw;
        }
    }

    public override async Task<object?> ExecuteScalarAsync(CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            var result = await _innerCommand.ExecuteScalarAsync(cancellationToken);
            stopwatch.Stop();
            LogSuccess(stopwatch.ElapsedMilliseconds);
            return result!;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            LogError(ex, stopwatch.ElapsedMilliseconds);
            throw;
        }
    }

    public override DbDataReader ExecuteReader()
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            var result = _innerCommand.ExecuteReader();
            stopwatch.Stop();
            LogSuccess(stopwatch.ElapsedMilliseconds);
            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            LogError(ex, stopwatch.ElapsedMilliseconds);
            throw;
        }
    }

    public override async Task<DbDataReader> ExecuteReaderAsync(CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            var result = await _innerCommand.ExecuteReaderAsync(cancellationToken);
            stopwatch.Stop();
            LogSuccess(stopwatch.ElapsedMilliseconds);
            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            LogError(ex, stopwatch.ElapsedMilliseconds);
            throw;
        }
    }

    private void LogSuccess(long elapsedMilliseconds)
    {
        if (_loggingService == null) return;

        var message = LogFormatter.FormatDatabaseRequest(
            _innerCommand, elapsedMilliseconds, null
        );

        _loggingService.WriteLog(_context, message);
    }

    private void LogError(Exception ex, long elapsedMilliseconds)
    {
        if (_loggingService == null) return;

        var message = LogFormatter.FormatDatabaseRequest(
            _innerCommand, elapsedMilliseconds, ex
        );

        _loggingService.WriteLog(_context, message);
        _loggingService.AddExceptionLog(ex);
    }

    // Reenvíos de otras propiedades y métodos a _innerCommand
    public override string CommandType => _innerCommand.CommandType.ToString();
    public override int CommandTimeout
    {
        get => _innerCommand.CommandTimeout;
        set => _innerCommand.CommandTimeout = value;
    }
    public override bool DesignTimeVisible
    {
        get => _innerCommand.DesignTimeVisible;
        set => _innerCommand.DesignTimeVisible = value;
    }
    public override UpdateRowSource UpdatedRowSource
    {
        get => _innerCommand.UpdatedRowSource;
        set => _innerCommand.UpdatedRowSource = value;
    }
    protected override DbConnection DbConnection
    {
        get => _innerCommand.Connection!;
        set => _innerCommand.Connection = value;
    }
    protected override DbTransaction DbTransaction
    {
        get => _innerCommand.Transaction!;
        set => _innerCommand.Transaction = value;
    }
    protected override DbParameterCollection DbParameterCollection => _innerCommand.Parameters;

    public override void Cancel() => _innerCommand.Cancel();
    public override void Prepare() => _innerCommand.Prepare();

    protected override DbParameter CreateDbParameter() => _innerCommand.CreateParameter();
    protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior) =>
        _innerCommand.ExecuteReader(behavior);

    public override Task<DbDataReader> ExecuteReaderAsync(CommandBehavior behavior, CancellationToken cancellationToken) =>
        _innerCommand.ExecuteReaderAsync(behavior, cancellationToken);
}
