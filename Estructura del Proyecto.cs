private async Task InsertarEnIbtSactaAsync(
    IDatabaseConnection _connection,
    IHttpContextAccessor _contextAccessor,
    DepositsResponse response)
{
    if (response?.Deposits == null || response.Deposits.Count == 0)
        return;

    foreach (var deposit in response.Deposits)
    {
        if (deposit?.Data == null) continue;

        var data = deposit.Data;
        FieldsQuery param = new();

        string insertSql = @"
            INSERT INTO BCAH96DTA.IBTSACTA (
                INOCONFIR, ISERVICD, IDESPAIS, IDESMONE,
                ISAGENCD, ISPAISCD, ISTATECD, IRAGENCD,
                ITICUENTA, INOCUENTA,
                ACODPAIS, ACODMONED, AMTOENVIA, AMTOCALCU, AFACTCAMB,
                ESALEDT, EMONREFER, ETASAREFE, EMTOREF
            ) VALUES (
                ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?
            )";

        using var command = _connection.GetDbCommand(_contextAccessor.HttpContext!);
        command.CommandText = insertSql;
        command.CommandType = CommandType.Text;

        param.AddOleDbParameter(command, "INOCONFIR", OleDbType.Char, data.ConfirmationNumber);
        param.AddOleDbParameter(command, "ISERVICD", OleDbType.Char, data.ServiceCode);
        param.AddOleDbParameter(command, "IDESPAIS", OleDbType.Char, data.DestinationCountryCode);
        param.AddOleDbParameter(command, "IDESMONE", OleDbType.Char, data.DestinationCurrencyCode);
        param.AddOleDbParameter(command, "ISAGENCD", OleDbType.Char, data.SenderAgentCode);
        param.AddOleDbParameter(command, "ISPAISCD", OleDbType.Char, data.SenderCountryCode);
        param.AddOleDbParameter(command, "ISTATECD", OleDbType.Char, data.SenderStateCode);
        param.AddOleDbParameter(command, "IRAGENCD", OleDbType.Char, data.RecipientAgentCode);
        param.AddOleDbParameter(command, "ITICUENTA", OleDbType.Char, data.RecipientAccountTypeCode);
        param.AddOleDbParameter(command, "INOCUENTA", OleDbType.Char, data.RecipientAccountNumber);
        param.AddOleDbParameter(command, "ACODPAIS", OleDbType.Char, data.OriginCountryCode);
        param.AddOleDbParameter(command, "ACODMONED", OleDbType.Char, data.OriginCurrencyCode);
        param.AddOleDbParameter(command, "AMTOENVIA", OleDbType.Char, data.OriginAmount);
        param.AddOleDbParameter(command, "AMTOCALCU", OleDbType.Char, data.DestinationAmount);
        param.AddOleDbParameter(command, "AFACTCAMB", OleDbType.Char, data.ExchangeRateFx);
        param.AddOleDbParameter(command, "ESALEDT", OleDbType.Char, data.SaleDate);
        param.AddOleDbParameter(command, "EMONREFER", OleDbType.Char, data.MarketRefCurrencyCode);
        param.AddOleDbParameter(command, "ETASAREFE", OleDbType.Char, data.MarketRefCurrencyFx);
        param.AddOleDbParameter(command, "EMTOREF", OleDbType.Char, data.MarketRefCurrencyAmount);

        await command.ExecuteNonQueryAsync();
    }
}
