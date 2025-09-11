using System;
using System.Globalization;

namespace QueryBuilder.Helpers
{
    // ==============================================================
    // Tipos auxiliares para valores tipados en DB2 for i (AS/400)
    // ==============================================================

    /// <summary>
    /// Representa un valor “tipado” para DB2 i que se usará con marcador <c>?</c>
    /// dentro de un fragmento SQL que declara su tipo, por ejemplo:
    /// <c>CAST(? AS VARCHAR(20))</c>, <c>TIMESTAMP(?)</c>, <c>CAST(? AS DECIMAL(10,0))</c>.
    /// <para>
    /// Útil en generadores como <c>MergeQueryBuilder.UsingValuesTyped</c> para construir
    /// <c>USING (SELECT ... FROM SYSIBM.SYSDUMMY1)</c> con parámetros posicionales.
    /// </para>
    /// </summary>
    public readonly struct Db2ITyped
    {
        /// <summary>
        /// Fragmento SQL del valor tipado (por ejemplo, <c>CAST(? AS VARCHAR(20))</c> o <c>TIMESTAMP(?)</c>).
        /// Debe contener exactamente un marcador <c>?</c>.
        /// </summary>
        public string Sql { get; }

        /// <summary>
        /// Valor a enlazar al marcador <c>?</c>. Se suele convertir a <see cref="DBNull.Value"/>
        /// por la capa de ejecución cuando es <c>null</c>.
        /// </summary>
        public object? Value { get; }

        private Db2ITyped(string sql, object? value)
        {
            Sql = sql;
            Value = value;
        }

        // -----------------------
        // Helpers de normalización
        // -----------------------

        /// <summary>Recorta la cadena a <paramref name="size"/> si excede; si es <c>null</c>, retorna cadena vacía.</summary>
        private static string Fit(string? s, int size)
        {
            if (string.IsNullOrEmpty(s)) return string.Empty;
            return s.Length > size ? s[..size] : s;
        }

        /// <summary>Convierte un <see cref="DateTime"/> al formato canónico DB2 para TIMESTAMP.</summary>
        private static string ToDb2TimestampString(DateTime dt)
            => dt.ToString("yyyy-MM-dd-HH.mm.ss.ffffff", CultureInfo.InvariantCulture);

        // -------------
        // Cadenas (CHAR/VARCHAR)
        // -------------

        /// <summary>
        /// Crea un tipado con <c>CAST(? AS VARCHAR(n))</c> recortando la cadena si excede el tamaño.
        /// </summary>
        /// <param name="value">Valor de entrada (se recorta si excede).</param>
        /// <param name="size">Longitud VARCHAR.</param>
        public static Db2ITyped VarChar(string? value, int size)
            => new($"CAST(? AS VARCHAR({size}))", Fit(value, size));

        /// <summary>
        /// Sobrecarga compatible: si <paramref name="value"/> es <see cref="string"/>, se recorta;
        /// en otro caso, se envía tal cual.
        /// </summary>
        public static Db2ITyped VarChar(object? value, int size)
            => value is string s
                ? VarChar(s, size)
                : new Db2ITyped($"CAST(? AS VARCHAR({size}))", value);

        /// <summary>
        /// Crea un tipado con <c>CAST(? AS CHAR(n))</c> recortando la cadena si excede el tamaño.
        /// (DB2 rellenará con espacios a la derecha si es más corta).
        /// </summary>
        public static Db2ITyped Char(string? value, int size)
            => new($"CAST(? AS CHAR({size}))", Fit(value, size));

        /// <summary>
        /// Sobrecarga compatible para <c>CHAR(n)</c>. Si es <see cref="string"/>, se recorta;
        /// si es <see cref="char"/>, se convierte a cadena; en otro caso, se envía tal cual.
        /// </summary>
        public static Db2ITyped Char(object? value, int size)
            => value switch
            {
                string s => Char(s, size),
                char c => Char(c.ToString(), size),
                _ => new Db2ITyped($"CAST(? AS CHAR({size}))", value)
            };

        // --------
        // Numéricos
        // --------

        /// <summary>
        /// Crea un tipado numérico con <c>CAST(? AS DECIMAL(p,s))</c>.
        /// </summary>
        public static Db2ITyped Decimal(decimal value, int precision, int scale)
            => new($"CAST(? AS DECIMAL({precision},{scale}))", value);

        /// <summary>
        /// Sobrecarga compatible para <c>DECIMAL(p,s)</c> desde <see cref="object"/>.
        /// Si es cadena, intenta parsear invariante; si falla, se envía tal cual.
        /// </summary>
        public static Db2ITyped Decimal(object? value, int precision, int scale)
        {
            if (value is string s && decimal.TryParse(s, NumberStyles.Number, CultureInfo.InvariantCulture, out var d))
                return Decimal(d, precision, scale);

            return new Db2ITyped($"CAST(? AS DECIMAL({precision},{scale}))", value);
        }

        /// <summary>Crea un tipado entero con <c>CAST(? AS INTEGER)</c>.</summary>
        public static Db2ITyped Integer(int value) => new("CAST(? AS INTEGER)", value);

        /// <summary>
        /// Sobrecarga compatible para <c>INTEGER</c>.
        /// </summary>
        public static Db2ITyped Integer(object? value) => new("CAST(? AS INTEGER)", value);

        /// <summary>Crea un tipado entero grande con <c>CAST(? AS BIGINT)</c>.</summary>
        public static Db2ITyped BigInt(long value) => new("CAST(? AS BIGINT)", value);

        /// <summary>Sobrecarga compatible para <c>BIGINT</c>.</summary>
        public static Db2ITyped BigInt(object? value) => new("CAST(? AS BIGINT)", value);

        /// <summary>Crea un tipado para <c>DOUBLE</c> con <c>CAST(? AS DOUBLE)</c>.</summary>
        public static Db2ITyped Double(double value) => new("CAST(? AS DOUBLE)", value);

        /// <summary>Sobrecarga compatible para <c>DOUBLE</c>.</summary>
        public static Db2ITyped Double(object? value) => new("CAST(? AS DOUBLE)", value);

        // ----------
        // Temporalidad
        // ----------

        /// <summary>
        /// Crea un tipado para <c>TIMESTAMP</c> enviando el valor como <b>cadena canónica DB2</b>
        /// (<c>yyyy-MM-dd-HH.mm.ss.ffffff</c>) y usando <c>TIMESTAMP(?)</c> en el SQL.
        /// <para>Esto evita errores de conversión del proveedor OleDb (overflow/mismatch).</para>
        /// </summary>
        public static Db2ITyped Timestamp(DateTime value)
            => new("TIMESTAMP(?)", ToDb2TimestampString(value));

        /// <summary>
        /// Sobrecarga para <c>TIMESTAMP</c> que acepta <see cref="DateTime?"/>.
        /// Si es <c>null</c>, envía <c>null</c> (lo que resultará en <c>NULL</c>).
        /// </summary>
        public static Db2ITyped Timestamp(DateTime? value)
            => value.HasValue
                ? new Db2ITyped("TIMESTAMP(?)", ToDb2TimestampString(value.Value))
                : new Db2ITyped("TIMESTAMP(?)", null);

        /// <summary>
        /// Crea un tipado para <c>TIMESTAMP</c> asumiendo que la cadena ya viene en formato DB2
        /// (<c>yyyy-MM-dd-HH.mm.ss[.ffffff]</c>). No modifica el contenido.
        /// </summary>
        public static Db2ITyped Timestamp(string db2TimestampLiteral)
            => new("TIMESTAMP(?)", db2TimestampLiteral ?? string.Empty);

        /// <summary>
        /// Crea un tipado para <c>DATE</c> enviando un string <c>yyyy-MM-dd</c> y usando <c>DATE(?)</c>.
        /// </summary>
        public static Db2ITyped Date(DateTime value)
            => new("DATE(?)", value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));

        /// <summary>
        /// Crea un tipado para <c>TIME</c> enviando un string <c>HH.mm.ss</c> y usando <c>TIME(?)</c>.
        /// </summary>
        public static Db2ITyped Time(TimeSpan value)
            => new("TIME(?)", DateTime.MinValue.Add(value).ToString("HH.mm.ss", CultureInfo.InvariantCulture));

        /// <summary>
        /// Sobrecarga para <c>TIME</c> desde <see cref="DateTime"/> (toma la parte de hora).
        /// </summary>
        public static Db2ITyped Time(DateTime value)
            => new("TIME(?)", value.ToString("HH.mm.ss", CultureInfo.InvariantCulture));
    }
}
