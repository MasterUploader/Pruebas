Este es mi SelectQueryBuilder actual, donde debo realizar la mejora, ademas si es posible agrega el SelectRaw

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
/// Generador de consultas SELECT compatible con AS400 (DB2 for i) y extensible a otros motores.
/// Ahora soporta parámetros (placeholders ?) también en SELECT.
/// </summary>
public class SelectQueryBuilder
{
    internal string? WhereClause { get; set; }
    internal string? HavingClause { get; set; }

    private readonly SqlDialect _dialect;
    private readonly List<object?> _parameters = [];

    private int? _offset;
    private int? _fetch;
    private readonly string? _tableName;
    private readonly string? _library;
    private string? _tableAlias;

    private readonly List<(string Column, string? Alias)> _columns = [];
    private readonly List<(string Column, SortDirection Direction)> _orderBy = [];
    private readonly List<string> _groupBy = [];
    private readonly List<JoinClause> _joins = [];
    private readonly List<CommonTableExpression> _ctes = [];

    private readonly Dictionary<string, string> _aliasMap = [];
    private int? _limit;
    private bool _distinct = false;
    private readonly Subquery? _derivedTable;

    /// <summary>
    /// Inicializa una nueva instancia con una tabla derivada (subconsulta).
    /// </summary>
    public SelectQueryBuilder(Subquery derivedTable, SqlDialect dialect = SqlDialect.Db2i)
    {
        _derivedTable = derivedTable;
        _dialect = dialect;
    }

    /// <summary>
    /// Inicializa una nueva instancia de <see cref="SelectQueryBuilder"/>.
    /// </summary>
    public SelectQueryBuilder(string tableName, string? library = null, SqlDialect dialect = SqlDialect.Db2i)
    {
        _tableName = tableName;
        _library = library;
        _dialect = dialect;
    }

    #region WHERE/HAVING helpers (parametrizados)

    /// <summary>
    /// Agrega SQL crudo al WHERE (úsese sólo cuando no sea posible parametrizar).
    /// </summary>
    public SelectQueryBuilder WhereRaw(string sql)
    {
        if (string.IsNullOrWhiteSpace(sql)) return this;
        AppendWhere(sql);
        return this;
    }

    /// <summary>
    /// Agrega condición parametrizada: COL = ? (y agrega el valor a la lista de parámetros).
    /// </summary>
    public SelectQueryBuilder WhereEq(string column, object? value)
    {
        if (string.IsNullOrWhiteSpace(column)) return this;
        AppendWhere($"{column} = ?");
        _parameters.Add(value);
        return this;
    }

    /// <summary>
    /// Agrega condición parametrizada: LOWER(COL) LIKE LOWER(?) (útil para búsquedas case-insensitive).
    /// </summary>
    public SelectQueryBuilder WhereLike(string column, string pattern, bool lower = true)
    {
        if (string.IsNullOrWhiteSpace(column)) return this;

        var lhs = lower ? $"LOWER({column})" : column;
        var rhs = lower ? "LOWER(?)" : "?";

        AppendWhere($"{lhs} LIKE {rhs}");
        _parameters.Add(pattern);
        return this;
    }

    /// <summary>
    /// Agrega condición parametrizada: COL BETWEEN ? AND ?
    /// </summary>
    public SelectQueryBuilder WhereBetween(string column, object start, object end)
    {
        if (string.IsNullOrWhiteSpace(column)) return this;
        AppendWhere($"{column} BETWEEN ? AND ?");
        _parameters.Add(start);
        _parameters.Add(end);
        return this;
    }

    /// <summary>
    /// Agrega condición parametrizada: COL IN (?, ?, ?)
    /// </summary>
    public SelectQueryBuilder WhereIn(string column, IEnumerable<object?> values)
    {
        var vals = values?.ToList() ?? [];
        if (string.IsNullOrWhiteSpace(column) || vals.Count == 0) return this;

        var ph = string.Join(", ", Enumerable.Repeat("?", vals.Count));
        AppendWhere($"{column} IN ({ph})");
        _parameters.AddRange(vals);
        return this;
    }

