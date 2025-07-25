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
    /// Construye y retorna la consulta INSERT generada junto con los parámetros.
    /// </summary>
    public QueryResult Build()
    {
        if (string.IsNullOrWhiteSpace(_tableName))
            throw new InvalidOperationException("Debe especificarse el nombre de la tabla para INSERT.");

        if (_columns.Count == 0)
            throw new InvalidOperationException("Debe especificar al menos una columna para el INSERT.");

        if (_selectSource != null && _rows.Count > 0)
            throw new InvalidOperationException("No se puede usar 'VALUES' y 'FROM SELECT' al mismo tiempo.");

        if (_selectSource == null && _rows.Count == 0 && _values.Count == 0)
            throw new InvalidOperationException("Debe especificar al menos una fila de valores para el INSERT.");

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
            var selectResult = _selectSource.Build();
            sb.Append(selectResult.Sql);
            parameters.AddRange(selectResult.Parameters);

            if (!string.IsNullOrWhiteSpace(_whereClause))
                sb.Append($" WHERE {_whereClause}");
        }
        else if (_rows.Count > 0)
        {
            sb.Append(" VALUES ");
            var placeholders = _rows.Select(row =>
            {
                parameters.AddRange(row);
                return "(" + string.Join(", ", row.Select(_ => "?")) + ")";
            });
            sb.Append(string.Join(", ", placeholders));
        }
        else if (_values.Count > 0)
        {
            var valuesSql = _values.Select(row =>
                "(" + string.Join(", ", row.Select(SqlHelper.FormatValue)) + ")");
            sb.Append(" VALUES ");
            sb.Append(string.Join(", ", valuesSql));
        }

        if (_onDuplicateUpdate.Count > 0)
        {
            sb.Append(" ON DUPLICATE KEY UPDATE ");
            sb.Append(string.Join(", ", _onDuplicateUpdate.Select(kv =>
            {
                parameters.Add(kv.Value);
                return $"{kv.Key} = ?";
            })));
        }

        return new QueryResult
        {
            Sql = sb.ToString(),
            Parameters = parameters
        };
    }
}
