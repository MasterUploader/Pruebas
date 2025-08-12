using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using RestUtilities.Connections.Abstractions; // <- donde vive tu IDatabaseConnection

namespace RestUtilities.Connections.Helpers
{
    /// <summary>
    /// Especifica un parámetro OUT/INOUT, incluyendo metadatos como tamaño y precisión/escala.
    /// </summary>
    public readonly struct OutSpec
    {
        public OutSpec(string name, DbType type, int? size = null, byte? precision = null, byte? scale = null, object? initial = null)
        {
            Name = name;
            Type = type;
            Size = size;
            Precision = precision;
            Scale = scale;
            Initial = initial;
        }
        public string Name { get; }
        public DbType Type { get; }
        public int? Size { get; }
        public byte? Precision { get; }
        public byte? Scale { get; }
        public object? Initial { get; }
    }

    /// <summary>
    /// Builder fluido para invocar programas CLLE/RPGLE mediante <c>{CALL LIB.PROG(?, ?, ...)}</c>
    /// con parámetros **posicionales** y helpers que exigen el **nombre lógico** de cada parámetro
    /// (solo para trazabilidad; el proveedor sigue usando el orden).
    /// </summary>
    public sealed class ProgramCallBuilder
    {
        // Dependencias / estado
        private readonly IDatabaseConnection? _connection;             // overload con interfaz
        private readonly Func<HttpContext?, DbCommand>? _getCmd;       // overload con delegado

        private string _library;
        private readonly string _program;

        // IN: lista de fábricas de parámetros en el orden en que se agregan
        private readonly List<Func<DbCommand, DbParameter>> _paramFactories = new();
        // OUT: especificaciones con metadatos
        private readonly List<OutSpec> _bulkOuts = new();

        // Configuración operativa
        private int? _commandTimeoutSeconds;
        private int _retryAttempts = 0;
        private TimeSpan _retryBackoff = TimeSpan.Zero;
        private string? _traceId;

        // SQL emitido
        private enum Naming { SqlDot, SystemSlash }
        private Naming _naming = Naming.SqlDot;     // LIB.PROG
        private bool _wrapWithBraces = true;        // {CALL ...}

        // Normalización de OUTs
        private bool _trimOutStringPadding = true;  // recorta '\0' y ' '
        private bool _emptyStringAsNull = false;    // "" -> null
        private bool _forceUnspecifiedDateTime = true;

        #region Creación

        private ProgramCallBuilder(IDatabaseConnection connection, string library, string program)
        {
            _connection = connection ?? throw new ArgumentNullException(nameof(connection));
            _library = EnsureText(library);
            _program = EnsureText(program);
        }

        private ProgramCallBuilder(Func<HttpContext?, DbCommand> getDbCommand, string library, string program)
        {
            _getCmd = getDbCommand ?? throw new ArgumentNullException(nameof(getDbCommand));
            _library = EnsureText(library);
            _program = EnsureText(program);
        }

        private static string EnsureText(string v)
            => string.IsNullOrWhiteSpace(v) ? throw new ArgumentNullException(nameof(v)) : v.Trim();

        /// <summary>Crea el builder usando una conexión de la librería (recomendado).</summary>
        public static ProgramCallBuilder For(IDatabaseConnection connection, string library, string program)
            => new(connection, library, program);

        /// <summary>
        /// Crea el builder usando un delegado que devuelve <see cref="DbCommand"/>.
        /// Útil si quieres evitar referenciar la interfaz directamente.
        /// </summary>
        public static ProgramCallBuilder For(Func<HttpContext?, DbCommand> getDbCommand, string library, string program)
            => new(getDbCommand, library, program);

        #endregion

        #region Configuración general

        /// <summary>Override puntual de la librería (schema).</summary>
        public ProgramCallBuilder OnLibrary(string library) { _library = EnsureText(library); return this; }

        /// <summary>Define el timeout del comando en segundos.</summary>
        public ProgramCallBuilder WithTimeout(int seconds)
        {
            _commandTimeoutSeconds = seconds >= 0 ? seconds : throw new ArgumentOutOfRangeException(nameof(seconds));
            return this;
        }

        /// <summary>Asigna un TraceId visible en <see cref="HttpContext.Items"/> con la clave <c>"TraceId"</c>.</summary>
        public ProgramCallBuilder WithTraceId(string? traceId) { _traceId = string.IsNullOrWhiteSpace(traceId) ? null : traceId; return this; }

