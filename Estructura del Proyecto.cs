using System;
using System.Buffers.Text;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Connections.Interfaces; // IDatabaseConnection
using Microsoft.AspNetCore.Http;

namespace RestUtilities.Connections.Helpers
{
    #region Atributos y helpers

    /// <summary>
    /// Marca una propiedad como sensible para enmascarar su valor en logs/diagnóstico.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public sealed class SensitiveAttribute : Attribute { }

    internal static class MaskingHelper
    {
        private static readonly string[] SuspiciousNames =
        {
            "password","pwd","pass","secret","token","apikey","api_key","pin","cvv","pan","card","tarjeta"
        };

        /// <summary>
        /// Devuelve un texto enmascarado si el nombre sugiere dato sensible o si el miembro está anotado con [Sensitive].
        /// </summary>
        public static string MaskIfSensitive(string logicalName, object? value, MemberInfo? sourceMember = null)
        {
            if (value is null) return "null";
            var name = logicalName?.ToLowerInvariant() ?? "";
            bool annotated = sourceMember?.GetCustomAttribute<SensitiveAttribute>() != null;
            bool heuristic = SuspiciousNames.Any(n => name.Contains(n));
            if (annotated || heuristic)
            {
                var s = value.ToString() ?? "";
                return s.Length <= 4 ? new string('*', s.Length) : $"{new string('*', Math.Max(0, s.Length - 4))}{s[^4..]}";
            }
            return value.ToString() ?? "";
        }
    }

    internal static class TypeCoercion
    {
        public static T? ChangeType<T>(object? v)
        {
            if (v is null || v is DBNull) return default;
            var t = typeof(T);
            var u = Nullable.GetUnderlyingType(t) ?? t;

            if (u.IsEnum)
            {
                if (v is string vs) return (T)Enum.Parse(u, vs, ignoreCase: true);
                return (T)Enum.ToObject(u, Convert.ChangeType(v, Enum.GetUnderlyingType(u))!);
            }
            return (T)Convert.ChangeType(v, u);
        }
    }

    #endregion

    #region Resultados y mapeo

    /// <summary>
    /// Resultado de invocar un programa CLLE/RPGLE mediante CALL.
    /// Incluye filas afectadas y parámetros OUT/INOUT.
    /// </summary>
    public sealed class ProgramCallResult
    {
        /// <summary>Filas afectadas reportadas por ExecuteNonQuery().</summary>
        public int RowsAffected { get; internal set; }

        /// <summary>Parámetros de salida (OUT/INOUT) con sus valores finales por clave lógica.</summary>
        public IReadOnlyDictionary<string, object?> OutValues => _outValues;
        private readonly Dictionary<string, object?> _outValues = new(StringComparer.OrdinalIgnoreCase);

        internal void AddOut(string name, object? value) => _outValues[name] = value;

        /// <summary>
        /// Intenta obtener un OUT por nombre fuertemente tipado.
        /// </summary>
        public bool TryGet<T>(string key, out T? value)
        {
            if (_outValues.TryGetValue(key, out var raw))
            {
                value = TypeCoercion.ChangeType<T>(raw);
                return true;
            }
            value = default;
            return false;
        }

        /// <summary>
        /// Mapea los OUT/INOUT a un DTO. Usa un diccionario "claveOUT" → "propiedadDTO".
        /// </summary>
        public T MapTo<T>(Action<OutputMapBuilder<T>> map) where T : new()
        {
            var builder = new OutputMapBuilder<T>();
            map(builder);
            var target = new T();

            foreach (var kv in builder.Bindings)
            {
                if (_outValues.TryGetValue(kv.Key, out var raw))
                {
                    var prop = kv.Value;
                    var destType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
                    object? converted = raw is null || raw is DBNull ? null : Convert.ChangeType(raw, destType);
                    prop.SetValue(target, converted);
                }
            }
            return target;
        }
    }

    /// <summary>
    /// Builder para definir el mapeo OUT → DTO.
    /// </summary>
    public sealed class OutputMapBuilder<T> where T : new()
    {
        internal Dictionary<string, PropertyInfo> Bindings { get; } = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Asocia la clave OUT "outName" a la propiedad del DTO seleccionada.
        /// </summary>
        public OutputMapBuilder<T> Bind(string outName, System.Linq.Expressions.Expression<Func<T, object?>> selector)
        {
            if (string.IsNullOrWhiteSpace(outName)) throw new ArgumentNullException(nameof(outName));
            var member = (selector.Body as System.Linq.Expressions.MemberExpression)
                         ?? (selector.Body as System.Linq.Expressions.UnaryExpression)?.Operand as System.Linq.Expressions.MemberExpression;
            if (member?.Member is not PropertyInfo pi)
                throw new InvalidOperationException("El selector debe apuntar a una propiedad.");

            Bindings[outName] = pi;
            return this;
        }
    }

    #endregion

    #region ProgramCallBuilder

    /// <summary>
    /// Builder fluido para ejecutar programas CLLE/RPGLE con sintaxis CALL LIB/PGM(?, ?, ...).
    /// Incluye mapeo desde DTO, OUT masivos, reintentos, y lectura de result sets.
    /// </summary>
    public sealed class ProgramCallBuilder
    {
        private readonly IDatabaseConnection _connection;
        private string _library;
        private readonly string _program;

        private readonly List<Func<DbCommand, DbParameter>> _paramFactories = new();
        private readonly List<(string Name, DbType Type, int? Size, object? Initial)> _bulkOuts = new();

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

