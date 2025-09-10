namespace QueryBuilder.Builders;

/// <summary>
/// Helper para declarar parámetros **tipados** en DB2 for i cuando se usan
/// marcadores en <c>USING (VALUES ...)</c> de un MERGE.
/// Genera el fragmento SQL (por ejemplo: <c>CAST(? AS VARCHAR(64))</c>, <c>TIMESTAMP(?)</c>)
/// y conserva el valor que debe ir en la lista de parámetros.
/// </summary>
public sealed class Db2iTyped
{
    /// <summary>Fragmento SQL del placeholder tipado (ej. <c>CAST(? AS VARCHAR(64))</c>).</summary>
    public string Sql { get; }

    /// <summary>Valor del parámetro que se enlazará al marcador <c>?</c>.</summary>
    public object? Value { get; }

    private Db2iTyped(string sql, object? value)
    {
        Sql = sql;
        Value = value;
    }

    /// <summary>Crea un <c>CAST(? AS CHAR(n))</c>.</summary>
    public static Db2iTyped Char(object? value, int length)
        => new($"CAST(? AS CHAR({length}))", value ?? string.Empty);

    /// <summary>Crea un <c>CAST(? AS VARCHAR(n))</c>.</summary>
    public static Db2iTyped VarChar(object? value, int length)
        => new($"CAST(? AS VARCHAR({length}))", value ?? string.Empty);

    /// <summary>
    /// Crea un <c>TIMESTAMP(?)</c>. Acepta <see cref="DateTime"/> o cadena en formato
    /// compatible con DB2 (por ejemplo, <c>yyyy-MM-dd-HH.mm.ss</c>).
    /// </summary>
    public static Db2iTyped Timestamp(object value)
        => new("TIMESTAMP(?)", value);

    /// <summary>Crea un <c>CAST(? AS INTEGER)</c>.</summary>
    public static Db2iTyped Int32(int value)
        => new("CAST(? AS INTEGER)", value);

    /// <summary>Crea un <c>CAST(? AS BIGINT)</c>.</summary>
    public static Db2iTyped BigInt(long value)
        => new("CAST(? AS BIGINT)", value);

    /// <summary>
    /// Crea un <c>CAST(? AS DECIMAL(p,s))</c>.
    /// </summary>
    public static Db2iTyped Decimal(object? value, int precision, int scale)
        => new($"CAST(? AS DECIMAL({precision},{scale}))", value ?? 0m);

    /// <summary>Crea un <c>CAST(? AS DATE)</c>. Acepta <see cref="DateTime"/> o string.</summary>
    public static Db2iTyped Date(object value)
        => new("CAST(? AS DATE)", value);

    /// <summary>Crea un <c>CAST(? AS TIME)</c>. Acepta <see cref="DateTime"/> o string.</summary>
    public static Db2iTyped Time(object value)
        => new("CAST(? AS TIME)", value);
}




namespace QueryBuilder.Builders;

public partial class MergeQueryBuilder
{
    // Internos que ya debes tener en la clase:
    private readonly List<string> _usingSourceColumns = [];   // alias S(...) – nombres de columnas de la fuente
    private readonly List<string> _usingValueSql = [];        // cada item: CAST(? AS ...), TIMESTAMP(?), etc.
    private readonly List<object?> _parameters = [];          // parámetros en orden
    // ...y en Build() usas _usingValueSql para armar (VALUES(...)) y devuelves _parameters en QueryResult.

