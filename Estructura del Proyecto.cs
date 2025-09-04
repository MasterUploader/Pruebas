Para las mejoras ten a consideración el código que tengo actualmente, estas son extras y no deben afectar el funcionamiento actual del codigo:

using QueryBuilder.Enums;
using QueryBuilder.Helpers;
using QueryBuilder.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices; // ITuple
using System.Text;

namespace QueryBuilder.Builders;

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
/// <remarks>
/// Crea un nuevo generador de sentencia INSERT.
/// </remarks>
/// <param name="tableName">Nombre de la tabla (obligatorio).</param>
/// <param name="library">Nombre de la librería/esquema (opcional). Para DB2 i suele ser la biblioteca.</param>
/// <param name="dialect">Dialecto SQL. Por defecto <see cref="SqlDialect.Db2i"/>.</param>
public class InsertQueryBuilder(string tableName, string? library = null, SqlDialect dialect = SqlDialect.Db2i)
{
    private readonly string _tableName = tableName ?? throw new ArgumentNullException(nameof(tableName));
    private readonly string? _library = library;
    private readonly SqlDialect _dialect = dialect;

    private readonly List<string> _columns = [];
    private readonly List<List<object?>> _rows = [];     // VALUES parametrizados (placeholders)
    private readonly List<List<object>> _valuesRaw = [];  // VALUES RAW (funciones SQL ya formateadas)
    private SelectQueryBuilder? _selectSource;

    private string? _comment;
    private string? _whereClause;
    private bool _insertIgnore = false; // No se emite en Db2i
    private readonly Dictionary<string, object?> _onDuplicateUpdate = new(StringComparer.OrdinalIgnoreCase); // No Db2i

    // === Soporte de inferencia por atributos ===
    private readonly Type? _mappedType;  // si se construye por Type
    private readonly IReadOnlyList<ModelInsertMapper.ColumnMeta>? _mappedColumns;

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

        // Nota: NO agregamos columnas aquí para permitir que IntoColumns(...) reemplace si el usuario lo desea.
        // La inferencia se integrará en Build() sólo si el usuario NO definió IntoColumns(...)
    }

    /// <summary>
    /// Versión genérica inferida por T con atributos.
    /// </summary>
    public static InsertQueryBuilder FromType<T>(SqlDialect dialect = SqlDialect.Db2i)
        => new(typeof(T), dialect);

    // ----------------- Configuración / API fluida -----------------

    /// <summary>
    /// Agrega un comentario SQL al inicio del comando (una línea), útil para trazabilidad.
    /// Se sanitiza para evitar inyección de comentarios.
    /// </summary>
    public InsertQueryBuilder WithComment(string? comment)
    {
        if (string.IsNullOrWhiteSpace(comment))
            return this;

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

    /// <summary>Agrega una fila de valores (modo parametrizado) recibiendo tuplas (columna, valor).</summary>
    public InsertQueryBuilder Values(params (string Column, object? Value)[] values)
    {
        if (_columns.Count == 0)
            _columns.AddRange(values.Select(v => v.Column));
        else if (_columns.Count != values.Length)
            throw new InvalidOperationException($"Se esperaban {_columns.Count} columnas, pero se recibieron {values.Length}.");

        _rows.Add([.. values.Select(v => v.Value)]);
        return this;
    }

    /// <summary>Agrega valores RAW (por ejemplo funciones SQL). Estos valores se insertan sin parametrizar.</summary>
    public InsertQueryBuilder ValuesRaw(params string[] rawValues)
    { _valuesRaw.Add([.. rawValues.Cast<object>()]); return this; }

    /// <summary>INSERT ... SELECT.</summary>
    public InsertQueryBuilder FromSelect(SelectQueryBuilder select)
    { _selectSource = select; _valuesRaw.Clear(); return this; }

    /// <summary>Condición opcional para INSERT ... SELECT.</summary>
    public InsertQueryBuilder WhereNotExists(Subquery subquery)
    { _whereClause = $"NOT EXISTS ({subquery.Sql})"; return this; }

    // --------- NUEVOS ATAJOS ---------

    /// <summary>
    /// Agrega una fila por posición (parametrizada). El orden debe coincidir con IntoColumns
    /// o, si no se definió, se infiere desde los atributos [ColumnName].
    /// </summary>
    public InsertQueryBuilder Row(params object?[] values)
    {
        // Inicialización perezosa de columnas:
        if (_columns.Count == 0)
        {
            if (_mappedColumns is { Count: > 0 })
            {
                _columns.AddRange(_mappedColumns.Select(c => c.ColumnName));
            }
            else
            {
                // Último intento: si Row fue llamado sin constructor por Type,
                // no sabemos de dónde inferir; en ese caso sí lanzamos.
                throw new InvalidOperationException("Debe llamar primero a IntoColumns(...) antes de Row(...).");
            }
        }

        if (values is null || values.Length != _columns.Count)
            throw new InvalidOperationException($"Se esperaban {_columns.Count} valores, pero se recibieron {values?.Length ?? 0}.");

        _rows.Add([.. values]);
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

    /// <summary>Agrega múltiples filas a partir de tuplas (ValueTuple). Evita construir object[] manuales.</summary>
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
    /// Agrega UNA fila desde un objeto decorado con [ColumnName] (en el orden de IntoColumns si se definió,
    /// de lo contrario en el orden de los atributos).
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

        // Asegura columnas antes de llamar a Row
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

    // ----------------- Build (con INFERENCIA integrada) -----------------

    /// <summary>
    /// Construye el SQL final y la lista de parámetros (para motores que usan placeholders '?', ej. DB2 i).
    /// Integra inferencia: si no llamaste a IntoColumns(...) y el builder conoce un tipo mapeado, infiere columnas [ColumnName].
    /// </summary>
    public QueryResult Build()
    {
        // INFERENCIA: si no definiste columnas pero el builder fue creado con Type (o ya sabe columnas mapeadas), infiérelas.
        if (_columns.Count == 0 && _mappedColumns is { Count: > 0 })
            _columns.AddRange(_mappedColumns.Select(c => c.ColumnName));

        // Validaciones base
        if (string.IsNullOrWhiteSpace(_tableName))
            throw new InvalidOperationException("Debe especificarse el nombre de la tabla para INSERT.");
        if (_columns.Count == 0)
            throw new InvalidOperationException("Debe especificar al menos una columna para el INSERT (o usar atributos [ColumnName]).");
        if (_selectSource != null && _rows.Count > 0)
            throw new InvalidOperationException("No se puede usar 'VALUES' y 'FROM SELECT' al mismo tiempo.");
        if (_selectSource == null && _rows.Count == 0 && _valuesRaw.Count == 0)
            throw new InvalidOperationException("Debe especificar al menos una fila de valores para el INSERT.");

        // Validar _rows
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

        // Validar _valuesRaw
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
        var parameters = new List<object?>();

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
            sb.Append(" VALUES ");

            var valueLines = new List<string>();

            // Filas parametrizadas: placeholders + parámetros
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

            // Filas RAW (funciones/literales ya formateados)
            foreach (var row in _valuesRaw)
                valueLines.Add($"({string.Join(", ", row.Select(SqlHelper.FormatValue))})");

            sb.Append(string.Join(", ", valueLines));
        }

        // UPSERT (no DB2 i)
        if (_onDuplicateUpdate.Count > 0 && _dialect == SqlDialect.MySql)
        {
            sb.Append(" ON DUPLICATE KEY UPDATE ");
            sb.Append(string.Join(", ", _onDuplicateUpdate.Select(kv =>
                $"{kv.Key} = {SqlHelper.FormatValue(kv.Value)}")));
        }

        return new QueryResult
        {
            Sql = sb.ToString(),
            Parameters = parameters
        };
    }
}





