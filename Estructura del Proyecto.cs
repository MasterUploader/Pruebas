Convierte este metodo privado para que use la nueva versión de Insert

 private async Task<bool> InsertarEnIbtSactaAsync(SDEPResponseData response)
 {
     _databaseConnection.Open();
     if (!_databaseConnection.IsConnected)
         return false;
     if (response?.Deposits == null || response.Deposits.Count == 0)
         return false;

     foreach (var deposit in response.Deposits)
     {
         if (deposit?.Data == null) continue;

         // Estatus de proceso basado en opcode
         string statusProceso = response.OpCode == "1308" ? "RECIBIDA" : "RECH-DENEG";

         var d = deposit.Data;

         string CDIRECCIO = d.Recipient.Address.AddressLine.Length > 65 ? d.Recipient.Address.AddressLine[..65] : d.Recipient.Address.AddressLine;

         string BDIRECCIO = d.Sender.Address.AddressLine.Length > 65 ? d.Sender.Address.AddressLine[..65] : d.Sender.Address.AddressLine;

         FieldsQueryL param = new();

         string insertSql = @"
         INSERT INTO BCAH96DTA.BTSACTA (
             INOCONFIR, IDATRECI, IHORRECI, IDATCONF, IHORCONF, IDATVAL, IHORVAL, IDATPAGO, IHORPAGO,
             IDATACRE, IHORACRE, IDATRECH, IHORRECH, ITIPPAGO, ISERVICD, IDESPAIS, IDESMONE, ISAGENCD,
             ISPAISCD, ISTATECD, IRAGENCD, ITICUENTA, INOCUENTA, INUMREFER, ISTSREM, ISTSPRO, IERR,
             IERRDSC, IDSCRECH, ACODPAIS, ACODMONED, AMTOENVIA, AMTOCALCU, AFACTCAMB,
             BPRIMNAME, BSECUNAME, BAPELLIDO, BSEGUAPE, BDIRECCIO, BCIUDAD, BESTADO, BPAIS,
             BCODPOST, BTELEFONO, CPRIMNAME, CSECUNAME, CAPELLIDO, CSEGUAPE, CDIRECCIO,
             CCIUDAD, CESTADO, CPAIS, CCODPOST, CTELEFONO, DTIDENT, ESALEDT, EMONREFER,
             ETASAREFE, EMTOREF
         ) VALUES (
             ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?
         )";

         using var command = _databaseConnection.GetDbCommand(_httpContextAccessor.HttpContext!);
         command.CommandText = insertSql;
         command.CommandType = CommandType.Text;

         // 🔹 Valores conocidos del XML
         param.AddOleDbParameter(command, "INOCONFIR", OleDbType.Char, d.ConfirmationNumber);
         param.AddOleDbParameter(command, "IDATRECI", OleDbType.Char, DateTime.Now.ToString("yyyyMMdd"));
         param.AddOleDbParameter(command, "IHORRECI", OleDbType.Char, DateTime.Now.ToString("HHmmssfff"));
         param.AddOleDbParameter(command, "IDATCONF", OleDbType.Char, " ");
         param.AddOleDbParameter(command, "IHORCONF", OleDbType.Char, " ");
         param.AddOleDbParameter(command, "IDATVAL", OleDbType.Char, " ");
         param.AddOleDbParameter(command, "IHORVAL", OleDbType.Char, " ");
         param.AddOleDbParameter(command, "IDATPAGO", OleDbType.Char, " ");
         param.AddOleDbParameter(command, "IHORPAGO", OleDbType.Char, " ");
         param.AddOleDbParameter(command, "IDATACRE", OleDbType.Char, " ");
         param.AddOleDbParameter(command, "IHORACRE", OleDbType.Char, " ");
         param.AddOleDbParameter(command, "IDATRECH", OleDbType.Char, " ");
         param.AddOleDbParameter(command, "IHORRECH", OleDbType.Char, " ");
         param.AddOleDbParameter(command, "ITIPPAGO", OleDbType.Char, d.PaymentTypeCode);
         param.AddOleDbParameter(command, "ISERVICD", OleDbType.Char, d.ServiceCode);
         param.AddOleDbParameter(command, "IDESPAIS", OleDbType.Char, d.DestinationCountryCode);
         param.AddOleDbParameter(command, "IDESMONE", OleDbType.Char, d.DestinationCurrencyCode);
         param.AddOleDbParameter(command, "ISAGENCD", OleDbType.Char, d.SenderAgentCode);
         param.AddOleDbParameter(command, "ISPAISCD", OleDbType.Char, d.SenderCountryCode);
         param.AddOleDbParameter(command, "ISTATECD", OleDbType.Char, d.SenderStateCode);
         param.AddOleDbParameter(command, "IRAGENCD", OleDbType.Char, d.RecipientAgentCode);
         param.AddOleDbParameter(command, "ITICUENTA", OleDbType.Char, d.RecipientAccountTypeCode);
         param.AddOleDbParameter(command, "INOCUENTA", OleDbType.Char, d.RecipientAccountNumber);
         param.AddOleDbParameter(command, "INUMREFER", OleDbType.Char, " ");
         param.AddOleDbParameter(command, "ISTSREM", OleDbType.Char, " ");
         param.AddOleDbParameter(command, "ISTSPRO", OleDbType.Char, statusProceso);
         param.AddOleDbParameter(command, "IERR", OleDbType.Char, response.OpCode ?? " ");
         param.AddOleDbParameter(command, "IERRDSC", OleDbType.Char, response.ProcessMsg ?? " ");
         param.AddOleDbParameter(command, "IDSCRECH", OleDbType.Char, " ");
         param.AddOleDbParameter(command, "ACODPAIS", OleDbType.Char, d.OriginCountryCode);
         param.AddOleDbParameter(command, "ACODMONED", OleDbType.Char, d.OriginCurrencyCode);
         param.AddOleDbParameter(command, "AMTOENVIA", OleDbType.Char, d.OriginAmount);
         param.AddOleDbParameter(command, "AMTOCALCU", OleDbType.Char, d.DestinationAmount);
         param.AddOleDbParameter(command, "AFACTCAMB", OleDbType.Char, d.ExchangeRateFx);
         param.AddOleDbParameter(command, "BPRIMNAME", OleDbType.Char, d.Sender.FirstName);
         param.AddOleDbParameter(command, "BSECUNAME", OleDbType.Char, d.Sender.MiddleName);
         param.AddOleDbParameter(command, "BAPELLIDO", OleDbType.Char, d.Sender.LastName);
         param.AddOleDbParameter(command, "BSEGUAPE", OleDbType.Char, d.Sender.MotherMaidenName);
         param.AddOleDbParameter(command, "BDIRECCIO", OleDbType.Char, BDIRECCIO);
         param.AddOleDbParameter(command, "BCIUDAD", OleDbType.Char, d.Sender.Address.City);
         param.AddOleDbParameter(command, "BESTADO", OleDbType.Char, d.Sender.Address.StateCode);
         param.AddOleDbParameter(command, "BPAIS", OleDbType.Char, d.Sender.Address.CountryCode);
         param.AddOleDbParameter(command, "BCODPOST", OleDbType.Char, d.Sender.Address.ZipCode);
         param.AddOleDbParameter(command, "BTELEFONO", OleDbType.Char, d.Sender.Address.Phone);
         param.AddOleDbParameter(command, "CPRIMNAME", OleDbType.Char, d.Recipient.FirstName);
         param.AddOleDbParameter(command, "CSECUNAME", OleDbType.Char, d.Recipient.MiddleName);
         param.AddOleDbParameter(command, "CAPELLIDO", OleDbType.Char, d.Recipient.LastName);
         param.AddOleDbParameter(command, "CSEGUAPE", OleDbType.Char, d.Recipient.MotherMaidenName);
         param.AddOleDbParameter(command, "CDIRECCIO", OleDbType.Char, CDIRECCIO);
         param.AddOleDbParameter(command, "CCIUDAD", OleDbType.Char, d.Recipient.Address.City);
         param.AddOleDbParameter(command, "CESTADO", OleDbType.Char, d.Recipient.Address.StateCode);
         param.AddOleDbParameter(command, "CPAIS", OleDbType.Char, d.Recipient.Address.CountryCode);
         param.AddOleDbParameter(command, "CCODPOST", OleDbType.Char, d.Recipient.Address.ZipCode);
         param.AddOleDbParameter(command, "CTELEFONO", OleDbType.Char, d.Recipient.Address.Phone);
         param.AddOleDbParameter(command, "DTIDENT", OleDbType.Char, " ");
         param.AddOleDbParameter(command, "ESALEDT", OleDbType.Char, d.SaleDate);
         param.AddOleDbParameter(command, "EMONREFER", OleDbType.Char, d.MarketRefCurrencyCode);
         param.AddOleDbParameter(command, "ETASAREFE", OleDbType.Char, d.MarketRefCurrencyFx);
         param.AddOleDbParameter(command, "EMTOREF", OleDbType.Char, d.MarketRefCurrencyAmount);

         await command.ExecuteNonQueryAsync();
     }
     return true;
 }
