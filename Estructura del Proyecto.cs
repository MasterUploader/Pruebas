var merge = new MergeQueryBuilder("ETD02LOG", "BCAH96DTA")
    .UsingValuesTyped(
        ("UID",   Db2Typed.VarChar(userID, 20)),
        ("NOWTS", Db2Typed.Timestamp(now)),
        ("EXI",   Db2Typed.Char(exitoso, 1)),
        ("IP",    Db2Typed.VarChar(machine.ClientIPAddress, 64)),
        ("DEV",   Db2Typed.VarChar(machine.Device, 64)),
        ("BRO",   Db2Typed.VarChar(machine.Browser, 64)),
        ("TOK",   Db2Typed.VarChar(idSesion, 512))
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
    // Usa la sobrecarga con tuplas (también evita arrays)
    .WhenNotMatchedInsert(
        ("LOGB01UID", "S.UID"),
        ("LOGB02UIL", "S.NOWTS"),
        ("LOGB03TIL", "CASE WHEN S.EXI = '1' THEN 1 ELSE 0 END"),
        ("LOGB04SEA", "S.EXI"),
        ("LOGB05UDI", "S.IP"),
        ("LOGB06UTD", "S.DEV"),
        ("LOGB07UNA", "S.BRO"),
        ("LOGB08CBI", "''"),          // vacío en inserción
        ("LOGB09UIF", "S.NOWTS"),
        ("LOGB10TOK", "S.TOK")
    )
    .Build();
