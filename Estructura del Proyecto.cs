using System;
using System.Linq;
using System.Linq.Expressions;
using QueryBuilder.Enums;
using QueryBuilder.Metadata;

namespace QueryBuilder.Builders
{
    /// <summary>
    /// Extensiones complementarias para SELECT:
    /// - HavingFunction / WhereFunction
    /// - HavingRaw (si no la tienes)
    /// - OrderBy(string...) con Direction opcional (incluye None ⇒ sin ASC/DESC)
    /// - OrderByCase(CaseWhenBuilder...)
    /// - SelectCase(CaseWhenBuilder...)
    /// - SelectComputed(sql, alias)
    /// - Join(table, onCondition, JoinType, library, alias) abreviado
    /// 
    /// Se implementan como extensiones para no colisionar con métodos existentes.
    /// </summary>
    public static class SelectQueryBuilderExtensions
    {
        // =========================
        // WHERE / HAVING helpers
        // =========================

        /// <summary>
        /// Agrega una condición basada en función/expresión SQL directamente en WHERE.
        /// Ejemplo: <c>WhereFunction("UPPER(NOMBRE) = 'PEDRO'")</c>.
        /// </summary>
        /// <param name="builder">Instancia del builder.</param>
        /// <param name="sqlFunctionCondition">Condición completa en SQL.</param>
        public static SelectQueryBuilder WhereFunction(this SelectQueryBuilder builder, string sqlFunctionCondition)
        {
            if (string.IsNullOrWhiteSpace(sqlFunctionCondition))
                return builder;

            if (string.IsNullOrWhiteSpace(builder.WhereClause))
                builder.WhereClause = sqlFunctionCondition;
            else
                builder.WhereClause += $" AND {sqlFunctionCondition}";

            return builder;
        }

        /// <summary>
        /// Agrega una condición basada en función/expresión SQL directamente en HAVING.
        /// Ejemplo: <c>HavingFunction("SUM(MONTO) &gt; 1000")</c>.
        /// </summary>
        /// <param name="builder">Instancia del builder.</param>
        /// <param name="sqlFunctionCondition">Condición completa en SQL.</param>
        public static SelectQueryBuilder HavingFunction(this SelectQueryBuilder builder, string sqlFunctionCondition)
        {
            if (string.IsNullOrWhiteSpace(sqlFunctionCondition))
                return builder;

            if (string.IsNullOrWhiteSpace(builder.HavingClause))
                builder.HavingClause = sqlFunctionCondition;
            else
                builder.HavingClause += $" AND {sqlFunctionCondition}";

            return builder;
        }

        /// <summary>
        /// Agrega SQL crudo a HAVING. Útil si tu versión no lo trae de serie.
        /// </summary>
        /// <param name="builder">Instancia.</param>
        /// <param name="sql">SQL crudo a incluir en HAVING.</param>
        public static SelectQueryBuilder HavingRaw(this SelectQueryBuilder builder, string sql)
        {
            if (string.IsNullOrWhiteSpace(sql))
                return builder;

            if (string.IsNullOrWhiteSpace(builder.HavingClause))
                builder.HavingClause = sql;
            else
                builder.HavingClause += $" AND {sql}";

            return builder;
        }

        // =========================
        // ORDER BY helpers
        // =========================

        /// <summary>
        /// Ordena por una columna literal. Si la dirección es <see cref="SortDirection.None"/>,
        /// no se agrega sufijo ASC/DESC (SQL limpio).
        /// </summary>
        /// <param name="builder">Instancia.</param>
        /// <param name="column">Nombre de columna o expresión.</param>
        /// <param name="direction">Dirección (None/Asc/Desc).</param>
        public static SelectQueryBuilder OrderBy(this SelectQueryBuilder builder, string column, SortDirection direction = SortDirection.None)
        {
            if (!string.IsNullOrWhiteSpace(column))
                builder.OrderBy((column, direction));
            return builder;
        }

