FieldsQuery param = new();

    DateTime nowUTC = DateTime.UtcNow;
    string fechaISO8601 = nowUTC.ToString("yyyy-dd-MMTHH:mm:ss.fffZ");

    if (_connection.IsConnected)
    {
        string sqlQuery = @"SELECT * FROM CYBERDTA.CYBUTHDP WHERE HDP00GUID = ?";

        using var command = _connection.GetDbCommand(_contextAccessor.HttpContext!);
        command.CommandText = sqlQuery;
        command.CommandType = System.Data.CommandType.Text;
        // command.Parameters.Add("HDP00GUID", OleDbType.Char).Value = guid;
        param.AddOleDbParameter(command, "HDP00GUID", OleDbType.Char, guid);

        using DbDataReader reader = command.ExecuteReader();

        if (reader.HasRows)
        {
            while (reader.Read())
            {
                //Datos

                postPaymentDtoFinal.Amount.Value = ConvertirAEnteroGinih(reader, "HDP01MTTO"); //Convert.ToInt32(reader.GetValue(reader.GetOrdinal("HDP01MTTO"))); //Value
                postPaymentDtoFinal.Amount.Currency = reader.GetString(reader.GetOrdinal("HDP02MONE")); //Moneda
                postPaymentDtoFinal.Amount.Breakdown.Subtotal = ConvertirAEnteroGinih(reader, "HDP03SUTO"); // Convert.ToInt32(reader.GetValue(reader.GetOrdinal("HDP03SUTO"))); //Subtotal
                postPaymentDtoFinal.Amount.Breakdown.ProcessingFee = ConvertirAEnteroGinih(reader, "HDP04PRFE"); //Convert.ToInt32(reader.GetValue(reader.GetOrdinal("HDP04PRFE"))); //Processing Fee
                postPaymentDtoFinal.Amount.Breakdown.Surcharge = ConvertirAEnteroGinih(reader, "HDP05MTCA"); //Convert.ToInt32(reader.GetValue(reader.GetOrdinal("HDP05MTCA"))); //Surcharge cargo
                postPaymentDtoFinal.Amount.Breakdown.Discount = ConvertirAEnteroGinih(reader, "HDP06MTDE"); //Convert.ToInt32(reader.GetValue(reader.GetOrdinal("HDP06MTDE"))); //Discount descuento
                postPaymentDtoFinal.Amount.Breakdown.Tax = ConvertirAEnteroGinih(reader, "HDP07MTIM"); //Convert.ToInt32(reader.GetValue(reader.GetOrdinal("HDP07MTIM"))); //Tax impuesto
                postPaymentDtoFinal.Amount.Breakdown.Total = ConvertirAEnteroGinih(reader, "HDP08MTTO"); //Convert.ToInt32(reader.GetValue(reader.GetOrdinal("HDP08MTTO"))); //Total
                postPaymentDtoFinal.CustomerID = reader.GetString(reader.GetOrdinal("HDP09CUID"));//Customer ID                                
                postPaymentDtoFinal.PaymentDate = fechaISO8601;//Utilitarios.ConvertirFecha(reader.GetString(reader.GetOrdinal("HDP10PADA")));//Fecha Pago
                postPaymentDtoFinal.ReferenceId = guid; //reader.GetString(reader.GetOrdinal("HDP11REID"));//Referencia
                postPaymentDtoFinal.PayableOption = reader.GetString(reader.GetOrdinal("HDP12PAOP"));//Opcion de Pago
                postPaymentDtoFinal.CompanyID = reader.GetString(reader.GetOrdinal("HDP13COID"));//Codigo Compañia
                postPaymentDtoFinal.ReceivableID = reader.GetString(reader.GetOrdinal("HDP14GEN1"));//ReceivableID

                postPaymentDtoFinal.AdditionalData = "{\"PaymentMethod\": \"cash\" }";
                postPaymentDtoFinal.Channel = "interbanca";

                //  postPaymentDtoFinal.PayableOption = reader.GetString(reader.GetOrdinal("HDP15GEN2"));//Opcion de Pago

                // postPaymentDtoFinal.PayableOption =  Convert.ToInt32(reader.GetValue(reader.GetOrdinal("HDP16GEN3")));//Opcion de Pago


                // postPaymentDtoFinal.PayableOption =  Convert.ToInt32(reader.GetValue(reader.GetOrdinal("HDP17GEN4")));//Opcion de Pago

                exitoso = true;
            }
        }
    }
    return postPaymentDtoFinal;
