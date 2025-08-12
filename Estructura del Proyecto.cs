using System;
using System.Collections.Generic;
using System.Reflection;

namespace RestUtilities.Connections.Helpers
{
    /// <summary>
    /// Resultado de invocar un programa CLLE/RPGLE mediante <c>CALL</c>.
    /// Contiene el número de filas afectadas y los valores de parámetros OUT/INOUT devueltos por el programa.
    /// </summary>
    public sealed class ProgramCallResult
    {
        /// <summary>
        /// Obtiene o establece el número de filas afectadas reportado por la ejecución del comando.
        /// Para llamadas <c>CALL</c> suele ser 0, pero si el programa realiza operaciones DML podría ser &gt; 0.
        /// </summary>
        public int RowsAffected { get; internal set; }

        /// <summary>
        /// Diccionario inmutable con los valores finales de los parámetros de salida (OUT/INOUT),
        /// indexados por la clave lógica (normalmente el nombre de parámetro declarado).
        /// </summary>
        public IReadOnlyDictionary<string, object?> OutValues => _outValues;
        private readonly Dictionary<string, object?> _outValues = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Agrega o reemplaza un valor OUT/INOUT en el resultado. Uso interno del builder.
        /// </summary>
        /// <param name="name">Nombre lógico del parámetro (clave).</param>
        /// <param name="value">Valor devuelto por el motor; puede ser <see cref="DBNull"/> o <c>null</c>.</param>
        internal void AddOut(string name, object? value) => _outValues[name] = value;

        /// <summary>
        /// Intenta obtener un valor OUT/INOUT fuertemente tipado.
        /// </summary>
        /// <typeparam name="T">Tipo de destino (por ejemplo, <c>int</c>, <c>decimal</c>, <c>string</c>).</typeparam>
        /// <param name="key">Nombre lógico del parámetro OUT/INOUT.</param>
        /// <param name="value">Valor convertido a <typeparamref name="T"/> si existe y puede convertirse.</param>
        /// <returns><c>true</c> si la clave existe y la conversión fue exitosa; de lo contrario, <c>false</c>.</returns>
        public bool TryGet<T>(string key, out T? value)
        {
            if (_outValues.TryGetValue(key, out var raw))
            {
                value = TypeCoercion.ChangeType<T>(raw);
                return true;
            }
            value = default;
            return false;
        }

        /// <summary>
        /// Mapea los valores OUT/INOUT a un DTO de salida, mediante un mapeo declarativo OUT→Propiedad.
        /// </summary>
        /// <typeparam name="T">Tipo del DTO de destino. Debe tener un constructor público sin parámetros.</typeparam>
        /// <param name="map">
        /// Acción que configura las asociaciones entre claves OUT y propiedades del DTO usando <see cref="OutputMapBuilder{T}"/>.
        /// </param>
        /// <returns>Instancia de <typeparamref name="T"/> con las propiedades asignadas desde los OUT encontrados.</returns>
        /// <remarks>
        /// Solo se asignan las claves OUT que existan en <see cref="OutValues"/> y tengan una propiedad asociada.
        /// Las conversiones usan <see cref="Convert.ChangeType(object, Type)"/> con manejo básico para enums y nullables.
        /// </remarks>
        public T MapTo<T>(Action<OutputMapBuilder<T>> map) where T : new()
        {
            if (map is null) throw new ArgumentNullException(nameof(map));

            var builder = new OutputMapBuilder<T>();
            map(builder);

            var target = new T();

            foreach (var kv in builder.Bindings)
            {
                var outKey = kv.Key;
                var prop = kv.Value;

                if (_outValues.TryGetValue(outKey, out var raw))
                {
                    var converted = TypeCoercion.ChangeType(raw, prop.PropertyType);
                    prop.SetValue(target, converted);
                }
            }

            return target;
        }

        /// <summary>
        /// Utilidades internas para conversión de tipos comunes, incluyendo nullables y enums.
        /// </summary>
        private static class TypeCoercion
        {
            public static T? ChangeType<T>(object? value)
            {
                if (value is null || value is DBNull) return default;
                var target = typeof(T);
                return (T?)ChangeType(value, target);
            }

