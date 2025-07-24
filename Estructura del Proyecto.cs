namespace QueryBuilder.Helpers;

/// <summary>
/// Representa funciones SQL agregadas como COUNT, SUM, etc.
/// </summary>
public static class SqlFunction
{
    /// <summary>
    /// Devuelve COUNT(columna).
    /// </summary>
    public static (string Column, string? Alias) Count(string column, string? alias = null) =>
        ($"COUNT({column})", alias);

    /// <summary>
    /// Devuelve SUM(columna).
    /// </summary>
    public static (string Column, string? Alias) Sum(string column, string? alias = null) =>
        ($"SUM({column})", alias);

    /// <summary>
    /// Devuelve AVG(columna).
    /// </summary>
    public static (string Column, string? Alias) Avg(string column, string? alias = null) =>
        ($"AVG({column})", alias);

    /// <summary>
    /// Devuelve MIN(columna).
    /// </summary>
    public static (string Column, string? Alias) Min(string column, string? alias = null) =>
        ($"MIN({column})", alias);

    /// <summary>
    /// Devuelve MAX(columna).
    /// </summary>
    public static (string Column, string? Alias) Max(string column, string? alias = null) =>
        ($"MAX({column})", alias);
}


using QueryBuilder.Enums;
using QueryBuilder.Expressions;
using QueryBuilder.Models;
using QueryBuilder.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace QueryBuilder.Builders;

/// <summary>
/// Generador de consultas SELECT compatible con AS400.
/// Soporta alias, ORDER BY, FETCH FIRST, GROUP BY y HAVING.
/// </summary>
public class SelectQueryBuilder
{
    internal string? WhereClause { get; set; }

    private readonly string _tableName;
    private readonly string? _library;
    private string? _tableAlias;
    private readonly List<(string Column, string? Alias)> _columns = [];
    private string? _whereClause;
    private readonly List<string> _orderBy = [];
    private int? _limit;
    private readonly List<JoinClause> _joins = [];
    private readonly List<string> _groupBy = [];
    private string? _havingClause;

    /// <summary>
    /// Inicializa una nueva instancia de <see cref="SelectQueryBuilder"/>.
    /// </summary>
    /// <param name="tableName">Nombre de la tabla.</param>
    /// <param name="library">Nombre de la librería en AS400 (opcional).</param>
    public SelectQueryBuilder(string tableName, string? library = null)
    {
        _tableName = tableName;
        _library = library;
    }

    /// <summary>
    /// Define un alias para la tabla principal.
    /// </summary>
    public SelectQueryBuilder As(string alias)
    {
        _tableAlias = alias;
        return this;
    }

    /// <summary>
    /// Define las columnas a seleccionar (sin alias).
    /// </summary>
    public SelectQueryBuilder Select(params string[] columns)
    {
        foreach (var column in columns)
            _columns.Add((column, null));
        return this;
    }

    /// <summary>
    /// Define columnas con alias mediante tuplas (Columna, Alias).
    /// </summary>
    public SelectQueryBuilder Select(params (string Column, string Alias)[] columns)
    {
        foreach (var (col, alias) in columns)
            _columns.Add((col, alias));
        return this;
    }

    /// <summary>
    /// Agrega una condición WHERE a la consulta usando expresiones lambda.
    /// </summary>
    /// <typeparam name="T">Tipo de entidad.</typeparam>
    /// <param name="expression">Expresión booleana.</param>
    public SelectQueryBuilder Where<T>(Expression<Func<T, bool>> expression)
    {
        LambdaWhereTranslator.Translate(this, expression);
        return this;
    }

    /// <summary>
    /// Ordena por una sola columna.
    /// </summary>
    public SelectQueryBuilder OrderBy(string column, SortDirection direction = SortDirection.Asc)
    {
        _orderBy.Add($"{column} {direction.ToString().ToUpper()}");
        return this;
    }

