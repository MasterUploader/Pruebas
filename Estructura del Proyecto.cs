Convierte este codigo a usar la libreria

string callProgramCL = "{CALL BCAH96.EVE03CTACL(?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?)}";
using (OleDbCommand command = new(callProgramCL, conection.Connect.OleDbConnection))
{


    //Encabezado
    command.Parameters.AddWithValue("v1", responseModel.HRequestId.Trim());
    command.Parameters.AddWithValue("v2", responseModel.HChannel.Trim());
    command.Parameters.AddWithValue("v3", responseModel.HTerminal.Trim());
    command.Parameters.AddWithValue("v4", responseModel.HOrganization.Trim());
    command.Parameters.AddWithValue("v5", responseModel.HUserId.Trim());
    command.Parameters.AddWithValue("v6", responseModel.HProvider.Trim());
    command.Parameters.AddWithValue("v7", responseModel.HSessionId.Trim());
    command.Parameters.AddWithValue("v8", responseModel.HClientIp.Trim());
    command.Parameters.AddWithValue("v9", responseModel.HTimestamp.Trim());

    //Parámetros de entrada
    command.Parameters.AddWithValue("v10", requestBodyCreTarjetaAdicional.tipoMensaje);
    command.Parameters.AddWithValue("v11", requestBodyCreTarjetaAdicional.codigoEmisor);
    command.Parameters.AddWithValue("v12", requestBodyCreTarjetaAdicional.numeroCuenta);
    command.Parameters.AddWithValue("v13", requestBodyCreTarjetaAdicional.localidadTramita);
    command.Parameters.AddWithValue("v14", requestBodyCreTarjetaAdicional.localidadRetira);
    command.Parameters.AddWithValue("v15", requestBodyCreTarjetaAdicional.identificacionAdicional);
    command.Parameters.AddWithValue("v16", requestBodyCreTarjetaAdicional.tipoTarjeta);
    command.Parameters.AddWithValue("v17", requestBodyCreTarjetaAdicional.disenoTarjeta);
    command.Parameters.AddWithValue("v18", requestBodyCreTarjetaAdicional.usuarioSiscard);
    command.Parameters.AddWithValue("v19", requestBodyCreTarjetaAdicional.version);


    command.Parameters.AddWithValue("v20", header.ReponseId);
    command.Parameters.AddWithValue("v21", header.Timestamp);
    command.Parameters.AddWithValue("v22", appTimer.Elapsed);
    command.Parameters.AddWithValue("v23", StatusCode);
    command.Parameters.AddWithValue("v24", Message);

    //Parámetros de salida            
    command.Parameters.AddWithValue("v25", responseCreTarjetaAdicional.status);
    command.Parameters.AddWithValue("v26", responseCreTarjetaAdicional.statusCode);
    command.Parameters.AddWithValue("v27", responseCreTarjetaAdicional.statusMessage);
    command.Parameters.AddWithValue("v28", responseCreTarjetaAdicional.numeroTarjetaAdicional);


    command.Parameters.AddWithValue("v29", "00");
    command.Parameters.AddWithValue("v30", "Proceso realizado exitosamente");


    //Ejecuta el procedimiento y verifica si se insertaron los datos correctamente. 
    int respuesta = command.ExecuteNonQuery();
    if (respuesta > 0)

        return true;

    return false;
