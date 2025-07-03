private async Task GuardarDetalleReporteAsync(
    IDatabaseConnection dbConnection,
    string agentCd,
    string agentTransTypeCode,
    string contractTypeCd,
    List<DetAct> detActList)
{
    if (detActList == null || detActList.Count == 0)
        return;

    var today = DateTime.Now.ToString("ddMMyyyy");
    int correlativo = 1;

    foreach (var act in detActList)
    {
        if (act?.Details?.DetailList == null)
            continue;

        foreach (var detail in act.Details.DetailList)
        {
            using var command = dbConnection.GetDbCommand();
            command.CommandText = @"
                INSERT INTO TU_BIBLIOTECA.TU_TABLA_REPORTE (
                    REP00UID, REP01DCOR, REP02ATTC, REP03ACTC, REP04ADT,
                    REP05ACD, REP06SCD, REP06OCCD, REP08OCCD, REP09DCCD,
                    REP10DCCD, REP11PTCD, REP12OACD, REP13DCOR, REP14MTC,
                    REP15CNM, REP16AON, REP17OAM, REP18DAM, REP19FAM,
                    REP20DISC, REP21COP, REP22COP, REP23COP, REP24COP,
                    REP25COP, REP26COP, REP27COP, REP28COP, REP29COP
                ) VALUES (
                    ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?,
                    ?, ?, ?, ?, ?, ?, ?, ?, ?, ?
                )";

            command.Parameters.Clear();
            command.Parameters.Add(CreateParameter(command, Guid.NewGuid().ToString()));     // REP00UID
            command.Parameters.Add(CreateParameter(command, correlativo));                  // REP01DCOR
            command.Parameters.Add(CreateParameter(command, agentTransTypeCode));           // REP02ATTC
            command.Parameters.Add(CreateParameter(command, contractTypeCd));               // REP03ACTC
            command.Parameters.Add(CreateParameter(command, act.ActivityDate));             // REP04ADT
            command.Parameters.Add(CreateParameter(command, agentCd));                      // REP05ACD
            command.Parameters.Add(CreateParameter(command, act.ServiceCode));              // REP06SCD
            command.Parameters.Add(CreateParameter(command, act.OriginCountryCode));        // REP06OCCD (probable duplicado, revisar)
            command.Parameters.Add(CreateParameter(command, act.OriginCurrencyCode));       // REP08OCCD
            command.Parameters.Add(CreateParameter(command, act.DestinationCountryCode));   // REP09DCCD
            command.Parameters.Add(CreateParameter(command, act.DestinationCurrencyCode));  // REP10DCCD
            command.Parameters.Add(CreateParameter(command, act.PaymentTypeCode));          // REP11PTCD
            command.Parameters.Add(CreateParameter(command, act.OriginAgentCode));          // REP12OACD
            command.Parameters.Add(CreateParameter(command, today));                        // REP13DCOR (fecha actual)
            command.Parameters.Add(CreateParameter(command, detail.MovementTypeCode));      // REP14MTC
            command.Parameters.Add(CreateParameter(command, detail.ConfirmationNumber));     // REP15CNM
            command.Parameters.Add(CreateParameter(command, detail.AgentOrderNumber));       // REP16AON
            command.Parameters.Add(CreateParameter(command, detail.OriginAmount));           // REP17OAM
            command.Parameters.Add(CreateParameter(command, detail.DestinationAmount));      // REP18DAM
            command.Parameters.Add(CreateParameter(command, detail.FeeAmount));              // REP19FAM
            command.Parameters.Add(CreateParameter(command, detail.DiscountAmount));         // REP20DISC

            // Rellenar REP21COP a REP29COP con espacios
            for (int i = 21; i <= 29; i++)
                command.Parameters.Add(CreateParameter(command, " "));                       // REP21COP - REP29COP

            await command.ExecuteNonQueryAsync();
            correlativo++;
        }
    }
}
