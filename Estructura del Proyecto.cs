using QueryBuilder.Enums;
using QueryBuilder.Helpers;
using QueryBuilder.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Runtime.CompilerServices; // ITuple
using System.Text;

namespace QueryBuilder.Builders
{
    /// <summary>
    /// Generador de sentencias INSERT compatibles con múltiples motores (DB2 for i, SQL Server, MySQL, etc.).
    /// Para DB2 for i se generan placeholders (?) y <see cref="QueryResult.Parameters"/> con los valores
    /// en el mismo orden, de modo que puedan ser enlazados con OleDbCommand/DbCommand de forma segura.
    /// </summary>
    /// <remarks>
    /// Características:
    /// <list type="bullet">
    /// <item><description>INSERT con columnas definidas vía <see cref="IntoColumns(string[])"/> o inferidas por atributos [ColumnName].</description></item>
    /// <item><description>Múltiples filas con <see cref="Values((string, object?)[])"/>, <see cref="Row(object?[])"/>, <see cref="Rows(IEnumerable{object?[]})"/>, <see cref="ListValues(object?[][])"/>.</description></item>
    /// <item><description>Atajos para tuplas: <see cref="ListValuesFromTuples{TTuple}(IEnumerable{TTuple})"/>.</description></item>
    /// <item><description>Atajos para objetos con atributos: <see cref="FromObject(object)"/>, <see cref="FromObjects{T}(IEnumerable{T})"/>.</description></item>
    /// <item><description>INSERT ... SELECT con <see cref="FromSelect(SelectQueryBuilder)"/> y <see cref="WhereNotExists(Subquery)"/>.</description></item>
    /// <item><description>Comentarios de trazabilidad con <see cref="WithComment(string)"/> (sanitizados).</description></item>
    /// <item><description>Soporte opcional de dialecto para <c>INSERT IGNORE</c> y <c>ON DUPLICATE KEY UPDATE</c> (no DB2 i).</description></item>
    /// </list>
    /// </remarks>
    /// <param name="tableName">Nombre de la tabla (obligatorio).</param>
    /// <param name="library">Nombre de la librería/esquema (opcional). Para DB2 i suele ser la biblioteca.</param>
    /// <param name="dialect">Dialecto SQL. Por defecto <see cref="SqlDialect.Db2i"/>.</param>
    public class InsertQueryBuilder(string tableName, string? library = null, SqlDialect dialect = SqlDialect.Db2i)
    {
        private readonly string _tableName = tableName ?? throw new ArgumentNullException(nameof(tableName));
        private readonly string? _library = library;
        private readonly SqlDialect _dialect = dialect;

        // Columnas explícitas del INSERT (en orden).
        private readonly List<string> _columns = [];

        // Filas parametrizadas (modo normal – placeholders + parámetros en Build()).
        private readonly List<List<object?>> _rows = [];

        // Filas RAW (funciones/expresiones ya formateadas, sin parámetros).
        private readonly List<List<object>> _valuesRaw = [];

        // Fuente SELECT (INSERT ... SELECT).
        private SelectQueryBuilder? _selectSource;

        // Opcionales.
        private string? _comment;
        private string? _whereClause;
        private bool _insertIgnore = false; // No se emite en Db2i
        private readonly Dictionary<string, object?> _onDuplicateUpdate = new(StringComparer.OrdinalIgnoreCase); // No Db2i

        // === Soporte de inferencia por atributos ===
        private readonly Type? _mappedType;  // si se construye por Type
        private readonly IReadOnlyList<ModelInsertMapper.ColumnMeta>? _mappedColumns;

        // === (Opcional) Modo streaming: enlaza un DbCommand para agregar parámetros y placeholders en Row(...) ===
        private DbCommand? _boundCommand;
        private bool _autoAssignTextWhenEmpty = true;
        private bool _autoSetCommandTypeText = true;
        private readonly StringBuilder _valuesSqlSb = []; // Porción incremental de VALUES(...) en modo streaming
        private int _streamingRowCount = 0;

