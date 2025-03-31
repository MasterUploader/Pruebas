begsr ProcesarRespuesta;

  dcl-s rootNode pointer;
  dcl-s headerNode pointer;
  dcl-s dataNode pointer;
  dcl-s innerDataNode pointer;

  // Cargar el JSON desde variable de respuesta
  rootNode = yajl_buf_load_tree(%addr(response): %len(%trim(response)));

  // ============================
  // 🔹 HEADER
  // ============================
  headerNode = yajl_object_find(rootNode: %addr('Header'));
  if headerNode <> *null;
     FullResponseDS.header.responseId =
       %subst(%str(yajl_get_string(yajl_object_find(headerNode: %addr('ResponseId')))): 1: 40);
     FullResponseDS.header.timestamp =
       %subst(%str(yajl_get_string(yajl_object_find(headerNode: %addr('Timestamp')))): 1: 30);
     FullResponseDS.header.processingTime =
       %subst(%str(yajl_get_string(yajl_object_find(headerNode: %addr('ProcessingTime')))): 1: 20);
     FullResponseDS.header.statusCode =
       %subst(%str(yajl_get_string(yajl_object_find(headerNode: %addr('StatusCode')))): 1: 10);
     FullResponseDS.header.message =
       %subst(%str(yajl_get_string(yajl_object_find(headerNode: %addr('Message')))): 1: 100);
  endif;

  // Mapeo a parámetros de salida
  HDR_RSPID  = FullResponseDS.header.responseId;
  HDR_TMSTMP = FullResponseDS.header.timestamp;
  HDR_PRTIME = FullResponseDS.header.processingTime;
  HDR_STSCD  = FullResponseDS.header.statusCode;
  HDR_MSG    = FullResponseDS.header.message;

  // ============================
  // 🔹 DATA (nivel 1)
  // ============================
  dataNode = yajl_object_find(rootNode: %addr('Data'));
  if dataNode <> *null;
     FullResponseDS.data.opCode =
       %subst(%str(yajl_get_string(yajl_object_find(dataNode: %addr('OPCODE')))): 1: 10);
     FullResponseDS.data.processMsg =
       %subst(%str(yajl_get_string(yajl_object_find(dataNode: %addr('PROCESS_MSG')))): 1: 100);
     FullResponseDS.data.transStatusCd =
       %subst(%str(yajl_get_string(yajl_object_find(dataNode: %addr('TRANS_STATUS_CD')))): 1: 10);
     FullResponseDS.data.transStatusDt =
       %subst(%str(yajl_get_string(yajl_object_find(dataNode: %addr('TRANS_STATUS_DT')))): 1: 8);
     FullResponseDS.data.processDt =
       %subst(%str(yajl_get_string(yajl_object_find(dataNode: %addr('PROCESS_DT')))): 1: 8);
     FullResponseDS.data.processTm =
       %subst(%str(yajl_get_string(yajl_object_find(dataNode: %addr('PROCESS_TM')))): 1: 6);

     // Mapeo de salida
     DAT_OPCODE = FullResponseDS.data.opCode;
     DAT_PRCMSG = FullResponseDS.data.processMsg;

     // ============================
     // 🔸 DATA.DATA (detalle anidado)
     // ============================
     innerDataNode = yajl_object_find(dataNode: %addr('DATA'));
     if innerDataNode <> *null;

        // Llamadas a subrutinas para descomponer detalle
        exsr ProcesarDataGenerales;
        exsr ProcesarSender;
        exsr ProcesarRecipient;
        exsr ProcesarSenderIdentification;
        exsr ProcesarRecipientIdentification;

     endif;
  endif;

  // Liberar memoria
  callp yajl_tree_free(rootNode);

endsr;