    /// <summary>
    /// Define la fila de <c>USING (VALUES ...)</c> con **placeholders tipados para DB2 for i**.
    /// <para>
    /// Ejemplo:
    /// <code>
    /// .UsingValuesTyped(
    ///     ("UID",  Db2iTyped.VarChar(userId, 20)),
    ///     ("NOWTS",Db2iTyped.Timestamp(now)),
    ///     ("EXI",  Db2iTyped.Char(exitoso, 1)),
    ///     ("IP",   Db2iTyped.VarChar(ip, 64)),
    ///     ("DEV",  Db2iTyped.VarChar(device, 64)),
    ///     ("BRO",  Db2iTyped.VarChar(browser, 64)),
    ///     ("TOK",  Db2iTyped.VarChar(token, 512))
    /// )
    /// </code>
    /// Esto genera:
    /// <c>USING (VALUES(CAST(? AS VARCHAR(20)), TIMESTAMP(?), CAST(? AS CHAR(1)), ...)) AS S(UID, NOWTS, EXI, ...)</c>
    /// y agrega los valores a <see cref="QueryResult.Parameters"/> en el mismo orden.
    /// </para>
    /// </summary>
    /// <param name="values">
    /// Pares (NombreDeColumna, ValorTipado) para la tabla fuente <c>S</c>.
    /// El orden define el orden de columnas y de parámetros.
    /// </param>
    /// <returns>El propio <see cref="MergeQueryBuilder"/> para encadenamiento.</returns>
    /// <exception cref="ArgumentException">Si no se envía ningún valor.</exception>
    public MergeQueryBuilder UsingValuesTyped(params (string Column, Db2iTyped Value)[] values)
    {
        if (values == null || values.Length == 0)
            throw new ArgumentException("Debe especificar al menos un valor para USING (VALUES ...).", nameof(values));

        _usingSourceColumns.Clear();
        _usingValueSql.Clear();

        foreach (var (col, val) in values)
        {
            if (string.IsNullOrWhiteSpace(col))
                throw new ArgumentException("El nombre de columna en USING no puede ser vacío.", nameof(values));

            _usingSourceColumns.Add(col);
            _usingValueSql.Add(val.Sql);     // ej. CAST(? AS VARCHAR(20)) / TIMESTAMP(?)
            _parameters.Add(val.Value);      // valor para el marcador ?
        }

        return this;
    }

    // --------------------------------------------------------------------
    // Pista de integración (por si aún no lo tienes dentro de Build()):
    // En el Build() del MergeQueryBuilder deberías tener algo así:
    //
    // sb.AppendLine("USING (VALUES(" + string.Join(", ", _usingValueSql) + ")) AS S(" +
    //               string.Join(", ", _usingSourceColumns) + ")");
    //
    // y al final:
    // return new QueryResult { Sql = sb.ToString(), Parameters = _parameters };
    // --------------------------------------------------------------------
}


var merge = new MergeQueryBuilder("ETD02LOG", "BCAH96DTA")
    .UsingValuesTyped(
        ("UID",   Db2iTyped.VarChar(userID, 20)),
        ("NOWTS", Db2iTyped.Timestamp(now)),
        ("EXI",   Db2iTyped.Char(exitoso, 1)),
        ("IP",    Db2iTyped.VarChar(machine.ClientIPAddress, 64)),
        ("DEV",   Db2iTyped.VarChar(machine.Device, 64)),
        ("BRO",   Db2iTyped.VarChar(machine.Browser, 64)),
        ("TOK",   Db2iTyped.VarChar(idSesion, 512))
    )
    .On("T.LOGB01UID = S.UID")
    .WhenMatchedUpdate(new[]
    {
        "T.LOGB02UIL = S.NOWTS",
        "T.LOGB03TIL = CASE WHEN S.EXI = '1' THEN COALESCE(T.LOGB03TIL, 0) + 1 ELSE 0 END",
        "T.LOGB04SEA = S.EXI",
        "T.LOGB05UDI = S.IP",
        "T.LOGB06UTD = S.DEV",
        "T.LOGB07UNA = S.BRO",
        "T.LOGB09UIF = COALESCE(T.LOGB02UIL, S.NOWTS)",
        "T.LOGB10TOK = S.TOK"
    })
    .WhenNotMatchedInsert(
        new[] { "LOGB01UID", "LOGB02UIL", "LOGB03TIL", "LOGB04SEA", "LOGB05UDI", "LOGB06UTD", "LOGB07UNA", "LOGB08CBI", "LOGB09UIF", "LOGB10TOK" },
        new[] { "S.UID", "S.NOWTS", "CASE WHEN S.EXI = '1' THEN 1 ELSE 0 END", "S.EXI", "S.IP", "S.DEV", "S.BRO", "''", "S.NOWTS", "S.TOK" }
    )
    .Build();

// Ejecutar (tu provider ya soporta QueryResult)
using var cmd = _connection.GetDbCommand(merge, _contextAccessor.HttpContext!);
var affected = await cmd.ExecuteNonQueryAsync();
