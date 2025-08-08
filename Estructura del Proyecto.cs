Por ejemplo tengo este codigo, como quedaria con la libreria RestUtils.QueryBuilder:


                int indexBalance = 0;
                var resultResp = responseConsulta.Result;
                var countryID = responseConsulta.CountryId;

                foreach (var balance in responseConsulta.Balances.Balance)
                {
                    foreach (var currencyB in balance.Currencys)
                    {
                        foreach (var denominationB in currencyB.Denominations.Denomination)
                        {
                            string responseId = Guid.NewGuid().ToString();
                            string row = $@"('{hUserId}', '{hProvider}', '{hSessionId}', '{hClientIp}', '{fechaActual}', '{responseId}', '{fechaActual}', '{processingTime}', '{statusCode}', '{message}', '{traceId}', '{httpCode}', '{systemMsg}', '{errorMsg}', '{balance.DeviceCode}', '{balance.DateBalance}',  '',  '',  '',  '', '', '', '','{currencyB.Code}', '{currencyB.Amount}', '{balance.BalanceType}','{denominationB.Value}', '{denominationB.Quantity}', '{denominationB.Amount}', '{denominationB.Type}')";
                            sqlInsertRows.Add(row.Trim());
                        }
                    }
                    indexBalance++;
                }

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
                                    string row = $@"('{hUserId}', '{hProvider}', '{hSessionId}', '{hClientIp}', '{fechaActual}','{responseId}', '{fechaActual}', '{processingTime}', '{statusCode}', '{message}', '{traceId}', '{httpCode}', '{systemMsg}', '{errorMsg}', '', '', '{trx.ActualId}', '{trx.TransactonDate}', '{trx.ServicePoint}', '{trx.ReceiptNumber}', '{currency.Code}', '{currency.Amount}', '{currency.CashierId}','{currency.CashierName}', '{trx.TipoTrans}', '', '{denomination.Value}', '{denomination.Quantity}', '{denomination.Amount}', '{denomination.Type}')";
                                    sqlInsertRows.Add(row.Trim());
                                }
                            }
                        }
                    }
                }

                if (sqlInsertRows.Any())
                {
                    string insertQuery = @"INSERT INTO bcah96dta.RS01BZI(BZI05HUSID, BZI06HPROV, BZI07HSESS, BZI08HCLIP, BZI09HTIME, BZI15IDJS, BZI16FCHR, BZI12PRTM, BZI13STCD, BZI14MSSG, BZI17IDTR, BZI18CDHT, BZI19MSGS, BZI20MSGE, BZI27CDDB, BZI26FECB, BZI39IATR, BZI38FHTR, BZI41PSTR, BZI42NRTR, BZI29CODM, BZI30MTMD, BZI53ICTR, BZI54NCTR, BZI51TTTR, BZI25TIPO, BZI32VRDN, BZI33CANT, BZI34IMTT, BZI35TNCN) VALUES " + string.Join(",", sqlInsertRows);

                    conection.Open();

                    if (conection.Connect.CheckConfigurationState)
                    {
                        using (var command = new OleDbCommand(insertQuery, conection.Connect.OleDbConnection))
                        {
                            int result = command.ExecuteNonQuery();
                            return result > 0;
                        }
                    }
