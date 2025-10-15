using QueryBuilder.Builders;
using QueryBuilder.Enums;

/// <summary>
/// Inserta depósitos en BCAH96DTA.BTSACTA mediante MERGE sin tipos explícitos:
/// si ya existe INOCONFIR no inserta; si no existe, inserta la fila.
/// </summary>
private async Task<bool> InsertarEnIbtSactaAsync(SDEPResponseData response)
{
    _databaseConnection.Open();
    if (!_databaseConnection.IsConnected) return false;
    if (response?.Deposits == null || response.Deposits.Count == 0) return false;

    try
    {
        // 1) Columnas fuente S (mismo orden que las columnas destino T)
        var cols = new[]
        {
            "INOCONFIR", "IDATRECI", "IHORRECI", "IDATCONF", "IHORCONF", "IDATVAL", "IHORVAL",
            "IDATPAGO", "IHORPAGO", "IDATACRE", "IHORACRE", "IDATRECH", "IHORRECH",
            "ITIPPAGO", "ISERVICD", "IDESPAIS", "IDESMONE", "ISAGENCD", "ISPAISCD", "ISTATECD",
            "IRAGENCD", "ITICUENTA", "INOCUENTA", "INUMREFER", "ISTSREM", "ISTSPRO", "IERR",
            "IERRDSC", "IDSCRECH", "ACODPAIS", "ACODMONED", "AMTOENVIA", "AMTOCALCU", "AFACTCAMB",
            "BPRIMNAME", "BSECUNAME", "BAPELLIDO", "BSEGUAPE", "BDIRECCIO", "BCIUDAD",
            "BESTADO", "BPAIS", "BCODPOST", "BTELEFONO",
            "CPRIMNAME", "CSECUNAME", "CAPELLIDO", "CSEGUAPE", "CDIRECCIO", "CCIUDAD",
            "CESTADO", "CPAIS", "CCODPOST", "CTELEFONO",
            "DTIDENT", "ESALEDT", "EMONREFER", "ETASAREFE", "EMTOREF"
        };

        // 2) Preparar filas VALUES (todas de tipo string/object, sin Db2ITyped)
        List<object?[]> rows = [];

        // Helper para normalizar nulos a un espacio (CHAR)
        static string C(object? v) => string.IsNullOrWhiteSpace(v?.ToString()) ? " " : v!.ToString()!;

        foreach (var deposit in response.Deposits)
        {
            if (deposit?.Data == null) continue;
            var d = deposit.Data;

            // Fechas/horas como cadenas (según tu tabla)
            string hoyYmd      = DateTime.Now.ToString("yyyyMMdd");
            string ahoraHmsfff = DateTime.Now.ToString("HHmmssfff");

            // Direcciones truncadas a 65
            string cDir = d.Recipient?.Address?.AddressLine ?? " ";
            if (cDir.Length > 65) cDir = cDir[..65];
            string bDir = d.Sender?.Address?.AddressLine ?? " ";
            if (bDir.Length > 65) bDir = bDir[..65];

            // Estado de proceso
            string statusProceso = response.OpCode == "1308" ? "RECIBIDA" : "RECH-DENEG";

            rows.Add(new object?[]
            {
                C(d.ConfirmationNumber),      // INOCONFIR (clave)
                hoyYmd,                       // IDATRECI
                ahoraHmsfff,                  // IHORRECI
                " ", " ", " ", " ",           // IDATCONF, IHORCONF, IDATVAL, IHORVAL
                " ", " ", " ", " ",           // IDATPAGO, IHORPAGO, IDATACRE, IHORACRE
                " ", " ",                     // IDATRECH, IHORRECH
                C(d.PaymentTypeCode),         // ITIPPAGO
                C(d.ServiceCode),             // ISERVICD
                C(d.DestinationCountryCode),  // IDESPAIS
                C(d.DestinationCurrencyCode), // IDESMONE
                C(d.SenderAgentCode),         // ISAGENCD
                C(d.SenderCountryCode),       // ISPAISCD
                C(d.SenderStateCode),         // ISTATECD
                C(d.RecipientAgentCode),      // IRAGENCD
                C(d.RecipientAccountTypeCode),// ITICUENTA
                C(d.RecipientAccountNumber),  // INOCUENTA
                " ",                          // INUMREFER
                " ",                          // ISTSREM
                statusProceso,                // ISTSPRO
                C(response.OpCode),           // IERR
                C(response.ProcessMsg),       // IERRDSC
                " ",                          // IDSCRECH
                C(d.OriginCountryCode),       // ACODPAIS
                C(d.OriginCurrencyCode),      // ACODMONED
                C(d.OriginAmount),            // AMTOENVIA
                C(d.DestinationAmount),       // AMTOCALCU
                C(d.ExchangeRateFx),          // AFACTCAMB
                C(d.Sender?.FirstName),       // BPRIMNAME
                C(d.Sender?.MiddleName),      // BSECUNAME
                C(d.Sender?.LastName),        // BAPELLIDO
                C(d.Sender?.MotherMaidenName),// BSEGUAPE
                bDir,                         // BDIRECCIO
                C(d.Sender?.Address?.City),   // BCIUDAD
                C(d.Sender?.Address?.StateCode),   // BESTADO
                C(d.Sender?.Address?.CountryCode), // BPAIS
                C(d.Sender?.Address?.ZipCode),     // BCODPOST
                C(d.Sender?.Address?.Phone),       // BTELEFONO
                C(d.Recipient?.FirstName),         // CPRIMNAME
                C(d.Recipient?.MiddleName),        // CSECUNAME
                C(d.Recipient?.LastName),          // CAPELLIDO
                C(d.Recipient?.MotherMaidenName),  // CSEGUAPE
                cDir,                         // CDIRECCIO
                C(d.Recipient?.Address?.City),      // CCIUDAD
                C(d.Recipient?.Address?.StateCode), // CESTADO
                C(d.Recipient?.Address?.CountryCode), // CPAIS
                C(d.Recipient?.Address?.ZipCode),     // CCODPOST
                C(d.Recipient?.Address?.Phone),       // CTELEFONO
                " ",                          // DTIDENT
                C(d.SaleDate),                // ESALEDT
                C(d.MarketRefCurrencyCode),   // EMONREFER
                C(d.MarketRefCurrencyFx),     // ETASAREFE
                C(d.MarketRefCurrencyAmount)  // EMTOREF
            });
        }

        if (rows.Count == 0) return false;

        // 3) MERGE por lote: si ya existe INOCONFIR, no inserta.
        var merge = new MergeQueryBuilder("BTSACTA", "BCAH96DTA", SqlDialect.Db2i)
            .UsingValues(cols, rows, alias: "S")          // sin Db2ITyped → placeholders "?"
            .On("T.INOCONFIR = S.INOCONFIR")              // clave de existencia
            .WhenNotMatchedInsert(                        // solo inserta si NO existe
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
        await cmd.ExecuteNonQueryAsync(); // inserta solo las que no existían
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
