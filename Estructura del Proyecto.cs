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
                INSERT INTO TU_TABLA_REPORTE (
                    CORRELATIVO, AGENT_CD, TRANS_TYPE_CD, ACTIVITY_DT,
                    CONTRACT_TYPE_CD, MOVEMENT_TYPE_CODE, CONFIRMATION_NM,
                    AGENT_ORDER_NM, ORIGIN_AM, DESTINATION_AM,
                    FEE_AM, DISCOUNT_AM, FECHA_PROCESO
                ) VALUES (
                    ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?
                )";

            command.Parameters.Clear();
            command.Parameters.Add(CreateParameter(command, correlativo));
            command.Parameters.Add(CreateParameter(command, agentCd));
            command.Parameters.Add(CreateParameter(command, agentTransTypeCode));
            command.Parameters.Add(CreateParameter(command, act.ActivityDate));
            command.Parameters.Add(CreateParameter(command, contractTypeCd));
            command.Parameters.Add(CreateParameter(command, detail.MovementTypeCode));
            command.Parameters.Add(CreateParameter(command, detail.ConfirmationNumber));
            command.Parameters.Add(CreateParameter(command, detail.AgentOrderNumber));
            command.Parameters.Add(CreateParameter(command, detail.OriginAmount));
            command.Parameters.Add(CreateParameter(command, detail.DestinationAmount));
            command.Parameters.Add(CreateParameter(command, detail.FeeAmount));
            command.Parameters.Add(CreateParameter(command, detail.DiscountAmount));
            command.Parameters.Add(CreateParameter(command, today));

            await command.ExecuteNonQueryAsync();
            correlativo++;
        }
    }
}
