El codigo lo tengo así actualmente:

using QueryBuilder.Helpers;
using QueryBuilder.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace QueryBuilder.Builders;

/// <summary>
/// Generador de sentencias INSERT compatibles con AS400 y otros motores.
/// Permite insertar datos con VALUES, SELECT, IGNORE, condiciones NOT EXISTS y actualización en conflicto.
/// Genera parámetros automáticamente para uso con DbCommand.
/// </summary>
public class InsertQueryBuilder(string _tableName, string? _library = null)
{
    private readonly List<string> _columns = [];
    private readonly List<List<object?>> _rows = [];
    private readonly List<List<object>> _values = [];
    private SelectQueryBuilder? _selectSource;
    private string? _comment;
    private string? _whereClause;
    private bool _insertIgnore = false;
    private readonly Dictionary<string, object?> _onDuplicateUpdate = [];

    /// <summary>
    /// Agrega un comentario SQL al inicio del INSERT para trazabilidad o debugging.
    /// </summary>
    public InsertQueryBuilder WithComment(string comment)
    {
        if (!string.IsNullOrWhiteSpace(comment))
            _comment = $"-- {comment}";
        return this;
    }

    /// <summary>
    /// Indica que se debe usar INSERT IGNORE para omitir errores de duplicado (si lo permite el motor).
    /// </summary>
    public InsertQueryBuilder InsertIgnore()
    {
        _insertIgnore = true;
        return this;
    }

    /// <summary>
    /// Define columnas que deben actualizarse si hay conflicto de clave duplicada.
    /// </summary>
    public InsertQueryBuilder OnDuplicateKeyUpdate(string column, object? value)
    {
        _onDuplicateUpdate[column] = value;
        return this;
    }

    /// <summary>
    /// Define múltiples columnas para actualizar en caso de duplicado.
    /// </summary>
    public InsertQueryBuilder OnDuplicateKeyUpdate(Dictionary<string, object?> updates)
    {
        foreach (var kvp in updates)
            _onDuplicateUpdate[kvp.Key] = kvp.Value;

        return this;
    }

    /// <summary>
    /// Define las columnas que se desean insertar.
    /// </summary>
    public InsertQueryBuilder IntoColumns(params string[] columns)
    {
        _columns.Clear();
        _columns.AddRange(columns);
        return this;
    }

    /// <summary>
    /// Agrega una fila de valores a insertar, junto con los nombres de las columnas en forma de tuplas.
    /// </summary>
    public InsertQueryBuilder Values(params (string Column, object? Value)[] values)
    {
        if (_columns.Count == 0)
        {
            _columns.AddRange(values.Select(v => v.Column));
        }
        else if (_columns.Count != values.Length)
        {
            throw new InvalidOperationException($"Se esperaban {_columns.Count} columnas, pero se recibieron {values.Length}.");
        }

        var row = values.Select(v => v.Value).ToList();
        _rows.Add(row);
        return this;
    }

    /// <summary>
    /// Agrega una fila de valores sin formato automático, útil para funciones como GETDATE().
    /// </summary>
    public InsertQueryBuilder ValuesRaw(params string[] values)
    {
        _values.Add(values.Cast<object>().ToList());
        return this;
    }

    /// <summary>
    /// Define una subconsulta SELECT como fuente de datos a insertar.
    /// </summary>
    public InsertQueryBuilder FromSelect(SelectQueryBuilder select)
    {
        _selectSource = select;
        _values.Clear(); // Se eliminan valores si se usa FROM SELECT
        return this;
    }

    /// <summary>
    /// Agrega una cláusula WHERE NOT EXISTS con una subconsulta, aplicable en INSERT ... SELECT.
    /// </summary>
    public InsertQueryBuilder WhereNotExists(Subquery subquery)
    {
        _whereClause = $"NOT EXISTS ({subquery.Sql})";
        return this;
    }