        /// <summary>Configura reintentos ante errores transitorios con backoff.</summary>
        public ProgramCallBuilder WithRetry(int attempts, TimeSpan backoff)
        {
            _retryAttempts = Math.Max(0, attempts);
            _retryBackoff = backoff < TimeSpan.Zero ? TimeSpan.Zero : backoff;
            return this;
        }

        /// <summary>Usa convención SQL (LIB.PROG). Es la opción por defecto.</summary>
        public ProgramCallBuilder UseSqlNaming() { _naming = Naming.SqlDot; return this; }
        /// <summary>Usa convención de sistema (LIB/PROG).</summary>
        public ProgramCallBuilder UseSystemNaming() { _naming = Naming.SystemSlash; return this; }
        /// <summary>Envuelve el CALL en llaves ODBC: <c>{CALL ...}</c>. Activado por defecto.</summary>
        public ProgramCallBuilder WrapCallWithBraces(bool enable = true) { _wrapWithBraces = enable; return this; }

        /// <summary>Recorta '\0' y espacios a la derecha en OUTs de texto.</summary>
        public ProgramCallBuilder TrimOutStringPadding(bool enabled = true) { _trimOutStringPadding = enabled; return this; }
        /// <summary>Convierte cadenas vacías a <c>null</c> en OUTs.</summary>
        public ProgramCallBuilder EmptyStringAsNull(bool enabled = true) { _emptyStringAsNull = enabled; return this; }
        /// <summary>Fuerza <see cref="DateTimeKind.Unspecified"/> en OUTs de fecha/hora.</summary>
        public ProgramCallBuilder ForceUnspecifiedDateTime(bool enabled = true) { _forceUnspecifiedDateTime = enabled; return this; }

        #endregion

        #region Parámetros IN (helpers con nombre obligatorio)

        /// <summary>
        /// Agrega un parámetro IN genérico (posicional). El nombre es solo para trazas.
        /// </summary>
        public ProgramCallBuilder In(
            string name,
            object? value,
            DbType? dbType = null,
            int? size = null,
            byte? precision = null,
            byte? scale = null)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentNullException(nameof(name));

            _paramFactories.Add(cmd =>
            {
                var p = cmd.CreateParameter();
                p.ParameterName = name;                  // etiqueta para logging; el binding sigue siendo posicional
                p.Direction = ParameterDirection.Input;

                if (dbType.HasValue)    p.DbType = dbType.Value;
                if (size.HasValue)      p.Size = size.Value;
                if (precision.HasValue) p.Precision = precision.Value;
                if (scale.HasValue)     p.Scale = scale.Value;

                p.Value = value ?? DBNull.Value;
                return p;
            });
            return this;
        }

        /// <summary>IN string (VARCHAR/NVARCHAR) con nombre obligatorio.</summary>
        public ProgramCallBuilder InString(string name, string? value, int? size = null)
            => In(name, value, DbType.String, size: size);

        /// <summary>IN carácter fijo (CHAR(n)) con nombre obligatorio.</summary>
        public ProgramCallBuilder InChar(string name, string? value, int size)
            => In(name, value, DbType.AnsiStringFixedLength, size: size);

        /// <summary>IN decimal con nombre obligatorio (precisión/escala opcionales).</summary>
        public ProgramCallBuilder InDecimal(string name, decimal? value, byte? precision = null, byte? scale = null)
            => In(name, value, DbType.Decimal, precision: precision, scale: scale);

        /// <summary>IN entero de 32 bits con nombre obligatorio.</summary>
        public ProgramCallBuilder InInt32(string name, int? value)
            => In(name, value, DbType.Int32);

        /// <summary>IN fecha/hora con nombre obligatorio.</summary>
        public ProgramCallBuilder InDateTime(string name, DateTime? value)
            => In(name, value, DbType.DateTime);

        #endregion

        #region Parámetros OUT/INOUT (helpers con nombre obligatorio)

