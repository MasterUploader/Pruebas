Necesito que en este metodo donde se inserta datos en una tabla uses merge, para que cuando exista el campo no lo inserte:


 private async Task<bool> InsertarEnIbtSactaAsync(SDEPResponseData response)
 {
     _databaseConnection.Open();
     if (!_databaseConnection.IsConnected) return false;
     if (response?.Deposits == null || response.Deposits.Count == 0) return false;

     try
     {
         // 1) Preparar columnas y builder (DB2 i → placeholders ? + Parameters en orden)
         var builder = new InsertQueryBuilder("BTSACTA", "BCAH96DTA", SqlDialect.Db2i)
             .IntoColumns(
                 "INOCONFIR", "IDATRECI", "IHORRECI", "IDATCONF", "IHORCONF", "IDATVAL", "IHORVAL",
                 "IDATPAGO", "IHORPAGO", "IDATACRE", "IHORACRE", "IDATRECH", "IHORRECH",
                 "ITIPPAGO", "ISERVICD", "IDESPAIS", "IDESMONE", "ISAGENCD", "ISPAISCD", "ISTATECD",
                 "IRAGENCD", "ITICUENTA", "INOCUENTA", "INUMREFER", "ISTSREM", "ISTSPRO", "IERR",
                 "IERRDSC", "IDSCRECH", "ACODPAIS", "ACODMONED", "AMTOENVIA", "AMTOCALCU",
                 "AFACTCAMB",
                 "BPRIMNAME", "BSECUNAME", "BAPELLIDO", "BSEGUAPE", "BDIRECCIO", "BCIUDAD",
                 "BESTADO", "BPAIS", "BCODPOST", "BTELEFONO",
                 "CPRIMNAME", "CSECUNAME", "CAPELLIDO", "CSEGUAPE", "CDIRECCIO", "CCIUDAD",
                 "CESTADO", "CPAIS", "CCODPOST", "CTELEFONO",
                 "DTIDENT", "ESALEDT", "EMONREFER", "ETASAREFE", "EMTOREF"
             );

         // 2) Cargar filas (una por depósito) usando Row(...) en el mismo orden de IntoColumns
         foreach (var deposit in response.Deposits)
         {
             if (deposit?.Data == null) continue;

             var d = deposit.Data;

             // Estatus de proceso basado en opcode (igual a tu código)
             string statusProceso = response.OpCode == "1308" ? "RECIBIDA" : "RECH-DENEG";

             // Truncados a 65 chars (igual a tu código)
             string CDIRECCIO = d.Recipient?.Address?.AddressLine ?? " ";
             if (CDIRECCIO.Length > 65) CDIRECCIO = CDIRECCIO[..65];

             string BDIRECCIO = d.Sender?.Address?.AddressLine ?? " ";
             if (BDIRECCIO.Length > 65) BDIRECCIO = BDIRECCIO[..65];

             // Tiempos actuales en formato requerido
             string hoyYmd = DateTime.Now.ToString("yyyyMMdd");
             string ahoraHmsfff = DateTime.Now.ToString("HHmmssfff");

             // Helper para evitar nulls en CHAR (tu código usaba " ")
             static string C(object? v) => string.IsNullOrWhiteSpace(v?.ToString()) ? " " : v!.ToString()!;

             builder.Row(
                 C(d.ConfirmationNumber),       // INOCONFIR
                 hoyYmd,                        // IDATRECI
                 ahoraHmsfff,                   // IHORRECI
                 " ",                           // IDATCONF
                 " ",                           // IHORCONF
                 " ",                           // IDATVAL
                 " ",                           // IHORVAL
                 " ",                           // IDATPAGO
                 " ",                           // IHORPAGO
                 " ",                           // IDATACRE
                 " ",                           // IHORACRE
                 " ",                           // IDATRECH
                 " ",                           // IHORRECH
                 C(d.PaymentTypeCode),          // ITIPPAGO
                 C(d.ServiceCode),              // ISERVICD
                 C(d.DestinationCountryCode),   // IDESPAIS
                 C(d.DestinationCurrencyCode),  // IDESMONE
                 C(d.SenderAgentCode),          // ISAGENCD
                 C(d.SenderCountryCode),        // ISPAISCD
                 C(d.SenderStateCode),          // ISTATECD
                 C(d.RecipientAgentCode),       // IRAGENCD
                 C(d.RecipientAccountTypeCode), // ITICUENTA
                 C(d.RecipientAccountNumber),   // INOCUENTA
                 " ",                           // INUMREFER
                 " ",                           // ISTSREM
                 statusProceso,                 // ISTSPRO
                 C(response.OpCode),            // IERR
                 C(response.ProcessMsg),        // IERRDSC
                 " ",                           // IDSCRECH
                 C(d.OriginCountryCode),        // ACODPAIS
                 C(d.OriginCurrencyCode),       // ACODMONED
                 C(d.OriginAmount),             // AMTOENVIA
                 C(d.DestinationAmount),        // AMTOCALCU
                 C(d.ExchangeRateFx),           // AFACTCAMB
                 C(d.Sender?.FirstName),        // BPRIMNAME
                 C(d.Sender?.MiddleName),       // BSECUNAME
                 C(d.Sender?.LastName),         // BAPELLIDO
                 C(d.Sender?.MotherMaidenName), // BSEGUAPE
                 C(BDIRECCIO),                  // BDIRECCIO
                 C(d.Sender?.Address?.City),    // BCIUDAD
                 C(d.Sender?.Address?.StateCode),   // BESTADO
                 C(d.Sender?.Address?.CountryCode), // BPAIS
                 C(d.Sender?.Address?.ZipCode),     // BCODPOST
                 C(d.Sender?.Address?.Phone),       // BTELEFONO
                 C(d.Recipient?.FirstName),         // CPRIMNAME
                 C(d.Recipient?.MiddleName),        // CSECUNAME
                 C(d.Recipient?.LastName),          // CAPELLIDO
                 C(d.Recipient?.MotherMaidenName),  // CSEGUAPE
                 C(CDIRECCIO),                  // CDIRECCIO
                 C(d.Recipient?.Address?.City),     // CCIUDAD
                 C(d.Recipient?.Address?.StateCode),// CESTADO
                 C(d.Recipient?.Address?.CountryCode),// CPAIS
                 C(d.Recipient?.Address?.ZipCode),  // CCODPOST
                 C(d.Recipient?.Address?.Phone),    // CTELEFONO
                 " ",                           // DTIDENT
                 C(d.SaleDate),                 // ESALEDT
                 C(d.MarketRefCurrencyCode),    // EMONREFER
                 C(d.MarketRefCurrencyFx),      // ETASAREFE
                 C(d.MarketRefCurrencyAmount)   // EMTOREF
             );
         }

         // 3) Construir SQL (INSERT con múltiples filas) + parámetros posicionales
         var query = builder.Build();

         using var cmd = _databaseConnection.GetDbCommand(query, _httpContextAccessor.HttpContext!);

         await cmd.ExecuteNonQueryAsync();
         return true;
     }
     catch
     {
         // (opcional) Log de error con tu LoggingService
         return false;
     }
     finally
     {
         _databaseConnection.Close();
     }
