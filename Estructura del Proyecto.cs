// Abrimos conexión
_connection.Open();

// Preparamos un único INSERT masivo
var insert = new InsertQueryBuilder("RS01BZI", "bcah96dta");

// --- Bloque de BALANCES ---
foreach (var balance in responseConsulta.Balances.Balance)
{
    foreach (var currencyB in balance.Currencys)
    {
        foreach (var denominationB in currencyB.Denominations.Denomination)
        {
            string responseId = Guid.NewGuid().ToString();

            insert.Values(
                ("BZI05HUSID", hUserId),
                ("BZI06HPROV", hProvider),
                ("BZI07HSESS", hSessionId),
                ("BZI08HCLIP", hClientIp),
                ("BZI09HTIME", fechaActual),
                ("BZI15IDJS", responseId),
                ("BZI16FCHR", fechaActual),
                ("BZI12PRTM", processingTime),
                ("BZI13STCD", statusCode),
                ("BZI14MSSG", message),
                ("BZI17IDTR", traceId),
                ("BZI18CDHT", httpCode),
                ("BZI19MSGS", systemMsg),
                ("BZI20MSGE", errorMsg),
                ("BZI27CDDB", balance.DeviceCode),
                ("BZI26FECB", balance.DateBalance),
                ("BZI39IATR", ""),                  // campos vacíos igual que tu SQL original
                ("BZI38FHTR", ""),
                ("BZI41PSTR", ""),
                ("BZI42NRTR", ""),
                ("BZI29CODM", currencyB.Code),
                ("BZI30MTMD", currencyB.Amount),
                ("BZI53ICTR", balance.BalanceType),
                ("BZI54NCTR", denominationB.Value),
                ("BZI51TTTR", denominationB.Quantity),
                ("BZI32VRDN", denominationB.Amount),
                ("BZI33CANT", denominationB.Type)
            );
        }
    }
}

// --- Bloque de TRANSACCIONES ---
foreach (var devcod in responseConsulta.Transaccion.DeviceCode)
{
    foreach (var DatBa in responseConsulta.Transaccion.DateBalance)
    {
        foreach (var trx in responseConsulta.Transaccion.Transaction)
        {
            foreach (var currency in trx.Currency)
            {
                foreach (var denomination in currency.Denominations.Denomination)
                {
                    string responseId = Guid.NewGuid().ToString();

                    insert.Values(
                        ("BZI05HUSID", hUserId),
                        ("BZI06HPROV", hProvider),
                        ("BZI07HSESS", hSessionId),
                        ("BZI08HCLIP", hClientIp),
                        ("BZI09HTIME", fechaActual),
                        ("BZI15IDJS", responseId),
                        ("BZI16FCHR", fechaActual),
                        ("BZI12PRTM", processingTime),
                        ("BZI13STCD", statusCode),
                        ("BZI14MSSG", message),
                        ("BZI17IDTR", traceId),
                        ("BZI18CDHT", httpCode),
                        ("BZI19MSGS", systemMsg),
                        ("BZI20MSGE", errorMsg),
                        ("BZI27CDDB", ""),                   // en tu SQL original aquí eran vacíos
                        ("BZI26FECB", ""),
                        ("BZI39IATR", trx.ActualId),
                        ("BZI38FHTR", trx.TransactonDate),
                        ("BZI41PSTR", trx.ServicePoint),
                        ("BZI42NRTR", trx.ReceiptNumber),
                        ("BZI29CODM", currency.Code),
                        ("BZI30MTMD", currency.Amount),
                        ("BZI53ICTR", currency.CashierId),
                        ("BZI54NCTR", currency.CashierName),
                        ("BZI51TTTR", trx.TipoTrans),
                        ("BZI25TIPO", ""),
                        ("BZI32VRDN", denomination.Value),
                        ("BZI33CANT", denomination.Quantity),
                        ("BZI34IMTT", denomination.Amount),
                        ("BZI35TNCN", denomination.Type)
                    );
                }
            }
        }
    }
}

// Construimos el SQL con placeholders y parámetros
var result = insert.Build();

// Ejecutamos con parámetros (sin concatenar SQL a mano)
using (var cmd = _connection.GetDbCommand(result, _httpContextAccessor.HttpContext))
{
    var rows = await cmd.ExecuteNonQueryAsync();
    // rows es el total de filas insertadas
    return rows > 0;
}
