using QueryBuilder.Enums;
using QueryBuilder.Models;
using QueryBuilder.Translators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace QueryBuilder.Builders;

/// <summary>
/// Generador de consultas SELECT compatible con AS400.
/// Soporta DISTINCT, alias, JOINs, GROUP BY, HAVING, ORDER BY y funciones agregadas.
/// </summary>
public class SelectQueryBuilder
{
    internal string? WhereClause { get; set; }
    internal string? HavingClause { get; set; }

    private readonly string _tableName;
    private readonly string? _library;
    private string? _tableAlias;
    private readonly List<(string Column, string? Alias)> _columns = [];
    private readonly List<(string Column, SortDirection Direction)> _orderBy = [];
    private readonly List<string> _groupBy = [];
    private readonly List<JoinClause> _joins = [];

    private readonly Dictionary<string, string> _aliasMap = [];
    private int? _limit;
    private bool _distinct = false;

    /// <summary>
    /// Inicializa una nueva instancia de <see cref="SelectQueryBuilder"/>.
    /// </summary>
    public SelectQueryBuilder(string tableName, string? library = null)
    {
        _tableName = tableName;
        _library = library;
    }

    /// <summary>
    /// Indica que se desea una consulta SELECT DISTINCT.
    /// </summary>
    public SelectQueryBuilder Distinct()
    {
        _distinct = true;
        return this;
    }

    /// <summary>
    /// Define un alias para la tabla.
    /// </summary>
    public SelectQueryBuilder As(string alias)
    {
        _tableAlias = alias;
        return this;
    }

    /// <summary>
    /// Define las columnas a seleccionar (sin alias explícito).
    /// Si detecta funciones agregadas, genera alias automáticos como "SUM_CAMPO".
    /// </summary>
    /// <param name="columns">Nombres de columnas o funciones agregadas.</param>
    public SelectQueryBuilder Select(params string[] columns)
    {
        foreach (var column in columns)
        {
            if (TryGenerateAlias(column, out var alias))
                _columns.Add((column, alias));
            else
                _columns.Add((column, null));
        }

        return this;
    }

    /// <summary>
    /// Define columnas con alias.
    /// </summary>
    public SelectQueryBuilder Select(params (string Column, string Alias)[] columns)
    {
        foreach (var (column, alias) in columns)
        {
            _columns.Add((column, alias));
            _aliasMap[column] = alias;
        }
        return this;
    }

    /// <summary>
    /// Agrega una condición WHERE.
    /// </summary>
    public SelectQueryBuilder Where<T>(Expression<Func<T, bool>> expression)
    {
        LambdaWhereTranslator.Translate(this, expression);
        return this;
    }

    /// <summary>
    /// Agrega una condición HAVING.
    /// </summary>
    public SelectQueryBuilder Having<T>(Expression<Func<T, bool>> expression)
    {
        LambdaHavingTranslator.Translate(this, expression);
        return this;
    }

    /// <summary>
    /// Establece las columnas para agrupar (GROUP BY).
    /// </summary>
    public SelectQueryBuilder GroupBy(params string[] columns)
    {
        _groupBy.AddRange(columns);
        return this;
    }

    /// <summary>
    /// Establece el límite de resultados (FETCH FIRST para AS400).
    /// </summary>
    public SelectQueryBuilder Limit(int rowCount)
    {
        _limit = rowCount;
        return this;
    }

    /// <summary>
    /// Ordena por una o varias columnas.
    /// </summary>
    public SelectQueryBuilder OrderBy(params (string Column, SortDirection Direction)[] columns)
    {
        _orderBy.AddRange(columns);
        return this;
    }

    /// <summary>
    /// Agrega un JOIN a la consulta.
    /// </summary>
    public SelectQueryBuilder Join(string table, string? library, string alias, string left, string right, string joinType = "INNER")
    {
        _joins.Add(new JoinClause
        {
            JoinType = joinType.ToUpper(),
            TableName = table,
            Library = library,
            Alias = alias,
            LeftColumn = left,
            RightColumn = right
        });
        return this;
    }

    /// <summary>
    /// Construye y retorna el SQL.
    /// </summary>
    public QueryResult Build()
    {
        var sb = new StringBuilder();
        sb.Append("SELECT ");
        if (_distinct) sb.Append("DISTINCT ");

        if (_columns.Count == 0)
            sb.Append("*");
        else
        {
            var colParts = _columns.Select(c =>
                string.IsNullOrWhiteSpace(c.Alias)
                    ? c.Column
                    : $"{c.Column} AS {c.Alias}"
            );
            sb.Append(string.Join(", ", colParts));
        }

        sb.Append(" FROM ");
        if (!string.IsNullOrWhiteSpace(_library))
            sb.Append($"{_library}.");
        sb.Append(_tableName);
        if (!string.IsNullOrWhiteSpace(_tableAlias))
            sb.Append($" {_tableAlias}");

        foreach (var join in _joins)
        {
            sb.Append($" {join.JoinType} JOIN ");
            if (!string.IsNullOrWhiteSpace(join.Library))
                sb.Append($"{join.Library}.");
            sb.Append($"{join.TableName} {join.Alias} ON {join.LeftColumn} = {join.RightColumn}");
        }

        if (!string.IsNullOrWhiteSpace(WhereClause))
            sb.Append($" WHERE {WhereClause}");

        if (_groupBy.Count > 0)
        {
            sb.Append(" GROUP BY ");
            var grouped = _groupBy.Select(col => _aliasMap.TryGetValue(col, out var alias) ? alias : col);
            sb.Append(string.Join(", ", grouped));
        }

        if (!string.IsNullOrWhiteSpace(HavingClause))
            sb.Append($" HAVING {HavingClause}");

        if (_orderBy.Count > 0)
        {
            sb.Append(" ORDER BY ");
            var ordered = _orderBy.Select(o =>
            {
                var col = _aliasMap.TryGetValue(o.Column, out var alias) ? alias : o.Column;
                return $"{col} {o.Direction.ToString().ToUpper()}";
            });
            sb.Append(string.Join(", ", ordered));
        }

        if (_limit.HasValue)
            sb.Append(GetLimitClause());

        return new QueryResult { Sql = sb.ToString() };
    }

    /// <summary>
    /// Genera un alias automático para funciones agregadas como SUM(CAMPO), COUNT(*), etc.
    /// </summary>
    /// <param name="column">Expresión de columna a analizar.</param>
    /// <param name="alias">Alias generado si aplica.</param>
    /// <returns>True si se generó un alias; false en caso contrario.</returns>
    private static bool TryGenerateAlias(string column, out string alias)
    {
        alias = string.Empty;

        if (string.IsNullOrWhiteSpace(column) || !column.Contains('(') || !column.Contains(')'))
            return false;

        int start = column.IndexOf('(');
        int end = column.IndexOf(')');

        if (start < 1 || end <= start)
            return false;

        var function = column.Substring(0, start).Trim().ToUpperInvariant();
        var argument = column.Substring(start + 1, end - start - 1).Trim();

        var validFunctions = new[] { "SUM", "COUNT", "AVG", "MIN", "MAX" };
        if (!validFunctions.Contains(function))
            return false;

        if (string.IsNullOrWhiteSpace(argument))
            return false;

        alias = $"{function}_{argument.Replace("*", "ALL")}";
        return true;
    }

    private string GetLimitClause()
    {
        return _limit.HasValue ? $" FETCH FIRST {_limit.Value} ROWS ONLY" : string.Empty;
    }
}
