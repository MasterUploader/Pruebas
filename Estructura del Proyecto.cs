Con el cambio que hiciste al ejecutar este codigo:

            // ======================================================================
            // 2) MERGE (UPSERT) → BCAH96DTA.ETD02LOG  (intentos y sesión activa)
            //    - Parametrizado con USING (VALUES ...) + columnas de la fuente
            //    Lógica de “intentos”:
            //       - Si exitoso = '1' → intentos = intentos(previos) + 1
            //       - Si exitoso = '0' → intentos = 0
            //
            //    Campos:
            //     - LOGB02UIL (último login)      ← now
            //     - LOGB03TIL (intentos)          ← CASE basado en exitoso
            //     - LOGB04SEA (sesión activa)     ← exitoso
            //     - LOGB05UDI (IP)                ← machine.ClientIPAddress
            //     - LOGB06UTD (Device)            ← machine.Device
            //     - LOGB07UNA (Browser)           ← machine.Browser
            //     - LOGB08CBI (Bloqueo intento)   ← '' (vacío en tu inserción)
            //     - LOGB09UIF (último intento)    ← COALESCE(previo, now)  (si no hay previo, cae en now)
            //     - LOGB10TOK (token/sesión)      ← idSesion
            // ======================================================================

            // Construimos la fuente S con nombres de columnas y UNA fila de valores.
            // OJO: aquí pasamos DateTime 'now' como parámetro: DB2 i (vía OleDb) lo bindeará como TIMESTAMP.
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


Me da este error:

SQL0418: La utilización de marcador de parámetro o NULL no es válida.
Causa . . . . . :   Los marcadores de parámetro y NULL no están permitidos: -- Como operando de algunas funciones escalares. Si la función escalar es VALUE, COALESCE, IFNULL, NULLIF, MIN, MAX, LAND, LOR, XOR, BITAND, BITANDNOT, BITOR, BITXOR o BITNOT, al menos uno de los argumentos debe tener un valor que no sea un marcador de parámetro o NULL. Los marcadores de parámetro tampoco están permitidos: -- En la cláusula SELECT de la serie de caracteres de la sentencia que va a prepararse. -- Como un valor en una sentencia VALUES INTO. -- En una sentencia SQL en SQL intercalado o en SQL interactivo. -- En una sentencia EXECUTE IMMEDIATE. -- En una sentencia CREATE VIEW, CREATE TABLE, ALTER TABLE, CREATE INDEX, CREATE MASK o CREATE PERMISSION. -- En una cláusula de valor predeterminado para un parámetro de una sentencia CREATE PROCEDURE o CREATE FUNCTION. -- En una sentencia procesada por el mandato RUNSQLSTM o RUNSQL. -- En una sentencia INSERT de bloque. La función escalar RAISE_ERROR no puede utilizarse en expresiones en las que no esté permitido un marcador de parámetro. Recuperación . .:   Cerciórese de que los marcadores de parámetro, NULL y la función escalar RAISE_ERROR sólo se especifiquen allí donde estén permitidos. Una especificación CAST puede utilizarse en muchas situaciones. Consulte la sentencia PREPARE del manual de consulta de SQL para obtener detalles de dónde pueden utilizarse marcadores de parámetro. Corrija los errores. Repita la solicitud.

    Por favor revisa el porque y entregame la clase MergeQueryBuilder, con los cambios aplicados.
