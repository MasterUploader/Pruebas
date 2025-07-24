using QueryBuilder.Helpers;
using QueryBuilder.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace QueryBuilder.Builders;

/// <summary>
/// Generador de sentencias UPDATE compatible con AS400 y otros motores.
/// Permite definir columnas a actualizar, condiciones WHERE, expresiones CASE y subconsultas.
/// </summary>
public class UpdateQueryBuilder
{
    private readonly string _tableName;
    private readonly string? _library;
    private readonly Dictionary<string, object?> _setValues = new();
    private readonly Dictionary<string, string> _setRawExpressions = new();
    private readonly List<(string Column, string CaseExpression)> _setCaseExpressions = [];
    private string? _whereClause;
    private string? _comment;

    /// <summary>
    /// Inicializa una nueva instancia de <see cref="UpdateQueryBuilder"/>.
    /// </summary>
    /// <param name="tableName">Nombre de la tabla a actualizar.</param>
    /// <param name="library">Nombre de la librería o esquema (opcional).</param>
    public UpdateQueryBuilder(string tableName, string? library = null)
    {
        _tableName = tableName;
        _library = library;
    }

    /// <summary>
    /// Agrega un comentario SQL al inicio de la consulta UPDATE.
    /// </summary>
    /// <param name="comment">Texto del comentario.</param>
    public UpdateQueryBuilder WithComment(string comment)
    {
        if (!string.IsNullOrWhiteSpace(comment))
            _comment = $"-- {comment}";
        return this;
    }

    /// <summary>
    /// Establece el valor para una columna.
    /// </summary>
    /// <param name="column">Nombre de la columna.</param>
    /// <param name="value">Valor a asignar.</param>
    public UpdateQueryBuilder Set(string column, object? value)
    {
        _setValues[column] = value;
        return this;
    }

    /// <summary>
    /// Establece un valor sin formato para una columna (por ejemplo: GETDATE()).
    /// </summary>
    /// <param name="column">Nombre de la columna.</param>
    /// <param name="rawSql">Expresión SQL cruda.</param>
    public UpdateQueryBuilder SetRaw(string column, string rawSql)
    {
        _setRawExpressions[column] = rawSql;
        return this;
    }

    /// <summary>
    /// Establece una expresión CASE WHEN como valor de una columna.
    /// </summary>
    /// <param name="column">Columna a actualizar.</param>
    /// <param name="caseExpression">Expresión CASE generada con <see cref="CaseWhenBuilder"/>.</param>
    public UpdateQueryBuilder SetCase(string column, string caseExpression)
    {
        _setCaseExpressions.Add((column, caseExpression));
        return this;
    }

    /// <summary>
    /// Agrega una cláusula WHERE.
    /// </summary>
    /// <param name="clause">Condición completa.</param>
    public UpdateQueryBuilder Where(string clause)
    {
        if (string.IsNullOrWhiteSpace(_whereClause))
            _whereClause = clause;
        else
            _whereClause += $" AND {clause}";

        return this;
    }

    /// <summary>
    /// Agrega una cláusula WHERE con expresión CASE WHEN.
    /// </summary>
    /// <param name="caseBuilder">Builder de CASE WHEN.</param>
    /// <param name="comparison">Comparación final (ej: = 1).</param>
    public UpdateQueryBuilder WhereCase(CaseWhenBuilder caseBuilder, string comparison)
    {
        var condition = $"{caseBuilder.Build()} {comparison}";
        return Where(condition);
    }

    /// <summary>
    /// Agrega una cláusula WHERE EXISTS con una subconsulta.
    /// </summary>
    /// <param name="subquery">Subconsulta a evaluar.</param>
    public UpdateQueryBuilder WhereExists(Subquery subquery)
    {
        return Where($"EXISTS ({subquery.Sql})");
    }

    /// <summary>
    /// Agrega una cláusula WHERE NOT EXISTS con una subconsulta.
    /// </summary>
    /// <param name="subquery">Subconsulta a evaluar.</param>
    public UpdateQueryBuilder WhereNotExists(Subquery subquery)
    {
        return Where($"NOT EXISTS ({subquery.Sql})");
    }

    /// <summary>
    /// Construye y retorna el SQL generado.
    /// </summary>
    public QueryResult Build()
    {
        if (string.IsNullOrWhiteSpace(_tableName))
            throw new InvalidOperationException("Debe especificarse el nombre de la tabla para UPDATE.");

        if (_setValues.Count == 0 && _setRawExpressions.Count == 0 && _setCaseExpressions.Count == 0)
            throw new InvalidOperationException("Debe definir al menos una columna a actualizar con SET.");

        var sb = new StringBuilder();

        if (!string.IsNullOrWhiteSpace(_comment))
            sb.AppendLine(_comment);

        sb.Append("UPDATE ");
        if (!string.IsNullOrWhiteSpace(_library))
            sb.Append($"{_library}.");
        sb.Append(_tableName);

        sb.Append(" SET ");

        var setParts = new List<string>();

        foreach (var (col, val) in _setValues)
            setParts.Add($"{col} = {SqlHelper.FormatValue(val)}");

        foreach (var (col, sql) in _setRawExpressions)
            setParts.Add($"{col} = {sql}");

        foreach (var (col, caseExp) in _setCaseExpressions)
            setParts.Add($"{col} = {caseExp}");

        sb.Append(string.Join(", ", setParts));

        if (!string.IsNullOrWhiteSpace(_whereClause))
            sb.Append($" WHERE {_whereClause}");

        return new QueryResult
        {
            Sql = sb.ToString()
        };
    }
}