        /// <summary>
        /// Crea el builder inferido desde atributos de clase/propiedad. Usará:
        /// [TableName], [Library] en la clase; [ColumnName] en propiedades.
        /// </summary>
        /// <param name="entityType">Tipo del DTO con atributos.</param>
        /// <param name="dialect">Dialecto (por defecto Db2i).</param>
        public InsertQueryBuilder(Type entityType, SqlDialect dialect = SqlDialect.Db2i)
            : this(
                  ModelInsertMapper.GetTableAndLibrary(entityType).table,
                  ModelInsertMapper.GetTableAndLibrary(entityType).library,
                  dialect)
        {
            _mappedType = entityType;
            _mappedColumns = ModelInsertMapper.GetColumns(entityType);
            // Nota: no agregamos columnas de inmediato; se integran en Build() si el usuario no definió IntoColumns(...)
        }

        /// <summary>
        /// Versión genérica inferida por T con atributos.
        /// </summary>
        public static InsertQueryBuilder FromType<T>(SqlDialect dialect = SqlDialect.Db2i)
            => new(typeof(T), dialect);

        /// <summary>
        /// (Opcional) Enlaza un <see cref="DbCommand"/> para habilitar el modo “streaming”.
        /// En este modo, cada llamada a <see cref="Row(object?[])"/> agrega los parámetros
        /// directamente al comando y compone los placeholders en el SQL en el mismo ciclo,
        /// evitando un segundo recorrido.
        /// </summary>
        /// <param name="command">Comando a enlazar (posicional para DB2 i / OleDb con '?').</param>
        /// <param name="assignTextIfEmpty">
        /// Si true (por defecto), al hacer <see cref="Build"/> y si <see cref="DbCommand.CommandText"/> está vacío,
        /// se asignará automáticamente el SQL generado.
        /// </param>
        /// <param name="setCommandTypeText">
        /// Si true (por defecto), al hacer <see cref="Build"/> y si el <see cref="DbCommand.CommandType"/> no fue configurado,
        /// se forzará a <see cref="CommandType.Text"/>.
        /// </param>
        public InsertQueryBuilder BindCommand(DbCommand command, bool assignTextIfEmpty = true, bool setCommandTypeText = true)
        {
            _boundCommand = command;
            _autoAssignTextWhenEmpty = assignTextIfEmpty;
            _autoSetCommandTypeText = setCommandTypeText;
            return this;
        }

        /// <summary>
        /// Agrega un comentario SQL al inicio del comando (una línea), útil para trazabilidad.
        /// Se sanitiza para evitar inyección de comentarios.
        /// </summary>
        public InsertQueryBuilder WithComment(string? comment)
        {
            if (string.IsNullOrWhiteSpace(comment))
                return this;

            // Sanitización simple de comentarios (S2681 + defensa básica)
            var sanitized = comment
                .Replace("\r", " ")
                .Replace("\n", " ")
                .Replace("--", "- -")
                .Trim();

            _comment = "-- " + sanitized;
            return this;
        }

        /// <summary>Habilita INSERT IGNORE (solo motores compatibles; no DB2 i).</summary>
        public InsertQueryBuilder InsertIgnore()
        { _insertIgnore = true; return this; }

        /// <summary>Define "ON DUPLICATE KEY UPDATE" (solo motores compatibles; no DB2 i).</summary>
        public InsertQueryBuilder OnDuplicateKeyUpdate(string column, object? value)
        { _onDuplicateUpdate[column] = value; return this; }

        /// <summary>Define múltiples columnas para "ON DUPLICATE KEY UPDATE" (solo motores compatibles; no DB2 i).</summary>
        public InsertQueryBuilder OnDuplicateKeyUpdate(Dictionary<string, object?> updates)
        {
            if (updates is null) return this;
            foreach (var kvp in updates) _onDuplicateUpdate[kvp.Key] = kvp.Value;
            return this;
        }

        /// <summary>
        /// Define la lista de columnas para el INSERT. El orden debe coincidir con los valores que se agreguen.
        /// </summary>
        /// <param name="columns">Columnas de la tabla, en el orden deseado.</param>
        /// <exception cref="ArgumentException">Si no se especifica ninguna columna.</exception>
        public InsertQueryBuilder IntoColumns(params string[] columns)
        {
            if (columns == null || columns.Length == 0)
                throw new ArgumentException("Debe especificar al menos una columna.", nameof(columns));

            _columns.Clear();
            _columns.AddRange(columns);
            return this;
        }

