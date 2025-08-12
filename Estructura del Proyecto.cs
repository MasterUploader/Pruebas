_connection.Open();

var result = await ProgramCallBuilder
    .For(_connection, "BCAH96", "EVE03CTACL")
    // Opcional (ya es el default): .UseSqlNaming().WrapCallWithBraces()

    // ========== Encabezado (v1..v9) ==========
    .InString(responseModel.HRequestId?.Trim())
    .InString(responseModel.HChannel?.Trim())
    .InString(responseModel.HTerminal?.Trim())
    .InString(responseModel.HOrganization?.Trim())
    .InString(responseModel.HUserId?.Trim())
    .InString(responseModel.HProvider?.Trim())
    .InString(responseModel.HSessionId?.Trim())
    .InString(responseModel.HClientIp?.Trim())
    .InString(responseModel.HTimestamp?.Trim())        // si es DateTime -> usa .InDateTime(...)

    // ========== Parámetros de entrada (v10..v19) ==========
    .InString(requestBodyCreTarjetaAdicional.tipoMensaje)
    .InString(requestBodyCreTarjetaAdicional.codigoEmisor)
    .InString(requestBodyCreTarjetaAdicional.numeroCuenta)
    .InString(requestBodyCreTarjetaAdicional.localidadTramita)
    .InString(requestBodyCreTarjetaAdicional.localidadRetira)
    .InString(requestBodyCreTarjetaAdicional.identificacionAdicional)
    .InString(requestBodyCreTarjetaAdicional.tipoTarjeta)
    .InString(requestBodyCreTarjetaAdicional.disenoTarjeta)
    .InString(requestBodyCreTarjetaAdicional.usuarioSiscard)
    .InString(requestBodyCreTarjetaAdicional.version)

    // ========== Trazabilidad (v20..v24) ==========
    .InString(header.ReponseId)
    .InDateTime(header.Timestamp)                      // si viene como texto, usa .InString(...)
    .InString(appTimer.Elapsed.ToString())             // o .InDecimal(...) si lo envías numérico
    .InInt32(StatusCode)
    .InString(Message)

    // ========== “Parámetros de salida” (v25..v30)
    // En tu código original los estabas enviando como IN.
    // Si el programa realmente espera OUT/INOUT, cambia por:
    //   .OutString("status", sizeX) o .OutString("status", sizeX, initialValue: responseCreTarjetaAdicional.status)
    .InString(responseCreTarjetaAdicional.status)
    .InString(responseCreTarjetaAdicional.statusCode)
    .InString(responseCreTarjetaAdicional.statusMessage)
    .InString(responseCreTarjetaAdicional.numeroTarjetaAdicional)

    .InString("00")                                    // v29
    .InString("Proceso realizado exitosamente")        // v30

    .WithTimeout(60)
    .CallAsync(_httpContextAccessor.HttpContext);

// Igual que antes: revisa filas afectadas
var ok = result.RowsAffected > 0;
return ok;
