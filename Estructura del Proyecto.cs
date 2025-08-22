using QueryBuilder.Enums;
using QueryBuilder.Helpers;
using QueryBuilder.Models;
using QueryBuilder.Translators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace QueryBuilder.Builders
{
    /// <summary>
    /// Generador de consultas SELECT compatible con AS400 (y otros motores).
    /// Soporta DISTINCT, alias, JOINs, GROUP BY, HAVING, ORDER BY, CTE (WITH) y funciones agregadas.
    /// 
    /// ⚠️ Corrección importante de alias:
    ///  - Select("COUNT(*)") → genera: SELECT COUNT(*)
    ///  - Select("COUNT(*) AS CNT") → genera: SELECT COUNT(*) AS CNT
    ///  - Select(("COUNT(*)","CNT")) → genera: SELECT COUNT(*) AS CNT
    ///  - No se generarán alias automáticos por defecto (se evita “AS COUNT_*”).
    /// </summary>
    public class SelectQueryBuilder
    {
        // ====== Estado interno y configuración ======
        internal string? WhereClause { get; set; }
        internal string? HavingClause { get; set; }

        private int? _offset;
        private int? _fetch;
        private readonly string? _tableName;
        private readonly string? _library;
        private string? _tableAlias;

        // Columnas seleccionadas: (expresión, aliasOpcional)
        private readonly List<(string Column, string? Alias)> _columns = new();

        // Ordenamientos: (expresión, dirección)
        private readonly List<(string Column, SortDirection Direction)> _orderBy = new();

        // Agrupaciones
        private readonly List<string> _groupBy = new();

        // JOINs
        private readonly List<JoinClause> _joins = new();

        // CTEs (WITH)
        private readonly List<CommonTableExpression> _ctes = new();

        // Mapeo de alias opcional (p.e., para reutilizar nombre de alias si fuese necesario)
        private readonly Dictionary<string, string> _aliasMap = new();

        private int? _limit;
        private bool _distinct = false;
        private readonly Subquery? _derivedTable;

        // ====== Constructores ======

        /// <summary>
        /// Inicializa una nueva instancia de <see cref="SelectQueryBuilder"/> con una tabla derivada (subconsulta).
        /// </summary>
        /// <param name="derivedTable">Subconsulta que actúa como tabla (FROM (subquery) T).</param>
        public SelectQueryBuilder(Subquery derivedTable)
        {
            _derivedTable = derivedTable;
        }

        /// <summary>
        /// Inicializa una nueva instancia de <see cref="SelectQueryBuilder"/> para una tabla concreta.
        /// </summary>
        /// <param name="tableName">Nombre de la tabla base (sin esquema/biblioteca).</param>
        /// <param name="library">Nombre opcional de la biblioteca/esquema (AS400 u otros).</param>
        public SelectQueryBuilder(string tableName, string? library = null)
        {
            _tableName = tableName;
            _library = library;
        }

        // ====== Helpers privados (ALIAS) ======

        /// <summary>
        /// Indica si la expresión de columna ya trae un alias explícito (contiene " AS ").
        /// Previene que el builder agregue un segundo alias.
        /// </summary>
        private static bool HasExplicitAlias(string expr)
        {
            if (string.IsNullOrWhiteSpace(expr)) return false;
            return expr.IndexOf(" AS ", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        /// <summary>
        /// Indica si la expresión es compleja (función, espacios, *, comas, etc.).
        /// Útil si en un futuro se desean reactivar alias automáticos en columnas simples.
        /// </summary>
        private static bool IsComplexExpression(string expr)
        {
            if (string.IsNullOrWhiteSpace(expr)) return false;
            var s = expr.Trim();
            return s.Contains('(') || s.Contains(')') || s.Contains(' ') || s.Contains('*') || s.Contains(',') || s.Contains('\t');
        }

        /// <summary>
        /// Intento de alias automático: DESACTIVADO deliberadamente para evitar alias no deseados.
        /// Siempre devuelve false.
        /// </summary>
        private static bool TryGenerateAlias(string expr, out string? alias)
        {
            alias = null;
            // Si en algún momento deseas alias automáticos (p.e., para columnas simples sin función):
            // 1) Detectar que NO sea compleja (IsComplexExpression == false)
            // 2) Construir un alias seguro (p.e., quitar comillas, espacios, etc.)
            // 3) Devolver true y asignar alias
            return false;
        }

        // ====== Selección de columnas ======

        /// <summary>
        /// Agrega columnas a seleccionar mediante expresiones literales.
        /// - Si la expresión ya incluye "AS ...", se respeta tal cual.
        /// - NO se generan alias automáticos para evitar duplicaciones (ej. COUNT(*) AS CNT).
        /// </summary>
        /// <param name="columns">Expresiones de columnas o funciones (p.ej., "COUNT(*)", "SUM(MTO) AS TOTAL").</param>
        public SelectQueryBuilder Select(params string[] columns)
        {
            foreach (var column in columns)
            {
                if (HasExplicitAlias(column))
                {
                    // Ya incluye "AS", se usa tal cual (sin alias adicional separado)
                    _columns.Add((column, null));
                }
                else
                {
                    // Alias automáticos desactivados
                    if (TryGenerateAlias(column, out var alias) && !string.IsNullOrWhiteSpace(alias))
                        _columns.Add((column, alias));
                    else
                        _columns.Add((column, null));
                }
            }
            return this;
        }

        /// <summary>
        /// Agrega columnas a seleccionar con alias explícitos, separados en tuplas.
        /// - Si la expresión ya incluye "AS ..." incrustado, se respeta tal cual y se ignora el alias separado.
        /// </summary>
        /// <param name="columns">Tuplas (Expresión, Alias) a proyectar.</param>
        public SelectQueryBuilder Select(params (string Column, string Alias)[] columns)
        {
            foreach (var (column, alias) in columns)
            {
                if (HasExplicitAlias(column))
                {
                    // La expresión ya trae "AS XYZ"; la dejamos intacta
                    _columns.Add((column, null));
                }
                else
                {
                    _columns.Add((column, alias));
                    _aliasMap[column] = alias;
                }
            }
            return this;
        }

        /// <summary>
        /// Agrega una subconsulta (Subquery) como columna, con alias obligatorio.
        /// </summary>
        public SelectQueryBuilder Select(Subquery subquery, string alias)
        {
            _columns.Add(($"({subquery.Sql})", alias));
            return this;
        }

        /// <summary>
        /// Agrega al SELECT una o varias expresiones CASE WHEN ya construidas, pudiendo asignar alias.
        /// </summary>
        public SelectQueryBuilder SelectCase(params (string ColumnSql, string? Alias)[] caseColumns)
        {
            foreach (var (column, alias) in caseColumns)
            {
                if (HasExplicitAlias(column))
                {
                    _columns.Add((column, null));
                }
                else
                {
                    _columns.Add((column, alias));
                    if (!string.IsNullOrWhiteSpace(alias))
                        _aliasMap[column] = alias;
                }
            }
            return this;
        }

        /// <summary>
        /// Agrega una única expresión CASE WHEN ya construida con alias.
        /// </summary>
        public SelectQueryBuilder SelectCase(string caseExpression, string alias)
        {
            if (HasExplicitAlias(caseExpression))
                _columns.Add((caseExpression, null));
            else
                _columns.Add((caseExpression, alias));

            return this;
        }

        // ====== FROM / Tabla y alias ======

        /// <summary>
        /// Define un alias para la tabla base.
        /// </summary>
        public SelectQueryBuilder As(string alias)
        {
            _tableAlias = alias;
            return this;
        }

        /// <summary>
        /// Marca el SELECT como DISTINCT.
        /// </summary>
        public SelectQueryBuilder Distinct()
        {
            _distinct = true;
            return this;
        }

        // ====== WHERE / HAVING (funciones y lambdas) ======

        /// <summary>
        /// Agrega condición WHERE con texto SQL crudo (útil para funciones).
        /// </summary>
        public SelectQueryBuilder WhereFunction(string sqlFunctionCondition)
        {
            if (string.IsNullOrWhiteSpace(sqlFunctionCondition))
                return this;

            WhereClause = string.IsNullOrWhiteSpace(WhereClause)
                ? sqlFunctionCondition
                : $"{WhereClause} AND {sqlFunctionCondition}";
            return this;
        }

        /// <summary>
        /// Agrega condición HAVING con texto SQL crudo (útil para funciones agregadas).
        /// </summary>
        public SelectQueryBuilder HavingFunction(string sqlFunctionCondition)
        {
            if (string.IsNullOrWhiteSpace(sqlFunctionCondition))
                return this;

            HavingClause = string.IsNullOrWhiteSpace(HavingClause)
                ? sqlFunctionCondition
                : $"{HavingClause} AND {sqlFunctionCondition}";
            return this;
        }

        /// <summary>
        /// Agrega condición WHERE a partir de una expresión lambda tipada.
        /// </summary>
        public SelectQueryBuilder Where<T>(Expression<Func<T, bool>> expression)
        {
            LambdaWhereTranslator.Translate(this, expression);
            return this;
        }

        /// <summary>
        /// Agrega condición HAVING a partir de una expresión lambda tipada.
        /// </summary>
        public SelectQueryBuilder Having<T>(Expression<Func<T, bool>> expression)
        {
            LambdaHavingTranslator.Translate(this, expression);
            return this;
        }

        /// <summary>
        /// Agrega condición WHERE del tipo "IN".
        /// </summary>
        public SelectQueryBuilder WhereIn(string column, IEnumerable<object> values)
        {
            if (values is null || !values.Any()) return this;

            string formatted = string.Join(", ", values.Select(SqlHelper.FormatValue));
            string clause = $"{column} IN ({formatted})";

            WhereClause = string.IsNullOrWhiteSpace(WhereClause) ? clause : $"{WhereClause} AND {clause}";
            return this;
        }

        /// <summary>
        /// Agrega condición WHERE del tipo "NOT IN".
        /// </summary>
        public SelectQueryBuilder WhereNotIn(string column, IEnumerable<object> values)
        {
            if (values is null || !values.Any()) return this;

            string formatted = string.Join(", ", values.Select(SqlHelper.FormatValue));
            string clause = $"{column} NOT IN ({formatted})";

            WhereClause = string.IsNullOrWhiteSpace(WhereClause) ? clause : $"{WhereClause} AND {clause}";
            return this;
        }

        /// <summary>
        /// Agrega condición WHERE del tipo BETWEEN.
        /// </summary>
        public SelectQueryBuilder WhereBetween(string column, object start, object end)
        {
            string formattedStart = SqlHelper.FormatValue(start);
            string formattedEnd = SqlHelper.FormatValue(end);
            string clause = $"{column} BETWEEN {formattedStart} AND {formattedEnd}";

            WhereClause = string.IsNullOrWhiteSpace(WhereClause) ? clause : $"{WhereClause} AND {clause}";
            return this;
        }

        /// <summary>
        /// Agrega condición WHERE basada en una expresión CASE WHEN literal.
        /// </summary>
        public SelectQueryBuilder WhereCase(string sqlCaseCondition)
        {
            if (string.IsNullOrWhiteSpace(sqlCaseCondition)) return this;

            WhereClause = string.IsNullOrWhiteSpace(WhereClause)
                ? sqlCaseCondition
                : $"{WhereClause} AND {sqlCaseCondition}";
            return this;
        }

        /// <summary>
        /// Agrega condición WHERE basada en <see cref="CaseWhenBuilder"/> + comparación.
        /// </summary>
        public SelectQueryBuilder WhereCase(CaseWhenBuilder caseBuilder, string comparison)
        {
            if (caseBuilder is null || string.IsNullOrWhiteSpace(comparison)) return this;

            string expression = $"{caseBuilder.Build()} {comparison}";
            WhereClause = string.IsNullOrWhiteSpace(WhereClause)
                ? expression
                : $"{WhereClause} AND {expression}";
            return this;
        }

        /// <summary>
        /// Agrega condición HAVING basada en una expresión CASE WHEN literal.
        /// </summary>
        public SelectQueryBuilder HavingCase(string sqlCaseCondition)
        {
            if (string.IsNullOrWhiteSpace(sqlCaseCondition)) return this;

            HavingClause = string.IsNullOrWhiteSpace(HavingClause)
                ? sqlCaseCondition
                : $"{HavingClause} AND {sqlCaseCondition}";
            return this;
        }

        /// <summary>
        /// Agrega condición HAVING basada en <see cref="CaseWhenBuilder"/> + comparación.
        /// </summary>
        public SelectQueryBuilder HavingCase(CaseWhenBuilder caseBuilder, string comparison)
        {
            if (caseBuilder is null || string.IsNullOrWhiteSpace(comparison)) return this;

            string expression = $"{caseBuilder.Build()} {comparison}";
            HavingClause = string.IsNullOrWhiteSpace(HavingClause)
                ? expression
                : $"{HavingClause} AND {expression}";
            return this;
        }

        // ====== EXISTS / NOT EXISTS (WHERE / HAVING) ======

        /// <summary>
        /// Agrega una condición WHERE EXISTS con subconsulta.
        /// </summary>
        public SelectQueryBuilder WhereExists(Action<SelectQueryBuilder> subqueryBuilderAction)
        {
            var subqueryBuilder = new SelectQueryBuilder("DUMMY");
            subqueryBuilderAction(subqueryBuilder);
            var subquerySql = subqueryBuilder.Build().Sql;

            var existsClause = $"EXISTS ({subquerySql})";
            WhereClause = string.IsNullOrWhiteSpace(WhereClause)
                ? existsClause
                : $"{WhereClause} AND {existsClause}";
            return this;
        }

        /// <summary>
        /// Agrega una condición WHERE NOT EXISTS con subconsulta.
        /// </summary>
        public SelectQueryBuilder WhereNotExists(Action<SelectQueryBuilder> subqueryBuilderAction)
        {
            var subqueryBuilder = new SelectQueryBuilder("DUMMY");
            subqueryBuilderAction(subqueryBuilder);
            var subquerySql = subqueryBuilder.Build().Sql;

            var notExistsClause = $"NOT EXISTS ({subquerySql})";
            WhereClause = string.IsNullOrWhiteSpace(WhereClause)
                ? notExistsClause
                : $"{WhereClause} AND {notExistsClause}";
            return this;
        }

        /// <summary>
        /// Agrega una condición HAVING EXISTS con subconsulta (ya construida).
        /// </summary>
        public SelectQueryBuilder HavingExists(Subquery subquery)
        {
            if (subquery == null || string.IsNullOrWhiteSpace(subquery.Sql)) return this;

            var clause = $"EXISTS ({subquery.Sql})";
            HavingClause = string.IsNullOrWhiteSpace(HavingClause)
                ? clause
                : $"{HavingClause} AND {clause}";
            return this;
        }

        /// <summary>
        /// Agrega una condición HAVING NOT EXISTS con subconsulta (ya construida).
        /// </summary>
        public SelectQueryBuilder HavingNotExists(Subquery subquery)
        {
            if (subquery == null || string.IsNullOrWhiteSpace(subquery.Sql)) return this;

            var clause = $"NOT EXISTS ({subquery.Sql})";
            HavingClause = string.IsNullOrWhiteSpace(HavingClause)
                ? clause
                : $"{HavingClause} AND {clause}";
            return this;
        }

        // ====== JOINs ======

        /// <summary>
        /// Agrega un JOIN con una subconsulta como tabla.
        /// </summary>
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
        /// Agrega un JOIN a otra tabla (con librería/opcional), especificando columnas ON (left/right).
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

        // ====== CTE (WITH) ======

        /// <summary>
        /// Agrega una o más expresiones CTE (WITH ...) a la consulta.
        /// </summary>
        public SelectQueryBuilder With(params CommonTableExpression[] ctes)
        {
            if (ctes != null && ctes.Length > 0)
                _ctes.AddRange(ctes);
            return this;
        }

        // ====== GROUP BY / ORDER BY / LIMIT / PAGINACIÓN ======

        /// <summary>
        /// Establece las columnas para agrupar (GROUP BY).
        /// </summary>
        public SelectQueryBuilder GroupBy(params string[] columns)
        {
            _groupBy.AddRange(columns);
            return this;
        }

        /// <summary>
        /// Establece un límite al número de filas (AS400: FETCH FIRST n ROWS ONLY).
        /// </summary>
        public SelectQueryBuilder Limit(int rowCount)
        {
            _limit = rowCount;
            return this;
        }

        /// <summary>
        /// Define el desplazamiento de filas (OFFSET).
        /// </summary>
        public SelectQueryBuilder Offset(int offset)
        {
            _offset = offset;
            return this;
        }

        /// <summary>
        /// Define cuántas filas recuperar después del OFFSET (FETCH NEXT).
        /// </summary>
        public SelectQueryBuilder FetchNext(int rowCount)
        {
            _fetch = rowCount;
            return this;
        }

        /// <summary>
        /// Agrega columnas al ORDER BY.
        /// Si la dirección es <see cref="SortDirection.None"/>, no se imprime "ASC"/"DESC".
        /// </summary>
        public SelectQueryBuilder OrderBy(params (string Column, SortDirection Direction)[] columns)
        {
            _orderBy.AddRange(columns);
            return this;
        }

        /// <summary>
        /// Agrega una expresión CASE WHEN al ORDER BY (con dirección opcional).
        /// </summary>
        public SelectQueryBuilder OrderByCase(CaseWhenBuilder caseWhen, SortDirection direction = SortDirection.None, string? alias = null)
        {
            if (caseWhen == null) throw new ArgumentNullException(nameof(caseWhen));

            var expression = caseWhen.Build();
            if (!string.IsNullOrWhiteSpace(alias))
                expression += $" AS {alias}"; // Se tolera, aunque ORDER BY no requiere alias

            _orderBy.Add((caseWhen.Build(), direction));
            return this;
        }

        // ====== Build (SQL final) ======

        /// <summary>
        /// Construye y retorna el SQL resultante.
        /// </summary>
        public QueryResult Build()
        {
            var sb = new StringBuilder();

            // WITH / CTEs
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
            {
                sb.Append('*');
            }
            else
            {
                var colParts = _columns.Select(c =>
                {
                    // Si ya viene "AS ..." incrustado, respetar
                    if (HasExplicitAlias(c.Column))
                        return c.Column;

                    // Si hay alias separado, agregar "AS ..."
                    if (!string.IsNullOrWhiteSpace(c.Alias))
                        return $"{c.Column} AS {c.Alias}";

                    // En caso contrario, imprimir literal
                    return c.Column;
                });

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
                sb.Append(' ');
                sb.Append(join.JoinType);
                sb.Append(" JOIN ");
                if (!string.IsNullOrWhiteSpace(join.Library))
                    sb.Append($"{join.Library}.");

                sb.Append(join.TableName);

                if (!string.IsNullOrWhiteSpace(join.Alias))
                    sb.Append($" {join.Alias}");

                // Condición ON
                if (!string.IsNullOrWhiteSpace(join.LeftColumn) && !string.IsNullOrWhiteSpace(join.RightColumn))
                {
                    sb.Append(" ON ");
                    sb.Append($"{join.LeftColumn} = {join.RightColumn}");
                }
            }

            // WHERE
            if (!string.IsNullOrWhiteSpace(WhereClause))
            {
                sb.Append(" WHERE ");
                sb.Append(WhereClause);
            }

            // GROUP BY
            if (_groupBy.Count > 0)
            {
                sb.Append(" GROUP BY ");
                sb.Append(string.Join(", ", _groupBy));
            }

            // HAVING
            if (!string.IsNullOrWhiteSpace(HavingClause))
            {
                sb.Append(" HAVING ");
                sb.Append(HavingClause);
            }

            // ORDER BY
            if (_orderBy.Count > 0)
            {
                sb.Append(" ORDER BY ");
                sb.Append(string.Join(", ", _orderBy.Select(o =>
                {
                    // Si la dirección es None, no imprimimos ASC/DESC
                    return o.Direction == SortDirection.None
                        ? o.Column
                        : $"{o.Column} {(o.Direction == SortDirection.Desc ? "DESC" : "ASC")}";
                })));
            }

            // Paginación (OFFSET / FETCH) para motores que lo soporten (AS400 / DB2 iSeries soporta FETCH FIRST)
            if (_offset.HasValue)
                sb.Append($" OFFSET {_offset.Value} ROWS");

            if (_fetch.HasValue)
                sb.Append($" FETCH NEXT {_fetch.Value} ROWS ONLY");
            else if (_limit.HasValue)
                sb.Append($" FETCH FIRST {_limit.Value} ROWS ONLY");

            return new QueryResult
            {
                Sql = sb.ToString()
            };
        }
    }
}
