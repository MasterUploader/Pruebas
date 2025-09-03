using Logging.Abstractions;
using Logging.Models;
using Microsoft.AspNetCore.Http;
using System.Collections;
using System.Collections.Generic; // <- needed for List<>
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Logging.Decorators;

/// <summary>
/// Decorador para interceptar y registrar información de comandos SQL con alto grado de fidelidad.
/// - Emite un bloque ESTRUCTURADO por CADA ejecución (ExecuteReader/ExecuteNonQuery/ExecuteScalar).
/// - Renderiza la sentencia SQL con los VALORES REALES (comillas simples en literales),
///   sustituyendo parámetros posicionales (?) y nombrados (@p0, :UserId), ignorando ocurrencias dentro de literales.
/// - Tolera ausencia del servicio de logging sin interrumpir la ejecución.
/// </summary>
public class LoggingDbCommandWrapper : DbCommand
{
    private readonly DbCommand _innerCommand;
    private readonly ILoggingService? _loggingService;
    private readonly IHttpContextAccessor? _httpContextAccessor;

    /// <summary>
    /// Crea el decorador con soporte opcional de servicios de logging y contexto HTTP.
    /// </summary>
    public LoggingDbCommandWrapper(
        DbCommand innerCommand,
        ILoggingService? loggingService = null,
        IHttpContextAccessor? httpContextAccessor = null)
    {
        _innerCommand = innerCommand ?? throw new ArgumentNullException(nameof(innerCommand));
        _loggingService = loggingService;
        _httpContextAccessor = httpContextAccessor;
    }

    #region Ejecuciones (bloque por ejecución)

