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

            // Helper para nulos en CHAR
            static string C(object? v) => string.IsNullOrWhiteSpace(v?.ToString()) ? " " : v!.ToString()!;

            // Truncados de direcciones (igual que tu código)
            string cDir = d.Recipient?.Address?.AddressLine ?? " ";
            if (cDir.Length > 65) cDir = cDir[..65];

            string bDir = d.Sender?.Address?.AddressLine ?? " ";
            if (bDir.Length > 65) bDir = bDir[..65];

            // Tiempos actuales (formato CHAR que ya usabas en la tabla)
            string hoyYmd      = DateTime.Now.ToString("yyyyMMdd");   // 8 chars
            string ahoraHmsfff = DateTime.Now.ToString("HHmmssfff");  // 9 chars

            string statusProceso = response.OpCode == "1308" ? "RECIBIDA" : "RECH-DENEG";

            // --- MERGE: si existe INOCONFIR, NO inserta; si no, inserta toda la fila ---
            // Tipados conservadores: CHAR para fijos y VARCHAR para textos; ajusta longitudes si conoces el esquema exacto.
            var merge = new MergeQueryBuilder("BTSACTA", "BCAH96DTA")
                .UsingSelect( // Seleccionamos una sola fila desde SYSDUMMY1 con CAST(? ...) por cada columna
                    new SelectQueryBuilder("SYSIBM.SYSDUMMY1")
                        .Select($"{Db2ITyped.VarChar(C(d.ConfirmationNumber), 40).Sql} AS INOCONFIR")
                        .AddParam(Db2ITyped.VarChar(C(d.ConfirmationNumber), 40).Value)

                        .Select($"{Db2ITyped.Char(hoyYmd, 8).Sql} AS IDATRECI")
                        .AddParam(hoyYmd)

                        .Select($"{Db2ITyped.Char(ahoraHmsfff, 9).Sql} AS IHORRECI")
                        .AddParam(ahoraHmsfff)

                        .Select($"{Db2ITyped.Char(" ", 1).Sql} AS IDATCONF").AddParam(" ")
                        .Select($"{Db2ITyped.Char(" ", 1).Sql} AS IHORCONF").AddParam(" ")
                        .Select($"{Db2ITyped.Char(" ", 1).Sql} AS IDATVAL").AddParam(" ")
                        .Select($"{Db2ITyped.Char(" ", 1).Sql} AS IHORVAL").AddParam(" ")
                        .Select($"{Db2ITyped.Char(" ", 1).Sql} AS IDATPAGO").AddParam(" ")
                        .Select($"{Db2ITyped.Char(" ", 1).Sql} AS IHORPAGO").AddParam(" ")
                        .Select($"{Db2ITyped.Char(" ", 1).Sql} AS IDATACRE").AddParam(" ")
                        .Select($"{Db2ITyped.Char(" ", 1).Sql} AS IHORACRE").AddParam(" ")
                        .Select($"{Db2ITyped.Char(" ", 1).Sql} AS IDATRECH").AddParam(" ")
                        .Select($"{Db2ITyped.Char(" ", 1).Sql} AS IHORRECH").AddParam(" ")

                        .Select($"{Db2ITyped.VarChar(C(d.PaymentTypeCode), 10).Sql} AS ITIPPAGO").AddParam(C(d.PaymentTypeCode))
                        .Select($"{Db2ITyped.VarChar(C(d.ServiceCode), 10).Sql} AS ISERVICD").AddParam(C(d.ServiceCode))
                        .Select($"{Db2ITyped.VarChar(C(d.DestinationCountryCode), 4).Sql} AS IDESPAIS").AddParam(C(d.DestinationCountryCode))
                        .Select($"{Db2ITyped.VarChar(C(d.DestinationCurrencyCode), 4).Sql} AS IDESMONE").AddParam(C(d.DestinationCurrencyCode))
                        .Select($"{Db2ITyped.VarChar(C(d.SenderAgentCode), 20).Sql} AS ISAGENCD").AddParam(C(d.SenderAgentCode))
                        .Select($"{Db2ITyped.VarChar(C(d.SenderCountryCode), 4).Sql} AS ISPAISCD").AddParam(C(d.SenderCountryCode))
                        .Select($"{Db2ITyped.VarChar(C(d.SenderStateCode), 10).Sql} AS ISTATECD").AddParam(C(d.SenderStateCode))
                        .Select($"{Db2ITyped.VarChar(C(d.RecipientAgentCode), 20).Sql} AS IRAGENCD").AddParam(C(d.RecipientAgentCode))
                        .Select($"{Db2ITyped.VarChar(C(d.RecipientAccountTypeCode), 5).Sql} AS ITICUENTA").AddParam(C(d.RecipientAccountTypeCode))
                        .Select($"{Db2ITyped.VarChar(C(d.RecipientAccountNumber), 64).Sql} AS INOCUENTA").AddParam(C(d.RecipientAccountNumber))

                        .Select($"{Db2ITyped.Char(" ", 1).Sql} AS INUMREFER").AddParam(" ")
                        .Select($"{Db2ITyped.Char(" ", 1).Sql} AS ISTSREM").AddParam(" ")
                        .Select($"{Db2ITyped.VarChar(statusProceso, 12).Sql} AS ISTSPRO").AddParam(statusProceso)
                        .Select($"{Db2ITyped.VarChar(C(response.OpCode), 10).Sql} AS IERR").AddParam(C(response.OpCode))
                        .Select($"{Db2ITyped.VarChar(C(response.ProcessMsg), 256).Sql} AS IERRDSC").AddParam(C(response.ProcessMsg))
                        .Select($"{Db2ITyped.Char(" ", 1).Sql} AS IDSCRECH").AddParam(" ")

                        .Select($"{Db2ITyped.VarChar(C(d.OriginCountryCode), 4).Sql} AS ACODPAIS").AddParam(C(d.OriginCountryCode))
                        .Select($"{Db2ITyped.VarChar(C(d.OriginCurrencyCode), 4).Sql} AS ACODMONED").AddParam(C(d.OriginCurrencyCode))
                        .Select($"{Db2ITyped.VarChar(C(d.OriginAmount), 32).Sql} AS AMTOENVIA").AddParam(C(d.OriginAmount))
                        .Select($"{Db2ITyped.VarChar(C(d.DestinationAmount), 32).Sql} AS AMTOCALCU").AddParam(C(d.DestinationAmount))
                        .Select($"{Db2ITyped.VarChar(C(d.ExchangeRateFx), 32).Sql} AS AFACTCAMB").AddParam(C(d.ExchangeRateFx))

                        .Select($"{Db2ITyped.VarChar(C(d.Sender?.FirstName), 40).Sql} AS BPRIMNAME").AddParam(C(d.Sender?.FirstName))
                        .Select($"{Db2ITyped.VarChar(C(d.Sender?.MiddleName), 40).Sql} AS BSECUNAME").AddParam(C(d.Sender?.MiddleName))
                        .Select($"{Db2ITyped.VarChar(C(d.Sender?.LastName), 40).Sql} AS BAPELLIDO").AddParam(C(d.Sender?.LastName))
                        .Select($"{Db2ITyped.VarChar(C(d.Sender?.MotherMaidenName), 40).Sql} AS BSEGUAPE").AddParam(C(d.Sender?.MotherMaidenName))
                        .Select($"{Db2ITyped.VarChar(bDir, 65).Sql} AS BDIRECCIO").AddParam(bDir)
                        .Select($"{Db2ITyped.VarChar(C(d.Sender?.Address?.City), 40).Sql} AS BCIUDAD").AddParam(C(d.Sender?.Address?.City))
                        .Select($"{Db2ITyped.VarChar(C(d.Sender?.Address?.StateCode), 10).Sql} AS BESTADO").AddParam(C(d.Sender?.Address?.StateCode))
                        .Select($"{Db2ITyped.VarChar(C(d.Sender?.Address?.CountryCode), 4).Sql} AS BPAIS").AddParam(C(d.Sender?.Address?.CountryCode))
                        .Select($"{Db2ITyped.VarChar(C(d.Sender?.Address?.ZipCode), 16).Sql} AS BCODPOST").AddParam(C(d.Sender?.Address?.ZipCode))
                        .Select($"{Db2ITyped.VarChar(C(d.Sender?.Address?.Phone), 24).Sql} AS BTELEFONO").AddParam(C(d.Sender?.Address?.Phone))

                        .Select($"{Db2ITyped.VarChar(C(d.Recipient?.FirstName), 40).Sql} AS CPRIMNAME").AddParam(C(d.Recipient?.FirstName))
                        .Select($"{Db2ITyped.VarChar(C(d.Recipient?.MiddleName), 40).Sql} AS CSECUNAME").AddParam(C(d.Recipient?.MiddleName))
                        .Select($"{Db2ITyped.VarChar(C(d.Recipient?.LastName), 40).Sql} AS CAPELLIDO").AddParam(C(d.Recipient?.LastName))
                        .Select($"{Db2ITyped.VarChar(C(d.Recipient?.MotherMaidenName), 40).Sql} AS CSEGUAPE").AddParam(C(d.Recipient?.MotherMaidenName))
                        .Select($"{Db2ITyped.VarChar(cDir, 65).Sql} AS CDIRECCIO").AddParam(cDir)
                        .Select($"{Db2ITyped.VarChar(C(d.Recipient?.Address?.City), 40).Sql} AS CCIUDAD").AddParam(C(d.Recipient?.Address?.City))
                        .Select($"{Db2ITyped.VarChar(C(d.Recipient?.Address?.StateCode), 10).Sql} AS CESTADO").AddParam(C(d.Recipient?.Address?.StateCode))
                        .Select($"{Db2ITyped.VarChar(C(d.Recipient?.Address?.CountryCode), 4).Sql} AS CPAIS").AddParam(C(d.Recipient?.Address?.CountryCode))
                        .Select($"{Db2ITyped.VarChar(C(d.Recipient?.Address?.ZipCode), 16).Sql} AS CCODPOST").AddParam(C(d.Recipient?.Address?.ZipCode))
                        .Select($"{Db2ITyped.VarChar(C(d.Recipient?.Address?.Phone), 24).Sql} AS CTELEFONO").AddParam(C(d.Recipient?.Address?.Phone))

                        .Select($"{Db2ITyped.Char(" ", 1).Sql} AS DTIDENT").AddParam(" ")
                        .Select($"{Db2ITyped.VarChar(C(d.SaleDate), 16).Sql} AS ESALEDT").AddParam(C(d.SaleDate))
                        .Select($"{Db2ITyped.VarChar(C(d.MarketRefCurrencyCode), 8).Sql} AS EMONREFER").AddParam(C(d.MarketRefCurrencyCode))
                        .Select($"{Db2ITyped.VarChar(C(d.MarketRefCurrencyFx), 16).Sql} AS ETASAREFE").AddParam(C(d.MarketRefCurrencyFx))
                        .Select($"{Db2ITyped.VarChar(C(d.MarketRefCurrencyAmount), 32).Sql} AS EMTOREF").AddParam(C(d.MarketRefCurrencyAmount))
                , // <-- fin del SelectQueryBuilder
                // y le decimos a Merge qué columnas expone la fuente S, en el mismo orden:
                new[]
                {
                    "INOCONFIR","IDATRECI","IHORRECI","IDATCONF","IHORCONF","IDATVAL","IHORVAL",
                    "IDATPAGO","IHORPAGO","IDATACRE","IHORACRE","IDATRECH","IHORRECH",
                    "ITIPPAGO","ISERVICD","IDESPAIS","IDESMONE","ISAGENCD","ISPAISCD","ISTATECD",
                    "IRAGENCD","ITICUENTA","INOCUENTA","INUMREFER","ISTSREM","ISTSPRO","IERR",
                    "IERRDSC","IDSCRECH","ACODPAIS","ACODMONED","AMTOENVIA","AMTOCALCU","AFACTCAMB",
                    "BPRIMNAME","BSECUNAME","BAPELLIDO","BSEGUAPE","BDIRECCIO","BCIUDAD",
                    "BESTADO","BPAIS","BCODPOST","BTELEFONO",
                    "CPRIMNAME","CSECUNAME","CAPELLIDO","CSEGUAPE","CDIRECCIO","CCIUDAD",
                    "CESTADO","CPAIS","CCODPOST","CTELEFONO",
                    "DTIDENT","ESALEDT","EMONREFER","ETASAREFE","EMTOREF"
                })
                .On("T.INOCONFIR = S.INOCONFIR")         // clave de unicidad
                // No hacemos UPDATE cuando existe (no especificamos WHEN MATCHED)
                .WhenNotMatchedInsert(                   // inserción completa desde la fuente S
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
            await cmd.ExecuteNonQueryAsync(); // INSERT solo si no existe INOCONFIR
        }

        return true;
    }
    catch
    {
        return false;
    }
    finally
    {
        _databaseConnection.Close();
    }
}
