El codigó de insert quedo así:

using QueryBuilder.Enums;
using QueryBuilder.Helpers;
using QueryBuilder.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace QueryBuilder.Builders;

/// <summary>
/// Generador de sentencias INSERT. Para DB2 for i usa placeholders (?) y parámetros seguros.
/// Opcionalmente soporta características específicas por dialecto (InsertIgnore/Upsert) en otros motores.
/// </summary>
/// <remarks>
/// Crea un builder de INSERT.
/// </remarks>
public class InsertQueryBuilder(string tableName, string? library = null, SqlDialect dialect = SqlDialect.Db2i)
{
    private readonly string _tableName = tableName;
    private readonly string? _library = library;
    private readonly SqlDialect _dialect = dialect;

    private readonly List<string> _columns = [];
    private readonly List<List<object?>> _rows = [];         // VALUES parametrizados
    private readonly List<List<object>> _valuesRaw = [];      // VALUES raw (funciones)
    private SelectQueryBuilder? _selectSource;

    private string? _comment;
    private string? _whereClause;
    private bool _insertIgnore = false;                          // No se emite en Db2i
    private readonly Dictionary<string, object?> _onDuplicateUpdate = []; // No se emite en Db2i

    /// <summary>
    /// Agrega un comentario SQL al inicio del comando para trazabilidad.
    /// Se sanea a una sola línea para evitar inyección de comentarios.
    /// </summary>
    public InsertQueryBuilder WithComment(string? comment)
    {
        if (string.IsNullOrWhiteSpace(comment))
            return this;

        // Saneamos: sin saltos de línea y sin secuencias peligrosas
        var sanitized = comment
            .Replace("\r", " ")
            .Replace("\n", " ")
            .Replace("--", "- -")
            .Trim();

        _comment = "-- " + sanitized;
        return this;
    }

    /// <summary>INSERT IGNORE (habilitar sólo en motores compatibles, no DB2 for i).</summary>
    public InsertQueryBuilder InsertIgnore()
    { _insertIgnore = true; return this; }

    /// <summary>Define actualización en conflicto (no DB2 for i).</summary>
    public InsertQueryBuilder OnDuplicateKeyUpdate(string column, object? value)
    { _onDuplicateUpdate[column] = value; return this; }

    /// <summary>Define varias columnas para actualización en conflicto (no DB2 for i).</summary>
    public InsertQueryBuilder OnDuplicateKeyUpdate(Dictionary<string, object?> updates)
    {
        foreach (var kvp in updates) _onDuplicateUpdate[kvp.Key] = kvp.Value;
        return this;
    }

    /// <summary>Lista de columnas del INSERT.</summary>
    public InsertQueryBuilder IntoColumns(params string[] columns)
    { _columns.Clear(); _columns.AddRange(columns); return this; }

    /// <summary>Agrega una fila de valores (parametrizados).</summary>
    public InsertQueryBuilder Values(params (string Column, object? Value)[] values)
    {
        if (_columns.Count == 0)
            _columns.AddRange(values.Select(v => v.Column));
        else if (_columns.Count != values.Length)
            throw new InvalidOperationException($"Se esperaban {_columns.Count} columnas, pero se recibieron {values.Length}.");

        _rows.Add([.. values.Select(v => v.Value)]);
        return this;
    }

    /// <summary>Agrega valores sin parámetros (por ejemplo funciones SQL).</summary>
    public InsertQueryBuilder ValuesRaw(params string[] rawValues)
    { _valuesRaw.Add([.. rawValues.Cast<object>()]); return this; }

    /// <summary>INSERT ... SELECT.</summary>
    public InsertQueryBuilder FromSelect(SelectQueryBuilder select)
    { _selectSource = select; _valuesRaw.Clear(); return this; }

    /// <summary>Condición opcional (típico en INSERT ... SELECT).</summary>
    public InsertQueryBuilder WhereNotExists(Subquery subquery)
    { _whereClause = $"NOT EXISTS ({subquery.Sql})"; return this; }

