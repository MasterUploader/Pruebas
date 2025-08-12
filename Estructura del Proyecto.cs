#region Helpers (IN / OUT) — uso simple y sin ambigüedades

/// <summary>
/// Declara un parámetro <c>OUT</c> de tipo <c>NVARCHAR/VARCHAR</c> (según proveedor) con tamaño fijo.
/// Evita la ambigüedad de overloads cuando solo se requiere <paramref name="size"/>.
/// </summary>
/// <param name="name">Nombre lógico del parámetro (clave para recuperar el valor).</param>
/// <param name="size">Tamaño máximo del texto.</param>
/// <param name="initialValue">
/// Valor inicial opcional. Si se establece, el parámetro operará como <c>INOUT</c> en lugar de <c>OUT</c>.
/// </param>
public ProgramCallBuilder OutString(string name, int size, object? initialValue = null)
    => Out(name: name, dbType: DbType.String, size: size, precision: null, scale: null, initialValue: initialValue);

/// <summary>
/// Declara un parámetro <c>OUT</c> de tipo carácter fijo (por ejemplo <c>CHAR(n)</c> / ANSI fixed).
/// Útil cuando el programa espera padding a derecha.
/// </summary>
/// <param name="name">Nombre lógico del parámetro.</param>
/// <param name="size">Longitud fija.</param>
/// <param name="initialValue">Si se define, el parámetro será <c>INOUT</c>.</param>
public ProgramCallBuilder OutChar(string name, int size, object? initialValue = null)
    => Out(name: name, dbType: DbType.AnsiStringFixedLength, size: size, precision: null, scale: null, initialValue: initialValue);

/// <summary>
/// Declara un parámetro <c>OUT</c> decimal con precisión/escala explícitas (p.ej. <c>DEC(10,0)</c>).
/// </summary>
/// <param name="name">Nombre lógico del parámetro.</param>
/// <param name="precision">Número total de dígitos.</param>
/// <param name="scale">Dígitos a la derecha del punto decimal.</param>
/// <param name="initialValue">Si se define, el parámetro será <c>INOUT</c>.</param>
public ProgramCallBuilder OutDecimal(string name, byte precision, byte scale, object? initialValue = null)
    => Out(name: name, dbType: DbType.Decimal, size: null, precision: precision, scale: scale, initialValue: initialValue);

/// <summary>
/// Declara un parámetro <c>OUT</c> entero de 32 bits. Útil para códigos numéricos simples.
/// </summary>
/// <param name="name">Nombre lógico del parámetro.</param>
/// <param name="initialValue">Si se define, el parámetro será <c>INOUT</c>.</param>
public ProgramCallBuilder OutInt32(string name, int? initialValue = null)
    => Out(name: name, dbType: DbType.Int32, size: null, precision: null, scale: null, initialValue: initialValue);

/// <summary>
/// Declara un parámetro <c>OUT</c> fecha/hora (p.ej. <c>DATE</c>/<c>TIMESTAMP</c> según proveedor).
/// </summary>
/// <param name="name">Nombre lógico del parámetro.</param>
/// <param name="initialValue">Si se define, el parámetro será <c>INOUT</c>.</param>
public ProgramCallBuilder OutDateTime(string name, DateTime? initialValue = null)
    => Out(name: name, dbType: DbType.DateTime, size: null, precision: null, scale: null, initialValue: initialValue);

/// <summary>
/// Agrega un parámetro de entrada <c>IN</c> string (NVARCHAR/VARCHAR) con tamaño opcional.
/// </summary>
/// <param name="value">Valor del parámetro (se permite <c>null</c>).</param>
/// <param name="size">Tamaño máximo; si se omite, el proveedor lo inferirá.</param>
public ProgramCallBuilder InString(string? value, int? size = null)
    => In(value, dbType: DbType.String, size: size);

/// <summary>
/// Agrega un parámetro de entrada <c>IN</c> carácter fijo (por ejemplo <c>CHAR(n)</c>).
/// </summary>
/// <param name="value">Valor del parámetro (se permite <c>null</c>).</param>
/// <param name="size">Longitud fija requerida.</param>
public ProgramCallBuilder InChar(string? value, int size)
    => In(value, dbType: DbType.AnsiStringFixedLength, size: size);

/// <summary>
/// Agrega un parámetro de entrada <c>IN</c> decimal con precisión/escala opcionales.
/// </summary>
/// <param name="value">Valor decimal (se permite <c>null</c>).</param>
/// <param name="precision">Dígitos totales (opcional).</param>
/// <param name="scale">Dígitos decimales (opcional).</param>
public ProgramCallBuilder InDecimal(decimal? value, byte? precision = null, byte? scale = null)
    => In(value, dbType: DbType.Decimal, size: null, precision: precision, scale: scale);

/// <summary>
/// Agrega un parámetro de entrada <c>IN</c> entero de 32 bits.
/// </summary>
/// <param name="value">Valor entero (se permite <c>null</c>).</param>
public ProgramCallBuilder InInt32(int? value)
    => In(value, dbType: DbType.Int32);

/// <summary>
/// Agrega un parámetro de entrada <c>IN</c> fecha/hora.
/// </summary>
/// <param name="value">Fecha/hora (se permite <c>null</c>).</param>
public ProgramCallBuilder InDateTime(DateTime? value)
    => In(value, dbType: DbType.DateTime);

#endregion
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

