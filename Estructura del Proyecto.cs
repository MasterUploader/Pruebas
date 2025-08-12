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
using RestUtilities.Connections.Abstractions; // <— donde vive tu IDatabaseConnection

namespace RestUtilities.Connections.Helpers
{
    /// <summary>
    /// Especificación de un parámetro OUT/INOUT para programas CLLE/RPGLE.
    /// Permite tamaño (Size) y precisión/escala (Precision/Scale) opcionales.
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

        /// <summary>Nombre lógico del parámetro (clave para recuperar el valor OUT).</summary>
        public string Name { get; }
        /// <summary>Tipo de dato .NET/DbType.</summary>
        public DbType Type { get; }
        /// <summary>Tamaño (aplica a strings/binarios).</summary>
        public int? Size { get; }
        /// <summary>Precisión (dígitos totales) para decimales.</summary>
        public byte? Precision { get; }
        /// <summary>Escala (dígitos decimales) para decimales.</summary>
        public byte? Scale { get; }
        /// <summary>Valor inicial (si se define, el parámetro actúa como INOUT).</summary>
        public object? Initial { get; }
    }

    /// <summary>
    /// Builder fluido para ejecutar programas CLLE/RPGLE mediante <c>CALL LIB/PGM(?, ?, ...)</c>.
    /// Soporta parámetros IN/OUT/INOUT (posicionales), timeout, retry, trazas con <see cref="HttpContext"/>,
    /// precisión/escala opcionales, convención de nombres configurable y lectura de result sets.
    /// Además normaliza automáticamente los valores OUT más comunes (strings, fechas, etc.).
    /// </summary>
    public sealed class ProgramCallBuilder
    {
        // ====== Dependencias / Estado ======
        private readonly IDatabaseConnection? _connection;                 // Si se usa For(connection, …)
        private readonly Func<HttpContext?, DbCommand>? _getCmd;           // Si se usa For(getDbCommand, …)

        private string _library;
        private readonly string _program;

        // IN (posicionales) y OUT (con metadatos)
        private readonly List<Func<DbCommand, DbParameter>> _paramFactories = new();
        private readonly List<OutSpec> _bulkOuts = new();

        private int? _commandTimeoutSeconds;
        private int _retryAttempts = 0;
        private TimeSpan _retryBackoff = TimeSpan.Zero;
        private string? _traceId;

        // —— Opciones de SQL emitido ——
        private enum Naming { SqlDot, SystemSlash }
        private Naming _naming = Naming.SqlDot;
        private bool _wrapWithBraces = true; // {CALL ...}

        // —— Normalización de salidas ——
        private bool _trimOutStringPadding = true;     // quita '\0' y espacios a la derecha
        private bool _emptyStringAsNull = false;       // convierte "" → null
        private bool _forceUnspecifiedDateTime = true; // OUT DateTime con Kind.Unspecified

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

        /// <summary>
        /// Crea un builder usando una conexión de la librería (recomendado para integrarse con tu logging).
        /// </summary>
        public static ProgramCallBuilder For(IDatabaseConnection connection, string library, string program)
            => new(connection, library, program);

        /// <summary>
        /// Crea un builder usando un delegado que produce el <see cref="DbCommand"/> (evita acoplarse a la interfaz).
        /// </summary>
        public static ProgramCallBuilder For(Func<HttpContext?, DbCommand> getDbCommand, string library, string program)
            => new(getDbCommand, library, program);

        #endregion

        #region Configuración general

        /// <summary>Override puntual de la librería (schema) destino.</summary>
        public ProgramCallBuilder OnLibrary(string library) { _library = EnsureText(library); return this; }

        /// <summary>Define el timeout del comando en segundos.</summary>
        public ProgramCallBuilder WithTimeout(int seconds)
        {
            _commandTimeoutSeconds = seconds >= 0 ? seconds : throw new ArgumentOutOfRangeException(nameof(seconds));
            return this;
        }

        /// <summary>Asigna un TraceId que se expondrá en <see cref="HttpContext.Items"/> con la clave "TraceId".</summary>
        public ProgramCallBuilder WithTraceId(string? traceId) { _traceId = string.IsNullOrWhiteSpace(traceId) ? null : traceId; return this; }

        /// <summary>Configura reintentos ante errores transitorios (deadlocks, timeouts, etc.).</summary>
        public ProgramCallBuilder WithRetry(int attempts, TimeSpan backoff)
        {
            _retryAttempts = Math.Max(0, attempts);
            _retryBackoff = backoff < TimeSpan.Zero ? TimeSpan.Zero : backoff;
            return this;
        }

        /// <summary>Usa convención SQL (LIB.PROG). Por omisión está activa.</summary>
        public ProgramCallBuilder UseSqlNaming() { _naming = Naming.SqlDot; return this; }
        /// <summary>Usa convención de sistema (LIB/PROG).</summary>
        public ProgramCallBuilder UseSystemNaming() { _naming = Naming.SystemSlash; return this; }
        /// <summary>Envuelve el CALL en llaves ODBC: <c>{CALL ...}</c>. Por omisión está activo.</summary>
        public ProgramCallBuilder WrapCallWithBraces(bool enable = true) { _wrapWithBraces = enable; return this; }

        /// <summary>Quita <c>'\0'</c> y espacios a la derecha en OUT de texto (recomendado).</summary>
        public ProgramCallBuilder TrimOutStringPadding(bool enabled = true) { _trimOutStringPadding = enabled; return this; }
        /// <summary>Convierte OUT <c>""</c> (vacío) a <c>null</c> para tipos texto.</summary>
        public ProgramCallBuilder EmptyStringAsNull(bool enabled = true) { _emptyStringAsNull = enabled; return this; }
        /// <summary>Fuerza <see cref="DateTimeKind.Unspecified"/> en OUT de fecha/hora (evita sorpresas con zonas horarias).</summary>
        public ProgramCallBuilder ForceUnspecifiedDateTime(bool enabled = true) { _forceUnspecifiedDateTime = enabled; return this; }

        #endregion

        #region Parámetros IN (base + helpers)

        /// <summary>
        /// Agrega un parámetro <c>IN</c> posicional. Usa esta sobrecarga para casos especiales (binarios o tipos no cubiertos por helpers).
        /// </summary>
        public ProgramCallBuilder In(object? value, DbType? dbType = null, int? size = null, byte? precision = null, byte? scale = null)
        {
            _paramFactories.Add(cmd =>
            {
                var p = cmd.CreateParameter();
                p.Direction = ParameterDirection.Input;
                if (dbType.HasValue) p.DbType = dbType.Value;
                if (size.HasValue) p.Size = size.Value;
                if (precision.HasValue) p.Precision = precision.Value;
                if (scale.HasValue) p.Scale = scale.Value;
                p.Value = value ?? DBNull.Value;
                return p;
            });
            return this;
        }

        /// <summary>Agrega un <c>IN</c> string (con tamaño opcional).</summary>
        public ProgramCallBuilder InString(string? value, int? size = null) => In(value, DbType.String, size: size);
        /// <summary>Agrega un <c>IN</c> carácter fijo (CHAR(n)).</summary>
        public ProgramCallBuilder InChar(string? value, int size) => In(value, DbType.AnsiStringFixedLength, size: size);
        /// <summary>Agrega un <c>IN</c> decimal con precisión/escala opcionales.</summary>
        public ProgramCallBuilder InDecimal(decimal? value, byte? precision = null, byte? scale = null) => In(value, DbType.Decimal, precision: precision, scale: scale);
        /// <summary>Agrega un <c>IN</c> entero de 32 bits.</summary>
        public ProgramCallBuilder InInt32(int? value) => In(value, DbType.Int32);
        /// <summary>Agrega un <c>IN</c> fecha/hora.</summary>
        public ProgramCallBuilder InDateTime(DateTime? value) => In(value, DbType.DateTime);

        #endregion

        #region Parámetros OUT/INOUT (base + helpers)

        /// <summary>
        /// Agrega un parámetro <c>OUT</c> o <c>INOUT</c> con tamaño y precisión/escala opcionales.
        /// Si <paramref name="initialValue"/> se define, el parámetro actúa como <c>INOUT</c>.
        /// </summary>
        public ProgramCallBuilder Out(string name, DbType dbType, int? size = null, byte? precision = null, byte? scale = null, object? initialValue = null)
        {
            if (string.IsNullOrWhiteSpace(name)) name = $"out{_bulkOuts.Count + 1}";
            _bulkOuts.Add(new OutSpec(name, dbType, size, precision, scale, initialValue));
            return this;
        }

        /// <summary>Compat: overload corto (redirige al completo).</summary>
        public ProgramCallBuilder Out(string name, DbType dbType, int? size = null, object? initialValue = null)
            => Out(name, dbType, size, precision: null, scale: null, initialValue: initialValue);

        /// <summary>Agrega múltiples OUT/INOUT de una sola vez.</summary>
        public ProgramCallBuilder OutMap(IEnumerable<OutSpec> outs)
        {
            if (outs is null) throw new ArgumentNullException(nameof(outs));
            _bulkOuts.AddRange(outs);
            return this;
        }

        /// <summary>Compat: agrega múltiples OUT/INOUT desde tuplas (sin precisión/escala).</summary>
        public ProgramCallBuilder OutMap(IEnumerable<(string Name, DbType Type, int? Size, object? Initial)> outs)
        {
            if (outs is null) throw new ArgumentNullException(nameof(outs));
            foreach (var o in outs)
                _bulkOuts.Add(new OutSpec(o.Name, o.Type, o.Size, precision: null, scale: null, initial: o.Initial));
            return this;
        }

        // Helpers OUT — evitan ambigüedades y hacen el código legible
        /// <summary>Declara un OUT de texto (VARCHAR/NVARCHAR) con tamaño fijo.</summary>
        public ProgramCallBuilder OutString(string name, int size, object? initialValue = null)
            => Out(name: name, dbType: DbType.String, size: size, precision: null, scale: null, initialValue: initialValue);
        /// <summary>Declara un OUT de carácter fijo (CHAR(n)).</summary>
        public ProgramCallBuilder OutChar(string name, int size, object? initialValue = null)
            => Out(name: name, dbType: DbType.AnsiStringFixedLength, size: size, precision: null, scale: null, initialValue: initialValue);
        /// <summary>Declara un OUT decimal con precisión/escala.</summary>
        public ProgramCallBuilder OutDecimal(string name, byte precision, byte scale, object? initialValue = null)
            => Out(name: name, dbType: DbType.Decimal, size: null, precision: precision, scale: scale, initialValue: initialValue);
        /// <summary>Declara un OUT entero de 32 bits.</summary>
        public ProgramCallBuilder OutInt32(string name, int? initialValue = null)
            => Out(name: name, dbType: DbType.Int32, initialValue: initialValue);
        /// <summary>Declara un OUT fecha/hora.</summary>
        public ProgramCallBuilder OutDateTime(string name, DateTime? initialValue = null)
            => Out(name: name, dbType: DbType.DateTime, initialValue: initialValue);

        #endregion

        #region Mapeo desde DTO (IN)

        /// <summary>
        /// Agrega parámetros <c>IN</c> posicionales a partir de un objeto origen, siguiendo el orden de nombres proporcionado.
        /// Útil para programas con muchos IN sin escribir uno por uno.
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
                _paramFactories.Add(cmd =>
                {
                    var p = cmd.CreateParameter();
                    p.Direction = ParameterDirection.Input;
                    p.Value = value ?? DBNull.Value;
                    return p;
                });
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
                    // Crear el comando desde la conexión o el delegado, según cómo se haya construido el builder
                    using var command = _getCmd is not null ? _getCmd(httpContext) : _connection!.GetDbCommand(httpContext);

                    command.CommandText = sql;
                    command.CommandType = CommandType.Text;
                    if (_commandTimeoutSeconds.HasValue) command.CommandTimeout = _commandTimeoutSeconds.Value;

                    // Exponer TraceId para que tu wrapper de logging lo recoja
                    if (httpContext != null && !string.IsNullOrWhiteSpace(_traceId))
                        httpContext.Items["TraceId"] = _traceId;

                    // Parámetros IN (posicionales primero)
                    foreach (var factory in _paramFactories)
                        command.Parameters.Add(factory(command));

                    // Parámetros OUT/INOUT (posicional también, después de los IN)
                    foreach (var o in _bulkOuts)
                    {
                        var p = command.CreateParameter();
                        p.ParameterName = string.IsNullOrWhiteSpace(o.Name) ? $"out{command.Parameters.Count + 1}" : o.Name;
                        p.Direction = o.Initial is null ? ParameterDirection.Output : ParameterDirection.InputOutput;
                        p.DbType = o.Type;
                        if (o.Size.HasValue) p.Size = o.Size.Value;
                        if (o.Precision.HasValue) p.Precision = o.Precision.Value;
                        if (o.Scale.HasValue) p.Scale = o.Scale.Value;
                        if (o.Initial is not null) p.Value = o.Initial;
                        command.Parameters.Add(p);
                    }

                    var sw = Stopwatch.StartNew();

                    ProgramCallResult result = new();

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

                    // Copiar OUT/INOUT al resultado con NORMALIZACIÓN
                    foreach (DbParameter p in command.Parameters)
                    {
                        if (p.Direction is ParameterDirection.Output or ParameterDirection.InputOutput or ParameterDirection.ReturnValue)
                        {
                            var key = string.IsNullOrWhiteSpace(p.ParameterName) ? $"out_{result.OutValues.Count + 1}" : p.ParameterName!;
                            var normalized = NormalizeOutValue(p.Value);
                            result.AddOut(key, normalized);
                        }
                    }

                    return result;
                }
                catch (DbException ex) when (attempt < _retryAttempts && IsTransient(ex))
                {
                    attempt++;
                    if (_retryBackoff > TimeSpan.Zero)
                        await Task.Delay(_retryBackoff, ct).ConfigureAwait(false);
                    // reintenta
                }
            }
        }

        /// <summary>
        /// Normaliza valores OUT provenientes del proveedor:
        /// - <see cref="string"/>: trim de NUL (0x00) y espacios; opcionalmente ""→null
        /// - <see cref="char[]"/>: se convierte a string y se trimea igual
        /// - <see cref="DateTime"/>: opcionalmente fuerza Kind=Unspecified
        /// - <see cref="DBNull"/>: se convierte a null
        /// Otros tipos se devuelven tal cual.
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
            // Placeholders totales = IN + OUT/INOUT (DB2/OleD
