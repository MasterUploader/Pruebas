using Logging.Abstractions;
using Logging.Models;
using Microsoft.AspNetCore.Http;
using System.Data;
using System.Data.Common;
using System.Diagnostics;

namespace Logging.Decorators;

/// <summary>
/// Decorador para interceptar y registrar información de comandos SQL.
/// Mantiene métricas acumuladas y emite un bloque estructurado en la finalización (Dispose),
/// sin generar encabezados duplicados ni salidas previas.
/// Tolera ausencia de servicio de logging sin afectar la ejecución del comando.
/// </summary>
public class LoggingDbCommandWrapper : DbCommand
{
    private readonly DbCommand _innerCommand;
    private readonly ILoggingService? _loggingService;
    private readonly IHttpContextAccessor? _httpContextAccessor;

    // Cronometría y estado interno protegidos por lock para exactitud en escenarios concurrentes.
    private readonly Stopwatch _stopwatch = new();
    private readonly object _lock = new();

    // Acumuladores y marcas de ciclo de vida de este comando.
    private int _executionCount = 0;
    private int _totalAffectedRows = 0;
    private DateTime _startTime;
    private string _commandText = string.Empty;
    private bool _isFinalized = false;

    /// <summary>
    /// Crea el decorador con soporte opcional de servicios de logging y contexto HTTP.
    /// </summary>
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

            // Aseguramos que las lecturas (típicamente SELECT) también generen bloque estructurado:
            // cuentan como ejecución con 0 filas afectadas (las lecturas no modifican datos).
            RegisterExecution(affectedRows: 0);

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

        // Para DML (INSERT/UPDATE/DELETE) el proveedor devuelve filas afectadas.
        RegisterExecution(affectedRows: result);

        return result;
    }

    /// <inheritdoc />
    public override object? ExecuteScalar()
    {
        StartIfNeeded();

        var result = _innerCommand.ExecuteScalar();

        // Scalar también debe generar bloque estructurado, exista o no valor.
        // Se registra con 0 filas afectadas (no es DML).
        RegisterExecution(affectedRows: 0);

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
    /// Consolida y guarda el bloque estructurado del comando (una sola vez).
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
                    Sql                 = _commandText,
                    ExecutionCount      = _executionCount,
                    TotalAffectedRows   = _totalAffectedRows, // 0 en SELECT/SCALAR
                    StartTime           = _startTime,
                    Duration            = _stopwatch.Elapsed,
                    DatabaseName        = connection?.Database   ?? "Desconocida",
                    Ip                  = connection?.DataSource ?? "Desconocida",
                    Port                = 0,
                    TableName           = ExtraerNombreTablaDesdeSql(_commandText),
                    Schema              = ExtraerEsquemaDesdeSql(_commandText)
                };

                // Emite el bloque “LOG DE EJECUCIÓN SQL” en el archivo correcto (ya ajustado en tu servicio).
                _loggingService.LogDatabaseSuccess(log, _httpContextAccessor?.HttpContext);
            }
            catch (Exception ex)
            {
                _loggingService?.LogDatabaseError(_innerCommand, ex, _httpContextAccessor?.HttpContext);
            }
        }
    }

    /// <summary>
    /// Inicializa la captura de información del comando la primera vez que se ejecuta.
    /// </summary>
    private void StartIfNeeded()
    {
        lock (_lock)
        {
            if (_executionCount == 0)
            {
                _startTime   = DateTime.Now;
                _commandText = _innerCommand.CommandText;
                _stopwatch.Restart();

                // Importante: NO se escribe ningún encabezado aquí para evitar duplicados.
                // El bloque se emitirá una sola vez al finalizar (FinalizeAndLog).
            }
        }
    }

    /// <summary>
    /// Incrementa contadores internos y acumula filas afectadas para DML.
    /// Para SELECT/SCALAR se recomienda pasar 0.
    /// </summary>
    private void RegisterExecution(int affectedRows)
    {
        lock (_lock)
        {
            _executionCount++;
            if (affectedRows > 0)
                _totalAffectedRows += affectedRows;
        }
    }

    /// <summary>Extrae el nombre de la tabla desde la sentencia SQL (heurístico defensivo).</summary>
    private static string ExtraerNombreTablaDesdeSql(string sql)
    {
        try
        {
            var tokens = sql.ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var index  = Array.FindIndex(tokens, t => t == "into" || t == "from" || t == "update");
            return index >= 0 && tokens.Length > index + 1 ? tokens[index + 1] : "Desconocida";
        }
        catch { return "Desconocida"; }
    }

    /// <summary>Extrae el esquema (library) desde la sentencia SQL, si está calificada.</summary>
    private static string ExtraerEsquemaDesdeSql(string sql)
    {
        var tabla  = ExtraerNombreTablaDesdeSql(sql);
        var partes = tabla.Split('.', StringSplitOptions.RemoveEmptyEntries);
        return partes.Length > 1 ? partes[0] : "Desconocida";
    }

    #region Delegación al comando interno (transparencia total)

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
