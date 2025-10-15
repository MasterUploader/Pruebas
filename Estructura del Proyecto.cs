using QueryBuilder.Builders;
using QueryBuilder.Enums;

/// <summary>
/// Inserta depósitos en BCAH96DTA.BTSACTA mediante MERGE por lotes.
/// - Sin Db2ITyped (placeholders "?" sin CAST).
/// - Evita duplicados por INOCONFIR (solo inserta cuando no existe).
/// - Parte el envío en bloques para no exceder límites de longitud/params del proveedor.
/// </summary>
private async Task<bool> InsertarEnIbtSactaAsync(SDEPResponseData response)
{
    _databaseConnection.Open();
    if (!_databaseConnection.IsConnected) return false;
    if (response?.Deposits == null || response.Deposits.Count == 0) return false;

    try
    {
        // Definición única de columnas (orden consistente entre S y T).
        string[] cols =
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
        };

        // Mapeo automático para INSERT NO-MATCH: T.col = S.col
        var autoMap = cols.Select(c => (c, $"S.{c}")).ToArray();

        // Normalizador: DB2 i suele preferir CHAR con al menos un espacio en blanco.
        static string C(object? v) => string.IsNullOrWhiteSpace(v?.ToString()) ? " " : v!.ToString()!;

        // Para evitar SQL gigante y límites de parámetros, procesamos en lotes.
        const int CHUNK_SIZE = 300; // Ajusta si necesitas menos/más (p.ej. 200–500)
        List<object?[]> rowsBatch = [];
        int insertedBatches = 0;

        foreach (var deposit in response.Deposits)
        {
            if (deposit?.Data == null) continue;
            var d = deposit.Data;

            // Fechas/horas como cadenas
            string hoyYmd      = DateTime.Now.ToString("yyyyMMdd");
            string ahoraHmsfff = DateTime.Now.ToString("HHmmssfff");

            // Direcciones (truncadas a 65)
            string cDir = d.Recipient?.Address?.AddressLine ?? " ";
            if (cDir.Length > 65) cDir = cDir[..65];

            string bDir = d.Sender?.Address?.AddressLine ?? " ";
            if (bDir.Length > 65) bDir = bDir[..65];

            // Estado proceso
            string statusProceso = response.OpCode == "1308" ? "RECIBIDA" : "RECH-DENEG";

            // Construimos la fila (mismo orden que 'cols')
            rowsBatch.Add(new object?[]
            {
                C(d.ConfirmationNumber),
                hoyYmd, ahoraHmsfff,
                " "," "," "," ",          // IDATCONF,IHORCONF,IDATVAL,IHORVAL
                " "," "," "," ",          // IDATPAGO,IHORPAGO,IDATACRE,IHORACRE
                " "," ",                  // IDATRECH,IHORRECH
                C(d.PaymentTypeCode),     // ITIPPAGO
                C(d.ServiceCode),         // ISERVICD
                C(d.DestinationCountryCode),   // IDESPAIS
                C(d.DestinationCurrencyCode),  // IDESMONE
                C(d.SenderAgentCode),     // ISAGENCD
                C(d.SenderCountryCode),   // ISPAISCD
                C(d.SenderStateCode),     // ISTATECD
                C(d.RecipientAgentCode),  // IRAGENCD
                C(d.RecipientAccountTypeCode), // ITICUENTA
                C(d.RecipientAccountNumber),   // INOCUENTA
                " ",                      // INUMREFER
                " ",                      // ISTSREM
                statusProceso,            // ISTSPRO
                C(response.OpCode),       // IERR
                C(response.ProcessMsg),   // IERRDSC
                " ",                      // IDSCRECH
                C(d.OriginCountryCode),   // ACODPAIS
                C(d.OriginCurrencyCode),  // ACODMONED
                C(d.OriginAmount),        // AMTOENVIA
                C(d.DestinationAmount),   // AMTOCALCU
                C(d.ExchangeRateFx),      // AFACTCAMB
                C(d.Sender?.FirstName),   // BPRIMNAME
                C(d.Sender?.MiddleName),  // BSECUNAME
                C(d.Sender?.LastName),    // BAPELLIDO
                C(d.Sender?.MotherMaidenName), // BSEGUAPE
                bDir,                     // BDIRECCIO
                C(d.Sender?.Address?.City),     // BCIUDAD
                C(d.Sender?.Address?.StateCode),// BESTADO
                C(d.Sender?.Address?.CountryCode), // BPAIS
                C(d.Sender?.Address?.ZipCode),  // BCODPOST
                C(d.Sender?.Address?.Phone),    // BTELEFONO
                C(d.Recipient?.FirstName),      // CPRIMNAME
                C(d.Recipient?.MiddleName),     // CSECUNAME
                C(d.Recipient?.LastName),       // CAPELLIDO
                C(d.Recipient?.MotherMaidenName), // CSEGUAPE
                cDir,                     // CDIRECCIO
                C(d.Recipient?.Address?.City),       // CCIUDAD
                C(d.Recipient?.Address?.StateCode),  // CESTADO
                C(d.Recipient?.Address?.CountryCode),// CPAIS
                C(d.Recipient?.Address?.ZipCode),    // CCODPOST
                C(d.Recipient?.Address?.Phone),      // CTELEFONO
                " ",                      // DTIDENT
                C(d.SaleDate),            // ESALEDT
                C(d.MarketRefCurrencyCode), // EMONREFER
                C(d.MarketRefCurrencyFx),   // ETASAREFE
                C(d.MarketRefCurrencyAmount)// EMTOREF
            });

            // Cuando llenamos el lote, ejecutamos MERGE y vaciamos
            if (rowsBatch.Count >= CHUNK_SIZE)
            {
                await EjecutarMergeBatchAsync(cols, autoMap, rowsBatch);
                rowsBatch.Clear();
                insertedBatches++;
            }
        }

        // Último lote pendiente
        if (rowsBatch.Count > 0)
        {
            await EjecutarMergeBatchAsync(cols, autoMap, rowsBatch);
            insertedBatches++;
        }

        return insertedBatches > 0;
    }
    catch
    {
        return false;
    }
    finally
    {
        _databaseConnection.Close();
    }

    // Ejecuta un MERGE con USING (VALUES ...) para el lote actual.
    async Task EjecutarMergeBatchAsync(string[] columns, (string Target, string SrcExpr)[] mapping, List<object?[]> rows)
    {
        var merge = new MergeQueryBuilder("BTSACTA", "BCAH96DTA", SqlDialect.Db2i)
            .UsingValues(columns, rows, alias: "S")             // sin tipos explícitos
            .On("T.INOCONFIR = S.INOCONFIR")                    // clave de existencia
            .WhenNotMatchedInsert(mapping)                      // T.col = S.col para todas
            .Build();

        using var cmd = _databaseConnection.GetDbCommand(merge, _httpContextAccessor.HttpContext!);
        await cmd.ExecuteNonQueryAsync();
    }
}
