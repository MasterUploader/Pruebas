using QueryBuilder.Models;
using QueryBuilder.Helpers;
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
    private readonly List<string> _columns = new();
    private readonly List<List<object?>> _rows = new();

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
    /// Genera y retorna la sentencia SQL INSERT completa.
    /// </summary>
    /// <returns>Instancia de <see cref="QueryResult"/> con el SQL generado.</returns>
    public QueryResult Build()
    {
        if (_columns.Count == 0)
            throw new InvalidOperationException("Debe especificar al menos una columna con IntoColumns.");

        if (_rows.Count == 0)
            throw new InvalidOperationException("Debe especificar al menos una fila con Values.");

        var sb = new StringBuilder();
        sb.Append("INSERT INTO ");

        if (!string.IsNullOrWhiteSpace(_library))
            sb.Append($"{_library}.");

        sb.Append(_tableName);

        sb.Append(" (");
        sb.Append(string.Join(", ", _columns));
        sb.Append(") VALUES ");

        var valueStrings = _rows.Select(row =>
            "(" + string.Join(", ", row.Select(SqlHelper.FormatValue)) + ")"
        );

        sb.Append(string.Join(", ", valueStrings));

        return new QueryResult
        {
            Sql = sb.ToString()
        };
    }
}