    /// <summary>
    /// Agrega condición parametrizada: COL NOT IN (?, ?, ?)
    /// </summary>
    public SelectQueryBuilder WhereNotIn(string column, IEnumerable<object?> values)
    {
        var vals = values?.ToList() ?? [];
        if (string.IsNullOrWhiteSpace(column) || vals.Count == 0) return this;

        var ph = string.Join(", ", Enumerable.Repeat("?", vals.Count));
        AppendWhere($"{column} NOT IN ({ph})");
        _parameters.AddRange(vals);
        return this;
    }

    /// <summary>
    /// Agrega SQL crudo al HAVING (úsese sólo cuando no sea posible parametrizar).
    /// </summary>
    public SelectQueryBuilder HavingRaw(string sql)
    {
        if (string.IsNullOrWhiteSpace(sql)) return this;
        AppendHaving(sql);
        return this;
    }

    #endregion

    #region API existente (se conserva)

    public SelectQueryBuilder Distinct() { _distinct = true; return this; }
    public SelectQueryBuilder As(string alias) { _tableAlias = alias; return this; }

    public SelectQueryBuilder Select(Subquery subquery, string alias)
    { _columns.Add(($"({subquery.Sql})", alias)); return this; }

    public SelectQueryBuilder SelectCase(string caseExpression, string alias)
    {
        _columns.Add((caseExpression, alias));
        _aliasMap[caseExpression] = alias;
        return this;
    }

    public SelectQueryBuilder SelectCase(params (string ColumnSql, string? Alias)[] caseColumns)
    {
        foreach (var (column, alias) in caseColumns)
        {
            _columns.Add((column, alias));
            if (!string.IsNullOrWhiteSpace(alias)) _aliasMap[column] = alias;
        }
        return this;
    }

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
    /// WHERE mediante expresión (se mantiene tal cual).
    /// </summary>
    public SelectQueryBuilder Where<T>(Expression<Func<T, bool>> expression)
    {
        LambdaWhereTranslator.Translate(this, expression);
        return this;
    }

    /// <summary>
    /// HAVING mediante expresión (se mantiene tal cual).
    /// </summary>
    public SelectQueryBuilder Having<T>(Expression<Func<T, bool>> expression)
    {
        LambdaHavingTranslator.Translate(this, expression);
        return this;
    }

    public SelectQueryBuilder HavingExists(Subquery subquery)
    {
        if (subquery == null || string.IsNullOrWhiteSpace(subquery.Sql)) return this;
        AppendHaving($"EXISTS ({subquery.Sql})");
        return this;
    }

    public SelectQueryBuilder HavingNotExists(Subquery subquery)
    {
        if (subquery == null || string.IsNullOrWhiteSpace(subquery.Sql)) return this;
        AppendHaving($"NOT EXISTS ({subquery.Sql})");
        return this;
    }

    public SelectQueryBuilder WhereCase(string sqlCaseCondition)
    {
        if (string.IsNullOrWhiteSpace(sqlCaseCondition)) return this;
        AppendWhere(sqlCaseCondition);
        return this;
    }

    public SelectQueryBuilder WhereCase(CaseWhenBuilder caseBuilder, string comparison)
    {
        if (caseBuilder is null || string.IsNullOrWhiteSpace(comparison)) return this;
        AppendWhere($"{caseBuilder.Build()} {comparison}");
        return this;
    }

    public SelectQueryBuilder HavingCase(string sqlCaseCondition)
    {
        if (string.IsNullOrWhiteSpace(sqlCaseCondition)) return this;
        AppendHaving(sqlCaseCondition);
        return this;
    }

    public SelectQueryBuilder HavingCase(CaseWhenBuilder caseBuilder, string comparison)
    {
        if (caseBuilder is null || string.IsNullOrWhiteSpace(comparison)) return this;
        AppendHaving($"{caseBuilder.Build()} {comparison}");
        return this;
    }

    public SelectQueryBuilder WhereExists(Action<SelectQueryBuilder> subqueryBuilderAction)
    {
        var subqueryBuilder = new SelectQueryBuilder("DUMMY", dialect: _dialect);
        subqueryBuilderAction(subqueryBuilder);
        var subquerySql = subqueryBuilder.Build().Sql;
        AppendWhere($"EXISTS ({subquerySql})");
        return this;
    }

