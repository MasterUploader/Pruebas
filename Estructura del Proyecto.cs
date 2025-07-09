foreach (var deposit in response.Deposits)
{
    if (deposit?.Data == null) continue;

    string statusProceso = response.OpCode == "1308" ? "RECIBIDA" : "RECH-DENEG";
    var d = deposit.Data;

    // Construir modelo desde la respuesta
    var model = new BtsaCtaModel
    {
        INOCONFIR = d.ConfirmationNumber,
        IDATRECI = DateTime.Now.ToString("yyyyMMdd"),
        IHORRECI = DateTime.Now.ToString("HHmmssfff"),
        IDATCONF = " ",
        IHORCONF = " ",
        IDATVAL = " ",
        IHORVAL = " ",
        IDATPAGO = " ",
        IHORPAGO = " ",
        IDATACRE = " ",
        IHORACRE = " ",
        IDATRECH = " ",
        IHORRECH = " ",
        ITIPPAGO = d.PaymentTypeCode,
        ISERVICD = d.ServiceCode,
        IDESPAIS = d.DestinationCountryCode,
        IDESMONE = d.DestinationCurrencyCode,
        ISAGENCD = d.SenderAgentCode,
        ISPAISCD = d.SenderCountryCode,
        ISTATECD = d.SenderStateCode,
        IRAGENCD = d.RecipientAgentCode,
        ITICUENTA = d.RecipientAccountTypeCode,
        INOCUENTA = d.RecipientAccountNumber,
        INUMREFER = " ",
        ISTSREM = " ",
        ISTSPRO = statusProceso,
        IERR = response.OpCode ?? " ",
        IERRDSC = response.ProcessMsg ?? " ",
        IDSCRECH = " ",
        ACODPAIS = d.OriginCountryCode,
        ACODMONED = d.OriginCurrencyCode,
        AMTOENVIA = d.OriginAmount,
        AMTOCALCU = d.DestinationAmount,
        AFACTCAMB = d.ExchangeRateFx,
        BPRIMNAME = d.Sender.FirstName,
        BSECUNAME = d.Sender.MiddleName,
        BAPELLIDO = d.Sender.LastName,
        BSEGUAPE = d.Sender.MotherMaidenName,
        BDIRECCIO = d.Sender.Address.AddressLine,
        BCIUDAD = d.Sender.Address.City,
        BESTADO = d.Sender.Address.StateCode,
        BPAIS = d.Sender.Address.CountryCode,
        BCODPOST = d.Sender.Address.ZipCode,
        BTELEFONO = d.Sender.Address.Phone,
        CPRIMNAME = d.Recipient.FirstName,
        CSECUNAME = d.Recipient.MiddleName,
        CAPELLIDO = d.Recipient.LastName,
        CSEGUAPE = d.Recipient.MotherMaidenName,
        CDIRECCIO = d.Recipient.Address.AddressLine,
        CCIUDAD = d.Recipient.Address.City,
        CESTADO = d.Recipient.Address.StateCode,
        CPAIS = d.Recipient.Address.CountryCode,
        CCODPOST = d.Recipient.Address.ZipCode,
        CTELEFONO = d.Recipient.Address.Phone,
        DTIDENT = " ",
        ESALEDT = d.SaleDate,
        EMONREFER = d.MarketRefCurrencyCode,
        ETASAREFE = d.MarketRefCurrencyFx,
        EMTOREF = d.MarketRefCurrencyAmount
    };

    // Generar la consulta INSERT desde QueryBuilder
    var insertSql = _sqlQueryService.BuildInsertQuery<BtsaCtaModel>(model);

    // Crear y ejecutar el comando con RestUtilities.Connections
    using var command = (OleDbCommand)_databaseConnection.GetDbCommand(_httpContextAccessor.HttpContext!);
    command.CommandText = insertSql;
    command.CommandType = CommandType.Text;

    // Agregar parámetros automáticamente desde el modelo
    FieldsQuery param = new();
    param.AddParametersFromModel(command, model);

    // Ejecutar INSERT
    await command.ExecuteNonQueryAsync();
}
