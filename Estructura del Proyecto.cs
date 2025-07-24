using QueryBuilder.Helpers;
using QueryBuilder.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace QueryBuilder.Builders;

/// <summary>
/// Generador de sentencias INSERT compatibles con AS400 y otros motores.
/// Soporta valores directos, inserciones basadas en SELECT y cláusulas CTE (WITH).
/// </summary>
public class InsertQueryBuilder
{
    private readonly string _tableName;
    private readonly string? _library;
    private readonly List<string> _columns = [];
    private readonly List<List<object>> _values = [];
    private SelectQueryBuilder? _selectSource;
    private readonly List<CommonTableExpression> _ctes = [];
    private string? _comment;
    private string? _whereClause;

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
    /// <param name="comment">Texto del comentario. Se pueden usar saltos de línea.</param>
    /// <returns>Instancia modificada de <see cref="InsertQueryBuilder"/>.</returns>
    public InsertQueryBuilder WithComment(string comment)
    {
        if (!string.IsNullOrWhiteSpace(comment))
            _comment = string.Join(Environment.NewLine, comment.Split('\n').Select(line => $"-- {line.Trim()}"));
        return this;
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
    /// Define columnas para la cláusula INSERT.
    /// </summary>
    /// <param name="columns">Lista de nombres de columnas.</param>
    /// <returns>Instancia modificada del builder.</returns>
    public InsertQueryBuilder IntoColumns(params string[] columns)
    {
        _columns.Clear();
        _columns.AddRange(columns);
        return this;
    }

    /// <summary>
    /// Agrega una fila de valores a insertar. El orden debe coincidir con <see cref="IntoColumns"/>.
    /// </summary>
    /// <param name="values">Valores a insertar en una fila.</param>
    /// <returns>Instancia modificada del builder.</returns>
    public InsertQueryBuilder Values(params object[] values)
    {
        if (values.Length != _columns.Count)
            throw new InvalidOperationException($"Se esperaban {_columns.Count} valores, pero se recibieron {values.Length}.");

        _values.Add(values.ToList());
        return this;
    }

    /// <summary>
    /// Agrega una fila de valores sin formato automático, útil para funciones como GETDATE().
    /// </summary>
    /// <param name="values">Valores SQL crudos.</param>
    public InsertQueryBuilder ValuesRaw(params string[] values)
    {
        _values.Add(values.Cast<object>().ToList());
        return this;
    }

    /// <summary>
    /// Define una subconsulta SELECT como fuente de datos del INSERT.
    /// </summary>
    /// <param name="select">Instancia de <see cref="SelectQueryBuilder"/>.</param>
    public InsertQueryBuilder FromSelect(SelectQueryBuilder select)
    {
        _selectSource = select;
        _values.Clear(); // Se descartan valores directos si se usa SELECT
        return this;
    }

    /// <summary>
    /// Agrega una o más expresiones CTE (WITH) al inicio de la consulta.
    /// Solo válido para INSERT ... SELECT.
    /// </summary>
    /// <param name="ctes">Expresiones de tipo <see cref="CommonTableExpression"/>.</param>
    public InsertQueryBuilder With(params CommonTableExpression[] ctes)
    {
        if (ctes != null && ctes.Length > 0)
            _ctes.AddRange(ctes);

        return this;
    }

    /// <summary>
    /// Construye la sentencia INSERT final.
    /// </summary>
    /// <returns>Instancia con la consulta SQL generada.</returns>
    public QueryResult Build()
    {
        if (string.IsNullOrWhiteSpace(_tableName))
            throw new InvalidOperationException("Debe especificarse el nombre de la tabla para INSERT.");

        if (_columns.Count == 0)
            throw new InvalidOperationException("Debe especificar al menos una columna para el INSERT.");

        if (_selectSource != null && _values.Count > 0)
            throw new InvalidOperationException("No se puede usar 'VALUES' y 'FROM SELECT' al mismo tiempo.");

        if (_selectSource == null && _values.Count == 0)
            throw new InvalidOperationException("Debe especificar al menos una fila de valores o un SELECT.");

        foreach (var row in _values)
        {
            if (row.Count != _columns.Count)
                throw new InvalidOperationException($"El número de valores ({row.Count}) no coincide con las columnas ({_columns.Count}).");
        }

        var sb = new StringBuilder();

        // Comentario inicial
        if (!string.IsNullOrWhiteSpace(_comment))
            sb.AppendLine(_comment);

        // WITH (CTE)
        if (_ctes.Count > 0)
        {
            sb.Append("WITH ");
            sb.AppendLine(string.Join(", ", _ctes.Select(cte => cte.ToString())));
        }

        // INSERT INTO esquema.tabla (col1, col2)
        sb.Append("INSERT INTO ");
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
            var valuesSql = _values.Select(row =>
                $"({string.Join(", ", row.Select(SqlHelper.FormatValue))})");
            sb.Append(string.Join(", ", valuesSql));
        }

        return new QueryResult
        {
            Sql = sb.ToString()
        };
    }
}