        /// <summary>
        /// Agrega una fila de valores (modo parametrizado) recibiendo tuplas (columna, valor).
        /// Si no se definieron columnas, se toma el orden de las tuplas.
        /// </summary>
        public InsertQueryBuilder Values(params (string Column, object? Value)[] values)
        {
            if (_columns.Count == 0)
                _columns.AddRange(values.Select(v => v.Column));
            else if (_columns.Count != values.Length)
                throw new InvalidOperationException($"Se esperaban {_columns.Count} columnas, pero se recibieron {values.Length}.");

            _rows.Add([.. values.Select(v => v.Value)]);
            return this;
        }

        /// <summary>
        /// Agrega valores RAW (por ejemplo, funciones SQL). Estos valores se insertan sin parametrizar.
        /// </summary>
        public InsertQueryBuilder ValuesRaw(params string[] rawValues)
        {
            _valuesRaw.Add([.. rawValues.Cast<object>()]);
            return this;
        }

        /// <summary>Define un SELECT como origen del INSERT (INSERT ... SELECT).</summary>
        public InsertQueryBuilder FromSelect(SelectQueryBuilder select)
        {
            _selectSource = select;
            _valuesRaw.Clear();    // Si se usa SELECT, no se mezclan VALUES RAW.
            _rows.Clear();         // Ni VALUES parametrizados.
            return this;
        }

        /// <summary>Condición opcional para INSERT ... SELECT (WHERE NOT EXISTS (...)).</summary>
        public InsertQueryBuilder WhereNotExists(Subquery subquery)
        {
            _whereClause = $"NOT EXISTS ({subquery.Sql})";
            return this;
        }

        /// <summary>
        /// Agrega UNA fila por posición (parametrizada). El orden debe coincidir con IntoColumns o,
        /// si no se definió, se infiere desde atributos [ColumnName] del tipo mapeado.
        /// </summary>
        public InsertQueryBuilder Row(params object?[] values)
        {
            // Inicialización perezosa de columnas si no se definieron explícitamente:
            if (_columns.Count == 0)
            {
                if (_mappedColumns is { Count: > 0 })
                {
                    _columns.AddRange(_mappedColumns.Select(c => c.ColumnName));
                }
                else
                {
                    throw new InvalidOperationException("Debe llamar primero a IntoColumns(...) antes de Row(...).");
                }
            }

            if (values is null || values.Length != _columns.Count)
                throw new InvalidOperationException($"Se esperaban {_columns.Count} valores, pero se recibieron {values?.Length ?? 0}.");

            // Si NO hay streaming, guardamos la fila para procesarla en Build().
            if (_boundCommand is null)
            {
                _rows.Add([.. values]);
                return this;
            }

            // Si hay streaming: placeholders + parámetros al comando en el mismo ciclo.
            if (_streamingRowCount == 0)
                _valuesSqlSb.Append(" VALUES ");

            // Creamos placeholders para la fila y, por cada valor, creamos un parámetro posicional.
            var placeholders = new string[values.Length];
            for (int i = 0; i < values.Length; i++)
            {
                placeholders[i] = "?";
                var p = _boundCommand.CreateParameter();
                p.Value = values[i] ?? DBNull.Value;
                _boundCommand.Parameters.Add(p);
            }

            if (_streamingRowCount > 0)
                _valuesSqlSb.Append(", ");

            _valuesSqlSb.Append('(')
                        .Append(string.Join(", ", placeholders))
                        .Append(')');

            _streamingRowCount++;
            return this;
        }

        /// <summary>Agrega múltiples filas por posición (parametrizadas).</summary>
        public InsertQueryBuilder Rows(IEnumerable<object?[]> rows)
        {
            if (rows is null) return this;
            foreach (var r in rows) Row(r);
            return this;
        }

        /// <summary>Azúcar sintáctico para múltiples filas usando params.</summary>
        public InsertQueryBuilder ListValues(params object?[][] rows) => Rows(rows);

