using Logging.Abstractions;
using Logging.Models;
using Microsoft.AspNetCore.Http;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Logging.Decorators;

/// <summary>
/// Decorador para interceptar y registrar información de comandos SQL.
/// - Consolida duración, conteos y filas afectadas.
/// - Emite un bloque estructurado al finalizar (Dispose), con la sentencia SQL
///   ya “renderizada” con los valores reales de los parámetros (comillas simples en literales).
/// - Evita encabezados intermedios para no duplicar salidas.
/// - Tolera ausencia de servicios de logging sin romper la ejecución.
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
    private string _sqlForLog   = string.Empty;   // ← SQL con parámetros sustituidos
    private bool _isFinalized   = false;

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
        StartIfNeeded();           // captura texto y cronómetro
        PreRenderSqlForLog();      // prepara SQL con valores reales
        try
        {
            var reader = _innerCommand.ExecuteReader(behavior);

            // SELECT/lecturas: cuentan como ejecución con 0 filas afectadas
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
        PreRenderSqlForLog();      // asegura SQL con los valores actuales
        var result = _innerCommand.ExecuteNonQuery();
        RegisterExecution(result);
        return result;
    }

    /// <inheritdoc />
    public override object? ExecuteScalar()
    {
        StartIfNeeded();
        PreRenderSqlForLog();      // asegura SQL con los valores actuales
        var result = _innerCommand.ExecuteScalar();

        // Scalar: registrar ejecución (no DML) con 0 filas afectadas
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
                    // Importante: enviamos la sentencia "bonita" con valores
                    Sql               = string.IsNullOrWhiteSpace(_sqlForLog) ? _commandText : _sqlForLog,
                    ExecutionCount    = _executionCount,
                    TotalAffectedRows = _totalAffectedRows,
                    StartTime         = _startTime,
                    Duration          = _stopwatch.Elapsed,
                    DatabaseName      = connection?.Database   ?? "Desconocida",
                    Ip                = connection?.DataSource ?? "Desconocida",
                    Port              = 0,
                    TableName         = ExtraerNombreTablaDesdeSql(_commandText),
                    Schema            = ExtraerEsquemaDesdeSql(_commandText)
                };

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
    /// No escribe nada aún; solo fija base de cronometraje y texto original.
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
            }
        }
    }

    /// <summary>
    /// Asegura que <see cref="_sqlForLog"/> contenga una versión de <see cref="_commandText"/>
    /// con los parámetros sustituidos por sus valores reales, usando comillas simples en literales.
    /// </summary>
    private void PreRenderSqlForLog()
    {
        // Evita recomputar si ya existe (para múltiples lecturas); si te interesa
        // reconstruir en cada ejecución porque cambiaste parámetros sobre el mismo
        // comando, cambia esta condición por un render siempre.
        if (!string.IsNullOrEmpty(_sqlForLog))
            return;

        try
        {
            _sqlForLog = RenderSqlWithParameters(_innerCommand.CommandText, _innerCommand.Parameters);
        }
        catch
        {
            // Si hay cualquier problema de render, deja el SQL crudo como fallback.
            _sqlForLog = _innerCommand.CommandText;
        }
    }

    /// <summary>
    /// Incrementa contadores internos y acumula filas afectadas.
    /// Para SELECT/SCALAR usar 0; para DML usar el valor devuelto por el proveedor.
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
    /// Devuelve el SQL con parámetros sustituidos por sus valores reales.
    /// Soporta:
    ///  - Placeholders posicionales "?" (OleDb/DB2 i)
    ///  - Parámetros nominales tipo "@p0", ":p0" (otros proveedores)
    /// </summary>
    private static string RenderSqlWithParameters(string sql, DbParameterCollection parameters)
    {
        if (string.IsNullOrEmpty(sql) || parameters.Count == 0)
            return sql;

        // Heurística: si hay "?" usamos reemplazo posicional; de lo contrario intentamos nominal.
        if (sql.Contains('?'))
        {
            return ReplacePositionalPlaceholders(sql, parameters);
        }
        else
        {
            return ReplaceNamedParameters(sql, parameters);
        }
    }

    /// <summary>
    /// Reemplaza cada '?' por el valor correspondiente en orden.
    /// No intenta parsear literales; asume uso estándar de placeholders.
    /// </summary>
    private static string ReplacePositionalPlaceholders(string sql, DbParameterCollection parameters)
    {
        var sb = new StringBuilder(sql.Length + parameters.Count * 10);
        int paramIndex = 0;

        for (int i = 0; i < sql.Length; i++)
        {
            char c = sql[i];
            if (c == '?' && paramIndex < parameters.Count)
            {
                var p = parameters[paramIndex++];
                sb.Append(FormatParameterValue(p));  // ← valor en comillas simples si aplica
            }
            else
            {
                sb.Append(c);
            }
        }

        return sb.ToString();
    }

    /// <summary>
    /// Reemplazo simple por nombre: busca tokens de parámetro comunes (@name, :name)
    /// respetando límites de palabra para evitar falsos positivos.
    /// </summary>
    private static string ReplaceNamedParameters(string sql, DbParameterCollection parameters)
    {
        string result = sql;

        foreach (DbParameter p in parameters)
        {
            // Algunos proveedores llenan ParameterName con o sin prefijo; normalizamos ambos.
            var name = p.ParameterName?.Trim() ?? string.Empty;
            if (string.IsNullOrEmpty(name))
                continue;

            // Construimos patrones @name y :name (case-insensitive, borde de palabra)
            var candidates = new[]
            {
                "@" + name,
                ":" + name
            };

            foreach (var cand in candidates)
            {
                // Regex: reemplaza el token como palabra completa (evita partes de identificadores).
                // Ej: @p0, :UserId
                var pattern = $@"(?<!\w){Regex.Escape(cand)}(?!\w)";
                result = Regex.Replace(
                    result,
                    pattern,
                    MatchEvaluator: _ => FormatParameterValue(p),
                    options: RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            }
        }

        return result;
    }

    /// <summary>
    /// Convierte el valor de un parámetro a su representación SQL “literal”
    /// usando comillas simples en cadenas/fechas, y notación invariante para numéricos.
    /// - Strings se escapan duplicando comillas simples.
    /// - DateTime como 'yyyy-MM-dd HH:mm:ss'.
    /// - NULL como literal SQL NULL.
    /// - Boolean como 1/0 (más compatible con DB2 i).
    /// - Binarios como &lt;binary N bytes&gt; (para evitar volúmenes enormes).
    /// </summary>
    private static string FormatParameterValue(DbParameter p)
    {
        var value = p.Value;

        if (value is null || value == DBNull.Value)
            return "NULL";

        // Detecta binarios desde varios tipos comunes
        if (value is byte[] bytes)
            return $"<binary {bytes.Length} bytes>";

        // Normaliza por DbType cuando está disponible; fallback al tipo CLR.
        switch (p.DbType)
        {
            case DbType.AnsiString:
            case DbType.AnsiStringFixedLength:
            case DbType.String:
            case DbType.StringFixedLength:
            case DbType.Xml:
                return "'" + EscapeSqlString(Convert.ToString(value) ?? string.Empty) + "'";

            case DbType.Date:
            case DbType.Time:
            case DbType.DateTime:
            case DbType.DateTime2:
            case DbType.DateTimeOffset:
            {
                // Usamos formato estándar compatible: 'yyyy-MM-dd HH:mm:ss'
                // Para Date puro, el tiempo quedará en 00:00:00 (no afecta el log).
                var dt = ConvertToDateTime(value);
                return $"'{dt:yyyy-MM-dd HH:mm:ss}'";
            }

            case DbType.Boolean:
                return Convert.ToBoolean(value) ? "1" : "0";

            case DbType.Byte:
            case DbType.SByte:
            case DbType.Int16:
            case DbType.Int32:
            case DbType.Int64:
            case DbType.UInt16:
            case DbType.UInt32:
            case DbType.UInt64:
            case DbType.Single:
            case DbType.Double:
            case DbType.Decimal:
            case DbType.VarNumeric:
                return Convert.ToString(value, CultureInfo.InvariantCulture) ?? "0";

            case DbType.Guid:
                return "'" + Convert.ToString(value) + "'";

            case DbType.Object:
            default:
                // Fallback: literal string con escape
                return "'" + EscapeSqlString(Convert.ToString(value) ?? string.Empty) + "'";
        }
    }

    /// <summary>
    /// Escapa comillas simples en cadenas SQL duplicándolas.
    /// </summary>
    private static string EscapeSqlString(string s) => s.Replace("'", "''");

    /// <summary>
    /// Conversión defensiva de cualquier valor a DateTime (sin perder el log en caso de formatos raros).
    /// </summary>
    private static DateTime ConvertToDateTime(object value)
    {
        if (value is DateTime d) return d;
        if (DateTime.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture), CultureInfo.InvariantCulture,
                             DateTimeStyles.None, out var parsed))
            return parsed;
        return DateTime.MinValue;
    }

    /// <summary>Heurística simple para extraer nombre de tabla desde la sentencia.</summary>
    private static string ExtraerNombreTablaDesdeSql(string sql)
    {
        try
        {
            var tokens = sql.ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var index  = Array.FindIndex(tokens, t => t is "into" or "from" or "update");
            return index >= 0 && tokens.Length > index + 1 ? tokens[index + 1] : "Desconocida";
        }
        catch { return "Desconocida"; }
    }

    /// <summary>Heurística para extraer la biblioteca/esquema desde la sentencia.</summary>
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