            public static object? ChangeType(object? value, Type destinationType)
            {
                if (value is null || value is DBNull) return null;

                var nonNullable = Nullable.GetUnderlyingType(destinationType) ?? destinationType;

                // Enum: soporta fuente numérica o string
                if (nonNullable.IsEnum)
                {
                    if (value is string s)
                        return Enum.Parse(nonNullable, s, ignoreCase: true);

                    var underlying = Enum.GetUnderlyingType(nonNullable);
                    var numeric = Convert.ChangeType(value, underlying);
                    return Enum.ToObject(nonNullable, numeric!);
                }

                // Convert.ChangeType maneja la mayoría de casos básicos
                return Convert.ChangeType(value, nonNullable);
            }
        }
    }

    /// <summary>
    /// Builder para declarar el mapeo entre claves OUT/INOUT devueltas por el programa
    /// y propiedades del DTO de salida al usar <see cref="ProgramCallResult.MapTo{T}(Action{OutputMapBuilder{T}})"/>.
    /// </summary>
    /// <typeparam name="T">Tipo del DTO de salida.</typeparam>
    public sealed class OutputMapBuilder<T> where T : new()
    {
        /// <summary>
        /// Colección interna de asociaciones OUT→Propiedad.
        /// </summary>
        internal Dictionary<string, PropertyInfo> Bindings { get; } = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Asocia la clave OUT/INOUT <paramref name="outName"/> a una propiedad del DTO <typeparamref name="T"/>.
        /// </summary>
        /// <param name="outName">Nombre lógico del parámetro OUT/INOUT (clave en <see cref="ProgramCallResult.OutValues"/>).</param>
        /// <param name="propertyName">
        /// Nombre de la propiedad pública del DTO a la que se asignará el valor.
        /// Use esta sobrecarga cuando no quiera usar expresiones lambda.
        /// </param>
        /// <returns>El mismo builder para encadenar llamadas.</returns>
        /// <exception cref="ArgumentNullException">Si <paramref name="outName"/> o <paramref name="propertyName"/> es nulo o vacío.</exception>
        /// <exception cref="ArgumentException">Si la propiedad no existe o no es legible/escribible.</exception>
        public OutputMapBuilder<T> Bind(string outName, string propertyName)
        {
            if (string.IsNullOrWhiteSpace(outName)) throw new ArgumentNullException(nameof(outName));
            if (string.IsNullOrWhiteSpace(propertyName)) throw new ArgumentNullException(nameof(propertyName));

            var prop = typeof(T).GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
            if (prop is null || !prop.CanWrite)
                throw new ArgumentException($"La propiedad '{propertyName}' no existe o no es asignable en el tipo {typeof(T).Name}.");

            Bindings[outName] = prop;
            return this;
        }

        /// <summary>
        /// Asocia la clave OUT/INOUT <paramref name="outName"/> a una propiedad del DTO mediante expresión lambda.
        /// </summary>
        /// <param name="outName">Nombre lógico del parámetro OUT/INOUT.</param>
        /// <param name="selector">
        /// Expresión que selecciona la propiedad del DTO, por ejemplo: <c>x =&gt; x.Codigo</c>.
        /// </param>
        /// <returns>El mismo builder para encadenar llamadas.</returns>
        /// <exception cref="ArgumentNullException">Si <paramref name="outName"/> o <paramref name="selector"/> es nulo.</exception>
        /// <exception cref="InvalidOperationException">Si la expresión no apunta a una propiedad válida.</exception>
        public OutputMapBuilder<T> Bind(string outName, System.Linq.Expressions.Expression<Func<T, object?>> selector)
        {
            if (string.IsNullOrWhiteSpace(outName)) throw new ArgumentNullException(nameof(outName));
            if (selector is null) throw new ArgumentNullException(nameof(selector));

            var member = selector.Body as System.Linq.Expressions.MemberExpression
                         ?? (selector.Body as System.Linq.Expressions.UnaryExpression)?.Operand as System.Linq.Expressions.MemberExpression;

            if (member?.Member is not PropertyInfo pi || !pi.CanWrite)
                throw new InvalidOperationException("El selector debe apuntar a una propiedad pública asignable del DTO.");

            Bindings[outName] = pi;
            return this;
        }
    }
}
