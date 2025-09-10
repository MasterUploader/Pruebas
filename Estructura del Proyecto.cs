Ahora si yo ejecuto la sentencia 

// =========================================================================
// 2) MERGE (UPSERT) → BCAH96DTA.ETD02LOG
//
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
//
//    Nota: Usamos VALUES(...) como tabla fuente “S” para MERGE.
// =========================================================================

// Escapes simples para literales:
static string esc(string? s) => (s ?? "").Replace("'", "''");

var mergeUpsert = $@"
                MERGE INTO BCAH96DTA.ETD02LOG AS T
                USING (VALUES(
                    '{esc(userID)}',
                    TIMESTAMP('{now:yyyy-MM-dd-HH.mm.ss}'),
                    '{esc(exitoso)}',
                    '{esc(machine.ClientIPAddress)}',
                    '{esc(machine.Device)}',
                    '{esc(machine.Browser)}',
                    '{esc(idSesion)}'
                )) AS S(UID, NOWTS, EXI, IP, DEV, BRO, TOK)
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
                    (S.UID, S.NOWTS, CASE WHEN S.EXI = '1' THEN 1 ELSE 0 END, S.EXI, S.IP, S.DEV, S.BRO, '', S.NOWTS, S.TOK);
                ";

using var cmd2 = _connection.GetDbCommand(_contextAccessor.HttpContext!);
cmd2.CommandText = mergeUpsert;
cmd2.CommandType = System.Data.CommandType.Text;
var aff2 = cmd2.ExecuteNonQuery(); // ≥1 si hizo UPDATE o INSERT

Me genero este SQL

MERGE INTO BCAH96DTA.ETD02LOG AS T
                            USING (VALUES(
                                '93421',
                                TIMESTAMP('2025-09-10-11.06.37'),
                                '0',
                                '::1',
                                'Other',
                                'Chrome 138',
                                'eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJuYW1laWQiOiI5MzQyMSIsInVuaXF1ZV9uYW1lIjoiOTM0MjEiLCJuYmYiOjE3NTc1MjM5OTcsImV4cCI6MTc1NzU1Mjc5NywiaWF0IjoxNzU3NTIzOTk3fQ.f4ZUONAV5LBeFFUcqBEng51pCVGrLr5uG1ZKCXQB6Ew'
                            )) AS S(UID, NOWTS, EXI, IP, DEV, BRO, TOK)
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
                                (S.UID, S.NOWTS, CASE WHEN S.EXI = '1' THEN 1 ELSE 0 END, S.EXI, S.IP, S.DEV, S.BRO, '', S.NOWTS, S.TOK);

Si lo ejecuto desde Visual studio code, me funciona y hace lo que tiene que hacer, pero cuando se ejecuta desde el visual studio debugueando y usando las librerías da este error:

{"SQL0104: Símbolo ; no válido. Símbolos válidos: <FIN DE SENTENCIA>.\r\nCausa . . . . . :   Se ha detectado un error de sintaxis en el símbolo ;. El símbolo ; no es un símbolo válido. Una lista parcial de símbolos válidos es <FIN DE SENTENCIA>. Esta lista presupone que la sentencia es correcta hasta el símbolo. El error puede estar anteriormente en la sentencia, pero la sintaxis de la sentencia aparece como válida hasta este punto. Recuperación . .:   Efectúe una o más de las siguientes acciones y vuelva a intentar la petición: -- Verifique la sentencia SQL en el área del símbolo ;. Corrija la sentencia. El error podría ser la omisión de una coma o comillas; podría tratarse de una palabra con errores ortográficos, o podría estar relacionado con el orden de las cláusulas. -- Si el símbolo de error es <FIN DE SENTENCIA>, corrija la sentencia SQL porque no finaliza con una cláusula válida."}
