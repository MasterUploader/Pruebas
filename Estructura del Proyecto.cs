using System.Data;
using System.Data.Common;
using Microsoft.AspNetCore.Http;
using RestUtilities.Logging.Services;
using RestUtilities.Logging.Utils;

namespace RestUtilities.Connections.Logging;

/// <summary>
/// Clase que actúa como decorador de <see cref="DbCommand"/> para registrar automáticamente la ejecución
/// de comandos SQL, incluyendo errores, duración, parámetros y metadatos del contexto HTTP si está disponible.
/// </summary>
public class LoggingDbCommand : DbCommand
{
    private readonly DbCommand _innerCommand;
    private readonly HttpContext? _context;
    private readonly ILoggingService _loggingService;

    /// <summary>
    /// Inicializa una nueva instancia del <see cref="LoggingDbCommand"/>.
    /// </summary>
    /// <param name="innerCommand">El comando original a ejecutar.</param>
    /// <param name="context">El contexto HTTP actual (opcional).</param>
    /// <param name="loggingService">Servicio de logging inyectado para registrar los logs.</param>
    public LoggingDbCommand(DbCommand innerCommand, HttpContext? context, ILoggingService loggingService)
    {
        _innerCommand = innerCommand ?? throw new ArgumentNullException(nameof(innerCommand));
        _context = context;
        _loggingService = loggingService ?? throw new ArgumentNullException(nameof(loggingService));
    }

    public override string CommandText
    {
        get => _innerCommand.CommandText;
        set => _innerCommand.CommandText = value;
    }

    public override int CommandTimeout
    {
        get => _innerCommand.CommandTimeout;
        set => _innerCommand.CommandTimeout = value;
    }

    public override CommandType CommandType
    {
        get => _innerCommand.CommandType;
        set => _innerCommand.CommandType = value;
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

    protected override DbTransaction? DbTransaction
    {
        get => _innerCommand.Transaction;
        set => _innerCommand.Transaction = value;
    }

    protected override DbParameterCollection DbParameterCollection => _innerCommand.Parameters;

    public override void Cancel() => _innerCommand.Cancel();

    public override void Prepare() => _innerCommand.Prepare();

    protected override DbParameter CreateDbParameter() => _innerCommand.CreateParameter();

    /// <summary>
    /// Ejecuta un lector de datos y registra el log de la ejecución.
    /// </summary>
    protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior)
    {
        var stopwatch = StopwatchHelper.Start();
        try
        {
            var result = _innerCommand.ExecuteReader(behavior);
            stopwatch.Stop();

            var log = LogFormatter.FormatDatabaseSuccess(_innerCommand, stopwatch.ElapsedMilliseconds);
            _loggingService.WriteLog(_context, log);
            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            var errorLog = LogFormatter.FormatDatabaseError(_innerCommand, ex, stopwatch.ElapsedMilliseconds);
            _loggingService.WriteLog(_context, errorLog);
            throw;
        }
    }

    /// <summary>
    /// Ejecuta un comando no query y registra el log de la ejecución.
    /// </summary>
    public override int ExecuteNonQuery()
    {
        var stopwatch = StopwatchHelper.Start();
        try
        {
            var result = _innerCommand.ExecuteNonQuery();
            stopwatch.Stop();

            var log = LogFormatter.FormatDatabaseSuccess(_innerCommand, stopwatch.ElapsedMilliseconds);
            _loggingService.WriteLog(_context, log);
            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            var errorLog = LogFormatter.FormatDatabaseError(_innerCommand, ex, stopwatch.ElapsedMilliseconds);
            _loggingService.WriteLog(_context, errorLog);
            throw;
        }
    }

    /// <summary>
    /// Ejecuta el comando y retorna la primera columna de la primera fila del resultado.
    /// </summary>
    public override object ExecuteScalar()
    {
        var stopwatch = StopwatchHelper.Start();
        try
        {
            var result = _innerCommand.ExecuteScalar();
            stopwatch.Stop();

            var log = LogFormatter.FormatDatabaseSuccess(_innerCommand, stopwatch.ElapsedMilliseconds);
            _loggingService.WriteLog(_context, log);
            return result!;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            var errorLog = LogFormatter.FormatDatabaseError(_innerCommand, ex, stopwatch.ElapsedMilliseconds);
            _loggingService.WriteLog(_context, errorLog);
            throw;
        }
    }

    /// <summary>
    /// Ejecuta de forma asincrónica un comando no query.
    /// </summary>
    public override async Task<int> ExecuteNonQueryAsync(CancellationToken cancellationToken)
    {
        var stopwatch = StopwatchHelper.Start();
        try
        {
            var result = await _innerCommand.ExecuteNonQueryAsync(cancellationToken);
            stopwatch.Stop();

            var log = LogFormatter.FormatDatabaseSuccess(_innerCommand, stopwatch.ElapsedMilliseconds);
            _loggingService.WriteLog(_context, log);
            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            var errorLog = LogFormatter.FormatDatabaseError(_innerCommand, ex, stopwatch.ElapsedMilliseconds);
            _loggingService.WriteLog(_context, errorLog);
            throw;
        }
    }

    /// <summary>
    /// Ejecuta de forma asincrónica el comando y retorna el primer valor.
    /// </summary>
    public override async Task<object> ExecuteScalarAsync(CancellationToken cancellationToken)
    {
        var stopwatch = StopwatchHelper.Start();
        try
        {
            var result = await _innerCommand.ExecuteScalarAsync(cancellationToken);
            stopwatch.Stop();

            var log = LogFormatter.FormatDatabaseSuccess(_innerCommand, stopwatch.ElapsedMilliseconds);
            _loggingService.WriteLog(_context, log);
            return result!;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            var errorLog = LogFormatter.FormatDatabaseError(_innerCommand, ex, stopwatch.ElapsedMilliseconds);
            _loggingService.WriteLog(_context, errorLog);
            throw;
        }
    }

    /// <summary>
    /// Ejecuta de forma asincrónica un lector de datos.
    /// </summary>
    protected override async Task<DbDataReader> ExecuteDbDataReaderAsync(CommandBehavior behavior, CancellationToken cancellationToken)
    {
        var stopwatch = StopwatchHelper.Start();
        try
        {
            var result = await _innerCommand.ExecuteReaderAsync(behavior, cancellationToken);
            stopwatch.Stop();

            var log = LogFormatter.FormatDatabaseSuccess(_innerCommand, stopwatch.ElapsedMilliseconds);
            _loggingService.WriteLog(_context, log);
            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            var errorLog = LogFormatter.FormatDatabaseError(_innerCommand, ex, stopwatch.ElapsedMilliseconds);
            _loggingService.WriteLog(_context, errorLog);
            throw;
        }
    }
}
