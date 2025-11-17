      // ===============================================================
      // Procedimiento ProcesarAuthorizationResponse
      //  - dataNode apunta al nodo "data" del JSON.
      //  - Navega:
      //      data.GetAuthorizationManualResponse.GetAuthorizationManualResult
      //  - Soporta:
      //      a) Respuesta TNP "completa" (ResponseCodeDescription, etc.)
      //      b) Respuesta normalizada de error (responseCode, message, etc.)
      // ===============================================================
     dcl-proc ProcesarAuthorizationResponse;
        dcl-pi *n;
           dataNode pointer value;
        end-pi;

        dcl-s responseNode pointer;
        dcl-s resultNode   pointer;

        // data.GetAuthorizationManualResponse
        responseNode = yajl_object_find(
                          dataNode:
                          'GetAuthorizationManualResponse');

        if responseNode = *null;
           return;
        endif;

        // ...GetAuthorizationManualResult
        resultNode = yajl_object_find(
                        responseNode:
                        'GetAuthorizationManualResult');

        if resultNode = *null;
           return;
        endif;

        // -----------------------------------------------------------
        // 1) Campos "normalizados" (los que SIEMPRE debería haber).
        //    Estos son los del contrato de tu API cuando hay error
        //    (responseCode, message, transactionId, timestamp).
        //    Si vienen vacíos pero existe el formato TNP completo,
        //    los sobreescribiremos más abajo.
        // -----------------------------------------------------------

        ResponseCode =
           GetStringFromJson(resultNode: 'responseCode': 20);

        // authorizationCode puede venir vacío en errores.
        AutIdResp =
           GetStringFromJson(resultNode: 'authorizationCode': 20);

        RetRefNumb =
           GetStringFromJson(resultNode: 'transactionId': 50);

        RespCodDesc =
           GetStringFromJson(resultNode: 'message': 500);

        // Si necesitas el timestamp normalizado:
        TimeLocalTrans =
           GetStringFromJson(resultNode: 'timestamp': 30);

        // -----------------------------------------------------------
        // 2) Campos del formato COMPLETO TNP (respuesta 200 del banco).
        //    Cuando el servicio externo responde con el JSON "rico",
        //    las claves vienen con mayúsculas (ResponseCode, Amount,
        //    MerchantID, etc.). GetStringFromJson debería devolver
        //    blanco si la clave no existe, así que es seguro llamarlo.
        // -----------------------------------------------------------

        // Descripción de código (si existe, suele ser más expresiva).
        dcl-s tmpDesc varchar(500);

        tmpDesc =
           GetStringFromJson(resultNode:
                             'ResponseCodeDescription': 500);
        if %len(%trim(tmpDesc)) > 0;
           RespCodDesc = tmpDesc;
        endif;

        // Código de respuesta en formato TNP (ej. "00", "94", "68").
        dcl-s tmpCode varchar(20);

        tmpCode =
           GetStringFromJson(resultNode: 'ResponseCode': 20);
        if %len(%trim(tmpCode)) > 0;
           ResponseCode = %trim(tmpCode);
        endif;

        // RRN y STAN (si vienen en la respuesta TNP).
        RetRefNumb =
           GetStringFromJson(resultNode:
                             'RetrievalReferenceNumber': 20);

        SysTraAudNum =
           GetStringFromJson(resultNode:
                             'SystemsTraceAuditNumber': 20);

        TransactionType =
           GetStringFromJson(resultNode: 'TransactionType': 20);

        TimeLocalTrans =
           GetStringFromJson(resultNode: 'TimeLocalTrans': 20);

        DateLocalTrans =
           GetStringFromJson(resultNode: 'DateLocalTrans': 20);

        Amount =
           GetStringFromJson(resultNode: 'Amount': 20);

        MerchantID =
           GetStringFromJson(resultNode: 'MerchantID': 20);

        MCC =
           GetStringFromJson(resultNode: 'MCC': 20);

        CurrencyCode =
           GetStringFromJson(resultNode: 'CurrencyCode': 20);

        PrimaryAccNumber =
           GetStringFromJson(resultNode: 'PrimaryAccountNumber': 20);

        TerminalID =
           GetStringFromJson(resultNode: 'TerminalID': 20);

        // Nota:
        //  - En errores 68/96 generados por tu API, muchos de estos
        //    campos vendrán en blanco (no existen en el JSON).
        //  - En éxitos/errores de negocio del banco (00, 94, etc.),
        //    sí vendrán llenos y quedarán mapeados en las variables.

     end-proc;