using Connections.Abstractions;
using Logging.Abstractions;
using Logging.Decorators;
using Microsoft.AspNetCore.Http;
using QueryBuilder.Models;
using System.Data.Common;
using System.Data.OleDb;
using System.Runtime.Versioning;

namespace Connections.Providers.Database;

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
        if (_oleDbConnection.State != System.Data.ConnectionState.Open)
            _oleDbConnection.Open();
    }

    /// <inheritdoc />
    [SupportedOSPlatform("windows")]
    public void Close()
    {
        if (_oleDbConnection.State != System.Data.ConnectionState.Closed)
            _oleDbConnection.Close();
    }

    /// <inheritdoc />
    [SupportedOSPlatform("windows")]
    public bool IsConnected => _oleDbConnection?.State == System.Data.ConnectionState.Open;

    /// <inheritdoc />
    [SupportedOSPlatform("windows")]
    public DbCommand GetDbCommand(HttpContext? context = null)
    {
        var command = _oleDbConnection.CreateCommand();

        // Si el servicio de logging está disponible, devolvemos el comando decorado
        if (_loggingService != null)
        {
            return new LoggingDbCommandWrapper(command, _loggingService, _httpContextAccessor);
        }

        // En caso contrario, devolvemos el comando básico
        return command;
    }

    /// <summary>
    /// Crea un comando configurado con la consulta SQL generada por QueryBuilder y sus parámetros asociados.
    /// </summary>
    /// <param name="queryResult">Objeto que contiene el SQL generado y la lista de parámetros.</param>
    /// <param name="context">Contexto HTTP actual para trazabilidad opcional.</param>
    /// <returns>DbCommand listo para ejecución.</returns>
    [SupportedOSPlatform("windows")]
    public DbCommand GetDbCommand(QueryResult queryResult, HttpContext? context)
    {
        var command = GetDbCommand(context);

        // Establece el SQL
        command.CommandText = queryResult.Sql;

        // Limpia cualquier parámetro anterior
        command.Parameters.Clear();

        // Agrega los parámetros a la posición correspondiente
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



Ademas siempre coloca comentarios XML y comentarios en linea detallados, sobre las funcionalidades existentes no sobre lo que te pido que mejores.
Cuando Declares listas o arreglos usa el formato nuevo y simplificado el cual usa List lista = []; por ejemplo.
