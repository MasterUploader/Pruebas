using Connections.Managers;
using Logging.Abstractions;
using System.Data;
using System.Data.Common;
using System.Data.OleDb;
using System.Diagnostics;

namespace Connections.Providers.Database;

/// <summary>
/// Proveedor de conexión para AS400 usando únicamente OleDbCommand.
/// No utiliza DbContext ni Entity Framework.
/// </summary>
public class As400ConnectionProvider : LoggingDatabaseConnection
{
    private readonly OleDbConnection _oleDbConnection;
    private readonly ILoggingService _loggingService;

    /// <summary>
    /// Inicializa una nueva instancia de <see cref="As400ConnectionProvider"/>.
    /// </summary>
    public As400ConnectionProvider(string connectionString, ILoggingService loggingService)
        : base(new OleDbConnection(connectionString), loggingService)
    {
        _oleDbConnection = new OleDbConnection(connectionString);
        _loggingService = loggingService;
    }

    public new void Open()
    {
        if (_oleDbConnection.State != ConnectionState.Open)
            _oleDbConnection.Open();
    }

    public new void Close()
    {
        if (_oleDbConnection.State == ConnectionState.Open)
            _oleDbConnection.Close();
    }

    public new bool IsConnected()
    {
        return _oleDbConnection.State == ConnectionState.Open;
    }

    /// <summary>
    /// Retorna un comando OleDb sin ejecución.
    /// </summary>
    public DbCommand GetDbCommand()
    {
        Open();
        return _oleDbConnection.CreateCommand();
    }

    public new void Dispose()
    {
        base.Dispose();
        _oleDbConnection.Dispose();
    }

    /// <summary>
    /// Ejecuta un comando con ExecuteReader y registra el log.
    /// </summary>
    public DbDataReader ExecuteReaderWithLog(DbCommand command)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            var reader = command.ExecuteReader();
            stopwatch.Stop();
            _loggingService?.LogDatabaseSuccess(command, stopwatch.ElapsedMilliseconds);
            return reader;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _loggingService?.LogDatabaseError(command, ex);
            throw;
        }
    }

    /// <summary>
    /// Ejecuta un comando con ExecuteNonQuery y registra el log.
    /// </summary>
    public int ExecuteNonQueryWithLog(DbCommand command)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            int affectedRows = command.ExecuteNonQuery();
            stopwatch.Stop();
            _loggingService?.LogDatabaseSuccess(command, stopwatch.ElapsedMilliseconds, null, $"Filas afectadas: {affectedRows}");
            return affectedRows;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _loggingService?.LogDatabaseError(command, ex);
            throw;
        }
    }

    /// <summary>
    /// Ejecuta un comando con ExecuteScalar y registra el log.
    /// </summary>
    public object? ExecuteScalarWithLog(DbCommand command)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            var result = command.ExecuteScalar();
            stopwatch.Stop();
            _loggingService?.LogDatabaseSuccess(command, stopwatch.ElapsedMilliseconds, null, $"Resultado: {result}");
            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _loggingService?.LogDatabaseError(command, ex);
            throw;
        }
    }

    /// <summary>
    /// Ejecuta un comando asincrónicamente con ExecuteReaderAsync y registra el log.
    /// </summary>
    public async Task<DbDataReader> ExecuteReaderWithLogAsync(DbCommand command)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            var reader = await ((OleDbCommand)command).ExecuteReaderAsync();
            stopwatch.Stop();
            _loggingService?.LogDatabaseSuccess(command, stopwatch.ElapsedMilliseconds);
            return reader;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _loggingService?.LogDatabaseError(command, ex);
            throw;
        }
    }

    /// <summary>
    /// Ejecuta un comando asincrónicamente con ExecuteNonQueryAsync y registra el log.
    /// </summary>
    public async Task<int> ExecuteNonQueryWithLogAsync(DbCommand command)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            int affectedRows = await ((OleDbCommand)command).ExecuteNonQueryAsync();
            stopwatch.Stop();
            _loggingService?.LogDatabaseSuccess(command, stopwatch.ElapsedMilliseconds, null, $"Filas afectadas: {affectedRows}");
            return affectedRows;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _loggingService?.LogDatabaseError(command, ex);
            throw;
        }
    }

    /// <summary>
    /// Ejecuta un comando asincrónicamente con ExecuteScalarAsync y registra el log.
    /// </summary>
    public async Task<object?> ExecuteScalarWithLogAsync(DbCommand command)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            var result = await ((OleDbCommand)command).ExecuteScalarAsync();
            stopwatch.Stop();
            _loggingService?.LogDatabaseSuccess(command, stopwatch.ElapsedMilliseconds, null, $"Resultado: {result}");
            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _loggingService?.LogDatabaseError(command, ex);
            throw;
        }
    }
}
