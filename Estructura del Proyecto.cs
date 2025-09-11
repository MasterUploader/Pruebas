using System;
using System.Globalization;

namespace QueryBuilder.Helpers
{
    // ==============================================================
    // Tipos auxiliares para valores tipados en DB2 for i (AS/400)
    // ==============================================================

    /// <summary>
    /// Valor “tipado” para DB2 i: genera un fragmento con un <c>?</c> y el tipo,
    /// p. ej. <c>CAST(? AS VARCHAR(20))</c>, <c>CAST(? AS TIMESTAMP)</c>, etc.
    /// Pensado para <c>USING (SELECT ... FROM SYSIBM.SYSDUMMY1)</c> y MERGE/INSERT.
    /// </summary>
    public readonly struct Db2ITyped
    {
        /// <summary>Fragmento SQL con el marcador <c>?</c> y el tipo (p. ej. <c>CAST(? AS VARCHAR(20))</c>).</summary>
        public string Sql { get; }

        /// <summary>Valor que se ligará al marcador <c>?</c>.</summary>
        public object? Value { get; }

        private Db2ITyped(string sql, object? value)
        {
            Sql = sql;
            Value = value;
        }

        // -----------------------
        // Helpers de normalización
        // -----------------------

        /// <summary>Recorta a <paramref name="size"/> si excede; null → "".</summary>
        private static string Fit(string? s, int size)
            => string.IsNullOrEmpty(s) ? string.Empty : (s.Length > size ? s[..size] : s);

        /// <summary>Convierte a timestamp canónico DB2: yyyy-MM-dd-HH.mm.ss.ffffff</summary>
        private static string ToDb2Ts(DateTime dt)
            => dt.ToString("yyyy-MM-dd-HH.mm.ss.ffffff", CultureInfo.InvariantCulture);

        // -------------
        // Cadenas
        // -------------

        public static Db2ITyped VarChar(string? value, int size)
            => new($"CAST(? AS VARCHAR({size}))", Fit(value, size));

        public static Db2ITyped VarChar(object? value, int size)
            => value is string s ? VarChar(s, size)
                                 : new($"CAST(? AS VARCHAR({size}))", value);

        public static Db2ITyped Char(string? value, int size)
            => new($"CAST(? AS CHAR({size}))", Fit(value, size));

        public static Db2ITyped Char(object? value, int size)
            => value switch
            {
                string s => Char(s, size),
                char c   => Char(c.ToString(), size),
                _        => new Db2ITyped($"CAST(? AS CHAR({size}))", value)
            };

        // --------
        // Numéricos
        // --------

        public static Db2ITyped Decimal(decimal value, int precision, int scale)
            => new($"CAST(? AS DECIMAL({precision},{scale}))", value);

        public static Db2ITyped Decimal(object? value, int precision, int scale)
        {
            if (value is string s &&
                decimal.TryParse(s, NumberStyles.Number, CultureInfo.InvariantCulture, out var d))
                return Decimal(d, precision, scale);

            return new($"CAST(? AS DECIMAL({precision},{scale}))", value);
        }

        public static Db2ITyped Integer(int value)  => new("CAST(? AS INTEGER)", value);
        public static Db2ITyped Integer(object? v)  => new("CAST(? AS INTEGER)", v);
        public static Db2ITyped BigInt(long value)  => new("CAST(? AS BIGINT)", value);
        public static Db2ITyped BigInt(object? v)   => new("CAST(? AS BIGINT)", v);
        public static Db2ITyped Double(double v)    => new("CAST(? AS DOUBLE)", v);
        public static Db2ITyped Double(object? v)   => new("CAST(? AS DOUBLE)", v);

        // ----------
        // Temporalidad
        // ----------

        /// <summary>
        /// TIMESTAMP tipado usando <c>CAST(? AS TIMESTAMP)</c>. Envía el valor como
        /// string canónico DB2 (yyyy-MM-dd-HH.mm.ss.ffffff) para evitar desbordes del proveedor.
        /// </summary>
        public static Db2ITyped Timestamp(DateTime value)
            => new("CAST(? AS TIMESTAMP)", ToDb2Ts(value));

        public static Db2ITyped Timestamp(DateTime? value)
            => value.HasValue ? new Db2ITyped("CAST(? AS TIMESTAMP)", ToDb2Ts(value.Value))
                              : new Db2ITyped("CAST(? AS TIMESTAMP)", null);

        /// <summary>
        /// TIMESTAMP desde cadena ya formateada en canónico DB2
        /// (yyyy-MM-dd-HH.mm.ss[.ffffff]).
        /// </summary>
        public static Db2ITyped Timestamp(string db2Canonical)
            => new("CAST(? AS TIMESTAMP)", db2Canonical ?? string.Empty);

        /// <summary>DATE como <c>CAST(? AS DATE)</c> con valor <c>yyyy-MM-dd</c>.</summary>
        public static Db2ITyped Date(DateTime value)
            => new("CAST(? AS DATE)", value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));

        /// <summary>TIME como <c>CAST(? AS TIME)</c> con valor <c>HH.mm.ss</c>.</summary>
        public static Db2ITyped Time(TimeSpan value)
            => new("CAST(? AS TIME)", DateTime.MinValue.Add(value).ToString("HH.mm.ss", CultureInfo.InvariantCulture));

        public static Db2ITyped Time(DateTime value)
            => new("CAST(? AS TIME)", value.ToString("HH.mm.ss", CultureInfo.InvariantCulture));
    }
}