    /// <summary>
    /// Ordena por múltiples columnas con dirección individual.
    /// </summary>
    public SelectQueryBuilder OrderBy(params (string Column, SortDirection Direction)[] columns)
    {
        foreach (var (column, direction) in columns)
            _orderBy.Add($"{column} {direction.ToString().ToUpper()}");
        return this;
    }

    /// <summary>
    /// Establece el número máximo de filas que se devolverán.
    /// </summary>
    public SelectQueryBuilder Limit(int rowCount)
    {
        _limit = rowCount;
        return this;
    }

    /// <summary>
    /// Agrega una cláusula JOIN a la consulta.
    /// </summary>
    public SelectQueryBuilder Join(
        string table,
        string? library,
        string alias,
        string leftColumn,
        string rightColumn,
        string joinType = "INNER")
    {
        _joins.Add(new JoinClause
        {
            JoinType = joinType.ToUpper(),
            TableName = table,
            Library = library,
            Alias = alias,
            LeftColumn = leftColumn,
            RightColumn = rightColumn
        });

        return this;
    }

    /// <summary>
    /// Agrega columnas para la cláusula GROUP BY.
    /// </summary>
    /// <param name="columns">Columnas a agrupar.</param>
    public SelectQueryBuilder GroupBy(params string[] columns)
    {
        _groupBy.AddRange(columns);
        return this;
    }

    /// <summary>
    /// Agrega una cláusula HAVING usando una expresión lambda.
    /// </summary>
    /// <typeparam name="T">Tipo de entidad.</typeparam>
    /// <param name="expression">Expresión booleana para HAVING.</param>
    public SelectQueryBuilder Having<T>(Expression<Func<T, bool>> expression)
    {
        if (_groupBy.Count == 0)
            throw new InvalidOperationException("No se puede usar HAVING sin definir previamente GROUP BY.");

        _havingClause = ExpressionToSqlConverter.Convert(expression);
        return this;
    }

    /// <summary>
    /// Construye y devuelve el SQL generado.
    /// </summary>
    public QueryResult Build()
    {
        var sb = new StringBuilder();

        // SELECT
        sb.Append("SELECT ");

        if (_columns.Count == 0)
        {
            sb.Append("*");
        }
        else
        {
            var columnSql = _columns.Select(c =>
                string.IsNullOrWhiteSpace(c.Alias)
                    ? c.Column
                    : $"{c.Column} AS {c.Alias}"
            );
            sb.Append(string.Join(", ", columnSql));
        }

        // FROM
        sb.Append(" FROM ");
        if (!string.IsNullOrWhiteSpace(_library))
            sb.Append($"{_library}.");
        sb.Append(_tableName);
        if (!string.IsNullOrWhiteSpace(_tableAlias))
            sb.Append($" {_tableAlias}");

        // JOINs
        foreach (var join in _joins)
        {
            sb.Append($" {join.JoinType} JOIN ");
            if (!string.IsNullOrWhiteSpace(join.Library))
                sb.Append($"{join.Library}.");
            sb.Append(join.TableName);
            if (!string.IsNullOrWhiteSpace(join.Alias))
                sb.Append($" {join.Alias}");
            sb.Append($" ON {join.LeftColumn} = {join.RightColumn}");
        }

        // WHERE
        if (!string.IsNullOrWhiteSpace(_whereClause))
            sb.Append(" WHERE ").Append(_whereClause);

        // GROUP BY
        if (_groupBy.Count > 0)
            sb.Append(" GROUP BY ").Append(string.Join(", ", _groupBy));

        // HAVING
        if (!string.IsNullOrWhiteSpace(_havingClause))
            sb.Append(" HAVING ").Append(_havingClause);

        // ORDER BY
        if (_orderBy.Count > 0)
            sb.Append(" ORDER BY ").Append(string.Join(", ", _orderBy));

        // LIMIT (AS400)
        if (_limit.HasValue)
            sb.Append($" FETCH FIRST {_limit.Value} ROWS ONLY");

        return new QueryResult
        {
            Sql = sb.ToString()
        };
    }
}

