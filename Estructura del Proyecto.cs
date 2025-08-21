using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace QueryBuilder.Metadata
{
    // ============================================================
    //  A T R I B U T O S   D E   M E T A D A T O S
    // ============================================================

    /// <summary>
    /// Indica la biblioteca/esquema donde se encuentra la tabla.
    /// Ej.: <c>[Library("IS4TECHDTA")]</c>
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class LibraryAttribute : Attribute
    {
        /// <summary>Nombre de la biblioteca/esquema.</summary>
        public string Name { get; }

        public LibraryAttribute(string name) => Name = name ?? string.Empty;
    }

    /// <summary>
    /// Indica el nombre de la tabla asociada a la clase/DTO.
    /// Ej.: <c>[TableName("PQR01CLI")]</c>
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class TableNameAttribute : Attribute
    {
        /// <summary>Nombre de la tabla.</summary>
        public string Name { get; }

        public TableNameAttribute(string name) => Name = name ?? string.Empty;
    }

    /// <summary>
    /// Mapea una propiedad con el nombre de columna en la base de datos.
    /// Permite opcionalmente especificar tipo, tamaño, precisión y escala para los parámetros.
    /// Ej.: <c>[Column("CLINOM", DbType.String, Size = 100)]</c>
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, Inherited = true, AllowMultiple = false)]
    public sealed class ColumnAttribute : Attribute
    {
        /// <summary>Nombre de la columna en DB.</summary>
        public string Name { get; }

        /// <summary>Tipo de dato del parámetro (opcional).</summary>
        public DbType? DbType { get; }

        /// <summary>Tamaño del parámetro (para strings/binary) (opcional).</summary>
        public int? Size { get; set; }

        /// <summary>Precisión (para decimales) (opcional).</summary>
        public byte? Precision { get; set; }

        /// <summary>Escala (para decimales) (opcional).</summary>
        public byte? Scale { get; set; }

        public ColumnAttribute(string name)
        {
            Name = name ?? string.Empty;
        }

        public ColumnAttribute(string name, DbType dbType)
        {
            Name = name ?? string.Empty;
            DbType = dbType;
        }
    }

    /// <summary>
    /// Alias de <see cref="ColumnAttribute"/> para compatibilidad con código existente
    /// donde se usaba <c>[ParameterName(...)]</c>. Tiene el mismo comportamiento.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, Inherited = true, AllowMultiple = false)]
    public sealed class ParameterNameAttribute : Attribute
    {
        /// <summary>Nombre de la columna/param en DB.</summary>
        public string Name { get; }

        /// <summary>Tipo de dato del parámetro (opcional).</summary>
        public DbType? DbType { get; }

        /// <summary>Tamaño del parámetro (para strings/binary) (opcional).</summary>
        public int? Size { get; set; }

        /// <summary>Precisión (para decimales) (opcional).</summary>
        public byte? Precision { get; set; }

        /// <summary>Escala (para decimales) (opcional).</summary>
        public byte? Scale { get; set; }

        public ParameterNameAttribute(string name)
        {
            Name = name ?? string.Empty;
        }

        public ParameterNameAttribute(string name, DbType dbType)
        {
            Name = name ?? string.Empty;
            DbType = dbType;
        }
    }

    // ============================================================
    //  M O D E L O S   D E   M E T A D A T O S   (C A C H É)
    // ============================================================

    /// <summary>
    /// Metadatos de una columna: nombre en DB y configuración de parámetros.
    /// </summary>
    public sealed class ColumnMetadata
    {
        /// <summary>Nombre de la columna en DB.</summary>
        public string ColumnName { get; init; } = string.Empty;

        /// <summary>Tipo de dato (si se especificó).</summary>
        public DbType? DbType { get; init; }

        /// <summary>Tamaño (si se especificó).</summary>
        public int? Size { get; init; }

        /// <summary>Precisión (si se especificó).</summary>
        public byte? Precision { get; init; }

        /// <summary>Escala (si se especificó).</summary>
        public byte? Scale { get; init; }
    }

    /// <summary>
    /// Metadatos de una entidad (tabla): biblioteca, nombre de tabla y columnas.
    /// </summary>
    public sealed class EntityMetadata
    {
        /// <summary>Biblioteca/esquema.</summary>
        public string? Library { get; init; }

        /// <summary>Nombre de la tabla.</summary>
        public string? TableName { get; init; }

        /// <summary>
        /// Mapa de propiedades (por nombre de miembro) a metadatos de columna.
        /// Case-insensitive para comodidad.
        /// </summary>
        public Dictionary<string, ColumnMetadata> Columns { get; init; } =
            new(StringComparer.OrdinalIgnoreCase);
    }

    // ============================================================
    //  C A C H É   D E   R E F L E X I Ó N
    // ============================================================

    /// <summary>
    /// Caché de metadatos basada en atributos. Descubre y guarda:
    /// - Biblioteca (<see cref="LibraryAttribute"/>)
    /// - Tabla (<see cref="TableNameAttribute"/>)
    /// - Columnas (<see cref="ColumnAttribute"/> o <see cref="ParameterNameAttribute"/>)
    /// </summary>
    public static class MetadataCache
    {
        private static readonly ConcurrentDictionary<Type, EntityMetadata> _cache = new();

        /// <summary>
        /// Obtiene (o construye) los metadatos para un tipo.
        /// </summary>
        public static EntityMetadata GetOrAdd(Type t) => _cache.GetOrAdd(t, BuildMetadata);

        /// <summary>
        /// Obtiene el nombre de la biblioteca (esquema) para <typeparamref name="T"/>.
        /// Devuelve <c>null</c> si no está definido.
        /// </summary>
        public static string? GetLibraryFor<T>() => GetOrAdd(typeof(T)).Library;

        /// <summary>
        /// Obtiene el nombre de tabla para <typeparamref name="T"/>.
        /// Devuelve <c>null</c> si no está definido.
        /// </summary>
        public static string? GetTableFor<T>() => GetOrAdd(typeof(T)).TableName;

        /// <summary>
        /// Devuelve el nombre de columna mapeado para la propiedad indicada. 
        /// Si no hay atributo, devuelve el nombre de la propiedad tal cual.
        /// Lanza si la propiedad no existe.
        /// </summary>
        public static string GetColumnFor<T>(string propertyName)
        {
            if (string.IsNullOrWhiteSpace(propertyName))
                throw new ArgumentNullException(nameof(propertyName));

            var meta = GetOrAdd(typeof(T));
            if (meta.Columns.TryGetValue(propertyName, out var col))
                return col.ColumnName;

            // Si no hubo atributo, verificar que la propiedad exista:
            var prop = typeof(T).GetMember(propertyName, MemberTypes.Property | MemberTypes.Field,
                                           BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            if (prop is { Length: > 0 })
                return prop[0].Name; // fallback: nombre del miembro

            throw new InvalidOperationException($"La propiedad '{propertyName}' no existe en {typeof(T).Name}.");
        }

        /// <summary>
        /// Devuelve el nombre de columna mapeado para una expresión de propiedad.
        /// </summary>
        public static string GetColumnFor<T>(Expression<Func<T, object?>> propertyExpression)
        {
            var memberName = GetMemberName(propertyExpression);
            return GetColumnFor<T>(memberName);
        }

        /// <summary>
        /// Intenta recuperar los metadatos completos de la columna (tipo, tamaño, etc.).
        /// </summary>
        public static bool TryGetColumnMeta<T>(string propertyName, out ColumnMetadata? meta)
        {
            var entity = GetOrAdd(typeof(T));
            if (entity.Columns.TryGetValue(propertyName, out var col))
            {
                meta = col;
                return true;
            }

            // Fallback: si no hay atributo, construir solo con el nombre
            var prop = typeof(T).GetMember(propertyName, MemberTypes.Property | MemberTypes.Field,
                                           BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            if (prop is { Length: > 0 })
            {
                meta = new ColumnMetadata { ColumnName = prop[0].Name };
                return true;
            }

            meta = null;
            return false;
        }

        /// <summary>
        /// Construye "LIB.TABLA" si hay biblioteca, o solo "TABLA" si no.
        /// </summary>
        public static string GetQualifiedTableName<T>()
        {
            var meta = GetOrAdd(typeof(T));
            var table = meta.TableName ?? throw new InvalidOperationException(
                $"No se definió [TableName] en {typeof(T).Name}.");
            return string.IsNullOrWhiteSpace(meta.Library) ? table : $"{meta.Library}.{table}";
        }

        /// <summary>
        /// Aplica metadatos (tipo, tamaño, precisión, escala) a un parámetro DB.
        /// Siempre asigna el valor, convirtiendo <c>null</c> a <see cref="DBNull.Value"/>.
        /// </summary>
        public static void ApplyToParameter(DbParameter parameter, object? value, ColumnMetadata? meta)
        {

                pi.SetValue(parameter, boxed);
        }
    }
}        /// <param name="builder">Instancia.</param>
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

