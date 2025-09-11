Este codigo:
            var merge = new MergeQueryBuilder("ETD02LOG", "BCAH96DTA")
                    .UsingValuesTyped(
                        ("UID", Db2ITyped.VarChar(userID, 20)),
                        ("NOWTS", Db2ITyped.Timestamp(now)),
                        ("EXI", Db2ITyped.Decimal(exitoso, 10, 0)),
                        ("IP", Db2ITyped.VarChar(machine.ClientIPAddress, 20)),
                        ("DEV", Db2ITyped.VarChar(machine.Device, 20)),
                        ("BRO", Db2ITyped.VarChar(machine.Browser, 20)),
                        ("TOK", Db2ITyped.VarChar(idSesion, 2000))
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

            // Ejecutar
            var cmd2 = _connection.GetDbCommand(merge, _contextAccessor.HttpContext!);

            var aff2 = cmd2.ExecuteNonQuery();


Me genero este SQL:

MERGE INTO BCAH96DTA.ETD02LOG AS T
USING (SELECT CAST(? AS VARCHAR(20)) AS UID, CAST(? AS TIMESTAMP) AS NOWTS, CAST(? AS DECIMAL(10,0)) AS EXI, CAST(? AS VARCHAR(20)) AS IP, CAST(? AS VARCHAR(20)) AS DEV, CAST(? AS VARCHAR(20)) AS BRO, CAST(? AS VARCHAR(2000)) AS TOK FROM SYSIBM.SYSDUMMY1) AS S(UID, NOWTS, EXI, IP, DEV, BRO, TOK)
ON T.LOGB01UID = S.UID
WHEN MATCHED THEN UPDATE SET
T.LOGB02UIL = S.NOWTS,
T.LOGB03TIL = CASE WHEN S.EXI = '1' THEN COALESCE(T.LOGB03TIL, 0) + 1 ELSE 0 END,
T.LOGB04SEA = S.EXI,
T.LOGB05UDI = S.IP,
T.LOGB06UTD = S.DEV,
T.LOGB07UNA = S.BRO,
T.LOGB09UIF = COALESCE(T.LOGB02UIL, S.NOWTS),
T.LOGB10TOK = S.TOK
WHEN NOT MATCHED THEN INSERT
(LOGB01UID, LOGB02UIL, LOGB03TIL, LOGB04SEA, LOGB05UDI, LOGB06UTD, LOGB07UNA, LOGB08CBI, LOGB09UIF, LOGB10TOK)
VALUES
(S.UID, S.NOWTS, CASE WHEN S.EXI = '1' THEN 1 ELSE 0 END, S.EXI, S.IP, S.DEV, S.BRO, '', S.NOWTS, S.TOK)


    PEro me da este error:

 failed for command parameter[1] '' because the data value overflowed the type used by the provider.


     La tabla tiene estas caracteristicas:

Campo              Archivo            Tipo               Longitud  Escal
LOGB01UID          ETD02LOG           CHARACTER                20       
LOGB02UIL          ETD02LOG           TIMESTAMP                26     6 
LOGB03TIL          ETD02LOG           NUMERIC                  10       
LOGB04SEA          ETD02LOG           CHARACTER                 1       
LOGB05UDI          ETD02LOG           CHARACTER                20       
LOGB06UTD          ETD02LOG           CHARACTER                20       
LOGB07UNA          ETD02LOG           CHARACTER                20       
LOGB08CBI          ETD02LOG           CHARACTER                 1       
LOGB09UIF          ETD02LOG           TIMESTAMP                26     6 
LOGB10TOK          ETD02LOG           CHARACTER              2000       

Sera que coloque mal el tipo de dato o su tamaño?

