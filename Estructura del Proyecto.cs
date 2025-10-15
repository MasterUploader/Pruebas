/// <summary>
/// Inserta depósitos en BCAH96DTA.BTSACTA mediante MERGE:
/// si ya existe INOCONFIR, no inserta; si no existe, inserta la fila completa.
/// </summary>
private async Task<bool> InsertarEnIbtSactaAsync(SDEPResponseData response)
{
    _databaseConnection.Open();
    if (!_databaseConnection.IsConnected) return false;
    if (response?.Deposits == null || response.Deposits.Count == 0) return false;

    try
    {
        foreach (var deposit in response.Deposits)
        {
            if (deposit?.Data == null) continue;
            var d = deposit.Data;

            // -- Helper: normaliza nulos/vacíos a " " para columnas CHAR
            static string C(object? v) => string.IsNullOrWhiteSpace(v?.ToString()) ? " " : v!.ToString()!;

            // -- Truncados de dirección como en tu código original (máx 65)
            string cDir = d.Recipient?.Address?.AddressLine ?? " ";
            if (cDir.Length > 65) cDir = cDir[..65];
            string bDir = d.Sender?.Address?.AddressLine ?? " ";
            if (bDir.Length > 65) bDir = bDir[..65];

            // -- Fechas/horas como CHAR (según tu tabla)
            string hoyYmd      = DateTime.Now.ToString("yyyyMMdd");   // 8
            string ahoraHmsfff = DateTime.Now.ToString("HHmmssfff");  // 9

            // -- Estado del proceso
            string statusProceso = response.OpCode == "1308" ? "RECIBIDA" : "RECH-DENEG";

            // =========================
            // MERGE (solo inserta si NO existe INOCONFIR)
            // =========================
            var merge = new MergeQueryBuilder("BTSACTA", "BCAH96DTA", SqlDialect.Db2i)
                // Fuente S: una fila con placeholders tipados (DB2 i)
                .UsingValuesTyped(
                    ("INOCONFIR", Db2ITyped.VarChar(C(d.ConfirmationNumber), 40)),
                    ("IDATRECI",  Db2ITyped.Char(hoyYmd, 8)),
                    ("IHORRECI",  Db2ITyped.Char(ahoraHmsfff, 9)),
                    ("IDATCONF",  Db2ITyped.Char(" ", 1)),
                    ("IHORCONF",  Db2ITyped.Char(" ", 1)),
                    ("IDATVAL",   Db2ITyped.Char(" ", 1)),
                    ("IHORVAL",   Db2ITyped.Char(" ", 1)),
                    ("IDATPAGO",  Db2ITyped.Char(" ", 1)),
                    ("IHORPAGO",  Db2ITyped.Char(" ", 1)),
                    ("IDATACRE",  Db2ITyped.Char(" ", 1)),
                    ("IHORACRE",  Db2ITyped.Char(" ", 1)),
                    ("IDATRECH",  Db2ITyped.Char(" ", 1)),
                    ("IHORRECH",  Db2ITyped.Char(" ", 1)),
                    ("ITIPPAGO",  Db2ITyped.VarChar(C(d.PaymentTypeCode), 10)),
                    ("ISERVICD",  Db2ITyped.VarChar(C(d.ServiceCode), 10)),
                    ("IDESPAIS",  Db2ITyped.VarChar(C(d.DestinationCountryCode), 4)),
                    ("IDESMONE",  Db2ITyped.VarChar(C(d.DestinationCurrencyCode), 4)),
                    ("ISAGENCD",  Db2ITyped.VarChar(C(d.SenderAgentCode), 20)),
                    ("ISPAISCD",  Db2ITyped.VarChar(C(d.SenderCountryCode), 4)),
                    ("ISTATECD",  Db2ITyped.VarChar(C(d.SenderStateCode), 10)),
                    ("IRAGENCD",  Db2ITyped.VarChar(C(d.RecipientAgentCode), 20)),
                    ("ITICUENTA", Db2ITyped.VarChar(C(d.RecipientAccountTypeCode), 5)),
                    ("INOCUENTA", Db2ITyped.VarChar(C(d.RecipientAccountNumber), 64)),
                    ("INUMREFER", Db2ITyped.Char(" ", 1)),
                    ("ISTSREM",   Db2ITyped.Char(" ", 1)),
                    ("ISTSPRO",   Db2ITyped.VarChar(statusProceso, 12)),
                    ("IERR",      Db2ITyped.VarChar(C(response.OpCode), 10)),
                    ("IERRDSC",   Db2ITyped.VarChar(C(response.ProcessMsg), 256)),
                    ("IDSCRECH",  Db2ITyped.Char(" ", 1)),
                    ("ACODPAIS",  Db2ITyped.VarChar(C(d.OriginCountryCode), 4)),
                    ("ACODMONED", Db2ITyped.VarChar(C(d.OriginCurrencyCode), 4)),
                    ("AMTOENVIA", Db2ITyped.VarChar(C(d.OriginAmount), 32)),
                    ("AMTOCALCU", Db2ITyped.VarChar(C(d.DestinationAmount), 32)),
                    ("AFACTCAMB", Db2ITyped.VarChar(C(d.ExchangeRateFx), 32)),
                    ("BPRIMNAME", Db2ITyped.VarChar(C(d.Sender?.FirstName), 40)),
                    ("BSECUNAME", Db2ITyped.VarChar(C(d.Sender?.MiddleName), 40)),
                    ("BAPELLIDO", Db2ITyped.VarChar(C(d.Sender?.LastName), 40)),
                    ("BSEGUAPE",  Db2ITyped.VarChar(C(d.Sender?.MotherMaidenName), 40)),
                    ("BDIRECCIO", Db2ITyped.VarChar(bDir, 65)),
                    ("BCIUDAD",   Db2ITyped.VarChar(C(d.Sender?.Address?.City), 40)),
                    ("BESTADO",   Db2ITyped.VarChar(C(d.Sender?.Address?.StateCode), 10)),
                    ("BPAIS",     Db2ITyped.VarChar(C(d.Sender?.Address?.CountryCode), 4)),
                    ("BCODPOST",  Db2ITyped.VarChar(C(d.Sender?.Address?.ZipCode), 16)),
                    ("BTELEFONO", Db2ITyped.VarChar(C(d.Sender?.Address?.Phone), 24)),
                    ("CPRIMNAME", Db2ITyped.VarChar(C(d.Recipient?.FirstName), 40)),
                    ("CSECUNAME", Db2ITyped.VarChar(C(d.Recipient?.MiddleName), 40)),
                    ("CAPELLIDO", Db2ITyped.VarChar(C(d.Recipient?.LastName), 40)),
                    ("CSEGUAPE",  Db2ITyped.VarChar(C(d.Recipient?.MotherMaidenName), 40)),
                    ("CDIRECCIO", Db2ITyped.VarChar(cDir, 65)),
                    ("CCIUDAD",   Db2ITyped.VarChar(C(d.Recipient?.Address?.City), 40)),
                    ("CESTADO",   Db2ITyped.VarChar(C(d.Recipient?.Address?.StateCode), 10)),
                    ("CPAIS",     Db2ITyped.VarChar(C(d.Recipient?.Address?.CountryCode), 4)),
                    ("CCODPOST",  Db2ITyped.VarChar(C(d.Recipient?.Address?.ZipCode), 16)),
                    ("CTELEFONO", Db2ITyped.VarChar(C(d.Recipient?.Address?.Phone), 24)),
                    ("DTIDENT",   Db2ITyped.Char(" ", 1)),
                    ("ESALEDT",   Db2ITyped.VarChar(C(d.SaleDate), 16)),
                    ("EMONREFER", Db2ITyped.VarChar(C(d.MarketRefCurrencyCode), 8)),
                    ("ETASAREFE", Db2ITyped.VarChar(C(d.MarketRefCurrencyFx), 16)),
                    ("EMTOREF",   Db2ITyped.VarChar(C(d.MarketRefCurrencyAmount), 32))
                )
                // Clave de existencia: solo inserta si NO existe
                .On("T.INOCONFIR = S.INOCONFIR")
                // No definimos WHEN MATCHED (no queremos actualizar)
                .WhenNotMatchedInsert(
                    ("INOCONFIR","S.INOCONFIR"),("IDATRECI","S.IDATRECI"),("IHORRECI","S.IHORRECI"),
                    ("IDATCONF","S.IDATCONF"),("IHORCONF","S.IHORCONF"),("IDATVAL","S.IDATVAL"),("IHORVAL","S.IHORVAL"),
                    ("IDATPAGO","S.IDATPAGO"),("IHORPAGO","S.IHORPAGO"),("IDATACRE","S.IDATACRE"),("IHORACRE","S.IHORACRE"),
                    ("IDATRECH","S.IDATRECH"),("IHORRECH","S.IHORRECH"),
                    ("ITIPPAGO","S.ITIPPAGO"),("ISERVICD","S.ISERVICD"),("IDESPAIS","S.IDESPAIS"),("IDESMONE","S.IDESMONE"),
                    ("ISAGENCD","S.ISAGENCD"),("ISPAISCD","S.ISPAISCD"),("ISTATECD","S.ISTATECD"),
                    ("IRAGENCD","S.IRAGENCD"),("ITICUENTA","S.ITICUENTA"),("INOCUENTA","S.INOCUENTA"),
                    ("INUMREFER","S.INUMREFER"),("ISTSREM","S.ISTSREM"),("ISTSPRO","S.ISTSPRO"),
                    ("IERR","S.IERR"),("IERRDSC","S.IERRDSC"),("IDSCRECH","S.IDSCRECH"),
                    ("ACODPAIS","S.ACODPAIS"),("ACODMONED","S.ACODMONED"),("AMTOENVIA","S.AMTOENVIA"),
                    ("AMTOCALCU","S.AMTOCALCU"),("AFACTCAMB","S.AFACTCAMB"),
                    ("BPRIMNAME","S.BPRIMNAME"),("BSECUNAME","S.BSECUNAME"),("BAPELLIDO","S.BAPELLIDO"),
                    ("BSEGUAPE","S.BSEGUAPE"),("BDIRECCIO","S.BDIRECCIO"),("BCIUDAD","S.BCIUDAD"),
                    ("BESTADO","S.BESTADO"),("BPAIS","S.BPAIS"),("BCODPOST","S.BCODPOST"),("BTELEFONO","S.BTELEFONO"),
                    ("CPRIMNAME","S.CPRIMNAME"),("CSECUNAME","S.CSECUNAME"),("CAPELLIDO","S.CAPELLIDO"),
                    ("CSEGUAPE","S.CSEGUAPE"),("CDIRECCIO","S.CDIRECCIO"),("CCIUDAD","S.CCIUDAD"),
                    ("CESTADO","S.CESTADO"),("CPAIS","S.CPAIS"),("CCODPOST","S.CCODPOST"),("CTELEFONO","S.CTELEFONO"),
                    ("DTIDENT","S.DTIDENT"),("ESALEDT","S.ESALEDT"),("EMONREFER","S.EMONREFER"),
                    ("ETASAREFE","S.ETASAREFE"),("EMTOREF","S.EMTOREF")
                )
                .Build();

            using var cmd = _databaseConnection.GetDbCommand(merge, _httpContextAccessor.HttpContext!);
            await cmd.ExecuteNonQueryAsync(); // si existe, 0 filas afectadas; si no, inserta 1
        }

        return true;
    }
    catch
    {
        // TODO: log de error con tu servicio de logging decorado
        return false;
    }
    finally
    {
        _databaseConnection.Close();
    }
}
