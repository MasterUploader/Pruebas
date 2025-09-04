using QueryBuilder.Helpers;
using QueryBuilder.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text;

namespace QueryBuilder.Builders
{
    /// <summary>
    /// Generador de sentencias INSERT compatibles con AS400 (DB2 i) y otros motores.
    /// Soporta:
    /// - INSERT de múltiples filas
    /// - FROM SELECT
    /// - ON DUPLICATE KEY UPDATE (si el motor lo permite)
    /// - Filas RAW (funciones/expresiones)
    /// 
    /// Además, expone un modo “streaming” opcional mediante <see cref="BindCommand(DbCommand, bool, bool)"/>:
    /// cuando está activo, cada llamada a <see cref="Row(object?[])"/> agrega simultáneamente
    /// los placeholders "?" al SQL y los parámetros (en orden) al <see cref="DbCommand"/> enlazado,
    /// evitando un segundo recorrido para cargar parámetros.
    /// </summary>
    public class InsertQueryBuilder
    {
        private readonly string _tableName;
        private readonly string? _library;

        private readonly List<string> _columns = new();
        private readonly List<List<object?>> _rowsBuffer = new();   // Solo se usa en modo NO streaming
        private readonly List<List<object>> _valuesRaw = new();     // Filas RAW (sin parámetros)
        private SelectQueryBuilder? _selectSource;
        private string? _comment;
        private string? _whereClause;
        private bool _insertIgnore = false;
        private readonly Dictionary<string, object?> _onDuplicateUpdate = new();

        // --- Soporte “streaming” de parámetros ---
        private DbCommand? _boundCommand;
        private bool _autoAssignTextWhenEmpty = true;
        private bool _autoSetCommandTypeText = true;

        // Para construir el VALUES (...) en streaming:
        private readonly StringBuilder _valuesSqlSb = new();
        private int _streamingRowCount = 0;

        /// <summary>
        /// Crea una nueva instancia del builder de INSERT.
        /// </summary>
        /// <param name="tableName">Nombre de la tabla.</param>
        /// <param name="library">Biblioteca/esquema (opcional; AS400 usa LIBRERIA.TABLA).</param>
        public InsertQueryBuilder(string tableName, string? library = null)
        {
            _tableName = tableName;
            _library = library;
        }

        /// <summary>
        /// (Opcional) Enlaza un <see cref="DbCommand"/> para habilitar el modo “streaming”.
        /// En este modo, cada llamada a <see cref="Row(object?[])"/> agrega los parámetros
        /// directamente al comando y compone los placeholders en el SQL en el mismo ciclo.
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
        /// Agrega un comentario SQL al inicio del INSERT.
        /// </summary>
        public InsertQueryBuilder WithComment(string comment)
        {
            if (!string.IsNullOrWhiteSpace(comment))
                _comment = $"-- {comment}";
            return this;
        }

        /// <summary>
        /// Usa INSERT IGNORE (si el motor lo soporta).
        /// </summary>
        public InsertQueryBuilder InsertIgnore()
        {
            _insertIgnore = true;
            return this;
        }

        /// <summary>
        /// On-duplicate-key-update (si el motor lo soporta).
        /// </summary>
        public InsertQueryBuilder OnDuplicateKeyUpdate(string column, object? value)
        {
            _onDuplicateUpdate[column] = value;
            return this;
        }

        /// <summary>
        /// On-duplicate-key-update múltiple (si el motor lo soporta).
        /// </summary>
        public InsertQueryBuilder OnDuplicateKeyUpdate(Dictionary<string, object?> updates)
        {
            foreach (var kvp in updates)
                _onDuplicateUpdate[kvp.Key] = kvp.Value;
            return this;
        }

        /// <summary>
        /// Define las columnas del INSERT.
        /// </summary>
        public InsertQueryBuilder IntoColumns(params string[] columns)
        {
            _columns.Clear();
            _columns.AddRange(columns);
            return this;
        }