    /// <inheritdoc />
    protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior)
    {
        var startedAt = DateTime.Now;
        var sw = Stopwatch.StartNew();

        try
        {
            var reader = _innerCommand.ExecuteReader(behavior);

            sw.Stop();
            // SELECT/lecturas: registramos filas afectadas = 0
            LogOneExecution(
                startedAt: startedAt,
                duration: sw.Elapsed,
                affectedRows: 0,
                sqlRendered: RenderSqlWithParametersSafe(_innerCommand.CommandText, _innerCommand.Parameters)
            );

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
        var startedAt = DateTime.Now;
        var sw = Stopwatch.StartNew();

        var result = _innerCommand.ExecuteNonQuery();

        sw.Stop();
        LogOneExecution(
            startedAt: startedAt,
            duration: sw.Elapsed,
            affectedRows: result,
            sqlRendered: RenderSqlWithParametersSafe(_innerCommand.CommandText, _innerCommand.Parameters)
        );

        return result;
    }

    /// <inheritdoc />
    public override object? ExecuteScalar()
    {
        var startedAt = DateTime.Now;
        var sw = Stopwatch.StartNew();

        var result = _innerCommand.ExecuteScalar();

        sw.Stop();
        // Scalar: no DML → filas afectadas = 0
        LogOneExecution(
            startedAt: startedAt,
            duration: sw.Elapsed,
            affectedRows: 0,
            sqlRendered: RenderSqlWithParametersSafe(_innerCommand.CommandText, _innerCommand.Parameters)
        );

        return result;
    }

    #endregion

    #region Logging por ejecución

    /// <summary>
    /// Emite un bloque “LOG DE EJECUCIÓN SQL” para una ejecución específica.
    /// </summary>
    private void LogOneExecution(DateTime startedAt, TimeSpan duration, int affectedRows, string sqlRendered)
    {
        if (_loggingService is null)
            return;

        try
        {
            var conn = _innerCommand.Connection;

            var model = new SqlLogModel
            {
                Sql               = sqlRendered,
                ExecutionCount    = 1,                  // bloque por ejecución
                TotalAffectedRows = affectedRows,       // 0 para SELECT/SCALAR
                StartTime         = startedAt,
                Duration          = duration,
                DatabaseName      = conn?.Database   ?? "Desconocida",
                Ip                = conn?.DataSource ?? "Desconocida",
                Port              = 0,
                TableName         = ExtraerNombreTablaDesdeSql(_innerCommand.CommandText),
                Schema            = ExtraerEsquemaDesdeSql(_innerCommand.CommandText)
            };

            _loggingService.LogDatabaseSuccess(model, _httpContextAccessor?.HttpContext);
        }
        catch (Exception logEx)
        {
            try { _loggingService?.WriteLog($"[LoggingDbCommandWrapper] Error al escribir el log SQL: {logEx.Message}", _httpContextAccessor?.HttpContext); } catch { }
        }
    }

    #endregion

    #region Render de SQL con parámetros (seguro con literales, soporta IEnumerable)

    /// <summary>
    /// Devuelve el SQL con parámetros sustituidos por sus valores reales (comillas simples en literales).
    /// Ignora placeholders que estén dentro de comillas simples en el propio SQL.
    /// </summary>
    private static string RenderSqlWithParametersSafe(string? sql, DbParameterCollection parameters)
    {
        if (string.IsNullOrEmpty(sql) || parameters.Count == 0)
            return sql ?? string.Empty;

        // 1) Detectamos rangos de literales '...'
        var literalRanges = ComputeSingleQuoteRanges(sql);

        // 2) Si hay "?" → reemplazo POSICIONAL respetando literales
        if (sql.IndexOf('?') >= 0)
            return ReplacePositionalIgnoringLiterals(sql, parameters, literalRanges);

        // 3) Si no, probamos parámetros NOMBRADOS (@name / :name), también ignorando literales
        return ReplaceNamedIgnoringLiterals(sql, parameters, literalRanges);
    }

    /// <summary>
    /// Identifica rangos [start, end] (inclusive) que están dentro de comillas simples,
    /// respetando el escape estándar SQL de comillas duplicadas ('').
    /// </summary>
    private static List<(int start, int end)> ComputeSingleQuoteRanges(string sql)
    {
        var ranges = new List<(int, int)>();
        bool inString = false;
        int start = -1;

        // Usamos while con índice manual para evitar S127 (no modificar contador de un for dentro del cuerpo)
        int idx = 0;
        while (idx < sql.Length)
        {
            char c = sql[idx];
            if (c == '\'')
            {
                bool isEscaped = (idx + 1 < sql.Length) && sql[idx + 1] == '\'';

                if (!inString)
                {
                    inString = true;
                    start = idx;
                }
                else if (!isEscaped)
                {
                    inString = false;
                    ranges.Add((start, idx));
                }

                idx += isEscaped ? 2 : 1;
                continue;
            }

            idx++;
        }

        return ranges;
    }

    /// <summary>Indica si un índice está contenido dentro de alguno de los rangos de literales.</summary>
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
    /// Reemplaza '?' por valores en orden, ignorando cualquier '?' que caiga dentro de literales '...'.
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
    /// Reemplazo de parámetros nombrados (@name, :name) ignorando coincidencias dentro de literales.
    /// </summary>
    private static string ReplaceNamedIgnoringLiterals(string sql, DbParameterCollection parameters, List<(int start, int end)> literalRanges)
    {
        if (parameters.Count == 0) return sql;

        string result = sql;
        foreach (DbParameter p in parameters)
        {
            var name = p.ParameterName?.Trim();
            if (string.IsNullOrEmpty(name))
                continue;

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
    /// Formatea un parámetro a literal SQL:
    /// - IEnumerable (no string/byte[]) → (v1, v2, v3)  // útil en IN (...)
    /// - String/Guid → 'escapado'
    /// - Date/DateTime/Offset → 'yyyy-MM-dd HH:mm:ss'
    /// - Bool → 1/0
    /// - Numéricos → invariante
    /// - byte[] → &lt;binary N bytes&gt;
    /// - null → NULL
    /// </summary>
    private static string FormatParameterValue(DbParameter p)
    {
        var value = p.Value;

        if (value is null || value == DBNull.Value)
            return "NULL";

        if (value is IEnumerable enumerable && value is not string && value is not byte[])
        {
            var parts = new List<string>();
            foreach (var item in enumerable)
                parts.Add(FormatScalar(item));
            return "(" + string.Join(", ", parts) + ")";
        }

        return FormatScalar(value, p.DbType);
    }

    /// <summary>
    /// Formateo escalar defensivo con heurística por DbType y/o tipo CLR.
    /// </summary>
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
            string s           => "'" + EscapeSqlString(s) + "'",
            char ch            => "'" + EscapeSqlString(ch.ToString()) + "'",
            DateTime dt        => $"'{dt:yyyy-MM-dd HH:mm:ss}'",
            DateTimeOffset dto => $"'{dto:yyyy-MM-dd HH:mm:ss}'",
            bool b             => b ? "1" : "0",
            float or double or decimal => Convert.ToString(value, CultureInfo.InvariantCulture) ?? "0",
            sbyte or byte or short or int or long or ushort or uint or ulong => Convert.ToString(value, CultureInfo.InvariantCulture) ?? "0",
            Guid guid          => "'" + guid.ToString() + "'",
            IEnumerable e      => "(" + string.Join(", ", EnumerateFormatted(e)) + ")",
            _                  => "'" + EscapeSqlString(Convert.ToString(value) ?? string.Empty) + "'"
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

    #region Utilidades para nombre de tabla/esquema (heurísticas)

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

    #region Delegación al comando interno (transparencia)

    // Ajustes de nullability para coincidir con la firma base y eliminar CS8765:
    public override string? CommandText { get => _innerCommand.CommandText; set => _innerCommand.CommandText = value; }
    public override int CommandTimeout { get => _innerCommand.CommandTimeout; set => _innerCommand.CommandTimeout = value; }
    public override CommandType CommandType { get => _innerCommand.CommandType; set => _innerCommand.CommandType = value; }
    public override bool DesignTimeVisible { get => _innerCommand.DesignTimeVisible; set => _innerCommand.DesignTimeVisible = value; }
    public override UpdateRowSource UpdatedRowSource { get => _innerCommand.UpdatedRowSource; set => _innerCommand.UpdatedRowSource = value; }
    protected override DbConnection? DbConnection { get => _innerCommand.Connection; set => _innerCommand.Connection = value; } // <- nullable
    protected override DbTransaction? DbTransaction { get => _innerCommand.Transaction; set => _innerCommand.Transaction = value; }
    protected override DbParameterCollection DbParameterCollection => _innerCommand.Parameters;
    public override void Cancel() => _innerCommand.Cancel();
    protected override DbParameter CreateDbParameter() => _innerCommand.CreateParameter();
    public override void Prepare() => _innerCommand.Prepare();

    #endregion

    /// <summary>
    /// Importante: el log se emite por ejecución; no imprimimos resumen aquí.
    /// </summary>
    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        _innerCommand.Dispose();
    }
}
