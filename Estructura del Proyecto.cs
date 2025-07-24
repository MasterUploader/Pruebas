using QueryBuilder.Helpers;
using QueryBuilder.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace QueryBuilder.Builders;

/// <summary>
/// Generador de sentencias INSERT compatibles con AS400 y otros motores.
/// Permite insertar en una tabla simple, con valores directos sin parámetros.
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

    /// <summary>
    /// Agrega un comentario SQL al inicio del INSERT para trazabilidad o debugging.
    /// </summary>
    /// <param name="comment">Texto del comentario.</param>
    /// <returns>Instancia modificada de <see cref="InsertQueryBuilder"/>.</returns>
    public InsertQueryBuilder WithComment(string comment)
    {
        if (!string.IsNullOrWhiteSpace(comment))
            _comment = $"-- {comment}";
        return this;
    }

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
    /// Agrega una cláusula WHERE NOT EXISTS con una subconsulta, aplicable en INSERT ... SELECT.
    /// </summary>
    /// <param name="subquery">Subconsulta a evaluar en NOT EXISTS.</param>
    /// <returns>Instancia modificada de <see cref="InsertQueryBuilder"/>.</returns>
    public InsertQueryBuilder WhereNotExists(Subquery subquery)
    {
        _whereClause = $"NOT EXISTS ({subquery.Sql})";
        return this;
    }

    /// <summary>
    /// Agrega una fila de valores sin formato automático, útil para funciones como GETDATE().
    /// </summary>
    /// <param name="values">Valores SQL en crudo, como funciones o expresiones directas.</param>
    /// <returns>Instancia modificada de <see cref="InsertQueryBuilder"/>.</returns>
    public InsertQueryBuilder ValuesRaw(params string[] values)
    {
        _values.Add(values.Cast<object>().ToList());
        return this;
    }

    /// <summary>
    /// Define las columnas que se desean insertar.
    /// </summary>
    /// <param name="columns">Nombres de las columnas.</param>
    /// <returns>Instancia modificada del builder.</returns>
    public InsertQueryBuilder IntoColumns(params string[] columns)
    {
        _columns.Clear();
        _columns.AddRange(columns);
        return this;
    }

    /// <summary>
    /// Agrega una fila de valores para insertar.
    /// El orden debe coincidir con el de las columnas definidas en <see cref="IntoColumns"/>.
    /// </summary>
    /// <param name="values">Valores a insertar en una fila.</param>
    /// <returns>Instancia modificada del builder.</returns>
    public InsertQueryBuilder Values(params object?[] values)
    {
        if (values.Length != _columns.Count)
            throw new InvalidOperationException($"Se esperaban {_columns.Count} valores, pero se recibieron {values.Length}.");

        _rows.Add(values.ToList());
        return this;
    }

    

    /// <summary>
    /// Define una subconsulta SELECT como fuente de datos a insertar.
    /// Anula el uso de valores directos.
    /// </summary>
    /// <param name="select">Instancia de <see cref="SelectQueryBuilder"/> que genera el SELECT.</param>
    public InsertQueryBuilder FromSelect(SelectQueryBuilder select)
    {
        _selectSource = select;
        _values.Clear(); // Se eliminan valores si se usa FROM SELECT
        return this;
    }

    /// <summary>
    /// Construye y retorna la consulta INSERT generada.
    /// </summary>
    public QueryResult Build()
    {
        // Validaciones básicas
        if (string.IsNullOrWhiteSpace(_tableName))
            throw new InvalidOperationException("Debe especificarse el nombre de la tabla para INSERT.");

        if (_columns.Count == 0)
            throw new InvalidOperationException("Debe especificar al menos una columna para el INSERT.");

        if (_selectSource != null && _values.Count > 0)
            throw new InvalidOperationException("No se puede usar 'VALUES' y 'FROM SELECT' al mismo tiempo.");

        if (_selectSource == null)
        {
            if (_values.Count == 0)
                throw new InvalidOperationException("Debe especificar al menos una fila de valores para el INSERT.");

            foreach (var fila in _values)
            {
                if (fila.Count != _columns.Count)
                    throw new InvalidOperationException($"El número de valores ({fila.Count}) no coincide con las columnas ({_columns.Count}).");
            }
        }

        var sb = new StringBuilder();

        if (!string.IsNullOrWhiteSpace(_comment))
            sb.AppendLine(_comment);

        // Parte inicial del INSERT
        sb.Append($"INSERT INTO {_tableName} (");
        sb.Append(string.Join(", ", _columns));
        sb.Append(")");


        if (_selectSource != null)
        {
            sb.AppendLine();
            sb.Append(_selectSource.Build().Sql);
        }
        else
        {
            var valuesSql = _values
                .Select(row => $"({string.Join(", ", row.Select(SqlHelper.FormatValue))})");
            sb.Append(" VALUES ");
            sb.Append(string.Join(", ", valuesSql));
        }

        return new QueryResult
        {
            Sql = sb.ToString()
        };
    }
}