        /// <summary>
        /// Azúcar sintáctico para varios ORDER BY de texto sin dirección (todos con <see cref="SortDirection.None"/>).
        /// </summary>
        /// <param name="builder">Instancia.</param>
        /// <param name="columns">Columnas o expresiones.</param>
        public static SelectQueryBuilder OrderBy(this SelectQueryBuilder builder, params string[] columns)
        {
            if (columns is { Length: > 0 })
            {
                foreach (var c in columns.Where(c => !string.IsNullOrWhiteSpace(c)))
                    builder.OrderBy((c, SortDirection.None));
            }
            return builder;
        }

        /// <summary>
        /// ORDER BY basado en una expresión CASE WHEN generada con <see cref="CaseWhenBuilder"/>.
        /// </summary>
        /// <param name="builder">Instancia.</param>
        /// <param name="caseWhen">Expresión CASE WHEN (builder).</param>
        /// <param name="direction">Dirección (None/Asc/Desc). Si se deja en None, no agrega ASC/DESC.</param>
        public static SelectQueryBuilder OrderByCase(this SelectQueryBuilder builder, CaseWhenBuilder caseWhen, SortDirection direction = SortDirection.None)
        {
            if (caseWhen == null)
                throw new ArgumentNullException(nameof(caseWhen));

            var expr = caseWhen.Build();
            builder.OrderBy((expr, direction));
            return builder;
        }

        // =========================
        // SELECT helpers
        // =========================

        /// <summary>
        /// Agrega al SELECT una expresión CASE WHEN con un alias.
        /// </summary>
        /// <param name="builder">Instancia.</param>
        /// <param name="caseWhen">Expresión CASE WHEN.</param>
        /// <param name="alias">Alias a usar para la columna.</param>
        public static SelectQueryBuilder SelectCase(this SelectQueryBuilder builder, CaseWhenBuilder caseWhen, string alias)
        {
            if (caseWhen == null)
                throw new ArgumentNullException(nameof(caseWhen));
            if (string.IsNullOrWhiteSpace(alias))
                throw new ArgumentNullException(nameof(alias));

            var expr = caseWhen.Build();
            return builder.SelectCase(expr, alias);
        }

        /// <summary>
        /// Agrega al SELECT una expresión computada arbitraria (por ejemplo, funciones SQL).
        /// Si <paramref name="alias"/> es nulo o vacío, no se agrega AS.
        /// </summary>
        /// <param name="builder">Instancia.</param>
        /// <param name="sqlExpression">Expresión/función SQL completa (ej: "COALESCE(A.B, 0)").</param>
        /// <param name="alias">Alias de la columna (opcional).</param>
        public static SelectQueryBuilder SelectComputed(this SelectQueryBuilder builder, string sqlExpression, string? alias = null)
        {
            if (string.IsNullOrWhiteSpace(sqlExpression))
                return builder;

            if (string.IsNullOrWhiteSpace(alias))
                return builder.Select(sqlExpression); // usa tu overload Select(params string[])

            return builder.Select((sqlExpression, alias)); // usa tu overload Select((col, alias)[])
        }

        // =========================
        // JOIN helper abreviado
        // =========================

