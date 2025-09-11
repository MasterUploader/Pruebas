Actualmente tengo así el struct:

namespace QueryBuilder.Helpers;

// ==============================================================
// Tipos auxiliares para valores tipados en DB2 i
// ==============================================================

/// <summary>
/// Representa un valor “tipado” para DB2 i que va a usarse en SELECT (USING) con marcador <c>?</c>
/// y una anotación de tipo, por ejemplo <c>CAST(? AS VARCHAR(20))</c>, <c>CAST(? AS TIMESTAMP)</c>.
/// </summary>
public readonly struct Db2ITyped
{
    /// <summary>Fragmento SQL del valor tipado (por ejemplo, <c>CAST(? AS VARCHAR(20))</c>).</summary>
    public string Sql { get; }

    /// <summary>Valor a enlazar al marcador <c>?</c>.</summary>
    public object? Value { get; }

    private Db2ITyped(string sql, object? value)
    {
        Sql = sql;
        Value = value;
    }

    /// <summary>Crea un tipado genérico con <c>CAST(? AS VARCHAR(n))</c>.</summary>
    public static Db2ITyped VarChar(object? value, int size) => new($"CAST(? AS VARCHAR({size}))", value);

    /// <summary>Crea un tipado genérico con <c>CAST(? AS CHAR(n))</c>.</summary>
    public static Db2ITyped Char(object? value, int size) => new($"CAST(? AS CHAR({size}))", value);

    /// <summary>Crea un tipado numérico con <c>CAST(? AS DECIMAL(p,s))</c>.</summary>
    public static Db2ITyped Decimal(object? value, int precision, int scale) => new($"CAST(? AS DECIMAL({precision},{scale}))", value);

    /// <summary>Crea un tipado para timestamp con <c>CAST(? AS TIMESTAMP)</c>.</summary>
    public static Db2ITyped Timestamp(object? value) => new("CAST(? AS TIMESTAMP)", value);

    /// <summary>Crea un tipado entero con <c>CAST(? AS INTEGER)</c>.</summary>
    public static Db2ITyped Integer(object? value) => new("CAST(? AS INTEGER)", value);

    /// <summary>Crea un tipado entero grande con <c>CAST(? AS BIGINT)</c>.</summary>
    public static Db2ITyped BigInt(object? value) => new("CAST(? AS BIGINT)", value);

    /// <summary>Crea un tipado para <c>DOUBLE</c> con <c>CAST(? AS DOUBLE)</c>.</summary>
    public static Db2ITyped Double(object? value) => new("CAST(? AS DOUBLE)", value);
}



Aplica las mejoras para y entregame el struct Db2ITyped completo
