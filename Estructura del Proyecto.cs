private async Task GuardarDetalleReporteAsync(
    IDatabaseConnection _connection,
    IHttpContextAccessor _contextAccessor,
    string agentCd,
    string agentTransTypeCode,
    string contractTypeCd,
    List<DetAct> detActList)
{
    if (detActList == null || detActList.Count == 0)
        return;

    var today = DateTime.Now.ToString("ddMMyyyy");
    int correlativoGrupo = 1;

    foreach (var act in detActList)
    {
        if (act?.Details?.DetailList == null)
            continue;

        int correlativoDetalle = 1;

        foreach (var detail in act.Details.DetailList)
        {
            FieldsQuery param = new();

            string insertQuery = @"
                INSERT INTO BCAH96DTA.TU_TABLA_REPORTE (
                    REP00UID, REP01DCOR, REP02ATTC, REP03ACTC, REP04ADT,
                    REP05ACD, REP06SCD, REP06OCCD, REP08OCCD, REP09DCCD,
                    REP10DCCD, REP11PTCD, REP12OACD, REP13DCOR, REP14MTC,
                    REP15CNM, REP16AON, REP17OAM, REP18DAM, REP19FAM,
                    REP20DISC, REP21COP, REP22COP, REP23COP, REP24COP,
                    REP25COP, REP26COP, REP27COP, REP28COP, REP29COP
                ) VALUES (
                    ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?
                )";

            using var command = _connection.GetDbCommand(_contextAccessor.HttpContext!);
            command.CommandText = insertQuery;
            command.CommandType = CommandType.Text;

            param.AddOleDbParameter(command, "REP00UID", OleDbType.Char, Guid.NewGuid().ToString());
            param.AddOleDbParameter(command, "REP01DCOR", OleDbType.Numeric, correlativoGrupo);
            param.AddOleDbParameter(command, "REP02ATTC", OleDbType.Char, agentTransTypeCode);
            param.AddOleDbParameter(command, "REP03ACTC", OleDbType.Char, contractTypeCd);
            param.AddOleDbParameter(command, "REP04ADT", OleDbType.Char, act.ActivityDate);
            param.AddOleDbParameter(command, "REP05ACD", OleDbType.Char, agentCd);
            param.AddOleDbParameter(command, "REP06SCD", OleDbType.Char, act.ServiceCode);
            param.AddOleDbParameter(command, "REP06OCCD", OleDbType.Char, act.OriginCountryCode);
            param.AddOleDbParameter(command, "REP08OCCD", OleDbType.Char, act.OriginCurrencyCode);
            param.AddOleDbParameter(command, "REP09DCCD", OleDbType.Char, act.DestinationCountryCode);
            param.AddOleDbParameter(command, "REP10DCCD", OleDbType.Char, act.DestinationCurrencyCode);
            param.AddOleDbParameter(command, "REP11PTCD", OleDbType.Char, act.PaymentTypeCode);
            param.AddOleDbParameter(command, "REP12OACD", OleDbType.Char, act.OriginAgentCode);
            param.AddOleDbParameter(command, "REP13DCOR", OleDbType.Numeric, correlativoDetalle);
            param.AddOleDbParameter(command, "REP14MTC", OleDbType.Char, detail.MovementTypeCode);
            param.AddOleDbParameter(command, "REP15CNM", OleDbType.Char, detail.ConfirmationNumber);
            param.AddOleDbParameter(command, "REP16AON", OleDbType.Char, detail.AgentOrderNumber);
            param.AddOleDbParameter(command, "REP17OAM", OleDbType.Numeric, detail.OriginAmount);
            param.AddOleDbParameter(command, "REP18DAM", OleDbType.Numeric, detail.DestinationAmount);
            param.AddOleDbParameter(command, "REP19FAM", OleDbType.Numeric, detail.FeeAmount);
            param.AddOleDbParameter(command, "REP20DISC", OleDbType.Numeric, detail.DiscountAmount);

            // Rellenar campos REP21COP a REP29COP con espacios
            for (int i = 21; i <= 29; i++)
            {
                string paramName = $"REP{i:D2}COP";
                param.AddOleDbParameter(command, paramName, OleDbType.Char, " ");
            }

            await command.ExecuteNonQueryAsync();
            correlativoDetalle++;
        }

        correlativoGrupo++;
    }
}
