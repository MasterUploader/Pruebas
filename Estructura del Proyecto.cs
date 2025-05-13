dcl-proc GetStringFromJsonSafe;
  dcl-pi *n char(200);
    parentNode pointer value;
    fieldName varchar(100) const;
    maxLength int(5) const;
  end-pi;

  dcl-s result char(200);
  dcl-s tempPtr pointer;
  dcl-s tempStr char(500) based(tempPtr);

  result = *blanks;
  tempPtr = yajl_object_find(parentNode: %trim(fieldName));

  if tempPtr <> *null;
    result = %subst(%str(%addr(tempStr)): 1: maxLength);
    if %trim(result) = '';
      result = ' ';
    endif;
  else;
    result = ' ';
  endif;

  return result;
end-proc;



dcl-proc ProcesarJsonResponse;
  dcl-s root like(yajl_val);
  dcl-s headerNode like(yajl_val);
  dcl-s reqHeaderNode like(yajl_val);
  dcl-s dataNode like(yajl_val);
  dcl-s dataDetailNode like(yajl_val);
  dcl-s errMsg varchar(500);

  root = yajl_stmf_load_tree(%trim(vFullFileR): errMsg);
  if errMsg <> '';
    // manejar error
    return;
  endif;

  // HEADER
  headerNode = yajl_object_find(root: 'header');
  if headerNode <> *null;
    hdr_responseId = GetStringFromJsonSafe(headerNode:'responseId': 100);
    hdr_timestamp  = GetStringFromJsonSafe(headerNode:'timestamp': 50);
    hdr_procTime   = GetStringFromJsonSafe(headerNode:'processingtime': 20);
    hdr_statusCode = GetStringFromJsonSafe(headerNode:'statuscode': 10);
    hdr_message    = GetStringFromJsonSafe(headerNode:'message': 200);

    // HEADER.REQUESTHEADER
    reqHeaderNode = yajl_object_find(headerNode: 'requestheader');
    if reqHeaderNode <> *null;
      hdr_req_id   = GetStringFromJsonSafe(reqHeaderNode:'h-request-id': 100);
      hdr_channel  = GetStringFromJsonSafe(reqHeaderNode:'h-channel': 20);
      hdr_term     = GetStringFromJsonSafe(reqHeaderNode:'h-terminal': 20);
      hdr_org      = GetStringFromJsonSafe(reqHeaderNode:'h-organization': 50);
      hdr_user     = GetStringFromJsonSafe(reqHeaderNode:'h-user-id': 20);
      hdr_prov     = GetStringFromJsonSafe(reqHeaderNode:'h-provider': 20);
      hdr_sess     = GetStringFromJsonSafe(reqHeaderNode:'h-session-id': 100);
      hdr_ip       = GetStringFromJsonSafe(reqHeaderNode:'h-client-ip': 50);
      hdr_time     = GetStringFromJsonSafe(reqHeaderNode:'h-timestamp': 20);
    endif;
  endif;

  // DATA
  dataNode = yajl_object_find(root: 'data');
  if dataNode <> *null;
    data_opcode  = GetStringFromJsonSafe(dataNode:'opCode': 10);
    data_msg     = GetStringFromJsonSafe(dataNode:'processMsg': 200);
    data_cd      = GetStringFromJsonSafe(dataNode:'transStatusCd': 10);
    data_dt      = GetStringFromJsonSafe(dataNode:'transStatusDt': 10);
    data_procDt  = GetStringFromJsonSafe(dataNode:'processDt': 10);
    data_procTm  = GetStringFromJsonSafe(dataNode:'processTm': 10);

    // DATA.DATA (anidado)
    dataDetailNode = yajl_object_find(dataNode: 'data');
    if dataDetailNode <> *null;
      data_saleDt = GetStringFromJsonSafe(dataDetailNode:'saleDt': 10);
      data_origAmt = GetStringFromJsonSafe(dataDetailNode:'origAmount': 20);
      // ... y así sucesivamente
    endif;
  endif;

  yajl_tree_free(root);
end-proc;
