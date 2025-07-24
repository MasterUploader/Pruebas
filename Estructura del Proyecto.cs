Con esto finalizamos Select?

    using QueryBuilder.Enums;
using QueryBuilder.Helpers;
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
    /// Inicializa una nueva instancia de <see cref="SelectQueryBuilder"/> con una tabla derivada.
    /// </summary>
    /// <param name="derivedTable">Subconsulta que actúa como tabla.</param>
    public SelectQueryBuilder(Subquery derivedTable)
    {
        _derivedTable = derivedTable;
    }

    /// <summary>
    /// Inicializa una nueva instancia de <see cref="SelectQueryBuilder"/>.
    /// </summary>
    public SelectQueryBuilder(string tableName, string? library = null)
    {
        _tableName = tableName;
        _library = library;
    }
    /// <summary>
    /// Agrega una condición WHERE del tipo "CAMPO IN (...)".
    /// </summary>
    /// <param name="column">Nombre de la columna a comparar.</param>
    /// <param name="values">Lista de valores a incluir.</param>
    public SelectQueryBuilder WhereIn(string column, IEnumerable<object> values)
    {
        if (values is null || !values.Any()) return this;

        string formatted = string.Join(", ", values.Select(SqlHelper.FormatValue));
        string clause = $"{column} IN ({formatted})";

        WhereClause = string.IsNullOrWhiteSpace(WhereClause) ? clause : $"{WhereClause} AND {clause}";
        return this;
    }

    /// <summary>
    /// Agrega una condición WHERE del tipo "CAMPO NOT IN (...)".
    /// </summary>
    /// <param name="column">Nombre de la columna a comparar.</param>
    /// <param name="values">Lista de valores a excluir.</param>
    public SelectQueryBuilder WhereNotIn(string column, IEnumerable<object> values)
    {
        if (values is null || !values.Any()) return this;

        string formatted = string.Join(", ", values.Select(SqlHelper.FormatValue));
        string clause = $"{column} NOT IN ({formatted})";

        WhereClause = string.IsNullOrWhiteSpace(WhereClause) ? clause : $"{WhereClause} AND {clause}";
        return this;
    }

    /// <summary>
    /// Agrega una condición WHERE del tipo "CAMPO BETWEEN VALOR1 AND VALOR2".
    /// </summary>
    /// <param name="column">Nombre de la columna a comparar.</param>
    /// <param name="start">Valor inicial del rango.</param>
    /// <param name="end">Valor final del rango.</param>
    public SelectQueryBuilder WhereBetween(string column, object start, object end)
    {
        string formattedStart = SqlHelper.FormatValue(start);
        string formattedEnd = SqlHelper.FormatValue(end);
        string clause = $"{column} BETWEEN {formattedStart} AND {formattedEnd}";

        WhereClause = string.IsNullOrWhiteSpace(WhereClause) ? clause : $"{WhereClause} AND {clause}";
        return this;
    }


    /// <summary>
    /// Agrega un JOIN con una subconsulta como tabla.
    /// </summary>
    /// <param name="subquery">Instancia de subconsulta a usar como tabla.</param>
    /// <param name="alias">Alias de la tabla derivada.</param>
    /// <param name="left">Columna izquierda para la condición ON.</param>
    /// <param name="right">Columna derecha para la condición ON.</param>
    /// <param name="joinType">Tipo de JOIN: INNER, LEFT, etc.</param>
    public SelectQueryBuilder Join(Subquery subquery, string alias, string left, string right, string joinType = "INNER")
    {
        _joins.Add(new JoinClause
        {
            JoinType = joinType.ToUpperInvariant(),
            TableName = $"({subquery.Sql})",
            Alias = alias,
            LeftColumn = left,
            RightColumn = right
        });
        return this;
    }

    /// <summary>
    /// Agrega una o más expresiones CTE a la consulta.
    /// </summary>
    /// <param name="ctes">CTEs a incluir en la cláusula WITH.</param>
    /// <returns>Instancia actual de <see cref="SelectQueryBuilder"/>.</returns>
    public SelectQueryBuilder With(params CommonTableExpression[] ctes)
    {
        if (ctes != null && ctes.Length > 0)
            _ctes.AddRange(ctes);

        return this;
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
    /// Agrega una subconsulta como una columna seleccionada.
    /// </summary>
    /// <param name="subquery">Subconsulta construida previamente.</param>
    /// <param name="alias">Alias de la columna resultante.</param>
    public SelectQueryBuilder Select(Subquery subquery, string alias)
    {
        _columns.Add(($"({subquery.Sql})", alias));
        return this;
    }

    /// <summary>
    /// Agrega una expresión CASE WHEN como columna seleccionada, con un alias explícito.
    /// </summary>
    /// <param name="caseExpression">Expresión CASE generada con <see cref="CaseWhenBuilder"/>.</param>
    /// <param name="alias">Alias para la columna resultante.</param>
    public SelectQueryBuilder SelectCase(string caseExpression, string alias)
    {
        _columns.Add((caseExpression, alias));
        _aliasMap[caseExpression] = alias;
        return this;
    }

    /// <summary>
    /// Agrega una o varias expresiones CASE WHEN al SELECT.
    /// </summary>
    /// <param name="caseColumns">
    /// Tuplas donde el primer valor es la expresión CASE WHEN generada por <see cref="CaseWhenBuilder"/>,
    /// y el segundo valor es el alias de la columna.
    /// </param>
    /// <returns>Instancia modificada de <see cref="SelectQueryBuilder"/>.</returns>
    public SelectQueryBuilder SelectCase(params (string ColumnSql, string? Alias)[] caseColumns)
    {
        foreach (var (column, alias) in caseColumns)
        {
            _columns.Add((column, alias));
            if (!string.IsNullOrWhiteSpace(alias))
                _aliasMap[column] = alias;
        }

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
    /// Agrega una condición WHERE con una función SQL directamente como string.
    /// Ejemplo: "UPPER(NOMBRE) = 'PEDRO'"
    /// </summary>
    /// <param name="sqlFunctionCondition">Condición completa en SQL.</param>
    public SelectQueryBuilder WhereFunction(string sqlFunctionCondition)
    {
        if (string.IsNullOrWhiteSpace(sqlFunctionCondition))
            return this;

        if (string.IsNullOrWhiteSpace(WhereClause))
            WhereClause = sqlFunctionCondition;
        else
            WhereClause += $" AND {sqlFunctionCondition}";

        return this;
    }

    /// <summary>
    /// Agrega una condición HAVING con una función SQL directamente como string.
    /// Ejemplo: "SUM(CANTIDAD) > 10"
    /// </summary>
    /// <param name="sqlFunctionCondition">Condición completa en SQL.</param>
    public SelectQueryBuilder HavingFunction(string sqlFunctionCondition)
    {
        if (string.IsNullOrWhiteSpace(sqlFunctionCondition))
            return this;

        if (string.IsNullOrWhiteSpace(HavingClause))
            HavingClause = sqlFunctionCondition;
        else
            HavingClause += $" AND {sqlFunctionCondition}";

        return this;
    }


    /// <summary>
    /// Agrega una cláusula WHERE con una expresión CASE WHEN directamente como string.
    /// Ej: "CASE WHEN TIPO = 'A' THEN 1 ELSE 0 END = 1"
    /// </summary>
    /// <param name="sqlCaseCondition">Condición CASE completa.</param>
    public SelectQueryBuilder WhereCase(string sqlCaseCondition)
    {
        if (string.IsNullOrWhiteSpace(sqlCaseCondition))
            return this;

        if (string.IsNullOrWhiteSpace(WhereClause))
            WhereClause = sqlCaseCondition;
        else
            WhereClause += $" AND {sqlCaseCondition}";

        return this;
    }

    /// <summary>
    /// Agrega una cláusula WHERE con una expresión CASE WHEN generada desde <see cref="CaseWhenBuilder"/>.
    /// </summary>
    /// <param name="caseBuilder">Instancia del builder de CASE.</param>
    /// <param name="comparison">Condición adicional (por ejemplo: "= 1").</param>
    public SelectQueryBuilder WhereCase(CaseWhenBuilder caseBuilder, string comparison)
    {
        if (caseBuilder is null || string.IsNullOrWhiteSpace(comparison))
            return this;

        string expression = $"{caseBuilder.Build()} {comparison}";

        if (string.IsNullOrWhiteSpace(WhereClause))
            WhereClause = expression;
        else
            WhereClause += $" AND {expression}";

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
    /// Agrega una cláusula HAVING EXISTS con una subconsulta.
    /// </summary>
    /// <param name="subquery">Subconsulta que se evaluará con EXISTS.</param>
    /// <returns>Instancia modificada de <see cref="SelectQueryBuilder"/>.</returns>
    public SelectQueryBuilder HavingExists(Subquery subquery)
    {
        if (subquery == null || string.IsNullOrWhiteSpace(subquery.Sql))
            return this;

        var clause = $"EXISTS ({subquery.Sql})";

        if (string.IsNullOrWhiteSpace(HavingClause))
            HavingClause = clause;
        else
            HavingClause += $" AND {clause}";

        return this;
    }

    /// <summary>
    /// Agrega una cláusula HAVING NOT EXISTS con una subconsulta.
    /// </summary>
    /// <param name="subquery">Subconsulta que se evaluará con NOT EXISTS.</param>
    /// <returns>Instancia modificada de <see cref="SelectQueryBuilder"/>.</returns>
    public SelectQueryBuilder HavingNotExists(Subquery subquery)
    {
        if (subquery == null || string.IsNullOrWhiteSpace(subquery.Sql))
            return this;

        var clause = $"NOT EXISTS ({subquery.Sql})";

        if (string.IsNullOrWhiteSpace(HavingClause))
            HavingClause = clause;
        else
            HavingClause += $" AND {clause}";

        return this;
    }

    /// <summary>
    /// Agrega una cláusula HAVING con una expresión CASE WHEN directamente como string.
    /// Ejemplo: "CASE WHEN SUM(CANTIDAD) > 10 THEN 1 ELSE 0 END = 1"
    /// </summary>
    /// <param name="sqlCaseCondition">Expresión CASE completa en SQL.</param>
    public SelectQueryBuilder HavingCase(string sqlCaseCondition)
    {
        if (string.IsNullOrWhiteSpace(sqlCaseCondition))
            return this;

        if (string.IsNullOrWhiteSpace(HavingClause))
            HavingClause = sqlCaseCondition;
        else
            HavingClause += $" AND {sqlCaseCondition}";

        return this;
    }

    /// <summary>
    /// Agrega una cláusula HAVING con una expresión CASE WHEN generada con <see cref="CaseWhenBuilder"/>.
    /// </summary>
    /// <param name="caseBuilder">Builder de CASE WHEN.</param>
    /// <param name="comparison">Comparación adicional (ej: "= 1").</param>
    public SelectQueryBuilder HavingCase(CaseWhenBuilder caseBuilder, string comparison)
    {
        if (caseBuilder is null || string.IsNullOrWhiteSpace(comparison))
            return this;

        string expression = $"{caseBuilder.Build()} {comparison}";

        if (string.IsNullOrWhiteSpace(HavingClause))
            HavingClause = expression;
        else
            HavingClause += $" AND {expression}";

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
    /// Agrega una condición EXISTS a la cláusula WHERE con una subconsulta generada dinámicamente.
    /// </summary>
    /// <param name="subqueryBuilderAction">
    /// Acción que configura un nuevo <see cref="SelectQueryBuilder"/> para representar la subconsulta dentro de EXISTS.
    /// </param>
    /// <returns>Instancia modificada de <see cref="SelectQueryBuilder"/>.</returns>
    public SelectQueryBuilder WhereExists(Action<SelectQueryBuilder> subqueryBuilderAction)
    {
        var subqueryBuilder = new SelectQueryBuilder("DUMMY"); // El nombre se sobreescribirá
        subqueryBuilderAction(subqueryBuilder);
        var subquerySql = subqueryBuilder.Build().Sql;

        var existsClause = $"EXISTS ({subquerySql})";

        if (string.IsNullOrWhiteSpace(WhereClause))
            WhereClause = existsClause;
        else
            WhereClause += $" AND {existsClause}";

        return this;
    }

    /// <summary>
    /// Agrega una condición NOT EXISTS a la cláusula WHERE con una subconsulta generada dinámicamente.
    /// </summary>
    /// <param name="subqueryBuilderAction">
    /// Acción que configura un nuevo <see cref="SelectQueryBuilder"/> para representar la subconsulta dentro de NOT EXISTS.
    /// </param>
    /// <returns>Instancia modificada de <see cref="SelectQueryBuilder"/>.</returns>
    public SelectQueryBuilder WhereNotExists(Action<SelectQueryBuilder> subqueryBuilderAction)
    {
        var subqueryBuilder = new SelectQueryBuilder("DUMMY");
        subqueryBuilderAction(subqueryBuilder);
        var subquerySql = subqueryBuilder.Build().Sql;

        var notExistsClause = $"NOT EXISTS ({subquerySql})";

        if (string.IsNullOrWhiteSpace(WhereClause))
            WhereClause = notExistsClause;
        else
            WhereClause += $" AND {notExistsClause}";

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
    /// Define el valor de desplazamiento de filas (OFFSET).
    /// </summary>
    /// <param name="offset">Cantidad de filas a omitir.</param>
    /// <returns>Instancia modificada de <see cref="SelectQueryBuilder"/>.</returns>
    public SelectQueryBuilder Offset(int offset)
    {
        _offset = offset;
        return this;
    }

    /// <summary>
    /// Define la cantidad de filas a recuperar después del OFFSET (FETCH NEXT).
    /// </summary>
    /// <param name="rowCount">Cantidad de filas a recuperar.</param>
    /// <returns>Instancia modificada de <see cref="SelectQueryBuilder"/>.</returns>
    public SelectQueryBuilder FetchNext(int rowCount)
    {
        _fetch = rowCount;
        return this;
    }


    /// <summary>
    /// Construye y retorna el SQL.
    /// </summary>
    public QueryResult Build()
    {
        var sb = new StringBuilder();

        // Si hay CTEs, agregarlas antes del SELECT
        if (_ctes.Count > 0)
        {
            sb.Append("WITH ");
            sb.Append(string.Join(", ", _ctes.Select(cte => cte.ToString())));
            sb.AppendLine();
        }

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
        if (_offset.HasValue && _fetch.HasValue)
            return $" OFFSET {_offset.Value} ROWS FETCH NEXT {_fetch.Value} ROWS ONLY";
        if (_fetch.HasValue)
            return $" FETCH FIRST {_fetch.Value} ROWS ONLY";
        return string.Empty;
    }
}
