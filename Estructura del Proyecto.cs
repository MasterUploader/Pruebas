Tengo este codigo RPGLE 

          // ===============================================================
          //Procedimiento Procesar Respuesta
          // ===============================================================
        dcl-proc ProcesarRespuesta;
          dcl-s root like(yajl_val);
          dcl-s headerNode like(yajl_val);
          dcl-s reqHeaderNode like(yajl_val);
          dcl-s dataNode like(yajl_val);
          dcl-s dataDetailNode like(yajl_val);
          dcl-s errMsg varchar(500);

          root = yajl_stmf_load_tree(%trim(vFullFileR): errMsg);
          if errMsg <> '';
              errorP = '1';
              ResponseCode = '400';
              RespCodDesc = 'Error en la lectura de la respuesta';
              return;
          endif;

          // ===============================================================
          //  Lectura de header
          // ===============================================================
          headerNode = YAJL_object_find(root: 'header');
          if headerNode <> *null;
            // HDR_RSPID = GetStringFromJson(headerNode:'responseId': 36);
            // HDR_TMSTMP  = GetStringFromJson(headerNode:'timestamp': 50);
            // HDR_PRTIME   = GetStringFromJson(headerNode
            // :'processingtime': 20);
            // HDR_STSCD = GetStringFromJson(headerNode
            // :'statuscode': 10);
            // HDR_MSG    = GetStringFromJson(headerNode
            // :'message': 200);

          endif;

            // HEADER.REQUESTHEADER
            // reqHeaderNode = yajl_object_find(headerNode: 'requestheader');
            // if reqHeaderNode <> *null;
              // hdr_req_id   = GetStringFromJson(reqHeaderNode
              // :'h-request-id': 100);
              // hdr_channel  = GetStringFromJson(reqHeaderNode
              // :'h-channel': 20);
              // hdr_term     = GetStringFromJson(reqHeaderNode
              // :'h-terminal': 20);
              // hdr_org      = GetStringFromJson(reqHeaderNode
              // :'h-organization': 50);
              // hdr_user     = GetStringFromJson(reqHeaderNode
              // :'h-user-id': 20);
              // hdr_prov     = GetStringFromJson(reqHeaderNode
              // :'h-provider': 20);
              // hdr_sess     = GetStringFromJson(reqHeaderNode
              // :'h-session-id': 100);
              // hdr_ip       = GetStringFromJson(reqHeaderNode
              // :'h-client-ip': 50);
              // hdr_time     = GetStringFromJson(reqHeaderNode
              // :'h-timestamp': 20);
          //   endif;
          // endif;

          // ===============================================================
          //  Lectura de data
          // ===============================================================
          dataNode = yajl_object_find(root: 'data');
        if dataNode <> *null;
              ProcesarAuthorizationResponse(innerDataNode);
        endif;

          yajl_tree_free(root);


        end-proc;

        // ===============================================================
        //Procedimiento ProcesarAuthorizationResponse
        // ===============================================================
        dcl-proc ProcesarAuthorizationResponse;
            dcl-pi *n;
              dataNode pointer value;
            end-pi;

            dcl-s result pointer;

            dataNode = yajl_object_find(
              dataNode: 'GetAuthorizationManualResponse');
            if dataNode <> *null;

              result =
              yajl_object_find(result: 'GetAuthorizationManualResult');
              if result <> *null;
                RespCodDesc =
                GetStringFromJson(result: 'ResponseCodeDescription': 500);
                ResponseCode =
                GetStringFromJson(result: 'ResponseCode': 20);
                RetRefNumb =
                GetStringFromJson(result: 'RetrievalReferenceNumber': 20);
                SysTraAudNum =
                GetStringFromJson(result: 'SystemsTraceAuditNumber': 20);
                TransactionType =
                GetStringFromJson(result: 'TransactionType': 20);
                TimeLocalTrans =
                GetStringFromJson(result: 'TimeLocalTrans': 20);
                DateLocalTrans =
                GetStringFromJson(result: 'DateLocalTrans': 20);
                Amount =
                GetStringFromJson(result: 'Amount': 20);
                MerchantID =
                GetStringFromJson(result: 'MerchantID': 20);
                MCC =
                GetStringFromJson(result: 'MCC': 20);
                CurrencyCode =
                GetStringFromJson(result: 'CurrencyCode': 20);
                PrimaryAccNumber =
                GetStringFromJson(result: 'PrimaryAccountNumber': 20);
                TerminalID = GetStringFromJson(result: 'TerminalID': 20);
              endif;

            endif;

          end-proc;

Y Debe leeer esta parte del json de respuesta cuando no es exitoso:

 {
                                "header": {
                                  "responseId": "b5b78b0bfe5a41c08b14ddeca46d4acf",
                                  "timestamp": "2025-11-17T23:07:47.4889839Z",
                                  "processingTime": "11453ms",
                                  "statusCode": "68",
                                  "message": "Timeout al invocar el servicio TNP.",
                                  "requestHeader": {
                                    "h-request-id": "REQ1234567890",
                                    "h-channel": "WEB",
                                    "h-terminal": "P0055468",
                                    "h-organization": "ORG01",
                                    "h-user-id": "USRTEST",
                                    "h-provider": "DaviviendaTNP",
                                    "h-session-id": "SESSION123",
                                    "h-client-ip": "192.168.1.50",
                                    "h-timestamp": "2025-11-17T21:00:00Z"
                                  }
                                },
                                "data": {
                                  "GetAuthorizationManualResponse": {
                                    "GetAuthorizationManualResult": {
                                      "responseCode": "68",
                                      "authorizationCode": "",
                                      "transactionId": "TXN-839BB650D87940BBB8D5D9920393B800",
                                      "message": "Timeout al invocar el servicio TNP.",
                                      "timestamp": "2025-11-17T23:07:47.4883965Z"
                                    }
                                  }
                                }
                              }
Cuando esta bien vienen el resto de campos
