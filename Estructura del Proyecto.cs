using Logging.Abstractions;
using Logging.Models;
using Microsoft.AspNetCore.Http;
using System.Data;
using System.Data.Common;
using System.Diagnostics;

namespace Logging.Decorators;

/// <summary>
/// Decorador para interceptar y registrar información de comandos SQL.
/// - Mantiene estadísticas acumuladas y registra un resumen estructurado al finalizar (Dispose).
/// - Escribe tempranamente el SQL en el archivo del request para asegurar que todo vaya
///   al MISMO archivo (evitando "GeneralLog_*").
/// - Tolera la ausencia de servicios de logging sin romper la ejecución.
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

    // Bandera para evitar múltiples escrituras del encabezado del SQL
    private bool _primedCurrentRequestLog = false;

    /// <summary>
    /// Crea el decorador.
    /// </summary>
    /// <param name="innerCommand">Comando original a decorar.</param>
    /// <param name="loggingService">Servicio de logging (opcional).</param>
    /// <param name="httpContextAccessor">Accessor HTTP (opcional).</param>
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
            return _innerCommand.ExecuteReader(behavior);
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
    /// Consolida y guarda el log estructurado (una sola vez) al finalizar la vida del comando.
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

                // Se delega al servicio. Con el ajuste propuesto en LoggingService,
                // esto irá al mismo archivo del request/endpoint.
                _loggingService.LogDatabaseSuccess(log, _httpContextAccessor?.HttpContext);
            }
            catch (Exception ex)
            {
                _loggingService?.LogDatabaseError(_innerCommand, ex, _httpContextAccessor?.HttpContext);
            }
        }
    }

    /// <summary>
    /// Inicializa mediciones y ancla el archivo de log del request escribiendo el SQL una sola vez.
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

                PrimeCurrentRequestLogOnce();
            }
        }
    }

    /// <summary>
    /// Incrementa contadores internos de ejecución para el resumen final.
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

    /// <summary>
    /// Escribe una sola vez un encabezado con el SQL al archivo del request, 
    /// garantizando que el servicio resuelva y recuerde el path correcto.
    /// </summary>
    private void PrimeCurrentRequestLogOnce()
    {
        if (_primedCurrentRequestLog || _loggingService is null)
            return;

        try
        {
            var header =
                "──────────────── SQL COMMAND ────────────────\n" +
                _commandText + "\n" +
                "──────────────────────────────────────────────";

            _loggingService.WriteLog(header, _httpContextAccessor?.HttpContext);
            _primedCurrentRequestLog = true;
        }
        catch
        {
            _primedCurrentRequestLog = true; // evita reintentos ruidosos
        }
    }

    /// <summary>Extrae el nombre de la tabla desde la sentencia SQL.</summary>
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

    /// <summary>Extrae el esquema (library) desde la sentencia SQL.</summary>
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



using Logging.Abstractions;
using Logging.Models;
using Microsoft.AspNetCore.Http;

public sealed class LoggingService : ILoggingService
{
    // ... resto de tu servicio ...

    /// <summary>
    /// Registra un bloque estructurado de éxito de ejecución SQL en el mismo
    /// archivo del request/endpoint, y ejecuta cualquier otra acción colateral
    /// (por ejemplo, CSV) que ya tuvieses implementada.
    /// </summary>
    /// <param name="model">Datos de la ejecución SQL.</param>
    /// <param name="httpContext">Contexto HTTP (opcional).</param>
    public void LogDatabaseSuccess(SqlLogModel model, HttpContext? httpContext = null)
    {
        try
        {
            // 1) Formateo del bloque estructurado (usa tu formateador actual)
            var block = LogFormatter.FormatDatabaseSuccess(model);

            // 2) Escribir en el MISMO archivo del request/endpoint.
            //    WriteLog -> internamente utiliza GetCurrentLogFile(httpContext)
            //    que ya lee/recicla HttpContext.Items["LogFileName"].
            WriteLog(block, httpContext);

            // 3) (Opcional) Si generas CSV aparte, mantenlo:
            // TryWriteCsv(model, httpContext); // si existe en tu servicio
        }
        catch
        {
            // Nunca romper por el log
        }
    }

    /// <summary>
    /// Registra un bloque estructurado de error de ejecución SQL en el mismo archivo del request.
    /// </summary>
    /// <param name="command">Comando que falló.</param>
    /// <param name="ex">Excepción.</param>
    /// <param name="httpContext">Contexto HTTP (opcional).</param>
    public void LogDatabaseError(DbCommand command, Exception ex, HttpContext? httpContext = null)
    {
        try
        {
            var block = LogFormatter.FormatDatabaseError(command, ex);
            WriteLog(block, httpContext);

            // (Opcional) CSV/telemetría adicional si la tienes:
            // TryWriteCsvError(command, ex, httpContext);
        }
        catch
        {
            // Nunca romper por el log
        }
    }

    // ---------------------------
    // Asegúrate de que WriteLog(string, HttpContext?) y GetCurrentLogFile(HttpContext?)
    // ya existen y usan HttpContext.Items["LogFileName"] si está seteado por el middleware.
    // ---------------------------
}


