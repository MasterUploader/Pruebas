using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using QueryBuilder.Builders;
using QueryBuilder.Models;

#nullable enable

namespace QueryBuilder.ReflectionMap
{
    /// <summary>
    /// Marca el nombre de biblioteca/esquema (AS400, etc.) para un DTO que representa una tabla.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = true)]
    public sealed class LibraryAttribute : Attribute
    {
        public string Name { get; }
        public LibraryAttribute(string name) => Name = name;
    }

    /// <summary>
    /// Marca el nombre de la tabla para un DTO.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = true)]
    public sealed class TableNameAttribute : Attribute
    {
        public string Name { get; }
        public TableNameAttribute(string name) => Name = name;
    }

    /// <summary>
    /// Marca una propiedad (en cualquier nivel) como mapeada a una columna de la tabla.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, Inherited = true)]
    public sealed class ColumnAttribute : Attribute
    {
        public string Name { get; }
        public ColumnAttribute(string name) => Name = name;
    }

    /// <summary>
    /// Marca una propiedad para que no sea considerada en el aplanado.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, Inherited = true)]
    public sealed class IgnoreAttribute : Attribute { }

    /// <summary>
    /// Opcional: marca propiedades complejas que deban considerarse embebidas (normalmente
    /// se detectan automáticamente; este atributo solo sirve para documentar la intención).
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, Inherited = true)]
    public sealed class EmbeddedAttribute : Attribute { }

    /// <summary>
    /// Utilidades para aplanar objetos (con clases anidadas) hacia un INSERT de una sola tabla.
    /// Respeta metadatos por atributos y genera SQL parametrizado con InsertQueryBuilder.
    /// </summary>
    public static class SingleTableInsertMapper
    {
        /// <summary>
        /// Genera un INSERT (SQL + parámetros) para una sola tabla a partir de un modelo con clases anidadas.
        /// Solo las propiedades decoradas con <see cref="ColumnAttribute"/> serán mapeadas a columnas,
        /// sin importar el nivel de anidación.
        /// </summary>
        /// <typeparam name="T">Tipo del modelo raíz (decorado con <see cref="TableNameAttribute"/> y opcionalmente <see cref="LibraryAttribute"/>).</typeparam>
        /// <param name="model">Instancia a insertar.</param>
        /// <returns><see cref="QueryResult"/> con SQL parametrizado y lista de parámetros.</returns>
        /// <exception cref="InvalidOperationException">
        /// Se lanza si faltan metadatos obligatorios o no se encuentra ninguna columna mapeada.
        /// </exception>
        public static QueryResult BuildInsert<T>(T model) where T : class
        {
            if (model is null) throw new ArgumentNullException(nameof(model));

            var (library, table) = GetTableInfo(typeof(T));

            // Recolecta (columna, valor) desde el objeto, recorriendo recursivamente.
            var pairs = new List<(string Column, object? Value)>();
            CollectColumns(model, pairs);

            if (pairs.Count == 0)
                throw new InvalidOperationException("No se encontró ninguna propiedad con [Column] para insertar.");

            // Orden estable por nombre de columna para tener SQL determinístico:
            var ordered = pairs
                .GroupBy(p => p.Column, StringComparer.OrdinalIgnoreCase) // si hay duplicados, último gana
                .Select(g => g.Last())
                .OrderBy(p => p.Column, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            // Construimos el InsertQueryBuilder usando la sobrecarga segura con placeholders.
            var builder = new InsertQueryBuilder(table, library)
                .Values(ordered.Select(x => (x.Column, x.Value)).ToArray());

            return builder.Build();
        }

        /// <summary>
        /// Obtiene la dupla (library, table) desde atributos en la clase.
        /// </summary>
        private static (string? Library, string Table) GetTableInfo(Type t)
        {
            var tableAttr = t.GetCustomAttribute<TableNameAttribute>(inherit: true)
                ?? throw new InvalidOperationException($"El tipo {t.Name} debe tener [TableName(\"...\")].");

            var libAttr = t.GetCustomAttribute<LibraryAttribute>(inherit: true);
            return (libAttr?.Name, tableAttr.Name);
        }

        /// <summary>
        /// Recorre recursivamente el objeto, tomando únicamente propiedades con [Column].
        /// Si encuentra propiedades complejas (clases anidadas), las atraviesa.
        /// </summary>
        private static void CollectColumns(object obj, List<(string Column, object? Value)> acc)
        {
            var type = obj.GetType();

            foreach (var prop in GetReadableInstanceProperties(type))
            {
                if (prop.GetCustomAttribute<IgnoreAttribute>(inherit: true) != null)
                    continue;

                var value = prop.GetValue(obj);
                var colAttr = prop.GetCustomAttribute<ColumnAttribute>(inherit: true);

                if (colAttr != null)
                {
                    // Es columna mapeada y puede ser de cualquier tipo simple; si es null, se envía DBNull luego.
                    acc.Add((colAttr.Name, value));
                    continue;
                }

                // Si no es columna directa, y es una clase compleja, intentamos aplanarla.
                if (value is null) continue; // nada que aplanar

                var propType = prop.PropertyType;

                if (IsSimple(propType) || IsEnumerable(propType))
                {
                    // Para single-table insert NO aplanamos colecciones ni simples sin [Column].
                    // (Colecciones implicarían varias filas; tu caso es 1 sola fila.)
                    continue;
                }

                // Es una clase compleja (no colección, no simple): aplanar recursivamente.
                CollectColumns(value, acc);
            }
        }

        /// <summary>
        /// Determina si un tipo es "simple" (valor, string, DateTime, Guid, etc.) para efectos de aplanado.
        /// </summary>
        private static bool IsSimple(Type t)
        {
            t = Nullable.GetUnderlyingType(t) ?? t;

            return t.IsPrimitive
                || t.IsEnum
                || t == typeof(string)
                || t == typeof(decimal)
                || t == typeof(DateTime)
                || t == typeof(DateTimeOffset)
                || t == typeof(Guid)
                || t == typeof(TimeSpan);
        }

        /// <summary>
        /// Determina si el tipo implementa IEnumerable (pero no string).
        /// </summary>
        private static bool IsEnumerable(Type t)
        {
            if (t == typeof(string)) return false;
            return typeof(IEnumerable).IsAssignableFrom(t);
        }

        /// <summary>
        /// Obtiene propiedades públicas de instancia, legibles.
        /// </summary>
        private static IEnumerable<PropertyInfo> GetReadableInstanceProperties(Type t) =>
            t.GetProperties(BindingFlags.Instance | BindingFlags.Public)
             .Where(p => p.CanRead);
    }
}






