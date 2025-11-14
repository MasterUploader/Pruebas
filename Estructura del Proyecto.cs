Este es el código:

using Logging.Abstractions;
using Logging.Models;
using Microsoft.AspNetCore.Http;
using System.Collections;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Logging.Decorators;

/// <summary>
/// Decorador de DbCommand que:
/// - Mide cada ejecución (Reader/NonQuery/Scalar) y construye un bloque estructurado.
/// - Sustituye parámetros por valores reales en el SQL sin romper literales.
/// - Propaga el INICIO (UTC) al contexto para ordenar cronológicamente en el middleware.
/// </summary>
public class LoggingDbCommandWrapper(
    DbCommand innerCommand,
    ILoggingService? loggingService = null,
    IHttpContextAccessor? httpContextAccessor = null) : DbCommand
{
    private readonly DbCommand _innerCommand = innerCommand ?? throw new ArgumentNullException(nameof(innerCommand));
    private readonly ILoggingService? _loggingService = loggingService;
    private readonly IHttpContextAccessor? _httpContextAccessor = httpContextAccessor;
    #region Ejecuciones (bloque por ejecución)

    /// <inheritdoc />
    protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior)
    {
        var startedLocal = DateTime.Now;
        var startedUtc = startedLocal.ToUniversalTime();
        var ctx = _httpContextAccessor?.HttpContext;
        if (ctx is not null) ctx.Items["__SqlStartedUtc"] = startedUtc;

        var sw = Stopwatch.StartNew();
        try
        {
            var reader = _innerCommand.ExecuteReader(behavior);
            sw.Stop();

            // SELECT: filas afectadas = 0
            LogOneExecution(
                startedAt: startedLocal,
                duration: sw.Elapsed,
                affectedRows: 0,
                sqlRendered: RenderSqlWithParametersSafe(_innerCommand.CommandText, _innerCommand.Parameters)
            );

            return reader;
        }
        catch (Exception ex)
        {
            _loggingService?.LogDatabaseError(_innerCommand, ex, ctx);
            throw;
        }
    }

    /// <inheritdoc />
    public override int ExecuteNonQuery()
    {
        var startedLocal = DateTime.Now;
        var startedUtc = startedLocal.ToUniversalTime();
        var ctx = _httpContextAccessor?.HttpContext;
        if (ctx is not null) ctx.Items["__SqlStartedUtc"] = startedUtc;

        var sw = Stopwatch.StartNew();

        var result = _innerCommand.ExecuteNonQuery();

        sw.Stop();
        LogOneExecution(
            startedAt: startedLocal,
            duration: sw.Elapsed,
            affectedRows: result,
            sqlRendered: RenderSqlWithParametersSafe(_innerCommand.CommandText, _innerCommand.Parameters)
        );

        return result;
    }

    /// <inheritdoc />
    public override object? ExecuteScalar()
    {
        var startedLocal = DateTime.Now;
        var startedUtc = startedLocal.ToUniversalTime();
        var ctx = _httpContextAccessor?.HttpContext;
        if (ctx is not null) ctx.Items["__SqlStartedUtc"] = startedUtc;

        var sw = Stopwatch.StartNew();

        var result = _innerCommand.ExecuteScalar();

        sw.Stop();
        LogOneExecution(
            startedAt: startedLocal,
            duration: sw.Elapsed,
            affectedRows: 0, // Scalar no afecta filas
            sqlRendered: RenderSqlWithParametersSafe(_innerCommand.CommandText, _innerCommand.Parameters)
        );

        return result;
    }

    #endregion

    #region Logging por ejecución (con agregación cronológica)

    /// <summary>
    /// Construye el modelo de log SQL y lo envía al servicio para que lo agregue a la lista “timed”.
    /// </summary>
    private void LogOneExecution(DateTime startedAt, TimeSpan duration, int affectedRows, string sqlRendered)
    {
        if (_loggingService is null) return; // Resiliencia: sin servicio, no interfiere.

        try
        {
            var conn = _innerCommand.Connection;

            var model = new SqlLogModel
            {
                Sql = sqlRendered,
                ExecutionCount = 1,               // bloque por ejecución
                TotalAffectedRows = affectedRows, // 0 para SELECT/SCALAR
                StartTime = startedAt,            // ⇦ INICIO real de la ejecución
                Duration = duration,
                DatabaseName = conn?.Database ?? "Desconocida",
                Ip = conn?.DataSource ?? "Desconocida",
                Port = 0, // Si puedes inferirlo, complétalo.
                TableName = ExtraerNombreTablaDesdeSql(_innerCommand.CommandText),
                Schema = ExtraerEsquemaDesdeSql(_innerCommand.CommandText)
            };

            _loggingService.LogDatabaseSuccess(model, _httpContextAccessor?.HttpContext);
        }
        catch (Exception logEx)
        {
            // El logging no debe detener la app: registramos en el mismo archivo como fallback.
            try
            {
                _loggingService?.WriteLog(_httpContextAccessor?.HttpContext,
                    $"[LoggingDbCommandWrapper] Error al escribir el log SQL: {logEx.Message}");
            }
            catch { /* silencio para evitar loops */ }
        }
    }

    #endregion

    #region Render de SQL con parámetros (respetando literales)

    /// <summary>
    /// Devuelve el SQL con parámetros sustituidos por sus valores reales (comillas en literales).
    /// Ignora coincidencias dentro de literales '...'.
    /// </summary>
    private static string RenderSqlWithParametersSafe(string? sql, DbParameterCollection parameters)
    {
        if (string.IsNullOrEmpty(sql) || parameters.Count == 0) return sql ?? string.Empty;

        var literalRanges = ComputeSingleQuoteRanges(sql);

        if (sql.Contains('?'))
            return ReplacePositionalIgnoringLiterals(sql, parameters, literalRanges);

        return ReplaceNamedIgnoringLiterals(sql, parameters, literalRanges);
    }

    /// <summary>
    /// Identifica rangos [start,end] dentro de comillas simples (maneja escape '').
    /// </summary>
    private static List<(int start, int end)> ComputeSingleQuoteRanges(string sql)
    {
        var ranges = new List<(int, int)>();
        bool inString = false;
        int start = -1;

        int idx = 0;
        while (idx < sql.Length)
        {
            char c = sql[idx];
            if (c == '\'')
            {
                bool isEscaped = (idx + 1 < sql.Length) && sql[idx + 1] == '\'';

                if (!inString) { inString = true; start = idx; }
                else if (!isEscaped) { inString = false; ranges.Add((start, idx)); }

                idx += isEscaped ? 2 : 1;
                continue;
            }

            idx++;
        }

        return ranges;
    }

    /// <summary>Indica si un índice está dentro de alguno de los rangos de literales.</summary>
    private static bool IsInsideAnyRange(int index, List<(int start, int end)> ranges)
    {
        for (int i = 0; i < ranges.Count; i++)
        {
            var (s, e) = ranges[i];
            if (index >= s && index <= e) return true;
        }
        return false;
    }

    /// <summary>
    /// Reemplaza '?' por valores en orden, ignorando las que caen dentro de literales.
    /// </summary>
    private static string ReplacePositionalIgnoringLiterals(string sql, DbParameterCollection parameters, List<(int start, int end)> literalRanges)
    {
        var sb = new StringBuilder(sql.Length + parameters.Count * 10);
        int paramIndex = 0;

        for (int i = 0; i < sql.Length; i++)
        {
            char c = sql[i];
            if (c == '?' && !IsInsideAnyRange(i, literalRanges) && paramIndex < parameters.Count)
            {
                var p = parameters[paramIndex++];
                sb.Append(FormatParameterValue(p));
            }
            else
            {
                sb.Append(c);
            }
        }

        return sb.ToString();
    }

    /// <summary>
    /// Reemplazo de parámetros nombrados (@name, :name) ignorando literales.
    /// </summary>
    private static string ReplaceNamedIgnoringLiterals(string sql, DbParameterCollection parameters, List<(int start, int end)> literalRanges)
    {
        if (parameters.Count == 0) return sql;

        string result = sql;
        foreach (DbParameter p in parameters)
        {
            var name = p.ParameterName?.Trim();
            if (string.IsNullOrEmpty(name)) continue;

            foreach (var token in new[] { "@" + name, ":" + name })
            {
                var rx = new Regex($@"(?<!\w){Regex.Escape(token)}(?!\w)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
                result = rx.Replace(result, m =>
                {
                    if (IsInsideAnyRange(m.Index, literalRanges)) return m.Value;
                    return FormatParameterValue(p);
                });
            }
        }

        return result;
    }

    /// <summary>
    /// Formatea un parámetro a literal SQL. Maneja IEnumerable, binarios y tipos escalar.
    /// </summary>
    private static string FormatParameterValue(DbParameter p)
    {
        var value = p.Value;

        if (value is null || value == DBNull.Value) return "NULL";

        if (value is IEnumerable enumerable && value is not string && value is not byte[])
        {
            var parts = new List<string>();
            foreach (var item in enumerable) parts.Add(FormatScalar(item));
            return "(" + string.Join(", ", parts) + ")";
        }

        return FormatScalar(value, p.DbType);
    }

    /// <summary>Formateo escalar con heurística por DbType/tipo CLR.</summary>
    private static string FormatScalar(object? value, DbType? hinted = null)
    {
        if (value is null || value == DBNull.Value) return "NULL";
        if (value is byte[] bytes) return $"<binary {bytes.Length} bytes>";

        if (hinted.HasValue)
        {
            switch (hinted.Value)
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
                    return $"'{ToDateTime(value):yyyy-MM-dd HH:mm:ss}'";

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
                    break;
            }
        }

        return value switch
        {
            string s => "'" + EscapeSqlString(s) + "'",
            char ch => "'" + EscapeSqlString(ch.ToString()) + "'",
            DateTime dt => $"'{dt:yyyy-MM-dd HH:mm:ss}'",
            DateTimeOffset dto => $"'{dto:yyyy-MM-dd HH:mm:ss}'",
            bool b => b ? "1" : "0",
            float or double or decimal => Convert.ToString(value, CultureInfo.InvariantCulture) ?? "0",
            sbyte or byte or short or int or long or ushort or uint or ulong => Convert.ToString(value, CultureInfo.InvariantCulture) ?? "0",
            Guid guid => "'" + guid.ToString() + "'",
            IEnumerable e => "(" + string.Join(", ", EnumerateFormatted(e)) + ")",
            _ => "'" + EscapeSqlString(Convert.ToString(value) ?? string.Empty) + "'"
        };
    }

    private static IEnumerable<string> EnumerateFormatted(IEnumerable e)
    {
        foreach (var it in e) yield return FormatScalar(it);
    }

    private static string EscapeSqlString(string s) => s.Replace("'", "''");

    private static DateTime ToDateTime(object value)
    {
        if (value is DateTime d) return d;
        if (value is DateTimeOffset dto) return dto.LocalDateTime;
        if (DateTime.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture), CultureInfo.InvariantCulture,
                              DateTimeStyles.AllowWhiteSpaces, out var parsed))
            return parsed;
        return DateTime.MinValue;
    }

    #endregion

    #region Heurísticas para nombre de tabla/esquema

    private static string ExtraerNombreTablaDesdeSql(string sql)
    {
        try
        {
            var tokens = sql.ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var idx = Array.FindIndex(tokens, t => t is "into" or "from" or "update");
            return idx >= 0 && tokens.Length > idx + 1 ? tokens[idx + 1] : "Desconocida";
        }
        catch { return "Desconocida"; }
    }

    private static string ExtraerEsquemaDesdeSql(string sql)
    {
        var tabla = ExtraerNombreTablaDesdeSql(sql);
        var partes = tabla.Split('.', StringSplitOptions.RemoveEmptyEntries);
        return partes.Length > 1 ? partes[0] : "Desconocida";
    }

    #endregion

    #region Delegación transparente al comando interno

    public override string? CommandText { get => _innerCommand.CommandText; set => _innerCommand.CommandText = value; }
    public override int CommandTimeout { get => _innerCommand.CommandTimeout; set => _innerCommand.CommandTimeout = value; }
    public override CommandType CommandType { get => _innerCommand.CommandType; set => _innerCommand.CommandType = value; }
    public override bool DesignTimeVisible { get => _innerCommand.DesignTimeVisible; set => _innerCommand.DesignTimeVisible = value; }
    public override UpdateRowSource UpdatedRowSource { get => _innerCommand.UpdatedRowSource; set => _innerCommand.UpdatedRowSource = value; }
    protected override DbConnection? DbConnection { get => _innerCommand.Connection; set => _innerCommand.Connection = value; }
    protected override DbTransaction? DbTransaction { get => _innerCommand.Transaction; set => _innerCommand.Transaction = value; }
    protected override DbParameterCollection DbParameterCollection => _innerCommand.Parameters;
    public override void Cancel() => _innerCommand.Cancel();
    protected override DbParameter CreateDbParameter() => _innerCommand.CreateParameter();
    public override void Prepare() => _innerCommand.Prepare();

    #endregion

    /// <summary>El decorador no imprime resumen en Dispose; los bloques son por ejecución.</summary>
    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        _innerCommand.Dispose();
    }
}




