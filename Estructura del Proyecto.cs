Si genero este query:

            // -- Sello de tiempo local para DB2 (se usa en ambas operaciones).
            var now = DateTime.Now;

            // -- Huella de máquina/cliente para trazabilidad (IP, device, browser, etc.).
            var machine = _machineInfoService.GetMachineInfo();

            // =========================================================================
            // 1) INSERT ... SELECT (QueryBuilder) → BCAH96DTA.ETD01LOG
            //    - Resuelve correlativo en el mismo INSERT (sin “preconsulta”).
            //    - Deja el contador de intentos en 0 (LOGB09ACO en tu diseño original).
            // =========================================================================

            // SELECT que produce la fila a insertar (con columnas en el mismo orden del INSERT)
            //  [LOGA01AID, LOGA02UID, LOGA03TST, LOGA04SUC, LOGA05IPA, LOGA06MNA, LOGA07SID,
            //   LOGA08FRE, LOGA09ACO, LOGA10UAG, LOGA11BRO, LOGA12SOP, LOGA13DIS]
            //  MAX + 1 se calcula contra la misma tabla destino.
            var selectInsert = new SelectQueryBuilder("ETD01LOG", "BCAH96DTA")
                .As("T")
                .Select("COALESCE(MAX(T.LOGA01AID), 0) + 1")      // Correlativo calculado
                .Select($"'{userID.Replace("'", "''")}'")          // LOGA02UID
                .Select($"TIMESTAMP('{now:yyyy-MM-dd-HH.mm.ss}')")// LOGA03TST (formato timestamp DB2)
                .Select($"'{exitoso.Replace("'", "''")}'")          // LOGA04SUC
                .Select($"'{machine.ClientIPAddress?.Replace("'", "''") ?? ""}'") // LOGA05IPA
                .Select($"'{machine.HostName?.Replace("'", "''") ?? ""}'")        // LOGA06MNA
                .Select($"'{idSesion.Replace("'", "''")}'")         // LOGA07SID
                .Select($"'{motivo.Replace("'", "''")}'")           // LOGA08FRE
                .Select("0")                                        // LOGA09ACO (Conteo Intentos para log general)
                .Select($"'{machine.UserAgent?.Replace("'", "''") ?? ""}'") // LOGA10UAG
                .Select($"'{machine.Browser?.Replace("'", "''") ?? ""}'")   // LOGA11BRO
                .Select($"'{machine.OS?.Replace("'", "''") ?? ""}'")        // LOGA12SOP
                .Select($"'{machine.Device?.Replace("'", "''") ?? ""}'")    // LOGA13DIS
                ;

            // Construcción del INSERT con FromSelect (todas las columnas explícitas)
            var insertLogGeneral = new InsertQueryBuilder("ETD01LOG", "BCAH96DTA")
                .IntoColumns(
                    "LOGA01AID", "LOGA02UID", "LOGA03TST", "LOGA04SUC", "LOGA05IPA", "LOGA06MNA", "LOGA07SID",
                    "LOGA08FRE", "LOGA09ACO", "LOGA10UAG", "LOGA11BRO", "LOGA12SOP", "LOGA13DIS"
                )
                .FromSelect(selectInsert) // Nota: si tu InsertQueryBuilder acepta SelectQueryBuilder directamente, pásalo sin .Sql
                .Build();

            using var cmd1 = _connection.GetDbCommand(_contextAccessor.HttpContext!);
            cmd1.CommandText = insertLogGeneral.Sql;
            cmd1.CommandType = System.Data.CommandType.Text;
            var aff1 = cmd1.ExecuteNonQuery(); // Debe ser 1 si insertó ok


        El SQL se crea así INSERT INTO BCAH96DTA.ETD01LOG (LOGA01AID, LOGA02UID, LOGA03TST, LOGA04SUC, LOGA05IPA, LOGA06MNA, LOGA07SID, LOGA08FRE, LOGA09ACO, LOGA10UAG, LOGA11BRO, LOGA12SOP, LOGA13DIS)\r\nSELECT COALESCE(MAX(T.LOGA01AID), 0) + 1, '93421', TIMESTAMP('2025-09-10-10.42.19'), '0', '::1', 'HNCSTG015243WAP', 'eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJuYW1laWQiOiI5MzQyMSIsInVuaXF1ZV9uYW1lIjoiOTM0MjEiLCJuYmYiOjE3NTc1MjI1MzgsImV4cCI6MTc1NzU1MTMzOCwiaWF0IjoxNzU3NTIyNTM4fQ.NB9cR8aKwW1FNEghQFRQVlX1-zzMpthyNWS7SAD4esE', 'Logueado Exitosamente', 0, 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/138.0.0.0 Safari/537.36', 'Chrome 138', 'Windows 10', 'Other' FROM BCAH96DTA.ETD01LOG T

        Donde el FromSelect, genera mal y aparece este simbolo \r\n y da error