        /// <summary>
        /// Agrega una fila de valores (parametrizada). El orden debe coincidir con <see cref="IntoColumns"/>.
        /// En modo NO streaming, la fila se encola y los parámetros se devuelven en <see cref="Build"/>.
        /// En modo streaming (si hay <see cref="BindCommand(DbCommand, bool, bool)"/>), los parámetros se agregan
        /// al comando de inmediato y se genera el bloque VALUES de forma incremental.
        /// </summary>
        /// <param name="values">Valores en el orden de las columnas.</param>
        public InsertQueryBuilder Row(params object?[] values)
        {
            if (_columns.Count == 0)
                throw new InvalidOperationException("Debe definir columnas con IntoColumns(...) antes de invocar Row(...).");

            if (values.Length != _columns.Count)
                throw new InvalidOperationException($"Se esperaban {_columns.Count} valores, pero se recibieron {values.Length}.");

            if (_selectSource != null)
                throw new InvalidOperationException("No puede combinar Row(...) con FromSelect(...). Use uno u otro.");

            if (_boundCommand is null)
            {
                // Modo normal: acumulamos filas para luego producir parámetros y SQL al final
                _rowsBuffer.Add(values.ToList());
            }
            else
            {
                // Modo streaming: producimos placeholders y anexamos parámetros al comando aquí mismo
                if (_streamingRowCount == 0)
                    _valuesSqlSb.Append(" VALUES ");

                // placeholders por fila
                var placeholders = new string[values.Length];
                for (int i = 0; i < values.Length; i++)
                {
                    placeholders[i] = "?";

                    // Par posicional en el mismo orden
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
            }

            return this;
        }

        /// <summary>
        /// Agrega una fila RAW (sin parámetros), útil para funciones como CURRENT_DATE, etc.
        /// </summary>
        public InsertQueryBuilder RowRaw(params string[] rawValues)
        {
            if (_columns.Count == 0)
                throw new InvalidOperationException("Debe definir columnas con IntoColumns(...) antes de invocar RowRaw(...).");
            if (_selectSource != null)
                throw new InvalidOperationException("No puede combinar RowRaw(...) con FromSelect(...).");

            _valuesRaw.Add(rawValues.Cast<object>().ToList());
            return this;
        }

        /// <summary>
        /// Define un SELECT como origen del INSERT (INSERT INTO ... SELECT ...).
        /// </summary>
        public InsertQueryBuilder FromSelect(SelectQueryBuilder select)
        {
            _selectSource = select;
            _valuesRaw.Clear();
            _rowsBuffer.Clear();
            return this;
        }

        /// <summary>
        /// WHERE NOT EXISTS (...) para INSERT ... SELECT.
        /// </summary>
        public InsertQueryBuilder WhereNotExists(Subquery subquery)
        {
            _whereClause = $"NOT EXISTS ({subquery.Sql})";
            return this;
        }

        /// <summary>
        /// Construye el SQL y, en modo normal, devuelve los parámetros recolectados.
        /// En modo streaming, el SQL se construye y los parámetros ya están en el <see cref="DbCommand"/> enlazado;
        /// de forma opcional, se asigna <see cref="DbCommand.CommandText"/> y <see cref="DbCommand.CommandType"/>.
        /// </summary>
        public QueryResult Build()
        {
            if (string.IsNullOrWhiteSpace(_tableName))
                throw new InvalidOperationException("Debe especificar un nombre de tabla.");
            if (_columns.Count == 0)
                throw new InvalidOperationException("Debe especificar columnas para el INSERT.");
            if (_selectSource != null && (_rowsBuffer.Count > 0 || _valuesRaw.Count > 0))
                throw new InvalidOperationException("No puede mezclar VALUES/RAW con FROM SELECT.");

            // Validaciones de filas (modo no streaming)
            if (_boundCommand is null)
            {
                foreach (var fila in _rowsBuffer)
                {
                    if (fila.Count != _columns.Count)
                        throw new InvalidOperationException($"El número de valores ({fila.Count}) no coincide con las columnas ({_columns.Count}).");
                }
            }
            foreach (var filaRaw in _valuesRaw)
            {
                if (filaRaw.Count != _columns.Count)
                    throw new InvalidOperationException($"El número de valores RAW ({filaRaw.Count}) no coincide con las columnas ({_columns.Count}).");
            }

            var sb = new StringBuilder();
            var parameters = new List<object?>(); // solo se usa en modo NO streaming

            if (!string.IsNullOrWhiteSpace(_comment))
                sb.AppendLine(_comment);

            sb.Append("INSERT ");
            if (_insertIgnore) sb.Append("IGNORE ");
            sb.Append("INTO ");
            if (!string.IsNullOrWhiteSpace(_library))
                sb.Append($"{_library}.");
            sb.Append(_tableName);
            sb.Append(" (").Append(string.Join(", ", _columns)).Append(')');

            if (_selectSource != null)
            {
                sb.AppendLine().Append(_selectSource.Build().Sql);
                if (!string.IsNullOrWhiteSpace(_whereClause))
                    sb.Append(" WHERE ").Append(_whereClause);
            }
            else
            {
                if (_boundCommand is null)
                {
                    // Modo NO streaming: construimos VALUES y cargamos parámetros al result
                    sb.Append(" VALUES ");
                    var valueLines = new List<string>();

                    foreach (var row in _rowsBuffer)
                    {
                        var placeholders = new List<string>();
                        foreach (var val in row)
                        {
                            placeholders.Add("?");
                            parameters.Add(val);
                        }
                        valueLines.Add($"({string.Join(", ", placeholders)})");
                    }

                    foreach (var raw in _valuesRaw)
                        valueLines.Add($"({string.Join(", ", raw.Select(SqlHelper.FormatValue))})");

                    sb.Append(string.Join(", ", valueLines));
                }
                else
                {
                    // Modo streaming: ya construimos la porción de VALUES incrementales en _valuesSqlSb
                    if (_streamingRowCount == 0 && _valuesRaw.Count == 0)
                        throw new InvalidOperationException("No se agregaron filas para insertar.");

                    if (_streamingRowCount > 0)
                        sb.Append(_valuesSqlSb.ToString());

                    if (_valuesRaw.Count > 0)
                    {
                        if (_streamingRowCount > 0)
                            sb.Append(", ");

                        var rawLines = _valuesRaw
                            .Select(r => $"({string.Join(", ", r.Select(SqlHelper.FormatValue))})");
                        sb.Append(" VALUES ").Append(string.Join(", ", rawLines));
                    }
                }
            }

            if (_onDuplicateUpdate.Count > 0)
            {
                sb.Append(" ON DUPLICATE KEY UPDATE ");
                sb.Append(string.Join(", ", _onDuplicateUpdate.Select(kv =>
                    $"{kv.Key} = {SqlHelper.FormatValue(kv.Value)}")));
            }

            var sql = sb.ToString();

            // Si estamos en streaming y se pidió auto-asignación: completar el DbCommand si faltan datos
            if (_boundCommand != null)
            {
                if (_autoAssignTextWhenEmpty && string.IsNullOrWhiteSpace(_boundCommand.CommandText))
                    _boundCommand.CommandText = sql;

                if (_autoSetCommandTypeText && _boundCommand.CommandType == CommandType.Text /*default*/ )
                {
                    // ya es Text por defecto; si quisieras forzarlo cuando es Unspecified, podrías tocarlo aquí.
                    _boundCommand.CommandType = CommandType.Text;
                }

                // En streaming, los parámetros ya están en _boundCommand. Devolvemos el SQL y sin lista de parámetros.
                return new QueryResult { Sql = sql, Parameters = new List<object?>() };
            }

            // Modo normal: devolvemos el SQL y la lista de parámetros posicionales
            return new QueryResult
            {
                Sql = sql,
                Parameters = parameters
            };
        }
    }
}



using Connections.Interfaces;
using Logging.Abstractions;
using Logging.Decorators;
using Microsoft.AspNetCore.Http;
using QueryBuilder.Models;
using System;
using System.Data;
using System.Data.Common;
using System.Data.OleDb;

namespace Connections.Providers.Database
{
    /// <summary>
    /// Proveedor OleDb para AS400 (DB2 i). Puede envolver comandos con logging
    /// y ofrece una sobrecarga que recibe un <see cref="QueryResult"/> para
    /// cargar SQL/Parámetros. Si el comando no tiene CommandText/Type definidos,
    /// los asigna automáticamente (opcionalidad de configuración).
    /// </summary>
    public partial class AS400ConnectionProvider : IDatabaseConnection, IDisposable
    {
        private readonly OleDbConnection _oleDbConnection;
        private readonly ILoggingService? _loggingService;
        private readonly IHttpContextAccessor? _httpContextAccessor;