    /// <summary>
    /// Construye y retorna el SQL y parámetros.
    /// </summary>
    public QueryResult Build()
    {
        if (string.IsNullOrWhiteSpace(_tableName))
            throw new InvalidOperationException("Debe especificarse el nombre de la tabla para INSERT.");
        if (_columns.Count == 0)
            throw new InvalidOperationException("Debe especificar al menos una columna para el INSERT.");
        if (_selectSource != null && _rows.Count > 0)
            throw new InvalidOperationException("No se puede usar 'VALUES' y 'FROM SELECT' al mismo tiempo.");
        if (_selectSource == null && _rows.Count == 0 && _valuesRaw.Count == 0)
            throw new InvalidOperationException("Debe especificar al menos una fila de valores para el INSERT.");

        // Validar _rows con LINQ puro
        int? badRowIndex = _rows
            .Select((row, idx) => new { row, idx })
            .Where(x => x.row == null || x.row.Count != _columns.Count)
            .Select(x => (int?)x.idx)
            .FirstOrDefault();

        if (badRowIndex.HasValue)
        {
            int idx = badRowIndex.Value;
            int count = _rows[idx]?.Count ?? 0;
            throw new InvalidOperationException(
                $"La fila #{idx} tiene {count} valores; se esperaban {_columns.Count}."
            );
        }

        // Validar _valuesRaw con LINQ puro
        int? badRawIndex = _valuesRaw
            .Select((row, idx) => new { row, idx })
            .Where(x => x.row == null || x.row.Count != _columns.Count)
            .Select(x => (int?)x.idx)
            .FirstOrDefault();

        if (badRawIndex.HasValue)
        {
            int idx = badRawIndex.Value;
            int count = _valuesRaw[idx]?.Count ?? 0;
            throw new InvalidOperationException(
                $"La fila RAW #{idx} tiene {count} valores; se esperaban {_columns.Count}."
            );
        }

        var sb = new StringBuilder();
        var parameters = new List<object?>();

        if (!string.IsNullOrWhiteSpace(_comment))
            sb.AppendLine(_comment);

        // INSERT cabecera
        sb.Append("INSERT ");
        // En DB2 for i NO existe INSERT IGNORE; sólo habilitar para otros dialectos
        if (_insertIgnore && _dialect == SqlDialect.MySql)
            sb.Append("IGNORE ");

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
            sb.Append(" VALUES ");
            var valueLines = new List<string>();

            // Filas parametrizadas -> placeholders + lista de parámetros en orden
            foreach (var row in _rows)
            {
                var placeholders = new List<string>();
                foreach (var val in row)
                {
                    placeholders.Add("?");
                    parameters.Add(val);
                }
                valueLines.Add($"({string.Join(", ", placeholders)})");
            }

            // Filas RAW (funciones)
            foreach (var row in _valuesRaw)
                valueLines.Add($"({string.Join(", ", row.Select(SqlHelper.FormatValue))})");

            sb.Append(string.Join(", ", valueLines));
        }

        // UPSERT según dialecto (NO emitir en Db2i)
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

Necesito que valides si puedo hacer inserts que queden de esta forma una vez generados:

INSERT INTO BIBLIOTECA.Customers (CustomerName, ContactName, Address, City, PostalCode, Country)
VALUES ('Cardinal', 'Tom B. Erichsen', 'Skagen 21', 'Stavanger', '4006', 'Norway');

INSERT INTO Customers (CustomerName, City, Country)
VALUES ('Cardinal', 'Stavanger', 'Norway');

INSERT INTO Customers (CustomerName, ContactName, Address, City, PostalCode, Country)
VALUES
('Cardinal', 'Tom B. Erichsen', 'Skagen 21', 'Stavanger', '4006', 'Norway'),
('Greasy Burger', 'Per Olsen', 'Gateveien 15', 'Sandnes', '4306', 'Norway'),
('Tasty Tee', 'Finn Egan', 'Streetroad 19B', 'Liverpool', 'L1 0AA', 'UK');

Dame ejemplos de como seria la declaración.
