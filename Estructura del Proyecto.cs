using Logging.Abstractions;
using Logging.Models;
using Microsoft.AspNetCore.Http;
using System.Data;
using System.Data.Common;
using System.Diagnostics;

namespace Logging.Decorators;

/// <summary>
/// Decorador para interceptar y registrar automáticamente logs SQL al ejecutar comandos de base de datos.
/// Guarda el log acumulado al hacer <see cref="Dispose"/>.
/// Funciona también si no se proporciona un servicio de logging.
/// </summary>
public class LoggingDbCommandWrapper : DbCommand
{
    private readonly DbCommand _innerCommand;
    private readonly ILoggingService? _loggingService;
    private readonly IHttpContextAccessor? _httpContextAccessor;
    private readonly Stopwatch _stopwatch = new();
    private readonly object _lock = new();

    private int _executionCount = 0;
    private int _totalAffectedRows = 0;
    private DateTime _startTime;
    private string _commandText = string.Empty;
    private bool _isFinalized = false;

    /// <summary>
    /// Inicializa una nueva instancia del decorador <see cref="LoggingDbCommandWrapper"/>.
    /// </summary>
    /// <param name="innerCommand">Comando original a decorar.</param>
    /// <param name="loggingService">Servicio de logging estructurado (opcional).</param>
    /// <param name="httpContextAccessor">Accessor del contexto HTTP (opcional).</param>
    public LoggingDbCommandWrapper(
        DbCommand innerCommand,
        ILoggingService? loggingService = null,
        IHttpContextAccessor? httpContextAccessor = null)
    {
        _innerCommand = innerCommand;
        _loggingService = loggingService;
        _httpContextAccessor = httpContextAccessor;
    }

    /// <inheritdoc />
    protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior)
    {
        StartIfNeeded();

        try
        {
            var reader = _innerCommand.ExecuteReader(behavior);
            return reader;
        }
        catch (Exception ex)
        {
            _loggingService?.LogDatabaseError(_innerCommand, ex, _httpContextAccessor?.HttpContext);
            throw;
        }
    }

    /// <inheritdoc />
    public override int ExecuteNonQuery()
    {
        StartIfNeeded();
        var result = _innerCommand.ExecuteNonQuery();
        RegisterExecution(result);
        return result;
    }

    /// <inheritdoc />
    public override object? ExecuteScalar()
    {
        StartIfNeeded();
        var result = _innerCommand.ExecuteScalar();
        RegisterExecution(result != null ? 1 : 0);
        return result;
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        _innerCommand.Dispose();
        FinalizeAndLog();
    }

    /// <summary>
    /// Consolida y guarda el log estructurado si no ha sido registrado aún.
    /// No lanza excepciones si la conexión es nula o si el logging no está habilitado.
    /// </summary>
    private void FinalizeAndLog()
    {
        lock (_lock)
        {
            if (_isFinalized || _executionCount == 0 || _loggingService == null)
                return;

            _stopwatch.Stop();
            _isFinalized = true;

            try
            {
                var connection = _innerCommand.Connection;

                var log = new SqlLogModel
                {
                    Sql = _commandText,
                    ExecutionCount = _executionCount,
                    TotalAffectedRows = _totalAffectedRows,
                    StartTime = _startTime,
                    Duration = _stopwatch.Elapsed,
                    DatabaseName = connection?.Database ?? "Desconocida",
                    Ip = connection?.DataSource ?? "Desconocida",
                    Port = 0,
                    TableName = ExtraerNombreTablaDesdeSql(_commandText),
                    Schema = ExtraerEsquemaDesdeSql(_commandText)
                };

                _loggingService.LogDatabaseSuccess(log, _httpContextAccessor?.HttpContext);
            }
            catch (Exception ex)
            {
                _loggingService.LogDatabaseError(_innerCommand, ex, _httpContextAccessor?.HttpContext);
            }
        }
    }

    /// <summary>
    /// Marca el inicio de la ejecución para calcular duración y registrar texto SQL.
    /// </summary>
    private void StartIfNeeded()
    {
        lock (_lock)
        {
            if (_executionCount == 0)
            {
                _startTime = DateTime.Now;
                _commandText = _innerCommand.CommandText;
                _stopwatch.Restart();
            }
        }
    }

    /// <summary>
    /// Registra estadísticas internas de ejecución para logging.
    /// </summary>
    /// <param name="affectedRows">Cantidad de filas afectadas.</param>
    private void RegisterExecution(int affectedRows)
    {
        lock (_lock)
        {
            _executionCount++;
            if (affectedRows > 0)
                _totalAffectedRows += affectedRows;
        }
    }

    /// <summary>
    /// Extrae el nombre de la tabla desde una sentencia SQL.
    /// </summary>
    private static string ExtraerNombreTablaDesdeSql(string sql)
    {
        try
        {
            var tokens = sql.ToLower().Split(' ');
            var index = Array.FindIndex(tokens, t => t == "into" || t == "from" || t == "update");
            return index >= 0 && tokens.Length > index + 1 ? tokens[index + 1] : "Desconocida";
        }
        catch { return "Desconocida"; }
    }

    /// <summary>
    /// Extrae el esquema (library) desde una sentencia SQL.
    /// </summary>
    private static string ExtraerEsquemaDesdeSql(string sql)
    {
        var tabla = ExtraerNombreTablaDesdeSql(sql);
        var partes = tabla.Split('.');
        return partes.Length > 1 ? partes[0] : "Desconocida";
    }

    #region Delegación al comando interno

    public override string CommandText { get => _innerCommand.CommandText; set => _innerCommand.CommandText = value; }
    public override int CommandTimeout { get => _innerCommand.CommandTimeout; set => _innerCommand.CommandTimeout = value; }
    public override CommandType CommandType { get => _innerCommand.CommandType; set => _innerCommand.CommandType = value; }
    public override bool DesignTimeVisible { get => _innerCommand.DesignTimeVisible; set => _innerCommand.DesignTimeVisible = value; }
    public override UpdateRowSource UpdatedRowSource { get => _innerCommand.UpdatedRowSource; set => _innerCommand.UpdatedRowSource = value; }
    protected override DbConnection DbConnection { get => _innerCommand.Connection!; set => _innerCommand.Connection = value; }
    protected override DbTransaction? DbTransaction { get => _innerCommand.Transaction; set => _innerCommand.Transaction = value; }
    protected override DbParameterCollection DbParameterCollection => _innerCommand.Parameters;
    public override void Cancel() => _innerCommand.Cancel();
    protected override DbParameter CreateDbParameter() => _innerCommand.CreateParameter();
    public override void Prepare() => _innerCommand.Prepare();

    #endregion
}