        /// <summary>
        /// Agrega múltiples filas a partir de tuplas (ValueTuple). Evita construir arreglos manuales.
        /// Cada tupla aporta una fila (en orden).
        /// </summary>
        public InsertQueryBuilder ListValuesFromTuples<TTuple>(IEnumerable<TTuple> rows)
        {
            if (rows is null) return this;
            foreach (var r in rows)
            {
                if (r is not ITuple tpl)
                    throw new InvalidOperationException("Cada elemento debe ser una tupla (ValueTuple).");

                var values = new object?[tpl.Length];
                for (int i = 0; i < tpl.Length; i++) values[i] = tpl[i];
                Row(values);
            }
            return this;
        }

        /// <summary>Azúcar sintáctico (params) para tuplas.</summary>
        public InsertQueryBuilder ListValuesFromTuples<TTuple>(params TTuple[] rows)
            => ListValuesFromTuples((IEnumerable<TTuple>)rows);

        /// <summary>
        /// Agrega UNA fila desde un objeto decorado con [ColumnName] (en el orden de IntoColumns si se definió
        /// o en el orden de los atributos).
        /// </summary>
        public InsertQueryBuilder FromObject(object entity)
        {
            if (entity is null) return this;

            var t = entity.GetType();
            var cols = _mappedType == t && _mappedColumns is { Count: > 0 }
                ? _mappedColumns
                : ModelInsertMapper.GetColumns(t);

            var targetCols = _columns.Count > 0
                ? _columns
                : new List<string>(cols.Select(c => c.ColumnName));

            var map = cols.ToDictionary(c => c.ColumnName, StringComparer.OrdinalIgnoreCase);
            var row = new object?[targetCols.Count];

            for (int i = 0; i < targetCols.Count; i++)
            {
                if (!map.TryGetValue(targetCols[i], out var meta))
                    throw new InvalidOperationException($"La columna '{targetCols[i]}' no existe como [ColumnName] en {t.Name}.");
                row[i] = meta.Property.GetValue(entity);
            }

            if (_columns.Count == 0)
                _columns.AddRange(targetCols);

            return Row(row);
        }

        /// <summary>Agrega MÚLTIPLES filas desde objetos decorados con [ColumnName].</summary>
        public InsertQueryBuilder FromObjects<T>(IEnumerable<T> entities)
        {
            if (entities is null) return this;
            foreach (var e in entities) FromObject(e!);
            return this;
        }

