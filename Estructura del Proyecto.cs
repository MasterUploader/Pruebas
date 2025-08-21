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
            if (parameter == null) throw new ArgumentNullException(nameof(parameter));

            // Tipo
            if (meta?.DbType is DbType dbt) parameter.DbType = dbt;

            // Tamaño
            if (meta?.Size is int size && size > 0) parameter.Size = size;

            // Precisión / Escala (si el proveedor las soporta)
            // OleDbParameter, SqlParameter, etc. suelen exponer estas propiedades.
            TrySetNumericProperty(parameter, "Precision", meta?.Precision);
            TrySetNumericProperty(parameter, "Scale", meta?.Scale);

            // Valor
            parameter.Value = value ?? DBNull.Value;
        }

        // ---------------------------
        // Helpers privados
        // ---------------------------

        private static EntityMetadata BuildMetadata(Type t)
        {
            var lib = t.GetCustomAttribute<LibraryAttribute>(inherit: true)?.Name;
            var tab = t.GetCustomAttribute<TableNameAttribute>(inherit: true)?.Name;

            var dict = new Dictionary<string, ColumnMetadata>(StringComparer.OrdinalIgnoreCase);

            foreach (var m in t.GetMembers(BindingFlags.Public | BindingFlags.Instance))
            {
                if (m is not PropertyInfo && m is not FieldInfo) continue;

                // ColumnAttribute
                var colAttr = m.GetCustomAttribute<ColumnAttribute>(inherit: true);
                // ParameterNameAttribute (compat)
                var parAttr = m.GetCustomAttribute<ParameterNameAttribute>(inherit: true);

                // Resolver preferencia: Column > ParameterName
                if (colAttr is not null)
                {
                    dict[m.Name] = new ColumnMetadata
                    {
                        ColumnName = colAttr.Name ?? m.Name,
                        DbType = colAttr.DbType,
                        Size = colAttr.Size,
                        Precision = colAttr.Precision,
                        Scale = colAttr.Scale
                    };
                }
                else if (parAttr is not null)
                {
                    dict[m.Name] = new ColumnMetadata
                    {
                        ColumnName = parAttr.Name ?? m.Name,
                        DbType = parAttr.DbType,
                        Size = parAttr.Size,
                        Precision = parAttr.Precision,
                        Scale = parAttr.Scale
                    };
                }
                else
                {
                    // Sin atributo: fallback al nombre del miembro
                    dict[m.Name] = new ColumnMetadata { ColumnName = m.Name };
                }
            }

            return new EntityMetadata
            {
                Library = lib,
                TableName = tab,
                Columns = dict
            };
        }

        private static string GetMemberName(LambdaExpression expr)
        {
            if (expr.Body is MemberExpression m) return m.Member.Name;
            if (expr.Body is UnaryExpression u && u.Operand is MemberExpression um) return um.Member.Name;
            throw new InvalidOperationException("La expresión debe referenciar una propiedad o campo público.");
        }

        private static void TrySetNumericProperty(DbParameter parameter, string propertyName, byte? value)
        {
            if (value is null) return;

            var pi = parameter.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
            if (pi is null || !pi.CanWrite) return;

            // Precision/Scale suelen ser byte en la mayoría de proveedores .NET
            // Si el proveedor usa otro tipo, intentamos convertir.
            object boxed = value.Value;
            if (pi.PropertyType == typeof(byte))
                pi.SetValue(parameter, value.Value);
            else if (pi.PropertyType == typeof(short))
                pi.SetValue(parameter, (short)value.Value);
            else if (pi.PropertyType == typeof(int))
                pi.SetValue(parameter, (int)value.Value);
            else
                pi.SetValue(parameter, boxed);
        }
    }
}