    public SelectQueryBuilder WhereNotExists(Action<SelectQueryBuilder> subqueryBuilderAction)
    {
        var subqueryBuilder = new SelectQueryBuilder("DUMMY", dialect: _dialect);
        subqueryBuilderAction(subqueryBuilder);
        var subquerySql = subqueryBuilder.Build().Sql;
        AppendWhere($"NOT EXISTS ({subquerySql})");
        return this;
    }

    public SelectQueryBuilder GroupBy(params string[] columns)
    { _groupBy.AddRange(columns); return this; }

    public SelectQueryBuilder Limit(int rowCount)
    { _limit = rowCount; return this; }

    public SelectQueryBuilder OrderBy(params (string Column, SortDirection Direction)[] columns)
    { _orderBy.AddRange(columns); return this; }

    /// <summary>
    /// JOIN estilo clásico (se conserva).
    /// </summary>
    public SelectQueryBuilder Join(string table, string? library, string alias, string left, string right, string joinType = "INNER")
    {
        _joins.Add(new JoinClause
        {
            JoinType = joinType.ToUpperInvariant(),
            TableName = table,
            Library = library,
            Alias = alias,
            LeftColumn = left,
            RightColumn = right
        });
        return this;
    }

    /// <summary>
    /// JOIN simplificado: JOIN {tableRef} ON {onCondition}. No rompe la API existente.
    /// </summary>
    /// <param name="table">Puede incluir esquema/alias: "LIB.TABLA" o "LIB.TABLA T".</param>
    /// <param name="onCondition">Condición ON completa: "A.Col = B.Col".</param>
    /// <param name="joinType">INNER, LEFT, etc.</param>
    public SelectQueryBuilder Join(string table, string onCondition, string joinType = "INNER")
    {
        if (string.IsNullOrWhiteSpace(table))
            throw new ArgumentNullException(nameof(table));
        if (string.IsNullOrWhiteSpace(onCondition))
            throw new ArgumentNullException(nameof(onCondition));

        // Extrae left/right de "A = B" para reusar el JOIN existente
        var split = onCondition.Split('=', 2, StringSplitOptions.TrimEntries);
        if (split.Length != 2)
            throw new ArgumentException("La condición ON debe ser del tipo 'X = Y'.", nameof(onCondition));

        string left = split[0];
        string right = split[1];

        // Derivar alias por el lado derecho (ej: "PQR01CLI.CLINRO" -> "PQR01CLI")
        string alias = right.Split('.', StringSplitOptions.TrimEntries).FirstOrDefault() ?? table.Trim();

        // Si el caller no incluyó esquema en 'table', úsalo desde _library si existe
        string? library = null;
        string tableName = table.Trim();
        if (!tableName.Contains('.') && !string.IsNullOrWhiteSpace(_library))
            library = _library;

        return Join(tableName, library, alias, left, right, joinType);
    }

    public SelectQueryBuilder Offset(int offset) { _offset = offset; return this; }
    public SelectQueryBuilder FetchNext(int rowCount) { _fetch = rowCount; return this; }

    /// <summary>
    /// Agrega CASE WHEN al ORDER BY. Si <see cref="SortDirection.None"/>, no imprime ASC/DESC.
    /// </summary>
    public SelectQueryBuilder OrderByCase(CaseWhenBuilder caseWhen, SortDirection direction = SortDirection.None, string? alias = null)
    {
        if (caseWhen == null) throw new ArgumentNullException(nameof(caseWhen));
        var expression = caseWhen.Build();
        if (!string.IsNullOrWhiteSpace(alias))
            expression += $" AS {alias}";
        _orderBy.Add((expression, direction));
        return this;
    }

    #endregion