        /// <summary>
        /// Construye el SQL final y la lista de parámetros (para motores con placeholders '?', ej. DB2 i).
        /// Integra inferencia: si no llamaste a IntoColumns(...) y el builder conoce un tipo mapeado, infiere columnas [ColumnName].
        /// </summary>
        public QueryResult Build()
        {
            // INFERENCIA de columnas si procede.
            if (_columns.Count == 0 && _mappedColumns is { Count: > 0 })
                _columns.AddRange(_mappedColumns.Select(c => c.ColumnName));

            // Validaciones base.
            if (string.IsNullOrWhiteSpace(_tableName))
                throw new InvalidOperationException("Debe especificarse el nombre de la tabla para INSERT.");
            if (_columns.Count == 0)
                throw new InvalidOperationException("Debe especificar al menos una columna para el INSERT (o usar atributos [ColumnName]).");
            if (_selectSource != null && (_rows.Count > 0 || _valuesRaw.Count > 0))
                throw new InvalidOperationException("No se puede usar 'VALUES/RAW' y 'FROM SELECT' al mismo tiempo.");
            if (_selectSource == null && _rows.Count == 0 && _valuesRaw.Count == 0 && _streamingRowCount == 0)
                throw new InvalidOperationException("Debe especificar al menos una fila de valores para el INSERT.");

            // Validaciones de filas parametrizadas (solo si NO estamos en streaming).
            if (_boundCommand is null)
            {
                int? badRowIndex = _rows
                    .Select((row, idx) => new { row, idx })
                    .Where(x => x.row == null || x.row.Count != _columns.Count)
                    .Select(x => (int?)x.idx)
                    .FirstOrDefault();

                if (badRowIndex.HasValue)
                {
                    int idx = badRowIndex.Value;
                    int count = _rows[idx]?.Count ?? 0;
                    throw new InvalidOperationException($"La fila #{idx} tiene {count} valores; se esperaban {_columns.Count}.");
                }
            }

            // Validaciones de filas RAW.
            int? badRawIndex = _valuesRaw
                .Select((row, idx) => new { row, idx })
                .Where(x => x.row == null || x.row.Count != _columns.Count)
                .Select(x => (int?)x.idx)
                .FirstOrDefault();

            if (badRawIndex.HasValue)
            {
                int idx = badRawIndex.Value;
                int count = _valuesRaw[idx]?.Count ?? 0;
                throw new InvalidOperationException($"La fila RAW #{idx} tiene {count} valores; se esperaban {_columns.Count}.");
            }

            var sb = new StringBuilder();
            var parameters = new List<object?>(); // Sólo se usa en modo normal.

            if (!string.IsNullOrWhiteSpace(_comment))
                sb.AppendLine(_comment);

            // Cabecera INSERT
            sb.Append("INSERT ");
            if (_insertIgnore && _dialect == SqlDialect.MySql)
                sb.Append("IGNORE ");

            sb.Append("INTO ");
            if (!string.IsNullOrWhiteSpace(_library))
                sb.Append($"{_library}.");
            sb.Append(_tableName);
            sb.Append(" (").Append(string.Join(", ", _columns)).Append(')');

            if (_selectSource != null)
            {
                var sel = _selectSource.Build();
                sb.AppendLine().Append(sel.Sql);

                if (!string.IsNullOrWhiteSpace(_whereClause))
                    sb.Append(" WHERE ").Append(_whereClause);
            }
            else
            {
                // Si estamos en modo normal: construimos VALUES + parámetros aquí.
                if (_boundCommand is null)
                {
                    sb.Append(" VALUES ");

                    var valueLines = new List<string>();

                    foreach (var row in _rows)
                    {
                        var placeholders = new string[row.Count];
                        for (int i = 0; i < row.Count; i++)
                        {
                            placeholders[i] = "?";
                            parameters.Add(row[i]);
                        }
                        valueLines.Add($"({string.Join(", ", placeholders)})");
                    }

                    foreach (var row in _valuesRaw)
                        valueLines.Add($"({string.Join(", ", row.Select(SqlHelper.FormatValue))})");

                    sb.Append(string.Join(", ", valueLines));
                }
                else
                {
                    // En modo streaming, ya se generó la porción VALUES(...) incrementales.
                    if (_streamingRowCount > 0)
                        sb.Append(_valuesSqlSb.ToString());

                    // Si además hay filas RAW, se anexan al final.
                    if (_valuesRaw.Count > 0)
                    {
                        if (_streamingRowCount == 0)
                            sb.Append(" VALUES ");
                        else
                            sb.Append(", ");

                        var rawLines = _valuesRaw.Select(r => $"({string.Join(", ", r.Select(SqlHelper.FormatValue))})");
                        sb.Append(string.Join(", ", rawLines));
                    }
                }
            }

            // UPSERT (solo MySQL; no DB2 i)
            if (_onDuplicateUpdate.Count > 0 && _dialect == SqlDialect.MySql)
            {
                sb.Append(" ON DUPLICATE KEY UPDATE ");
                sb.Append(string.Join(", ", _onDuplicateUpdate.Select(kv =>
                    $"{kv.Key} = {SqlHelper.FormatValue(kv.Value)}")));
            }

            var sql = sb.ToString();

            // Si estamos en streaming y se pidió autoconfigurar el comando:
            if (_boundCommand != null)
            {
                if (_autoAssignTextWhenEmpty && string.IsNullOrWhiteSpace(_boundCommand.CommandText))
                    _boundCommand.CommandText = sql;

                if (_autoSetCommandTypeText && _boundCommand.CommandType == CommandType.Text)
                {
                    // En OleDb suele ser Text por defecto, lo reafirmamos para claridad.
                    _boundCommand.CommandType = CommandType.Text;
                }

                // En streaming, los parámetros ya están en el DbCommand.
                return new QueryResult { Sql = sql, Parameters = [] };
            }

            // Modo normal: devolvemos SQL y lista de parámetros posicionales.
            return new QueryResult
            {
                Sql = sql,
                Parameters = parameters
            };
        }
    }
}