    /// <summary>
    /// Construye y retorna la consulta INSERT generada junto con la lista de parámetros si corresponde.
    /// </summary>
    public QueryResult Build()
    {
        // Validaciones básicas
        if (string.IsNullOrWhiteSpace(_tableName))
            throw new InvalidOperationException("Debe especificarse el nombre de la tabla para INSERT.");

        if (_columns.Count == 0)
            throw new InvalidOperationException("Debe especificar al menos una columna para el INSERT.");

        if (_selectSource != null && _rows.Count > 0)
            throw new InvalidOperationException("No se puede usar 'VALUES' y 'FROM SELECT' al mismo tiempo.");

        if (_selectSource == null && _rows.Count == 0 && _values.Count == 0)
            throw new InvalidOperationException("Debe especificar al menos una fila de valores para el INSERT.");

        // Validar filas manuales
        foreach (var fila in _rows)
        {
            if (fila.Count != _columns.Count)
                throw new InvalidOperationException($"El número de valores ({fila.Count}) no coincide con las columnas ({_columns.Count}).");
        }

        // Validar valores RAW si existen
        foreach (var fila in _values)
        {
            if (fila.Count != _columns.Count)
                throw new InvalidOperationException($"El número de valores RAW ({fila.Count}) no coincide con las columnas ({_columns.Count}).");
        }

        var sb = new StringBuilder();
        var parameters = new List<object?>();

        // Comentario si se agregó
        if (!string.IsNullOrWhiteSpace(_comment))
            sb.AppendLine(_comment);

        // Cláusula INSERT
        sb.Append("INSERT ");
        if (_insertIgnore) sb.Append("IGNORE ");
        sb.Append("INTO ");
        if (!string.IsNullOrWhiteSpace(_library))
            sb.Append($"{_library}.");
        sb.Append(_tableName);
        sb.Append(" (");
        sb.Append(string.Join(", ", _columns));
        sb.Append(")");

        if (_selectSource != null)
        {
            sb.AppendLine();
            sb.Append(_selectSource.Build().Sql);

            if (!string.IsNullOrWhiteSpace(_whereClause))
                sb.Append($" WHERE {_whereClause}");
        }
        else
        {
            sb.Append(" VALUES ");

            var valueLines = new List<string>();

            foreach (var row in _rows)
            {
                var placeholders = new List<string>();

                foreach (var value in row)
                {
                    placeholders.Add("?");
                    parameters.Add(value); // Aquí agregamos a la lista de parámetros
                }

                valueLines.Add($"({string.Join(", ", placeholders)})");
            }

            // También soportamos filas de valores RAW (ej: funciones SQL)
            foreach (var row in _values)
            {
                valueLines.Add($"({string.Join(", ", row.Select(SqlHelper.FormatValue))})");
            }

            sb.Append(string.Join(", ", valueLines));
        }

        // ON DUPLICATE KEY UPDATE
        if (_onDuplicateUpdate.Count > 0)
        {
            sb.Append(" ON DUPLICATE KEY UPDATE ");
            sb.Append(string.Join(", ", _onDuplicateUpdate.Select(kv =>
            {
                if (kv.Value is string str && str.Trim().StartsWith("(")) // función como (GETDATE())
                    return $"{kv.Key} = {str}";
                return $"{kv.Key} = {SqlHelper.FormatValue(kv.Value)}";
            })));
        }

        return new QueryResult
        {
            Sql = sb.ToString(),
            Parameters = parameters // Se asignan los parámetros aquí
        };
    }
}

using Connections.Interfaces;
using Logging.Abstractions;
using Logging.Decorators;
using Microsoft.AspNetCore.Http;
using QueryBuilder.Models;
using System.Data.Common;
using System.Data.OleDb;

namespace Connections.Providers.Database;

/// <summary>
/// Proveedor de conexión a base de datos AS400 utilizando OleDb.
/// Esta implementación permite la ejecución de comandos SQL con o sin logging estructurado.
/// </summary>
public partial class AS400ConnectionProvider : IDatabaseConnection, IDisposable
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
    public void Open()
    {
        if (_oleDbConnection.State != System.Data.ConnectionState.Open)
            _oleDbConnection.Open();
    }

    /// <inheritdoc />
    public void Close()
    {
        if (_oleDbConnection.State != System.Data.ConnectionState.Closed)
            _oleDbConnection.Close();
    }

    /// <inheritdoc />
    public bool IsConnected => _oleDbConnection?.State == System.Data.ConnectionState.Open;

    /// <inheritdoc />
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

Asegurate de usar placeholders, pero tambien asegurate de que los valores se llenen correctamente.