        /// <summary>
        /// Declara un parámetro OUT/INOUT. Si <paramref name="initialValue"/> se define, el parámetro actuará como INOUT.
        /// </summary>
        public ProgramCallBuilder Out(
            string name,
            DbType dbType,
            int? size = null,
            byte? precision = null,
            byte? scale = null,
            object? initialValue = null)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentNullException(nameof(name));
            _bulkOuts.Add(new OutSpec(name, dbType, size, precision, scale, initialValue));
            return this;
        }

        /// <summary>OUT string (VARCHAR/NVARCHAR) con tamaño y nombre obligatorios.</summary>
        public ProgramCallBuilder OutString(string name, int size, object? initialValue = null)
            => Out(name, DbType.String, size: size, initialValue: initialValue);

        /// <summary>OUT carácter fijo (CHAR(n)) con nombre obligatorio.</summary>
        public ProgramCallBuilder OutChar(string name, int size, object? initialValue = null)
            => Out(name, DbType.AnsiStringFixedLength, size: size, initialValue: initialValue);

        /// <summary>OUT decimal con precisión/escala y nombre obligatorios.</summary>
        public ProgramCallBuilder OutDecimal(string name, byte precision, byte scale, object? initialValue = null)
            => Out(name, DbType.Decimal, precision: precision, scale: scale, initialValue: initialValue);

        /// <summary>OUT entero de 32 bits con nombre obligatorio.</summary>
        public ProgramCallBuilder OutInt32(string name, int? initialValue = null)
            => Out(name, DbType.Int32, initialValue: initialValue);

        /// <summary>OUT fecha/hora con nombre obligatorio.</summary>
        public ProgramCallBuilder OutDateTime(string name, DateTime? initialValue = null)
            => Out(name, DbType.DateTime, initialValue: initialValue);

        #endregion

        #region Mapeo desde DTO (IN)

        /// <summary>
        /// Agrega parámetros IN posicionales a partir de un objeto,
        /// siguiendo el orden de nombres de propiedades proporcionado.
        /// Útil para llamadas con muchos IN.
        /// </summary>
        public ProgramCallBuilder FromObject(object source, IEnumerable<string> order)
        {
            if (source is null) throw new ArgumentNullException(nameof(source));
            if (order is null) throw new ArgumentNullException(nameof(order));

            var type = source.GetType();
            var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                            .ToDictionary(p => p.Name, p => p, StringComparer.OrdinalIgnoreCase);

            foreach (var name in order)
            {
                if (!props.TryGetValue(name, out var pi))
                    throw new ArgumentException($"La propiedad '{name}' no existe en {type.Name}.");

                var value = pi.GetValue(source);
                In(name, value); // reusa la API IN con nombre
            }
            return this;
        }

        #endregion

        #region Ejecución

        /// <summary>Ejecuta el <c>CALL</c> y devuelve filas afectadas + OUT/INOUT.</summary>
        public Task<ProgramCallResult> CallAsync(HttpContext? httpContext = null, CancellationToken cancellationToken = default)
            => ExecuteInternalAsync(httpContext, readerCallback: null, cancellationToken);

        /// <summary>Ejecuta el <c>CALL</c> y permite leer un result set si el programa hace <c>SELECT</c>.</summary>
        public Task<ProgramCallResult> CallAndReadAsync(HttpContext? httpContext, Func<DbDataReader, Task> readerCallback, CancellationToken cancellationToken = default)
            => ExecuteInternalAsync(httpContext, readerCallback, cancellationToken);

        private async Task<ProgramCallResult> ExecuteInternalAsync(HttpContext? httpContext, Func<DbDataReader, Task>? readerCallback, CancellationToken ct)
        {
            var sql = BuildSql();
            int attempt = 0;

            while (true)
            {
                ct.ThrowIfCancellationRequested();

                try
                {
                    // Crea el comando (según overload usado al construir el builder)
                    using var command = _getCmd is not null ? _getCmd(httpContext) : _connection!.GetDbCommand(httpContext);

                    command.CommandText = sql;
                    command.CommandType = CommandType.Text;
                    if (_commandTimeoutSeconds.HasValue) command.CommandTimeout = _commandTimeoutSeconds.Value;

                    // Enviar TraceId a logging (si lo usas en tu wrapper)
                    if (httpContext != null && !string.IsNullOrWhiteSpace(_traceId))
                        httpContext.Items["TraceId"] = _traceId;

                    // 1) IN en orden de registro
                    foreach (var factory in _paramFactories)
                        command.Parameters.Add(factory(command));

                    // 2) OUT/INOUT (van después de IN, también posicionales)
                    foreach (var o in _bulkOuts)
                    {
                        var p = command.CreateParameter();
                        p.ParameterName = o.Name;
                        p.Direction = o.Initial is null ? ParameterDirection.Output : ParameterDirection.InputOutput;
                        p.DbType = o.Type;
                        if (o.Size.HasValue)      p.Size = o.Size.Value;
                        if (o.Precision.HasValue) p.Precision = o.Precision.Value;
                        if (o.Scale.HasValue)     p.Scale = o.Scale.Value;
                        if (o.Initial is not null) p.Value = o.Initial;
                        command.Parameters.Add(p);
                    }

                    var sw = Stopwatch.StartNew();

                    var result = new ProgramCallResult();

                    if (readerCallback is null)
                    {
                        var rows = await ExecuteNonQueryAsync(command, ct).ConfigureAwait(false);
                        result.RowsAffected = rows;
                    }
                    else
                    {
                        using var reader = await ExecuteReaderAsync(command, ct).ConfigureAwait(false);
                        await readerCallback(reader).ConfigureAwait(false);
                        result.RowsAffected = reader.RecordsAffected;
                    }

                    sw.Stop();
                    if (httpContext != null) httpContext.Items["ProgramCallDurationMs"] = sw.ElapsedMilliseconds;

                    // Captura de OUT/INOUT con normalización
                    foreach (DbParameter p in command.Parameters)
                    {
                        if (p.Direction is ParameterDirection.Output or ParameterDirection.InputOutput or ParameterDirection.ReturnValue)
                        {
                            var key = string.IsNullOrWhiteSpace(p.ParameterName)
                                ? $"out_{result.OutValues.Count + 1}"
                                : p.ParameterName!;
                            result.AddOut(key, NormalizeOutValue(p.Value));
                        }
                    }

                    return result;
                }
                catch (DbException ex) when (attempt < _retryAttempts && IsTransient(ex))
                {
                    attempt++;
                    if (_retryBackoff > TimeSpan.Zero)
                        await Task.Delay(_retryBackoff, ct).ConfigureAwait(false);
                    // reintentar
                }
            }
        }

        /// <summary>
        /// Normaliza valores OUT: 
        /// - string/char[]: quita NUL (0x00) y espacios a la derecha; opcionalmente ""→null
        /// - DateTime: fuerza Kind=Unspecified si está activado
        /// - DBNull → null
        /// </summary>
        private object? NormalizeOutValue(object? raw)
        {
            if (raw is null || raw is DBNull) return null;

            switch (raw)
            {
                case string s:
                    if (_trimOutStringPadding) s = s.TrimEnd('\0', ' ');
                    if (_emptyStringAsNull && s.Length == 0) return null;
                    return s;

                case char[] chars:
                    var s2 = new string(chars);
                    if (_trimOutStringPadding) s2 = s2.TrimEnd('\0', ' ');
                    if (_emptyStringAsNull && s2.Length == 0) return null;
                    return s2;

                case DateTime dt:
                    return _forceUnspecifiedDateTime && dt.Kind != DateTimeKind.Unspecified
                        ? DateTime.SpecifyKind(dt, DateTimeKind.Unspecified)
                        : dt;

                default:
                    return raw;
            }
        }

        private string BuildSql()
        {
            // Total de placeholders = IN + OUT/INOUT (todo posicional)
            int paramCount = _paramFactories.Count + _bulkOuts.Count;
            var placeholders = paramCount == 0 ? "" : string.Join(", ", Enumerable.Repeat("?", paramCount));

            var sep = _naming == Naming.SqlDot ? "." : "/";
            var target = $"{_library}{sep}{_program}".ToUpperInvariant();
            var core = paramCount == 0 ? $"CALL {target}()" : $"CALL {target}({placeholders})";

            return _wrapWithBraces ? "{" + core + "}" : core;
        }

        private static bool IsTransient(DbException ex)
        {
            // Ajusta con tus SQLSTATE/SQLCODE según necesites
            var msg = ex.Message?.ToLowerInvariant() ?? "";
            return msg.Contains("deadlock") || msg.Contains("timeout") || msg.Contains("temporar")
                   || msg.Contains("lock") || msg.Contains("08001") || msg.Contains("08004")
                   || msg.Contains("40001") || msg.Contains("57033") || msg.Contains("57014") || msg.Contains("57016");
        }

        private static async Task<int> ExecuteNonQueryAsync(DbCommand command, CancellationToken ct)
        {
            try { return await command.ExecuteNonQueryAsync(ct).ConfigureAwait(false); }
            catch (NotSupportedException) { return await Task.Run(() => command.ExecuteNonQuery(), ct).ConfigureAwait(false); }
        }

        private static async Task<DbDataReader> ExecuteReaderAsync(DbCommand command, CancellationToken ct)
        {
            try { return await command.ExecuteReaderAsync(ct).ConfigureAwait(false); }
            catch (NotSupportedException) { return await Task.Run(() => command.ExecuteReader(), ct).ConfigureAwait(false); }
        }

        #endregion
    }
}
