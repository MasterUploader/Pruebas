namespace QueryBuilder.Enums
{
    /// <summary>
    /// Dialecto/driver de base de datos para ajustar sintaxis y capacidades.
    /// Db2i = IBM i (AS/400, DB2 for i).
    /// </summary>
    public enum SqlDialect
    {
        Db2i = 0,      // AS/400 (por defecto)
        SqlServer = 1,
        MySql = 2,
        PostgreSql = 3,
        Oracle = 4,
        Generic = 9
    }
}

using QueryBuilder.Builders;
using QueryBuilder.Enums;

namespace QueryBuilder.Core
{
    /// <summary>
    /// Punto de entrada principal para construir consultas SQL.
    /// </summary>
    public static class QueryBuilder
    {
        /// <summary>
        /// Inicia la construcción de una consulta SELECT (dialecto por defecto: DB2 for i).
        /// </summary>
        public static SelectQueryBuilder From(string tableName, string? library = null)
            => new SelectQueryBuilder(tableName, library, SqlDialect.Db2i);

        /// <summary>
        /// Inicia la construcción de una consulta SELECT especificando el dialecto.
        /// </summary>
        public static SelectQueryBuilder From(string tableName, string? library, SqlDialect dialect)
            => new SelectQueryBuilder(tableName, library, dialect);
    }
}

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
    /// Generador de consultas SELECT compatible con AS400 (DB2 for i) y extensible a otros motores.
    /// Ahora soporta parámetros (placeholders ?) también en SELECT.
    /// </summary>
    public class SelectQueryBuilder
    {
        internal string? WhereClause { get; set; }
        internal string? HavingClause { get; set; }

        private readonly SqlDialect _dialect;
        private readonly List<object?> _parameters = new();

        private int? _offset;
        private int? _fetch;
        private readonly string? _tableName;
        private readonly string? _library;
        private string? _tableAlias;

        private readonly List<(string Column, string? Alias)> _columns = new();
        private readonly List<(string Column, SortDirection Direction)> _orderBy = new();
        private readonly List<string> _groupBy = new();
        private readonly List<JoinClause> _joins = new();
        private readonly List<CommonTableExpression> _ctes = new();

        private readonly Dictionary<string, string> _aliasMap = new();
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
}

using QueryBuilder.Enums;
using QueryBuilder.Helpers;
using QueryBuilder.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace QueryBuilder.Builders
{
    /// <summary>
    /// Generador de sentencias INSERT. Para DB2 for i usa placeholders (?) y parámetros seguros.
    /// Opcionalmente soporta características específicas por dialecto (InsertIgnore/Upsert) en otros motores.
    /// </summary>
    public class InsertQueryBuilder
    {
        private readonly string _tableName;
        private readonly string? _library;
        private readonly SqlDialect _dialect;

        private readonly List<string> _columns = new();
        private readonly List<List<object?>> _rows = new();         // VALUES parametrizados
        private readonly List<List<object>> _valuesRaw = new();      // VALUES raw (funciones)
        private SelectQueryBuilder? _selectSource;

        private string? _comment;
        private string? _whereClause;
        private bool _insertIgnore = false;                          // No se emite en Db2i
        private readonly Dictionary<string, object?> _onDuplicateUpdate = new(); // No se emite en Db2i

        /// <summary>
        /// Crea un builder de INSERT.
        /// </summary>
        public InsertQueryBuilder(string tableName, string? library = null, SqlDialect dialect = SqlDialect.Db2i)
        {
            _tableName = tableName;
            _library = library;
            _dialect = dialect;
        }

        /// <summary>Comentario inicial (útil para trazabilidad).</summary>
        public InsertQueryBuilder WithComment(string comment)
        { if (!string.IsNullOrWhiteSpace(comment)) _comment = $"-- {comment}"; return this; }

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

            _rows.Add(values.Select(v => v.Value).ToList());
            return this;
        }

        /// <summary>Agrega valores sin parámetros (por ejemplo funciones SQL).</summary>
        public InsertQueryBuilder ValuesRaw(params string[] rawValues)
        { _valuesRaw.Add(rawValues.Cast<object>().ToList()); return this; }

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

            // Validaciones
            foreach (var fila in _rows)
            {
                if (fila.Count != _columns.Count)
                    throw new InvalidOperationException($"El número de valores ({fila.Count}) no coincide con las columnas ({_columns.Count}).");
            }
            foreach (var fila in _valuesRaw)
            {
                if (fila.Count != _columns.Count)
                    throw new InvalidOperationException($"El número de valores RAW ({fila.Count}) no coincide con las columnas ({_columns.Count}).");
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
            if (_onDuplicateUpdate.Count > 0)
            {
                if (_dialect == SqlDialect.MySql)
                {
                    sb.Append(" ON DUPLICATE KEY UPDATE ");
                    sb.Append(string.Join(", ", _onDuplicateUpdate.Select(kv =>
                        $"{kv.Key} = {SqlHelper.FormatValue(kv.Value)}")));
                }
                // Para otros dialectos (Postgres: ON CONFLICT, SQL Server/DB2: MERGE) implementar aquí cuando se requiera.
            }

            return new QueryResult
            {
                Sql = sb.ToString(),
                Parameters = parameters
            };
        }
    }
}

