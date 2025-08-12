_connection.Open();

var call = await ProgramCallBuilder
    .For(_connection, "BCAH96", "PQRIFZ04CL")
    // IN
    .In(iniciarSesionDto.Email,    DbType.String)
    .In(iniciarSesionDto.Password, DbType.String)
    // OUT / INOUT con precisión/escala opcionales
    .Out("NAME",       DbType.String, size: 50)
    .Out("TYPE",       DbType.String, size: 1)
    .Out("ROLEDID",    DbType.Decimal, precision: 10, scale: 0) // opcionales
    .Out("RESPCODE",   DbType.Decimal, precision: 3, scale: 0)  // opcionales
    .Out("RESPDESCRI", DbType.String, size: 80)
    .WithTimeout(30)
    .CallAsync(_httpContextAccessor.HttpContext);

// Lectura directa
call.TryGet<string>("NAME",        out var nombre);
call.TryGet<string>("TYPE",        out var tipo);
call.TryGet<decimal>("ROLEDID",    out var roleId);
call.TryGet<decimal>("RESPCODE",   out var codigoRespuesta);
call.TryGet<string>("RESPDESCRI",  out var descripcionRespuesta);




using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Connections.Interfaces;
using Microsoft.AspNetCore.Http;

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

        public string Name { get; }
        public DbType Type { get; }
        public int? Size { get; }
        public byte? Precision { get; }
        public byte? Scale { get; }
        public object? Initial { get; }
    }

    /// <summary>
    /// Builder fluido para ejecutar programas CLLE/RPGLE con sintaxis CALL LIB/PGM(?, ?, ...).
    /// Incluye mapeo desde DTO, OUT masivos, reintentos, lectura de result sets y
    /// precisión/escala opcionales para parámetros decimales.
    /// </summary>
    public sealed class ProgramCallBuilder
    {
        private readonly IDatabaseConnection _connection;
        private string _library;
        private readonly string _program;

        private readonly List<Func<DbCommand, DbParameter>> _paramFactories = new();

        // Ahora almacenamos OUT/INOUT con Size + Precision + Scale
        private readonly List<OutSpec> _bulkOuts = new();

        private int? _commandTimeoutSeconds;
        private int _retryAttempts = 0;
        private TimeSpan _retryBackoff = TimeSpan.Zero;
        private string? _traceId;

        private ProgramCallBuilder(IDatabaseConnection connection, string library, string program)
        {
            _connection = connection ?? throw new ArgumentNullException(nameof(connection));
            _library = string.IsNullOrWhiteSpace(library) ? throw new ArgumentNullException(nameof(library)) : library.Trim();
            _program = string.IsNullOrWhiteSpace(program) ? throw new ArgumentNullException(nameof(program)) : program.Trim();
        }

        /// <summary>Crea un builder para CALL LIBRARY/PROGRAM usando la conexión de RestUtilities.</summary>
        public static ProgramCallBuilder For(IDatabaseConnection connection, string library, string program)
            => new(connection, library, program);

        /// <summary>Override puntual de librería.</summary>
        public ProgramCallBuilder OnLibrary(string library)
        {
            _library = string.IsNullOrWhiteSpace(library) ? throw new ArgumentNullException(nameof(library)) : library.Trim();
            return this;
        }

        /// <summary>Define timeout del comando en segundos.</summary>
        public ProgramCallBuilder WithTimeout(int seconds)
        {
            _commandTimeoutSeconds = seconds >= 0 ? seconds : throw new ArgumentOutOfRangeException(nameof(seconds));
            return this;
        }

        /// <summary>Asigna un TraceId que puede ser recogido por tu LoggingDbCommandWrapper.</summary>
        public ProgramCallBuilder WithTraceId(string? traceId)
        {
            _traceId = string.IsNullOrWhiteSpace(traceId) ? null : traceId;
            return this;
        }

        /// <summary>Configura reintentos ante errores transitorios.</summary>
        public ProgramCallBuilder WithRetry(int attempts, TimeSpan backoff)
        {
            _retryAttempts = Math.Max(0, attempts);
            _retryBackoff = backoff < TimeSpan.Zero ? TimeSpan.Zero : backoff;
            return this;
        }

        /// <summary>Agrega un parámetro de entrada (IN) posicional.</summary>
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

        /// <summary>
        /// Agrega un parámetro OUT/INOUT con tamaño, precisión y escala opcionales.
        /// </summary>
        public ProgramCallBuilder Out(string name, DbType dbType, int? size = null, byte? precision = null, byte? scale = null, object? initialValue = null)
        {
            if (string.IsNullOrWhiteSpace(name)) name = $"out{_bulkOuts.Count + 1}";
            _bulkOuts.Add(new OutSpec(name, dbType, size, precision, scale, initialValue));
            return this;
        }

        /// <summary>
        /// Mantén compatibilidad con firmas previas (sin precisión/escala).
        /// </summary>
        public ProgramCallBuilder Out(string name, DbType dbType, int? size = null, object? initialValue = null)
            => Out(name, dbType, size, null, null, initialValue);

        /// <summary>
        /// Agrega múltiples OUT/INOUT de una sola vez con soporte de precisión/escala.
        /// </summary>
        public ProgramCallBuilder OutMap(IEnumerable<OutSpec> outs)
        {
            if (outs is null) throw new ArgumentNullException(nameof(outs));
            _bulkOuts.AddRange(outs);
            return this;
        }

        /// <summary>
        /// (Compat) Agrega múltiples OUT/INOUT usando tuplas antiguas (sin precisión/escala).
        /// </summary>
        public ProgramCallBuilder OutMap(IEnumerable<(string Name, DbType Type, int? Size, object? Initial)> outs)
        {
            if (outs is null) throw new ArgumentNullException(nameof(outs));
            foreach (var o in outs)
                _bulkOuts.Add(new OutSpec(o.Name, o.Type, o.Size, null, null, o.Initial));
            return this;
        }

        /// <summary>
        /// Agrega IN posicionales desde un DTO según el orden indicado.
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

        /// <summary>Ejecuta el CALL y devuelve filas afectadas + OUT/INOUT.</summary>
        public Task<ProgramCallResult> CallAsync(HttpContext? httpContext = null, CancellationToken cancellationToken = default)
            => ExecuteInternalAsync(httpContext, readerCallback: null, cancellationToken);

        /// <summary>Ejecuta el CALL y permite leer un result set si el programa devuelve filas.</summary>
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
                    using var command = _connection.GetDbCommand(httpContext);
                    command.CommandText = sql;
                    command.CommandType = CommandType.Text;
                    if (_commandTimeoutSeconds.HasValue) command.CommandTimeout = _commandTimeoutSeconds.Value;

                    // Expone TraceId (si lo hay) para que el wrapper lo registre
                    if (httpContext != null && !string.IsNullOrWhiteSpace(_traceId))
                        httpContext.Items["TraceId"] = _traceId;

                    // IN
                    foreach (var factory in _paramFactories)
                        command.Parameters.Add(factory(command));

                    // OUT/INOUT (posicional también)
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

                    // Recupera OUT/INOUT
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
                    continue;
                }
            }
        }

        private string BuildSql()
        {
            int paramCount = _paramFactories.Count + _bulkOuts.Count;
            if (paramCount == 0) return $"CALL {_library}/{_program}()";
            var placeholders = string.Join(", ", Enumerable.Repeat("?", paramCount));
            return $"CALL {_library}/{_program}({placeholders})";
        }

        private static bool IsTransient(DbException ex)
        {
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
    }
}
