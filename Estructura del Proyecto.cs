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
using RestUtilities.Connections.Abstractions; // IDatabaseConnection
// ^ Ajusta este using si tu interfaz vive en otro namespace/assembly

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
    /// precisión/escala opcionales y lectura de result sets.
    /// </summary>
    public sealed class ProgramCallBuilder
    {
        // --- Estado y dependencias ---
        private readonly IDatabaseConnection? _connection;                 // cuando se usa For(connection, …)
        private readonly Func<HttpContext?, DbCommand>? _getCmd;           // cuando se usa For(getDbCommand, …)
        private string _library;
        private readonly string _program;

        // Factories de parámetros IN (se agregan en orden; OleDb/DB2 usa posicionales)
        private readonly List<Func<DbCommand, DbParameter>> _paramFactories = new();

        // OUT/INOUT como especificación (tamaño/precisión/escala)
        private readonly List<OutSpec> _bulkOuts = new();

        private int? _commandTimeoutSeconds;
        private int _retryAttempts = 0;
        private TimeSpan _retryBackoff = TimeSpan.Zero;
        private string? _traceId;

        #region Creación

        private ProgramCallBuilder(IDatabaseConnection connection, string library, string program)
        {
            _connection = connection ?? throw new ArgumentNullException(nameof(connection));
            _library = StringOrThrow(library);
            _program = StringOrThrow(program);
        }

        private ProgramCallBuilder(Func<HttpContext?, DbCommand> getDbCommand, string library, string program)
        {
            _getCmd = getDbCommand ?? throw new ArgumentNullException(nameof(getDbCommand));
            _library = StringOrThrow(library);
            _program = StringOrThrow(program);
        }

        private static string StringOrThrow(string value)
            => string.IsNullOrWhiteSpace(value) ? throw new ArgumentNullException(nameof(value)) : value.Trim();

        /// <summary>
        /// Crea un builder usando una conexión de la librería (se recomienda para integrarse con tu logging interno).
        /// </summary>
        public static ProgramCallBuilder For(IDatabaseConnection connection, string library, string program)
            => new(connection, library, program);

        /// <summary>
        /// Crea un builder usando un delegado que produce el <see cref="DbCommand"/> (útil si quieres evitar acoplarte a la interfaz).
        /// </summary>
        public static ProgramCallBuilder For(Func<HttpContext?, DbCommand> getDbCommand, string library, string program)
            => new(getDbCommand, library, program);

        #endregion

        #region Configuración

        /// <summary>Override puntual de la librería (schema) destino.</summary>
        public ProgramCallBuilder OnLibrary(string library)
        {
            _library = StringOrThrow(library);
            return this;
        }

        /// <summary>Define el timeout del comando en segundos.</summary>
        public ProgramCallBuilder WithTimeout(int seconds)
        {
            _commandTimeoutSeconds = seconds >= 0 ? seconds : throw new ArgumentOutOfRangeException(nameof(seconds));
            return this;
        }

        /// <summary>Asigna un TraceId que se expondrá en <see cref="HttpContext.Items"/> con la clave "TraceId".</summary>
        public ProgramCallBuilder WithTraceId(string? traceId)
        {
            _traceId = string.IsNullOrWhiteSpace(traceId) ? null : traceId;
            return this;
        }

        /// <summary>Configura reintentos ante errores transitorios (deadlocks, timeouts, etc.).</summary>
        public ProgramCallBuilder WithRetry(int attempts, TimeSpan backoff)
        {
            _retryAttempts = Math.Max(0, attempts);
            _retryBackoff = backoff < TimeSpan.Zero ? TimeSpan.Zero : backoff;
            return this;
        }

        #endregion

        #region Parámetros IN (básico)

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

        #endregion

        #region Parámetros OUT/INOUT (básico + masivo)

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

        /// <summary>
        /// Compatibilidad con firmas previas (solo tamaño + initial). Internamente redirige al overload completo.
        /// </summary>
        public ProgramCallBuilder Out(string name, DbType dbType, int? size = null, object? initialValue = null)
            => Out(name, dbType, size, precision: null, scale: null, initialValue: initialValue);

        /// <summary>
        /// Agrega múltiples parámetros OUT/INOUT de una sola vez (permite tamaño y precisión/escala).
        /// </summary>
        public ProgramCallBuilder OutMap(IEnumerable<OutSpec> outs)
        {
            if (outs is null) throw new ArgumentNullException(nameof(outs));
            _bulkOuts.AddRange(outs);
            return this;
        }

        /// <summary>
        /// (Compatibilidad) Agrega múltiples OUT/INOUT a partir de tuplas antiguas (sin precisión/escala).
        /// </summary>
        public ProgramCallBuilder OutMap(IEnumerable<(string Name, DbType Type, int? Size, object? Initial)> outs)
        {
            if (outs is null) throw new ArgumentNullException(nameof(outs));
            foreach (var o in outs)
                _bulkOuts.Add(new OutSpec(o.Name, o.Type, o.Size, precision: null, scale: null, initial: o.Initial));
            return this;
        }

        #endregion

        #region Helpers (IN / OUT) — uso simple y sin ambigüedades

        /// <summary>Declara un parámetro <c>OUT</c> de texto (<c>VARCHAR/NVARCHAR</c>) con tamaño fijo.</summary>
        public ProgramCallBuilder OutString(string name, int size, object? initialValue = null)
            => Out(name: name, dbType: DbType.String, size: size, precision: null, scale: null, initialValue: initialValue);

        /// <summary>Declara un parámetro <c>OUT</c> de carácter fijo (p.ej. <c>CHAR(n)</c>).</summary>
        public ProgramCallBuilder OutChar(string name, int size, object? initialValue = null)
            => Out(name: name, dbType: DbType.AnsiStringFixedLength, size: size, precision: null, scale: null, initialValue: initialValue);

        /// <summary>Declara un parámetro <c>OUT</c> decimal con precisión/escala explícitas (p.ej. <c>DEC(10,0)</c>).</summary>
        public ProgramCallBuilder OutDecimal(string name, byte precision, byte scale, object? initialValue = null)
            => Out(name: name, dbType: DbType.Decimal, size: null, precision: precision, scale: scale, initialValue: initialValue);

        /// <summary>Declara un parámetro <c>OUT</c> entero de 32 bits.</summary>
        public ProgramCallBuilder OutInt32(string name, int? initialValue = null)
            => Out(name: name, dbType: DbType.Int32, size: null, precision: null, scale: null, initialValue: initialValue);

        /// <summary>Declara un parámetro <c>OUT</c> de fecha/hora (p.ej. <c>TIMESTAMP</c>).</summary>
        public ProgramCallBuilder OutDateTime(string name, DateTime? initialValue = null)
            => Out(name: name, dbType: DbType.DateTime, size: null, precision: null, scale: null, initialValue: initialValue);

        /// <summary>Agrega un parámetro <c>IN</c> string (con tamaño opcional).</summary>
        public ProgramCallBuilder InString(string? value, int? size = null)
            => In(value, dbType: DbType.String, size: size);

        /// <summary>Agrega un parámetro <c>IN</c> carácter fijo (CHAR(n)).</summary>
        public ProgramCallBuilder InChar(string? value, int size)
            => In(value, dbType: DbType.AnsiStringFixedLength, size: size);

        /// <summary>Agrega un parámetro <c>IN</c> decimal con precisión/escala opcionales.</summary>
        public ProgramCallBuilder InDecimal(decimal? value, byte? precision = null, byte? scale = null)
            => In(value, dbType: DbType.Decimal, size: null, precision: precision, scale: scale);

        /// <summary>Agrega un parámetro <c>IN</c> entero de 32 bits.</summary>
        public ProgramCallBuilder InInt32(int? value)
            => In(value, dbType: DbType.Int32);

        /// <summary>Agrega un parámetro <c>IN</c> fecha/hora.</summary>
        public ProgramCallBuilder InDateTime(DateTime? value)
            => In(value, dbType: DbType.DateTime);

        #endregion

        #region Mapeo desde DTO (IN)

        /// <summary>
        /// Agrega parámetros <c>IN</c> posicionales a partir de un objeto origen, siguiendo el orden de nombres proporcionado.
        /// Útil para llamar programas con muchos IN sin escribir uno por uno.
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

        /// <summary>
        /// Ejecuta el <c>CALL</c> y devuelve filas afectadas + OUT/INOUT.
        /// </summary>
        public Task<ProgramCallResult> CallAsync(HttpContext? httpContext = null, CancellationToken cancellationToken = default)
            => ExecuteInternalAsync(httpContext, readerCallback: null, cancellationToken);

        /// <summary>
        /// Ejecuta el <c>CALL</c> y permite leer un result set si el programa hace <c>SELECT</c>/<c>abre cursor</c>.
        /// </summary>
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
                    using var command = _getCmd is not null
                        ? _getCmd(httpContext)
                        : _connection!.GetDbCommand(httpContext);

                    command.CommandText = sql;
                    command.CommandType = CommandType.Text;
                    if (_commandTimeoutSeconds.HasValue) command.CommandTimeout = _commandTimeoutSeconds.Value;

                    // Exponer TraceId para que tu wrapper de logging lo recoja
                    if (httpContext != null && !string.IsNullOrWhiteSpace(_traceId))
                        httpContext.Items["TraceId"] = _traceId;

                    // Parámetros IN (posicionales primero)
                    foreach (var factory in _paramFactories)
                        command.Parameters.Add(factory(command));

                    // Parámetros OUT/INOUT (posicional también, van después de los IN)
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
                    if (httpContext != null)
                        httpContext.Items["ProgramCallDurationMs"] = sw.ElapsedMilliseconds;

                    // Copiar OUT/INOUT al resultado
                    foreach (DbParameter p in command.Parameters)
                    {
                        if (p.Direction is ParameterDirection.Output or ParameterDirection.InputOutput or ParameterDirection.ReturnValue)
                        {
                            var key = string.IsNullOrWhiteSpace(p.ParameterName)
                                ? $"out_{result.OutValues.Count + 1}"
                                : p.ParameterName!;
                            result.AddOut(key, p.Value is DBNull ? null : p.Value);
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

        private string BuildSql()
        {
            // Placeholders totales = IN + OUT/INOUT (DB2/OleDb esperan todos posicionales)
            int paramCount = _paramFactories.Count + _bulkOuts.Count;
            if (paramCount == 0) return $"CALL {_library}/{_program}()";
            var placeholders = string.Join(", ", Enumerable.Repeat("?", paramCount));
            return $"CALL {_library}/{_program}({placeholders})";
        }

        private static bool IsTransient(DbException ex)
        {
            // Heurística básica (ajusta con tus SQLSTATE/SQLCODE si lo deseas)
            var msg = ex.Message?.ToLowerInvariant() ?? "";
            return msg.Contains("deadlock") || msg.Contains("timeout") || msg.Contains("temporar")
                   || msg.Contains("lock") || msg.Contains("08001") || msg.Contains("08004")
                   || msg.Contains("40001") || msg.Contains("57033") || msg.Contains("57014") || msg.Contains("57016");
        }

        private static async Task<int> ExecuteNonQueryAsync(DbCommand command, CancellationToken ct)
        {
            try { return await command.ExecuteNonQueryAsync(ct).ConfigureAwait(false); }
            catch (NotSupportedException)
            {
                // Fallback síncrono en Task para cumplir reglas async de análisis estático
                return await Task.Run(() => command.ExecuteNonQuery(), ct).ConfigureAwait(false);
            }
        }

        private static async Task<DbDataReader> ExecuteReaderAsync(DbCommand command, CancellationToken ct)
        {
            try { return await command.ExecuteReaderAsync(ct).ConfigureAwait(false); }
            catch (NotSupportedException)
            {
                return await Task.Run(() => command.ExecuteReader(), ct).ConfigureAwait(false);
            }
        }

        #endregion
    }
}

_connection.Open();

var call = await ProgramCallBuilder
    .For(_connection, "BCAH96", "PQRIFZ04CL")
    .InString(iniciarSesionDto.Email)
    .InString(iniciarSesionDto.Password)
    .OutString("NAME", 50)
    .OutString("TYPE", 1)
    .OutDecimal("ROLEDID", 10, 0)
    .OutDecimal("RESPCODE", 3, 0)
    .OutString("RESPDESCRI", 80)
    .WithTimeout(30)
    .CallAsync(_httpContextAccessor.HttpContext);

// Lectura tipada
call.TryGet<string>("NAME", out var nombre);