        public AS400ConnectionProvider(
            string connectionString,
            ILoggingService? loggingService = null,
            IHttpContextAccessor? httpContextAccessor = null)
        {
            _oleDbConnection = new OleDbConnection(connectionString);
            _loggingService = loggingService;
            _httpContextAccessor = httpContextAccessor;
        }

        public void Open()
        {
            if (_oleDbConnection.State != ConnectionState.Open)
                _oleDbConnection.Open();
        }

        public void Close()
        {
            if (_oleDbConnection.State != ConnectionState.Closed)
                _oleDbConnection.Close();
        }

        public bool IsConnected => _oleDbConnection?.State == ConnectionState.Open;

        public DbCommand GetDbCommand(HttpContext? context = null)
        {
            var command = _oleDbConnection.CreateCommand();

            if (_loggingService != null)
                return new LoggingDbCommandWrapper(command, _loggingService, _httpContextAccessor);

            return command;
        }

        /// <summary>
        /// Crea un comando y le aplica SQL/Parámetros en el orden provisto por <see cref="QueryResult"/>.
        /// Si el <see cref="DbCommand.CommandText"/> aún no está definido, lo asigna (opcionalidad).
        /// Si el <see cref="DbCommand.CommandType"/> no fue configurado, lo establece en <see cref="CommandType.Text"/>.
        /// </summary>
        public DbCommand GetDbCommand(QueryResult queryResult, HttpContext? context)
        {
            var command = GetDbCommand(context);

            // Asignación “opcional”: solo si está vacío.
            if (string.IsNullOrWhiteSpace(command.CommandText))
                command.CommandText = queryResult.Sql;

            // CommandType opcional: si no está definido, usa Text
            if (command.CommandType == CommandType.Text) // por defecto ya es Text, esto asegura explícitamente
                command.CommandType = CommandType.Text;

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

        public void Dispose()
        {
            _oleDbConnection?.Dispose();
        }
    }
}





