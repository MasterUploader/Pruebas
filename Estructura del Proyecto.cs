dcl-proc ProcesarRespuesta;

  dcl-s docNode pointer;
  dcl-s headerNode pointer;
  dcl-s dataNode pointer;
  dcl-s innerDataNode pointer;
  dcl-s requestHeader pointer;
  dcl-s senderNode pointer;
  dcl-s recipientNode pointer;
  dcl-s foreignNode pointer;
  dcl-s senderAddr pointer;
  dcl-s recipientAddr pointer;
  dcl-s senderIdent pointer;
  dcl-s recipientIdent pointer;

  dcl-s errmsg varchar(500);

  docNode = yajl_stmf_load_tree(%trim(vFullFileR): errmsg);

  if errmsg <> *blanks;
     // Manejo del error de lectura
     HDR_MSG = 'Error al cargar JSON: ' + errmsg;
     return;
  endif;

  //-------------------------------------------------------
  // HEADER
  //-------------------------------------------------------
  headerNode = yajl_object_find(docNode: 'header');
  if headerNode <> *null;
     HDR_RSPID  = GetString(headerNode: 'responseId': 100);
     HDR_TMSTMP = GetString(headerNode: 'timestamp': 50);
     HDR_PRTIME = GetString(headerNode: 'processingtime': 20);
     HDR_STSCD  = GetString(headerNode: 'statuscode': 10);
     HDR_MSG    = GetString(headerNode: 'message': 500);

     requestHeader = yajl_object_find(headerNode: 'requestheader');
     if requestHeader <> *null;
        // Puedes mapear los campos de requestHeader aquí si lo necesitas
     endif;
  endif;

  //-------------------------------------------------------
  // DATA
  //-------------------------------------------------------
  dataNode = yajl_object_find(docNode: 'data');
  if dataNode <> *null;
     OPCODE         = GetString(dataNode: 'opCode': 4);
     ProcessMsg     = GetString(dataNode: 'processMsg': 70);
     ErrorParamFullName = GetString(dataNode: 'errorParamFullName': 255);
     TransStatusCd  = GetString(dataNode: 'transStatusCd': 3);
     TransStatusDt  = GetString(dataNode: 'transStatusDt': 8);
     ProcessDt      = GetString(dataNode: 'processDt': 8);
     ProcessTm      = GetString(dataNode: 'processTm': 6);

     innerDataNode = yajl_object_find(dataNode: 'data');
     if innerDataNode <> *null;
        ProcesarDataGeneral(innerDataNode);
        ProcesarSender(innerDataNode);
        ProcesarRecipient(innerDataNode);
        ProcesarSenderIdent(innerDataNode);
        ProcesarRecipientIdent(innerDataNode);
     endif;
  endif;

  yajl_tree_free(docNode);

end-proc;