        /// <summary>
        /// Atajo de JOIN recibiendo una condición compacta "LEFT = RIGHT".
        /// Internamente llama a tu Join(table, library, alias, left, right, joinType).
        /// </summary>
        /// <param name="builder">Instancia.</param>
        /// <param name="table">Tabla a unir (sin esquema o con esquema "LIB.TAB").</param>
        /// <param name="onCondition">Condición ON en formato "A = B".</param>
        /// <param name="type">Tipo de JOIN.</param>
        /// <param name="library">Biblioteca/esquema si <paramref name="table"/> se pasó sin punto.</param>
        /// <param name="alias">Alias de la tabla unida (recomendado).</param>
        public static SelectQueryBuilder Join(this SelectQueryBuilder builder,
                                              string table,
                                              string onCondition,
                                              JoinType type = JoinType.Inner,
                                              string? library = null,
                                              string? alias = null)
        {
            if (string.IsNullOrWhiteSpace(table))
                throw new ArgumentNullException(nameof(table));
            if (string.IsNullOrWhiteSpace(onCondition))
                throw new ArgumentNullException(nameof(onCondition));

            // Partir "A = B"
            var parts = onCondition.Split('=', 2, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 2)
                throw new ArgumentException("La condición ON debe tener el formato 'A = B'.", nameof(onCondition));

            var left = parts[0];
            var right = parts[1];

            // Si el table viene como "LIB.TAB" y no se pasó library, extraerlo.
            string tbl = table;
            string? lib = library;
            if (lib is null && table.Contains('.'))
            {
                var p = table.Split('.', 2);
                lib = p[0];
                tbl = p[1];
            }

            var joinType = type.ToString().ToUpperInvariant();
            var usableAlias = string.IsNullOrWhiteSpace(alias) ? tbl : alias;

            return builder.Join(tbl, lib, usableAlias, left, right, joinType);
        }
    }

    /// <summary>
    /// Extensiones tipadas extra (si ya adoptaste los metadatos con atributos).
    /// Solo se agregan las que no interactúan con miembros privados.
    /// </summary>
    public static class SelectQueryBuilderTypedExtras
    {
        /// <summary>
        /// ORDER BY tipado para varias propiedades de la misma entidad, todas con la misma dirección.
        /// </summary>
        /// <typeparam name="T">Tipo de entidad anotada con atributos.</typeparam>
        /// <param name="builder">Instancia.</param>
        /// <param name="direction">Dirección (None/Asc/Desc).</param>
        /// <param name="props">Expresiones de propiedades.</param>
        public static SelectQueryBuilder OrderBy<T>(this SelectQueryBuilder builder, SortDirection direction, params Expression<Func<T, object?>>[] props)
        {
            var alias = GetAlias(builder, typeof(T));
            foreach (var p in props)
            {
                var col = MetadataCache.GetColumnFor<T>(GetMemberName(p));
                var full = string.IsNullOrWhiteSpace(alias) ? col : $"{alias}.{col}";
                builder.OrderBy((full, direction));
            }
            return builder;
        }

        /// <summary>
        /// GROUP BY tipado para varias propiedades (azúcar sintáctico).
        /// </summary>
        public static SelectQueryBuilder GroupBy<T>(this SelectQueryBuilder builder, params Expression<Func<T, object?>>[] props)
        {
            var alias = GetAlias(builder, typeof(T));
            foreach (var p in props)
            {
                var col = MetadataCache.GetColumnFor<T>(GetMemberName(p));
                var full = string.IsNullOrWhiteSpace(alias) ? col : $"{alias}.{col}";
                builder.GroupBy(full);
            }
            return builder;
        }

        // ===== Helpers (privados a esta clase de extensión) =====

        private static string GetMemberName(LambdaExpression expr)
        {
            if (expr.Body is MemberExpression m) return m.Member.Name;
            if (expr.Body is UnaryExpression u && u.Operand is MemberExpression um) return um.Member.Name;
            throw new InvalidOperationException("La expresión debe referenciar una propiedad.");
        }

        private static string? GetAlias(SelectQueryBuilder builder, Type t)
        {
            // Intento de recuperar alias tipado registrado:
            // Si implementaste Register<T>(alias) en tu builder, intenta leerlo por reflexión (opcional).
            var field = builder.GetType().GetField("_typeAliases", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            if (field?.GetValue(builder) is System.Collections.IDictionary map && map.Contains(t))
                return map[t]?.ToString();

            // Fallback: si builder tiene _tableAlias y el tipo T coincide con la tabla "base", úsalo.
            var fAlias = builder.GetType().GetField("_tableAlias", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)?.GetValue(builder)?.ToString();
            return string.IsNullOrWhiteSpace(fAlias) ? null : fAlias;
        }
    }
}