    /// <summary>
    /// Construye y retorna el SQL (y parámetros).
    /// </summary>
    public QueryResult Build()
    {
        var sb = new StringBuilder();

        // WITH
        if (_ctes.Count > 0)
        {
            sb.Append("WITH ");
            sb.Append(string.Join(", ", _ctes.Select(cte => cte.ToString())));
            sb.AppendLine();
        }

        // SELECT
        sb.Append("SELECT ");
        if (_distinct) sb.Append("DISTINCT ");

        if (_columns.Count == 0)
            sb.Append('*');
        else
        {
            var colParts = _columns.Select(c =>
                string.IsNullOrWhiteSpace(c.Alias)
                    ? c.Column
                    : $"{c.Column} AS {c.Alias}");
            sb.Append(string.Join(", ", colParts));
        }

        // FROM
        sb.Append(" FROM ");
        if (_derivedTable != null)
        {
            sb.Append(_derivedTable.ToString());
        }
        else
        {
            if (!string.IsNullOrWhiteSpace(_library))
                sb.Append($"{_library}.");
            sb.Append(_tableName);
            if (!string.IsNullOrWhiteSpace(_tableAlias))
                sb.Append($" {_tableAlias}");
        }

        // JOINs
        foreach (var join in _joins)
        {
            sb.Append($" {join.JoinType} JOIN ");
            if (!string.IsNullOrWhiteSpace(join.Library))
                sb.Append($"{join.Library}.");
            sb.Append($"{join.TableName}");
            if (!string.IsNullOrWhiteSpace(join.Alias))
                sb.Append($" {join.Alias}");
            sb.Append($" ON {join.LeftColumn} = {join.RightColumn}");
        }

        // WHERE
        if (!string.IsNullOrWhiteSpace(WhereClause))
            sb.Append($" WHERE {WhereClause}");

        // GROUP BY
        if (_groupBy.Count > 0)
        {
            sb.Append(" GROUP BY ");
            var grouped = _groupBy.Select(col => _aliasMap.TryGetValue(col, out var alias) ? alias : col);
            sb.Append(string.Join(", ", grouped));
        }

        // HAVING
        if (!string.IsNullOrWhiteSpace(HavingClause))
            sb.Append($" HAVING {HavingClause}");

        // ORDER BY
        if (_orderBy.Count > 0)
        {
            sb.Append(" ORDER BY ");
            var ordered = _orderBy.Select(o =>
            {
                var col = _aliasMap.TryGetValue(o.Column, out var alias) ? alias : o.Column;
                // Si la dirección es None, no imprimir ASC/DESC
                return o.Direction == SortDirection.None
                    ? col
                    : $"{col} {(o.Direction == SortDirection.Desc ? "DESC" : "ASC")}";
            });
            sb.Append(string.Join(", ", ordered));
        }

        // Paginación (DB2 for i usa OFFSET/FETCH a partir de ciertas versiones; si no, adaptar aquí por dialecto)
        if (_offset.HasValue || _fetch.HasValue)
        {
            if (_offset.HasValue) sb.Append($" OFFSET {_offset.Value} ROWS");
            if (_fetch.HasValue) sb.Append($" FETCH NEXT {_fetch.Value} ROWS ONLY");
        }
        else if (_limit.HasValue)
        {
            // Alternativa para compatibilidad
            sb.Append($" FETCH FIRST {_limit.Value} ROWS ONLY");
        }

        return new QueryResult
        {
            Sql = sb.ToString(),
            Parameters = _parameters
        };
    }

    #region Internos

    private void AppendWhere(string fragment)
    {
        if (string.IsNullOrWhiteSpace(WhereClause)) WhereClause = fragment;
        else WhereClause += $" AND {fragment}";
    }

    private void AppendHaving(string fragment)
    {
        if (string.IsNullOrWhiteSpace(HavingClause)) HavingClause = fragment;
        else HavingClause += $" AND {fragment}";
    }

    private static bool TryGenerateAlias(string column, out string? alias)
    {
        alias = null;
        // Alias automático para agregados: SUM(CAMPO) -> SUM_CAMPO
        var trimmed = column.Trim();
        var start = trimmed.IndexOf('(');
        var end = trimmed.IndexOf(')');
        if (start > 0 && end > start)
        {
            var func = trimmed[..start].Trim().ToUpperInvariant();
            var inner = trimmed.Substring(start + 1, end - start - 1).Trim();
            if (!string.IsNullOrWhiteSpace(func) && !string.IsNullOrWhiteSpace(inner))
            {
                alias = $"{func}_{inner.Replace('.', '_')}";
                return true;
            }
        }
        return false;
    }

    #endregion
}
