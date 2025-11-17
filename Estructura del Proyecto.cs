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




