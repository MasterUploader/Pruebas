using QueryBuilder.Helpers;
using QueryBuilder.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace QueryBuilder.Builders;

/// <summary>
/// Generador de sentencias INSERT compatibles con AS400 y otros motores.
/// Permite insertar en una tabla simple, con valores directos o subconsultas.
/// </summary>
public class InsertQueryBuilder
{
    private readonly string _tableName;
    private readonly string? _library;
    private readonly List<string> _columns = [];
    private readonly List<List<object?>> _rows = [];
    private readonly List<List<object>> _values = [];
    private SelectQueryBuilder? _selectSource;
    private string? _comment;
    private string? _whereClause;
    private bool _ignoreDuplicates = false;
    private readonly Dictionary<string, object?> _onDuplicateKeyUpdates = [];

    /// <summary>
    /// Inicializa una nueva instancia del constructor de sentencias INSERT.
    /// </summary>
    /// <param name="tableName">Nombre de la tabla.</param>
    /// <param name="library">Nombre de la librería o esquema (opcional).</param>
    public InsertQueryBuilder(string tableName, string? library = null)
    {
        _tableName = tableName;
        _library = library;
    }

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
    /// Define las columnas que se desean insertar.
    /// </summary>
    public InsertQueryBuilder IntoColumns(params string[] columns)
    {
        _columns.Clear();
        _columns.AddRange(columns);
        return this;
    }

    /// <summary>
    /// Agrega una fila de valores para insertar.
    /// El orden debe coincidir con las columnas definidas.
    /// </summary>
    public InsertQueryBuilder Values(params object?[] values)
    {
        if (values.Length != _columns.Count)
            throw new InvalidOperationException($"Se esperaban {_columns.Count} valores, pero se recibieron {values.Length}.");
        _rows.Add(values.ToList());
        return this;
    }

    /// <summary>
    /// Agrega una fila de valores SQL sin formato (crudo), útil para funciones como GETDATE().
    /// </summary>
    public InsertQueryBuilder ValuesRaw(params string[] values)
    {
        _values.Add(values.Cast<object>().ToList());
        return this;
    }

    /// <summary>
    /// Define una subconsulta SELECT como fuente de datos del INSERT.
    /// </summary>
    public InsertQueryBuilder FromSelect(SelectQueryBuilder select)
    {
        _selectSource = select;
        _values.Clear(); // Se eliminan valores si se usa FROM SELECT
        return this;
    }

    /// <summary>
    /// Agrega una cláusula WHERE NOT EXISTS con una subconsulta.
    /// Solo aplicable a INSERT ... SELECT.
    /// </summary>
    public InsertQueryBuilder WhereNotExists(Subquery subquery)
    {
        _whereClause = $"NOT EXISTS ({subquery.Sql})";
        return this;
    }

    /// <summary>
    /// Indica que se deben ignorar filas duplicadas (simula INSERT IGNORE).
    /// </summary>
    public InsertQueryBuilder IgnoreDuplicates()
    {
        _ignoreDuplicates = true;
        return this;
    }

    /// <summary>
    /// Define las columnas a actualizar si ocurre duplicado (equivalente a ON DUPLICATE KEY UPDATE).
    /// </summary>
    public InsertQueryBuilder OnDuplicateKeyUpdate(string column, object? value)
    {
        _onDuplicateKeyUpdates[column] = value;
        return this;
    }

    /// <summary>
    /// Construye y retorna la consulta INSERT generada.
    /// </summary>
    public QueryResult Build()
    {
        if (string.IsNullOrWhiteSpace(_tableName))
            throw new InvalidOperationException("Debe especificarse el nombre de la tabla para INSERT.");

        if (_columns.Count == 0)
            throw new InvalidOperationException("Debe especificar al menos una columna para el INSERT.");

        if (_selectSource != null && _values.Count > 0)
            throw new InvalidOperationException("No se puede usar 'VALUES' y 'FROM SELECT' al mismo tiempo.");

        if (_selectSource == null && _values.Count == 0)
            throw new InvalidOperationException("Debe especificar al menos una fila de valores para el INSERT.");

        foreach (var fila in _values)
        {
            if (fila.Count != _columns.Count)
                throw new InvalidOperationException($"El número de valores ({fila.Count}) no coincide con las columnas ({_columns.Count}).");
        }

        var sb = new StringBuilder();

        if (!string.IsNullOrWhiteSpace(_comment))
            sb.AppendLine(_comment);

        sb.Append("INSERT ");

        if (_ignoreDuplicates)
            sb.Append("IGNORE "); // Para otros motores como MySQL

        sb.Append($"INTO {_tableName} (");
        sb.Append(string.Join(", ", _columns));
        sb.Append(")");

        if (_selectSource != null)
        {
            sb.AppendLine();
            sb.Append(_selectSource.Build().Sql);

            if (!string.IsNullOrWhiteSpace(_whereClause))
            {
                sb.AppendLine();
                sb.Append("WHERE ");
                sb.Append(_whereClause);
            }
        }
        else
        {
            var valuesSql = _values
                .Select(row => $"({string.Join(", ", row.Select(SqlHelper.FormatValue))})");
            sb.Append(" VALUES ");
            sb.Append(string.Join(", ", valuesSql));
        }

        // ON DUPLICATE KEY UPDATE
        if (_onDuplicateKeyUpdates.Count > 0)
        {
            sb.AppendLine();
            sb.Append("ON DUPLICATE KEY UPDATE ");
            sb.Append(string.Join(", ", _onDuplicateKeyUpdates.Select(kvp =>
                $"{kvp.Key} = {SqlHelper.FormatValue(kvp.Value)}")));
        }

        return new QueryResult
        {
            Sql = sb.ToString()
        };
    }
}