using Connections.Abstractions;
using Logging.Abstractions;
using Logging.Decorators;
using Microsoft.AspNetCore.Http;
using QueryBuilder.Models;
using System;
using System.Data;
using System.Data.Common;
using System.Data.OleDb;
using System.Runtime.Versioning;

namespace Connections.Providers.Database
{
    /// <summary>
    /// Proveedor de conexión a base de datos AS400 utilizando OleDb.
    /// Esta implementación permite la ejecución de comandos SQL con o sin logging estructurado.
    /// </summary>
    public sealed class AS400ConnectionProvider : IDatabaseConnection, IDisposable
    {
        private readonly OleDbConnection _oleDbConnection;
        private readonly ILoggingService? _loggingService;
        private readonly IHttpContextAccessor? _httpContextAccessor;

        /// <summary>
        /// Inicializa una nueva instancia de <see cref="AS400ConnectionProvider"/>.
        /// </summary>
        /// <param name="connectionString">Cadena de conexión a AS400 en formato OleDb.</param>
        /// <param name="loggingService">Servicio de logging estructurado (opcional).</param>
        /// <param name="httpContextAccessor">Accessor del contexto HTTP (opcional).</param>
        public AS400ConnectionProvider(
            string connectionString,
            ILoggingService? loggingService = null,
            IHttpContextAccessor? httpContextAccessor = null)
        {
            _oleDbConnection = new OleDbConnection(connectionString);
            _loggingService = loggingService;
            _httpContextAccessor = httpContextAccessor;
        }

        /// <inheritdoc />
        [SupportedOSPlatform("windows")]
        public void Open()
        {
            if (_oleDbConnection.State != ConnectionState.Open)
                _oleDbConnection.Open();
        }

        /// <inheritdoc />
        [SupportedOSPlatform("windows")]
        public void Close()
        {
            if (_oleDbConnection.State != ConnectionState.Closed)
                _oleDbConnection.Close();
        }

        /// <inheritdoc />
        [SupportedOSPlatform("windows")]
        public bool IsConnected => _oleDbConnection?.State == ConnectionState.Open;

        /// <inheritdoc />
        [SupportedOSPlatform("windows")]
        public DbCommand GetDbCommand(HttpContext? context = null)
        {
            var command = _oleDbConnection.CreateCommand();

            // Si el servicio de logging está disponible, devolvemos el comando decorado.
            if (_loggingService != null)
                return new LoggingDbCommandWrapper(command, _loggingService, _httpContextAccessor);

            // En caso contrario, devolvemos el comando básico.
            return command;
        }

        /// <summary>
        /// Crea un comando configurado con la consulta SQL generada por QueryBuilder y sus parámetros asociados.
        /// Si el CommandText aún no está definido, lo asigna automáticamente.
        /// Si el CommandType no fue configurado, se establece en Text.
        /// </summary>
        /// <param name="queryResult">Objeto que contiene el SQL generado y la lista de parámetros.</param>
        /// <param name="context">Contexto HTTP actual para trazabilidad opcional.</param>
        /// <returns>DbCommand listo para ejecución.</returns>
        [SupportedOSPlatform("windows")]
        public DbCommand GetDbCommand(QueryResult queryResult, HttpContext? context)
        {
            var command = GetDbCommand(context);

            // Asignación “opcional” para no interferir si ya fue configurado por el consumidor.
            if (string.IsNullOrWhiteSpace(command.CommandText))
                command.CommandText = queryResult.Sql;

            if (command.CommandType == CommandType.Text) // por defecto ya es Text; lo reafirmamos
                command.CommandType = CommandType.Text;

            // Limpiamos y reponemos los parámetros (posicionales) si vienen en QueryResult.
            command.Parameters.Clear();

            if (queryResult.Parameters is not null && queryResult.Parameters.Count > 0)
            {
                foreach (var paramValue in queryResult.Parameters)
                {
                    var parameter = command.CreateParameter();
                    parameter.Value = paramValue ?? DBNull.Value;
                    command.Parameters.Add(parameter);
                }
            }

            return command;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            _oleDbConnection?.Dispose();
        }
    }
}