        /// <summary>
        /// Crea un builder para CALL LIBRARY/PROGRAM usando la conexión de RestUtilities.
        /// </summary>
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

        /// <summary>Asigna un TraceId que tu wrapper de logging pueda propagar.</summary>
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

        /// <summary>
        /// Agrega un parámetro de entrada (IN). Usa parámetros posicionales.
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

        /// <summary>
        /// Agrega un parámetro de salida OUT/INOUT de manera individual.
        /// </summary>
        public ProgramCallBuilder Out(string name, DbType dbType, int? size = null, object? initialValue = null)
        {
            _bulkOuts.Add((name ?? $"out{_bulkOuts.Count + 1}", dbType, size, initialValue));
            return this;
        }

        /// <summary>
        /// Agrega múltiples parámetros de salida mediante un diccionario.
        /// Use tuplas (DbType,int?) para tamaño cuando aplique. Para INOUT, setea initialValue en el tercer componente.
        /// </summary>
        public ProgramCallBuilder OutMap(IEnumerable<(string Name, DbType Type, int? Size, object? Initial)> outs)
        {
            foreach (var o in outs) _bulkOuts.Add(o);
            return this;
        }

        /// <summary>
        /// Agrega IN posicionales a partir de un objeto/DTO según el orden especificado.
        /// </summary>
        /// <param name="source">Objeto origen.</param>
        /// <param name="order">Lista de nombres de propiedad en el orden deseado.</param>
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

        /// <summary>
        /// Ejecuta el CALL y devuelve filas afectadas + OUT/INOUT.
        /// </summary>
        public Task<ProgramCallResult> CallAsync(HttpContext? httpContext = null, CancellationToken cancellationToken = default)
            => ExecuteInternalAsync(httpContext, readerCallback: null, cancellationToken);

        /// <summary>
        /// Ejecuta el CALL y permite leer un result set si el programa devuelve filas (SELECT/cursores).
        /// </summary>
        public Task<ProgramCallResult> CallAndReadAsync(HttpContext? httpContext, Func<DbDataReader, Task> readerCallback, CancellationToken cancellationToken = default)
            => ExecuteInternalAsync(httpContext, readerCallback, cancellationToken);

        private async Task<ProgramCallResult> ExecuteInternalAsync(HttpContext? httpContext, Func<DbDataReader, Task>? readerCallback, CancellationToken ct)
        {
            var sql = BuildSql();

            for (int attempt = 0; ; attempt++)
            {
                ct.ThrowIfCancellationRequested();

                try
                {
                    using var command = httpContext is null
                        ? _connection.GetDbCommand()
                        : _connection.GetDbCommand(httpContext);

                    command.CommandText = sql;
                    command.CommandType = CommandType.Text;
                    if (_commandTimeoutSeconds.HasValue) command.CommandTimeout = _commandTimeoutSeconds.Value;

                    // Parámetros IN
                    foreach (var factory in _paramFactories)
                        command.Parameters.Add(factory(command));

                    // Parámetros OUT/INOUT (en orden posicional también)
                    foreach (var o in _bulkOuts)
                    {
                        var p = command.CreateParameter();
                        p.ParameterName = o.Name;
                        p.Direction = o.Initial is null ? ParameterDirection.Output : ParameterDirection.InputOutput;
                        p.DbType = o.Type;
                        if (o.Size.HasValue) p.Size = o.Size.Value;
                        if (o.Initial is not null) p.Value = o.Initial;
                        command.Parameters.Add(p);
                    }

                    var started = DateTime.UtcNow;

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

                    // (Opcional) aquí puedes aprovechar _traceId para enriquecer tus logs con tu LoggingDbCommandWrapper

                    return result;
                }
                catch (DbException ex) when (attempt < _retryAttempts && IsTransient(ex))
                {
                    // Espera exponencial lineal/simple
                    if (_retryBackoff > TimeSpan.Zero)
                        await Task.Delay(_retryBackoff, ct).ConfigureAwait(false);
                    continue; // reintenta
                }
            }
        }

        private string BuildSql()
        {
            // Conteo total de placeholders: IN + OUT/INOUT
            int paramCount = _paramFactories.Count + _bulkOuts.Count;
            if (paramCount == 0) return $"CALL {_library}/{_program}()";
            var placeholders = string.Join(", ", Enumerable.Repeat("?", paramCount));
            return $"CALL {_library}/{_program}({placeholders})";
        }

        private static bool IsTransient(DbException ex)
        {
            // Heurística básica (ajusta SQLSTATE/SQLCODE de DB2 i):
            // 57033, 57014, 57016: resource busy/cancelled; 40001: deadlock; 08001/08004: connection
            var msg = ex.Message?.ToLowerInvariant() ?? "";
            return msg.Contains("deadlock") || msg.Contains("timeout") || msg.Contains("temporar")
                   || msg.Contains("lock") || msg.Contains("08001") || msg.Contains("08004")
                   || msg.Contains("40001") || msg.Contains("57033") || msg.Contains("57014") || msg.Contains("57016");
        }

        private static async Task<int> ExecuteNonQueryAsync(DbCommand command, CancellationToken ct)
        {
            try { return await command.ExecuteNonQueryAsync(ct).ConfigureAwait(false); }
            catch (NotSupportedException) { return command.ExecuteNonQuery(); }
        }

        private static async Task<DbDataReader> ExecuteReaderAsync(DbCommand command, CancellationToken ct)
        {
            try { return await command.ExecuteReaderAsync(ct).ConfigureAwait(false); }
            catch (NotSupportedException) { return command.ExecuteReader(); }
        }
    }

    #endregion
}
