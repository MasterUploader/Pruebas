Este es el codigo actual con los cambios que habias dicho:

      // ctl-opt DFTACTGRP(*NO) ACTGRP(*NEW) BNDDIR('HTTPAPI':'QC2LE':'YAJL')
      // option(*srcstmt: *nodebugio) DECEDIT('0.')
      // BNDDIR('HTTPAPI':'QC2LE')
      // BNDDIR('YAJL') DECEDIT('0.')
       // ===============================================================
       //Includes y prototipos externos
       // ===============================================================
      /copy qrpglesrc,httpapi_h
      /copy qrpglesrc,ifsio_h
      /include yajl_h

       dcl-pr TNP02POST;
            RequestId           char(100);
            HChannel             char(20);
            HTerminal            char(20);
            HOrganization        char(20);
            HUserId              char(20);
            HProvider            char(20);
            HSessionId           char(100);
            HClientIp            char(20);
            HTimestamp           char(50);
            RespCodDesc          char(500);
            ResponseCode               char(20);
            AuthIdResp           char(20);
            RetRefNumb           char(20);
            SysTraAudNum               char(20);
            TransactionType          char(20);
            TimeLocalTrans           char(20);
            DateLocalTrans           char(20);
            Amount                   char(20);
            MerchantID           char(20);
            MCC                  char(20);
            CurrencyCode               char(20);
            PrimaryAccNumber       char(20);
            TerminalID           char(20);
            DateExpiration           char(20);
            CVV2                       char(20);
       end-pr;

       // ===============================================================
       //PI: Párametros de entrada
       // ===============================================================
          dcl-pi TNP02POST;
            HRequestId           char(100);
            HChannel             char(20);
            HTerminal            char(20);
            HOrganization        char(20);
            HUserId              char(20);
            HProvider            char(20);
            HSessionId           char(100);
            HClientIp            char(20);
            HTimestamp           char(50);
            RespCodDesc          char(500);
            ResponseCode               char(20);
            AuthIdResp           char(20);
            RetRefNumb           char(20);
            SysTraAudNum               char(20);
            TransactionType          char(20);
            TimeLocalTrans           char(20);
            DateLocalTrans           char(20);
            Amount                   char(20);
            MerchantID           char(20);
            MCC                  char(20);
            CurrencyCode               char(20);
            PrimaryAccNumber       char(20);
            TerminalID           char(20);
            DateExpiration           char(20);
            CVV2                       char(20);
       end-pi;

          // ===============================================================
          // Llamado a programa de configuración de SUNITP
          // ===============================================================
          dcl-pr PrgParameter extpgm('BCAH96/PWS00SER');
                iCodArea zoned(8:0);
                iCodOpti char(40) const;
                iCodKeys char(40) const;
                iCodType char(2) const;
                oRespons char(300);
          end-pr;

          dcl-s errorP char(1);

          // ===============================================================
          // Variable de respuesta JSON (simulada)
          // En la implementación real, esta cadena vendrá del POST HTTP
          // ===============================================================
          dcl-s iCodArea zoned(8:0);
          dcl-s iCodOpti char(40);
          dcl-s iCodKeys char(40);
          dcl-s iCodType char(2);
          dcl-s oRespons char(300);
          dcl-s response char(100);

          // ===============================================================
          // Variables para configuración obtenida dinámicamente
          // ===============================================================
          dcl-s pUrlPost varchar(200);
          dcl-s pFileSav varchar(200);
          dcl-s vFullFileC varchar(300);
          dcl-s vFullFileR varchar(300);
          dcl-s vFullFileH varchar(300);
          dcl-s vDate8 char(8);
          dcl-s vTime6 char(6);
          // ===============================================================
          // Variables YAJL para generación del request JSON
          // ===============================================================
          dcl-s jsonBuffer varchar(32700); // ccsid(1208);
          dcl-s jsonLen int(10);

          // ===============================================================
          // Buffers auxiliares
          // ===============================================================
          dcl-s innerDataNode pointer;
          dcl-s rc int(10);

          // Define una estructura con los campos necesarios
          dcl-ds HeaderInputDS qualified;
            HRequestId     char(100);
            HChannel       char(20);
            HSessionId     char(100);
            HClientIp      char(20);
            HUserId        char(20);
            HProvider      char(20);
            HOrganization  char(20);
            HTerminal      char(20);
            HTimestamp     char(50);
            SysTraAudNum               char(20);
            Amount                   char(20);
            MerchantID           char(20);
            PrimaryAccNumber       char(20);
            TerminalID           char(20);
            DateExpiration           char(20);
            CVV2                       char(20);
          end-ds;

          // ===============================================================
          // ---------- SBR100 ----------
          // ===============================================================
          dcl-pr sbr100 ;
                input likeDS(HeaderInputDS);
          end-pr;

        // ===============================================================
        // ---------- Inicio Parametros de Entrada ----------
        // ===============================================================
            HeaderInputDS.HRequestId     = HRequestId;
            HeaderInputDS.HChannel       = HChannel;
            HeaderInputDS.HSessionId     = HSessionId;
            HeaderInputDS.HClientIp      = HClientIp;
            HeaderInputDS.HUserId        = HUserId;
            HeaderInputDS.HProvider      = HProvider;
            HeaderInputDS.HOrganization  = HOrganization;
            HeaderInputDS.HTerminal      = HTerminal;
            HeaderInputDS.HTimestamp     = HTimestamp;
            HeaderInputDS.SysTraAudNum   = SysTraAudNum;
            HeaderInputDS.Amount         = Amount;
            HeaderInputDS.MerchantID     = MerchantID;
            HeaderInputDS.PrimaryAccNumber    = PrimaryAccNumber;
            HeaderInputDS.TerminalID     = TerminalID;
            HeaderInputDS.DateExpiration = DateExpiration;
            HeaderInputDS.CVV2           = CVV2;

            sbr100(HeaderInputDS);
          *inlr = *on;
          return;
        // ===============================================================
        // ------------------- PROCEDIMIENTOS --------------------
        // ===============================================================

        // ===============================================================
        //Procedimiento GetStringFromJson
        // ===============================================================
        dcl-proc GetStringFromJson;
             dcl-pi *n char(200);
              parentNode pointer value;
              fieldName varchar(50) const;
              maxLength int(5) const;
            end-pi;

            dcl-s result char(200);
            dcl-s val pointer;

            result = *blanks;

            val = yajl_object_find(parentNode: %trim(fieldName));

            if val <> *null;
              result = yajl_get_string(val);
              if %trim(result) = '';
                result = ' ';
              endif;
            else;
              result = ' ';
            endif;

            if %len(%trim(result)) > 0;
              return %subst(result:1:maxLength);
            else;
             return *blanks;
            endif;
        end-proc;

        // ===============================================================
        // Procedimiento ProcesarRespuesta
        //  - Carga el árbol JSON desde el IFS.
        //  - Lee HEADER (opcional).
        //  - Ubica nodo "data" y delega en ProcesarAuthorizationResponse.
        // ===============================================================
        dcl-proc ProcesarRespuesta;
            dcl-s root          like(yajl_val);
            dcl-s headerNode    like(yajl_val);
            dcl-s reqHeaderNode like(yajl_val);
            dcl-s dataNode      like(yajl_val);
            dcl-s errMsg        varchar(500);

            // Cargar JSON desde archivo IFS a estructura YAJL
            root = yajl_stmf_load_tree(%trim(vFullFileR): errMsg);

            if errMsg <> '';
              errorP     = '1';
              ResponseCode = '400';
              RespCodDesc  = 'Error en la lectura de la respuesta';
              return;
            endif;

            // ============================================================
            //  Lectura de HEADER (si lo necesitas)
            // ============================================================
            headerNode = yajl_object_find(root: 'header');

            if headerNode <> *null;
              // Ejemplo: si deseas tomar el mensaje desde el header:
              // HDR_RSPID  = GetStringFromJson(headerNode: 'responseId': 36);
              // HDR_TMSTMP = GetStringFromJson(headerNode: 'timestamp': 50);
              // HDR_PRTIME = GetStringFromJson(headerNode:
              //                                'processingTime': 20);
              // HDR_STSCD  = GetStringFromJson(headerNode:
              //                                'statusCode': 10);
              // HDR_MSG    = GetStringFromJson(headerNode:
              //                                'message': 200);

              // HEADER.requestHeader (opcional)
              // reqHeaderNode = yajl_object_find(headerNode:
              //                                   'requestHeader');
              // if reqHeaderNode <> *null;
              //    hdr_req_id = GetStringFromJson(reqHeaderNode:
              //                      'h-request-id': 100);
              //    hdr_channel = GetStringFromJson(reqHeaderNode:
              //                      'h-channel': 20);
              //    ...
              // endif;
            endif;

            // ============================================================
            //  Lectura de DATA
            //  data → GetAuthorizationManualResponse → GetAuthorizationManualResult
            // ============================================================
            dataNode = yajl_object_find(root: 'data');

            if dataNode <> *null;
              // Aquí sí pasamos el nodo correcto
              callp ProcesarAuthorizationResponse(dataNode);
            endif;

            // Liberar memoria del árbol YAJL
            yajl_tree_free(root);

        end-proc;

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
            AuthIdResp =
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

        // ===============================================================
        //Procedimiento encargada de Generar el JsonRequest
        //para la petición al API
        // ===============================================================
        dcl-proc GenerarRequestJson;
            dcl-pi *n;
            input likeDs(HeaderInputDS);
            end-pi;

          dcl-s jsonPtr pointer;
          //dcl-s jsonLen int(10);
          dcl-s jsonGen int(10);
          dcl-s errMsg varchar(500);
          dcl-s fechaHoy char(8);
          dcl-s vTimestamp char(19);

          vTimestamp = %char(%date():*ISO) + 'T' + %char(%time());
          fechaHoy = %char(%date():*ISO0);

          // Abrir el generador JSON (N o Y, *OFF o *ON)
          //OFF: fromato compacto
          //ON: formato legible para humanos
          //N:
          //Y:
           jsonGen = yajl_genOpen(*OFF);

          // Comenzar el objeto JSON principal
          callp yajl_beginObj();
          callp yajl_beginObj();

          // --- HEADER ---
          callp yajl_addChar('header');
          callp yajl_beginObj();
          callp yajl_addChar('h-request-id': %trim(input.HRequestId));
          callp yajl_addChar('h-channel': %trim(input.HChannel));
          callp yajl_addChar('h-session-id': %trim(input.HSessionId));
          callp yajl_addChar('h-client-ip': %trim(input.HClientIp));
          callp yajl_addChar('h-user-id': %trim(input.HUserId));
          callp yajl_addChar('h-provider': %trim(input.HProvider));
          callp yajl_addChar('h-organization': %trim(input.HOrganization));
          callp yajl_addChar('h-terminal': %trim(input.HTerminal));
          callp yajl_addChar('h-timestamp': %trim(input.HTimestamp));
          callp yajl_endObj();
          // --- HEADER ---

          // --- Body ---//
          callp yajl_addChar('body');
          callp yajl_beginObj();

          //--- GetAuthorizationManual ---//
          callp yajl_addChar('GetAuthorizationManual');
          callp yajl_beginObj();

          callp yajl_addChar(
                'pMerchantID': %trim(input.MerchantID));
          callp yajl_addChar(
                'pTerminalID': %trim(input.TerminalID));
          callp yajl_addChar(
                'pPrimaryAccountNumber'
                : %trim(input.PrimaryAccNumber));
          callp yajl_addChar(
            'pDateExpiration': %trim(input.DateExpiration));
          callp yajl_addChar('pCVV2': %trim(input.CVV2));
          callp yajl_addChar('pAmount': %trim(input.Amount));
          callp yajl_addChar(
            'pSystemsTraceAuditNumber'
            : %trim(input.SysTraAudNum));

          callp yajl_endObj();
          //--- GetAuthorizationManual ---//

          callp yajl_endObj();
          // --- Body ---//

          callp yajl_endObj();
          // --- Comenzar el objeto JSON principal ---//

          //Limpieza de Buffer
          jsonBuffer = *blanks;
          jsonLen = 0;

          // Obtener el buffer JSON generado
           CALLP yajl_copyBuf( 0
                      : %addr(jsonBuffer)
                      : %size(jsonBuffer)
                      : jsonLen );

            yajl_saveBuf(vFullFileC: errMsg);

          // Cerrar el generador de JSON
          callp yajl_genClose();

        end-proc;

        // ========================================================
        // Procedimiento Enviar Posteo al API
        // ========================================================
        dcl-proc EnviarPost;
          dcl-s fd int(10);
          dcl-s filePath pointer;
          dcl-s bytesRead int(10);
          dcl-s headers varchar(200);
          dcl-s responseLen int(10);

          errorP = *blanks;

          // ----------------------------------------
          // Realiza el POST, guarda en archivo IFS
          // ----------------------------------------
          //http_debug(*on: vFullFileH); //Guarda archivo log
          callp http_debug(*on); //No guarda archivo log

          callp HTTP_setCCSIDs(1208: 0);

          rc = HTTP_POST(%Trim(pUrlPost)
                     : %addr(jsonBuffer )
                     : jsonLen
                     : %Trim(vFullFileR)
                     : 60
                     : HTTP_USERAGENT
                     : 'application/json');

          // Validaciones del resultado
          if rc < 0;
              // Error grave de conexión
              errorP = '1';
              ResponseCode = %char(rc);
              RespCodDesc = 'Error: Fallo de conexión o red. Código RC=' +
              %char(rc) + '" }';
          elseif rc = 0;
              // Respuesta vacía
              errorP = '1';
              ResponseCode = '0';
              RespCodDesc = 'Error: error de conexión HTTP. Código RC = 0.';
          elseif rc > 1;
              // Error de HTTP
              errorP = '2';
              ResponseCode = %char(rc);
              RespCodDesc = 'Error: erro http RC =' + %char(rc);
           endif;

        end-proc;

        // ===============================================================
        // Procedimiento Obtiene la configuración del API
        // ===============================================================
        dcl-proc GetApiConfig  export;
            dcl-pi GetApiConfig;
                  iCodArea zoned(8:0);
                  iCodOpti char(40);
                  iCodKeys char(40);
                  iCodType char(2);
                  oRespons char(300);
            end-pi;

            // === Obtener URL ===
            iCodArea = 182;
            iCodType = 'CH';
            iCodOpti = 'OP-PAGOS-TNP';
            iCodKeys = 'KEY-API-V1-TNP-AUTHO';
            callp PrgParameter(iCodArea:
                         iCodOpti:
                         iCodKeys:
                         iCodType:
                         oRespons);
            pUrlPost = %trim(oRespons);  // Asignar a variable global

            // === Obtener ruta para archivo ===
            iCodKeys = 'KEY-TNP-AS-LOG';
            callp PrgParameter(iCodArea:
                         iCodOpti:
                         iCodKeys:
                         iCodType:
                         oRespons);
            pFileSav = %trim(oRespons); // Asignar a variable global

        end-proc;

        // ===============================================================
        //Procedimiento SetfileName
        // ===============================================================
        dcl-proc SetFileName;
           //Extrayendo Hora con Milesimas
            dcl-s ts timestamp;
            dcl-s s  char(26);   // 'YYYY-MM-DD-HH.MM.SS.mmmmmm' (ISO)
            dcl-s hh char(2);
            dcl-s mm char(2);
            dcl-s ss char(2);
            dcl-s cc char(2);
            dcl-s vTime8 char(10);

            dcl-s vTime time;

            //Extrayendo fecha
            dcl-s vDate date;
            dcl-s vFileNameC char(30);
            dcl-s vFileNameR char(30);
            dcl-s vFileNameH char(30);

            vDate = %date();
            vDate8 = %subst(%char(vDate):3:2) +
            %subst(%char(vDate):6:2) +
            %subst(%char(vDate):9:2);

            vTime = %time();

            vTime6 = %subst(%char(vTime):1:2) +
            %subst(%char(vTime):4:2) +
            %subst(%char(vTime):7:2);

            //Hora
            ts = %timestamp();
            s  = %char(ts : *ISO);    // ISO: YYYY-MM-DD-HH.MM.SS.mmmmmm

            hh = %subst(s : 12 : 2);  // HH
            mm = %subst(s : 15 : 2);  // MM
            ss = %subst(s : 18 : 2);  // SS
            cc = %subst(s : 21 : 2);  // 2 primeras de microsegundos

             vTime8 = hh + mm + ss + cc;

            vFileNameC = 'C02_' + %trim(vDate8) + %trim(vTime8) + '_' +
            HeaderInputDS.TerminalID + '.json';
            vFileNameR = 'R02_' + %trim(vDate8) + %trim(vTime8) + '_' +
            HeaderInputDS.TerminalID + '.json';
            vFileNameH = 'H02_' + %trim(vDate8) + %trim(vTime8) + '_' +
            HeaderInputDS.TerminalID + '.txt';

            // Concatenar con ruta obtenida
            vFullFileC = %trim(pFileSav) + 'Autho/Request/' + vFileNameC;
            vFullFileR = %trim(pFileSav)  + 'Autho/Response/' + vFileNameR;
            vFullFileH = %trim(pFileSav)  +  vFileNameH;
        end-proc;

          // ===============================================================
          // BLOQUE PRINCIPAL DE EJECUCIÓN
          // ===============================================================
        dcl-proc sbr100 export;
            dcl-pi sbr100;
                   input likeDs(HeaderInputDS);
            end-pi;
          // 1. Obtener la configuración
           GetApiConfig(iCodArea :
                        iCodOpti :
                        iCodKeys :
                        iCodType :
                        oRespons);

          // 2. Generar nombre de archivo para guardar el response
           SetFileName();

          // 3. Generar JSON de request
           GenerarRequestJson(input);

          // 4. Enviar POST
           EnviarPost();

          // 5. Procesar respuesta
          if errorP <> '1';
            ProcesarRespuesta();
           endif;

        end-proc;
