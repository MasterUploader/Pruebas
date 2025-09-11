// Helpers para asegurar longitudes
static string S(string? v) => v ?? "";
static string Clamp(string? v, int max) => (v ?? "").Length <= max ? (v ?? "") : (v ?? "").Substring(0, max);

// Normaliza 'exitoso' a '0'/'1'
var exi = (S(exitoso) == "1") ? "1" : "0";

var merge = new MergeQueryBuilder("ETD02LOG", "BCAH96DTA")
    .UsingValuesTyped(
        ("UID",  Db2ITyped.Char(  Clamp(userID,                   20), 20)),
        ("NOWTS",Db2ITyped.Timestamp(now)),                               // TIMESTAMP correcto
        ("EXI",  Db2ITyped.Char(  exi,                                    1)),  // ❗ antes DECIMAL -> ahora CHAR(1)
        ("IP",   Db2ITyped.Char(  Clamp(machine.ClientIPAddress,  20), 20)),
        ("DEV",  Db2ITyped.Char(  Clamp(machine.Device,           20), 20)),
        ("BRO",  Db2ITyped.Char(  Clamp(machine.Browser,          20), 20)),
        ("TOK",  Db2ITyped.Char(  Clamp(idSesion,               2000),2000))
    )
    .On("T.LOGB01UID = S.UID")
    .WhenMatchedUpdate(
        "T.LOGB02UIL = S.NOWTS",
        "T.LOGB03TIL = CASE WHEN S.EXI = '1' THEN COALESCE(T.LOGB03TIL, 0) + 1 ELSE 0 END",
        "T.LOGB04SEA = S.EXI",
        "T.LOGB05UDI = S.IP",
        "T.LOGB06UTD = S.DEV",
        "T.LOGB07UNA = S.BRO",
        "T.LOGB09UIF = COALESCE(T.LOGB02UIL, S.NOWTS)",
        "T.LOGB10TOK = S.TOK"
    )
    .WhenNotMatchedInsert(
        ("LOGB01UID", "S.UID"),
        ("LOGB02UIL", "S.NOWTS"),
        ("LOGB03TIL", "CASE WHEN S.EXI = '1' THEN 1 ELSE 0 END"),
        ("LOGB04SEA", "S.EXI"),
        ("LOGB05UDI", "S.IP"),
        ("LOGB06UTD", "S.DEV"),
        ("LOGB07UNA", "S.BRO"),
        ("LOGB08CBI", "''"),
        ("LOGB09UIF", "S.NOWTS"),
        ("LOGB10TOK", "S.TOK")
    )
    .Build();

// Ejecutar (tu helper ya añade parámetros en orden)
using var cmd2 = _connection.GetDbCommand(merge, _contextAccessor.HttpContext!);
var aff2 = cmd2.ExecuteNonQuery();
