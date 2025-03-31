**FREE
ctl-opt dftactgrp(*no)
        actgrp(*new)
        bnddir('HTTPAPI':'QILE')
        bnddir('YAJL')
        decedit('0.');

/copy qgpl/qrpglesrc,httpapi_h
/copy qgpl/qrpglesrc,ifsio_h
/include yajl_h

// =======================================
// Programa: BTS01POST
// Descripción: Recibe datos, genera JSON, hace POST, procesa respuesta
// =======================================

// PI: Parámetros de entrada (simplificados para ejemplo)
dcl-pi *n;
  // Entradas (ejemplo básico, completar con los 20 requeridos)
  AGTCD         char(3);
  TRNTYPCD      char(4);
  CNFNM         char(20);
  REGNSD        char(10);
  BRNCHSD       char(10);
  STCD          char(10);
  CTRYCD        char(10);
  USRNAME       char(20);
  TRMINAL       char(20);
  AGTDATE       char(8);
  AGTTIME       char(6);
  REQID         char(36);
  CHNL          char(10);
  SESSID        char(36);
  CLNTIP        char(15);
  USRID         char(20);
  PROVIDR       char(20);
  ORGZTN        char(20);
  TRMNLHD       char(10);
  TMSTMPHD      char(14);

  // Salidas (ejemplo, solo unos campos de Header y Data)
  HDR_RSPID     char(40);
  HDR_TMSTMP    char(30);
  HDR_PRTIME    char(20);
  HDR_STSCD     char(10);
  HDR_MSG       char(100);
  DAT_OPCODE    char(10);
  DAT_PRCMSG    char(100);
  DDT_SALEDT    char(8);
  DDT_DSTAMT    char(20);
end-pi;

// =======================================
// Variable de respuesta JSON (simulada)
// En la implementación real, esta cadena vendrá del POST HTTP
// =======================================
dcl-s response varchar(32700);

// Prototipo para obtener configuraciones del API
dcl-pr PrgParameter extpgm('PWS00SER');
  iCodArea char(85) const;
  iCodOpti char(40) const;
  iCodKeys char(40) const;
  iCodType char(2) const;
  oRespons char(300);
end-pr;


// Variables para configuración obtenida dinámicamente
dcl-s pUrlPost varchar(200);
dcl-s pFileSav varchar(200);
dcl-s vFullFile varchar(300);

// Variables YAJL para generación del request JSON
dcl-s jsonGen pointer;
dcl-s jsonBuffer varchar(32700);

// Para libhttp
dcl-pr libhttp_post int(10) extproc('libhttp_post');
  req pointer value;
  reqlen int(10) value;
  res pointer;
  reslen int(10) value;
  hdr pointer value;
  url pointer value;
end-pr;

// Variables para uso de libhttp_post
dcl-s reqPtr pointer;
dcl-s resPtr pointer;
dcl-s hdrPtr pointer;
dcl-s urlPtr pointer;
dcl-s response varchar(32700);
dcl-s responseLen int(10);
dcl-s url varchar(200);
dcl-s contentType varchar(50);

// Buffers auxiliares
dcl-s headers varchar(200);


// =======================================
// Estructura principal de respuesta JSON
// =======================================
dcl-ds FullResponseDS qualified;
  header likeds(HeaderDS);
  data   likeds(DataDS);
end-ds;

// ----------- HEADER -------------------
dcl-ds HeaderDS qualified;
  responseId      char(40);
  timestamp       char(30);
  processingTime  char(20);
  statusCode      char(10);
  message         char(100);
end-ds;

// ----------- DATA (nivel 1) -----------
dcl-ds DataDS qualified;
  opCode         char(10);
  processMsg     char(100);
  errorParamFullName char(100);
  transStatusCd  char(10);
  transStatusDt  char(8);
  processDt      char(8);
  processTm      char(6);
  detail         likeds(DataDetailDS);
end-ds;

// ---------- DATA.DATA (detalle) -------
dcl-ds DataDetailDS qualified;
  saleDt         char(8);
  saleTm         char(6);
  serviceCd      char(10);
  paymentTypeCd  char(10);
  origCountryCd  char(5);
  origCurrencyCd char(5);
  destCountryCd  char(5);
  destCurrencyCd char(5);
  originAm       char(20);
  destinationAm  char(20);
  exchRateFx     char(20);
  marketRefCurrencyCd  char(5);
  marketRefCurrencyAm  char(20);
  sAgentCd       char(10);
  rAccountTypeCd char(10);
  rAccountNm     char(50);
  rAgentCd       char(10);
  rAgentRegionSd char(10);
  rAgentBranchSd char(10);

  sender         likeds(PersonDS);
  recipient      likeds(RecipientDS);
  senderIdent    likeds(IdentificationDS);
  recipientIdent likeds(IdentificationDS);
end-ds;

// ---------- PERSONA BASE (Sender, ForeignName) ----------
dcl-ds PersonDS qualified;
  firstName     char(20);
  middleName    char(20);
  lastName      char(20);
  motherMName   char(20);
  address       likeds(AddressDS);
end-ds;

// ---------- RECIPIENT (con ForeignName) ----------
dcl-ds RecipientDS qualified;
  firstName     char(20);
  middleName    char(20);
  lastName      char(20);
  motherMName   char(20);
  identifTypeCd char(10);
  identifNm     char(30);
  foreignName   likeds(PersonDS);
  address       likeds(AddressDS);
end-ds;

// ---------- ADDRESS ----------
dcl-ds AddressDS qualified;
  address    char(50);
  city       char(30);
  stateCd    char(5);
  countryCd  char(5);
  zipCode    char(10);
  phone      char(20);
end-ds;

// ---------- IDENTIFICACIONES ----------
dcl-ds IdentificationDS qualified;
  typeCd           char(10);
  issuerCd         char(10);
  issuerStateCd    char(10);
  issuerCountryCd  char(10);
  identifNm        char(30);
  expirationDt     char(8);
end-ds;


// =======================================
// Devuelve una cadena desde un nodo JSON
// Si el nodo o valor es nulo, retorna *blanks
// =======================================
dcl-proc SafeGetString;
  dcl-pi *n char(100);
    jsonPtr pointer value;
  end-pi;

  if jsonPtr <> *null and yajl_get_string(jsonPtr) <> *null;
    return %str(yajl_get_string(jsonPtr));
  else;
    return *blanks;
  endif;
end-proc;


// =======================================
// Devuelve un valor decimal desde un nodo JSON
// Si el nodo o valor es nulo, retorna cero
// =======================================
dcl-proc SafeGetDecimal;
  dcl-pi *n packed(15:4);
    jsonPtr pointer value;
  end-pi;

  dcl-s result packed(15:4);

  if jsonPtr <> *null and yajl_get_number(jsonPtr) <> *null;
    result = %dec(%str(yajl_get_number(jsonPtr)): 15: 4);
  else;
    result = 0;
  endif;

  return result;
end-proc;


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
  // 🔹 DATA
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
     // 🔸 DATA.DATA
     // ============================
     innerDataNode = yajl_object_find(dataNode: %addr('DATA'));
     if innerDataNode <> *null;
        exsr ProcesarDataGenerales;
        exsr ProcesarSender;
        exsr ProcesarRecipient;
        exsr ProcesarSenderIdentification;
        exsr ProcesarRecipientIdentification;
     endif;
  endif;

  callp yajl_tree_free(rootNode);

endsr;


begsr ProcesarDataGenerales;

  FullResponseDS.data.detail.saleDt =
    %subst(%str(yajl_get_string(yajl_object_find(innerDataNode: %addr('SALE_DT')))): 1: 8);
  DDT_SALEDT = FullResponseDS.data.detail.saleDt;

  FullResponseDS.data.detail.saleTm =
    %subst(%str(yajl_get_string(yajl_object_find(innerDataNode: %addr('SALE_TM')))): 1: 6);

  FullResponseDS.data.detail.destinationAm =
    %subst(%str(yajl_get_string(yajl_object_find(innerDataNode: %addr('DESTINATION_AM')))): 1: 20);
  DDT_DSTAMT = FullResponseDS.data.detail.destinationAm;

  // Puedes continuar con el resto de campos si los necesitas como salida
  FullResponseDS.data.detail.serviceCd =
    %subst(%str(yajl_get_string(yajl_object_find(innerDataNode: %addr('SERVICE_CD')))): 1: 10);

  FullResponseDS.data.detail.paymentTypeCd =
    %subst(%str(yajl_get_string(yajl_object_find(innerDataNode: %addr('PAYMENT_TYPE_CD')))): 1: 10);

  FullResponseDS.data.detail.origCountryCd =
    %subst(%str(yajl_get_string(yajl_object_find(innerDataNode: %addr('ORIG_COUNTRY_CD')))): 1: 5);

  FullResponseDS.data.detail.destCurrencyCd =
    %subst(%str(yajl_get_string(yajl_object_find(innerDataNode: %addr('DEST_CURRENCY_CD')))): 1: 5);

  // ... continuar con otros campos si lo deseas ...

endsr;


begsr ProcesarSender;

  dcl-s senderNode pointer;
  dcl-s senderAddr pointer;

  senderNode = yajl_object_find(innerDataNode: %addr('SENDER'));

  if senderNode <> *null;

    FullResponseDS.data.detail.sender.firstName =
      %subst(%str(yajl_get_string(yajl_object_find(senderNode: %addr('FIRST_NAME')))): 1: 20);
    SND_FNAME = FullResponseDS.data.detail.sender.firstName;

    FullResponseDS.data.detail.sender.middleName =
      %subst(%str(yajl_get_string(yajl_object_find(senderNode: %addr('MIDDLE_NAME')))): 1: 20);
    SND_MNAME = FullResponseDS.data.detail.sender.middleName;

    FullResponseDS.data.detail.sender.lastName =
      %subst(%str(yajl_get_string(yajl_object_find(senderNode: %addr('LAST_NAME')))): 1: 20);
    SND_LNAME = FullResponseDS.data.detail.sender.lastName;

    FullResponseDS.data.detail.sender.motherMName =
      %subst(%str(yajl_get_string(yajl_object_find(senderNode: %addr('MOTHER_M_NAME')))): 1: 20);
    SND_MOMNM = FullResponseDS.data.detail.sender.motherMName;

    // Dirección del remitente
    senderAddr = yajl_object_find(senderNode: %addr('ADDRESS'));
    if senderAddr <> *null;

      FullResponseDS.data.detail.sender.address.address =
        %subst(%str(yajl_get_string(yajl_object_find(senderAddr: %addr('ADDRESS')))): 1: 50);
      SND_ADDR = FullResponseDS.data.detail.sender.address.address;

      FullResponseDS.data.detail.sender.address.city =
        %subst(%str(yajl_get_string(yajl_object_find(senderAddr: %addr('CITY')))): 1: 30);
      SND_CITY = FullResponseDS.data.detail.sender.address.city;

      FullResponseDS.data.detail.sender.address.stateCd =
        %subst(%str(yajl_get_string(yajl_object_find(senderAddr: %addr('STATE_CD')))): 1: 5);
      SND_STCD = FullResponseDS.data.detail.sender.address.stateCd;

      FullResponseDS.data.detail.sender.address.countryCd =
        %subst(%str(yajl_get_string(yajl_object_find(senderAddr: %addr('COUNTRY_CD')))): 1: 5);
      SND_CTRY = FullResponseDS.data.detail.sender.address.countryCd;

      FullResponseDS.data.detail.sender.address.zipCode =
        %subst(%str(yajl_get_string(yajl_object_find(senderAddr: %addr('ZIP_CODE')))): 1: 10);
      SND_ZIP = FullResponseDS.data.detail.sender.address.zipCode;

      FullResponseDS.data.detail.sender.address.phone =
        %subst(%str(yajl_get_string(yajl_object_find(senderAddr: %addr('PHONE')))): 1: 20);
      SND_PHONE = FullResponseDS.data.detail.sender.address.phone;

    endif;
  endif;

endsr;


begsr ProcesarRecipient;

  dcl-s recipientNode pointer;
  dcl-s foreignNode pointer;
  dcl-s recipientAddr pointer;

  recipientNode = yajl_object_find(innerDataNode: %addr('RECIPIENT'));

  if recipientNode <> *null;

    // 📌 Datos básicos
    FullResponseDS.data.detail.recipient.firstName =
      %subst(%str(yajl_get_string(yajl_object_find(recipientNode: %addr('FIRST_NAME')))): 1: 20);
    REC_FNAME = FullResponseDS.data.detail.recipient.firstName;

    FullResponseDS.data.detail.recipient.middleName =
      %subst(%str(yajl_get_string(yajl_object_find(recipientNode: %addr('MIDDLE_NAME')))): 1: 20);
    REC_MNAME = FullResponseDS.data.detail.recipient.middleName;

    FullResponseDS.data.detail.recipient.lastName =
      %subst(%str(yajl_get_string(yajl_object_find(recipientNode: %addr('LAST_NAME')))): 1: 20);
    REC_LNAME = FullResponseDS.data.detail.recipient.lastName;

    FullResponseDS.data.detail.recipient.motherMName =
      %subst(%str(yajl_get_string(yajl_object_find(recipientNode: %addr('MOTHER_M_NAME')))): 1: 20);
    REC_MOMNM = FullResponseDS.data.detail.recipient.motherMName;

    FullResponseDS.data.detail.recipient.identifTypeCd =
      %subst(%str(yajl_get_string(yajl_object_find(recipientNode: %addr('IDENTIF_TYPE_CD')))): 1: 10);
    REC_IDTYP = FullResponseDS.data.detail.recipient.identifTypeCd;

    FullResponseDS.data.detail.recipient.identifNm =
      %subst(%str(yajl_get_string(yajl_object_find(recipientNode: %addr('IDENTIF_NM')))): 1: 30);
    REC_IDNM = FullResponseDS.data.detail.recipient.identifNm;

    // 📌 FOREIGN_NAME
    foreignNode = yajl_object_find(recipientNode: %addr('FOREIGN_NAME'));
    if foreignNode <> *null;
      FullResponseDS.data.detail.recipient.foreignName.firstName =
        %subst(%str(yajl_get_string(yajl_object_find(foreignNode: %addr('FIRST_NAME')))): 1: 20);
      RFC_FNAME = FullResponseDS.data.detail.recipient.foreignName.firstName;

      FullResponseDS.data.detail.recipient.foreignName.middleName =
        %subst(%str(yajl_get_string(yajl_object_find(foreignNode: %addr('MIDDLE_NAME')))): 1: 20);
      RFC_MNAME = FullResponseDS.data.detail.recipient.foreignName.middleName;

      FullResponseDS.data.detail.recipient.foreignName.lastName =
        %subst(%str(yajl_get_string(yajl_object_find(foreignNode: %addr('LAST_NAME')))): 1: 20);
      RFC_LNAME = FullResponseDS.data.detail.recipient.foreignName.lastName;

      FullResponseDS.data.detail.recipient.foreignName.motherMName =
        %subst(%str(yajl_get_string(yajl_object_find(foreignNode: %addr('MOTHER_M_NAME')))): 1: 20);
      RFC_MOMNM = FullResponseDS.data.detail.recipient.foreignName.motherMName;
    endif;

    // 📌 Dirección del destinatario
    recipientAddr = yajl_object_find(recipientNode: %addr('ADDRESS'));
    if recipientAddr <> *null;
      FullResponseDS.data.detail.recipient.address.address =
        %subst(%str(yajl_get_string(yajl_object_find(recipientAddr: %addr('ADDRESS')))): 1: 50);
      REC_ADDR = FullResponseDS.data.detail.recipient.address.address;

      FullResponseDS.data.detail.recipient.address.city =
        %subst(%str(yajl_get_string(yajl_object_find(recipientAddr: %addr('CITY')))): 1: 30);
      REC_CITY = FullResponseDS.data.detail.recipient.address.city;

      FullResponseDS.data.detail.recipient.address.stateCd =
        %subst(%str(yajl_get_string(yajl_object_find(recipientAddr: %addr('STATE_CD')))): 1: 5);
      REC_STCD = FullResponseDS.data.detail.recipient.address.stateCd;

      FullResponseDS.data.detail.recipient.address.countryCd =
        %subst(%str(yajl_get_string(yajl_object_find(recipientAddr: %addr('COUNTRY_CD')))): 1: 5);
      REC_CTRY = FullResponseDS.data.detail.recipient.address.countryCd;

      FullResponseDS.data.detail.recipient.address.zipCode =
        %subst(%str(yajl_get_string(yajl_object_find(recipientAddr: %addr('ZIP_CODE')))): 1: 10);
      REC_ZIP = FullResponseDS.data.detail.recipient.address.zipCode;

      FullResponseDS.data.detail.recipient.address.phone =
        %subst(%str(yajl_get_string(yajl_object_find(recipientAddr: %addr('PHONE')))): 1: 20);
      REC_PHONE = FullResponseDS.data.detail.recipient.address.phone;
    endif;

  endif;

endsr;

begsr ProcesarSenderIdentification;

  dcl-s senderIdentNode pointer;

  senderIdentNode = yajl_object_find(innerDataNode: %addr('SENDER_IDENTIFICATION'));

  if senderIdentNode <> *null;
    FullResponseDS.data.detail.senderIdent.typeCd =
      %subst(%str(yajl_get_string(yajl_object_find(senderIdentNode: %addr('TYPE_CD')))): 1: 10);
    SID_TYPCD = FullResponseDS.data.detail.senderIdent.typeCd;

    FullResponseDS.data.detail.senderIdent.issuerCd =
      %subst(%str(yajl_get_string(yajl_object_find(senderIdentNode: %addr('ISSUER_CD')))): 1: 10);
    SID_ISSCD = FullResponseDS.data.detail.senderIdent.issuerCd;

    FullResponseDS.data.detail.senderIdent.issuerStateCd =
      %subst(%str(yajl_get_string(yajl_object_find(senderIdentNode: %addr('ISSUER_STATE_CD')))): 1: 10);
    SID_ISSST = FullResponseDS.data.detail.senderIdent.issuerStateCd;

    FullResponseDS.data.detail.senderIdent.issuerCountryCd =
      %subst(%str(yajl_get_string(yajl_object_find(senderIdentNode: %addr('ISSUER_COUNTRY_CD')))): 1: 10);
    SID_ISSCT = FullResponseDS.data.detail.senderIdent.issuerCountryCd;

    FullResponseDS.data.detail.senderIdent.identifNm =
      %subst(%str(yajl_get_string(yajl_object_find(senderIdentNode: %addr('IDENTIF_NM')))): 1: 30);
    SID_IDNM = FullResponseDS.data.detail.senderIdent.identifNm;

    FullResponseDS.data.detail.senderIdent.expirationDt =
      %subst(%str(yajl_get_string(yajl_object_find(senderIdentNode: %addr('EXPIRATION_DT')))): 1: 8);
    SID_EXPDT = FullResponseDS.data.detail.senderIdent.expirationDt;
  endif;

endsr;


begsr ProcesarRecipientIdentification;

  dcl-s recipientIdentNode pointer;

  recipientIdentNode = yajl_object_find(innerDataNode: %addr('RECIPIENT_IDENTIFICATION'));

  if recipientIdentNode <> *null;
    FullResponseDS.data.detail.recipientIdent.typeCd =
      %subst(%str(yajl_get_string(yajl_object_find(recipientIdentNode: %addr('TYPE_CD')))): 1: 10);
    RID_TYPCD = FullResponseDS.data.detail.recipientIdent.typeCd;

    FullResponseDS.data.detail.recipientIdent.issuerCd =
      %subst(%str(yajl_get_string(yajl_object_find(recipientIdentNode: %addr('ISSUER_CD')))): 1: 10);
    RID_ISSCD = FullResponseDS.data.detail.recipientIdent.issuerCd;

    FullResponseDS.data.detail.recipientIdent.issuerStateCd =
      %subst(%str(yajl_get_string(yajl_object_find(recipientIdentNode: %addr('ISSUER_STATE_CD')))): 1: 10);
    RID_ISSST = FullResponseDS.data.detail.recipientIdent.issuerStateCd;

    FullResponseDS.data.detail.recipientIdent.issuerCountryCd =
      %subst(%str(yajl_get_string(yajl_object_find(recipientIdentNode: %addr('ISSUER_COUNTRY_CD')))): 1: 10);
    RID_ISSCT = FullResponseDS.data.detail.recipientIdent.issuerCountryCd;

    FullResponseDS.data.detail.recipientIdent.identifNm =
      %subst(%str(yajl_get_string(yajl_object_find(recipientIdentNode: %addr('IDENTIF_NM')))): 1: 30);
    RID_IDNM = FullResponseDS.data.detail.recipientIdent.identifNm;

    FullResponseDS.data.detail.recipientIdent.expirationDt =
      %subst(%str(yajl_get_string(yajl_object_find(recipientIdentNode: %addr('EXPIRATION_DT')))): 1: 8);
    RID_EXPDT = FullResponseDS.data.detail.recipientIdent.expirationDt;
  endif;

endsr;


begsr GenerarRequestJson;

  jsonGen = yajl_genOpen('N'); // N = No UTF-8 escape

  // ======= Inicio del objeto JSON =======
  callp yajl_beginObj(jsonGen);

    // Header
    callp yajl_addChar(jsonGen: 'Header');
    callp yajl_beginObj(jsonGen);
      callp yajl_addChar(jsonGen: 'h-request-id');
      callp yajl_addChar(jsonGen: %trim(REQID));
      callp yajl_addChar(jsonGen: 'channel');
      callp yajl_addChar(jsonGen: %trim(CHNL));
      callp yajl_addChar(jsonGen: 'session-id');
      callp yajl_addChar(jsonGen: %trim(SESSID));
      callp yajl_addChar(jsonGen: 'client-ip');
      callp yajl_addChar(jsonGen: %trim(CLNTIP));
      callp yajl_addChar(jsonGen: 'user-id');
      callp yajl_addChar(jsonGen: %trim(USRID));
      callp yajl_addChar(jsonGen: 'provider');
      callp yajl_addChar(jsonGen: %trim(PROVIDR));
      callp yajl_addChar(jsonGen: 'organization');
      callp yajl_addChar(jsonGen: %trim(ORGZTN));
      callp yajl_addChar(jsonGen: 'terminal');
      callp yajl_addChar(jsonGen: %trim(TRMNLHD));
      callp yajl_addChar(jsonGen: 'timestamp');
      callp yajl_addChar(jsonGen: %trim(TMSTMPHD));
    callp yajl_endObj(jsonGen);

    // Request
    callp yajl_addChar(jsonGen: 'Request');
    callp yajl_beginObj(jsonGen);
      callp yajl_addChar(jsonGen: 'AGENT_CD');
      callp yajl_addChar(jsonGen: %trim(AGTCD));
      callp yajl_addChar(jsonGen: 'AGENT_TRANS_TYPE_CODE');
      callp yajl_addChar(jsonGen: %trim(TRNTYPCD));
      callp yajl_addChar(jsonGen: 'CONFIRMATION_NM');
      callp yajl_addChar(jsonGen: %trim(CNFNM));
      callp yajl_addChar(jsonGen: 'REGION_SD');
      callp yajl_addChar(jsonGen: %trim(REGNSD));
      callp yajl_addChar(jsonGen: 'BRANCH_SD');
      callp yajl_addChar(jsonGen: %trim(BRNCHSD));
      callp yajl_addChar(jsonGen: 'STATE_CD');
      callp yajl_addChar(jsonGen: %trim(STCD));
      callp yajl_addChar(jsonGen: 'COUNTRY_CD');
      callp yajl_addChar(jsonGen: %trim(CTRYCD));
      callp yajl_addChar(jsonGen: 'USER_NM');
      callp yajl_addChar(jsonGen: %trim(USRNAME));
      callp yajl_addChar(jsonGen: 'TERMINAL_ID');
      callp yajl_addChar(jsonGen: %trim(TRMINAL));
      callp yajl_addChar(jsonGen: 'AGENT_DT');
      callp yajl_addChar(jsonGen: %trim(AGTDATE));
      callp yajl_addChar(jsonGen: 'AGENT_TM');
      callp yajl_addChar(jsonGen: %trim(AGTTIME));
    callp yajl_endObj(jsonGen);

  // ======= Fin del objeto JSON =======
  callp yajl_endObj(jsonGen);

  // Convertir a cadena
  jsonBuffer = yajl_writeBufStr(jsonGen);
  callp yajl_genClose(jsonGen);

endsr;


begsr EnviarPost;

  // Preparar URL del endpoint
  url = 'https://tudominio/api/ExecTR';
  urlPtr = %addr(url);

  // Encabezados HTTP
  headers = 'Content-Type: application/json';
  hdrPtr = %addr(headers);

  // Convertir JSON a puntero
  reqPtr = %addr(jsonBuffer);

  // Asumimos máximo de 32K para respuesta
  resPtr = %addr(response);
  responseLen = %len(response);

  // Realizar POST
  dcl-s rc int(10);

rc = libhttp_post(reqPtr: %len(%trim(jsonBuffer)): resPtr: responseLen: hdrPtr: urlPtr);

if rc <> 0;
   // En caso de error en el POST, puedes loguear, retornar código o asignar respuesta vacía
   response = '{ "error": "Error al enviar POST, código RC=' + %char(rc) + '" }';
endif;

// Guardar archivo de respuesta en IFS (opcional pero útil para debugging)
dcl-pr IFS_WRITE extproc('_C_IFS_write');
  fileName pointer value;
  buffer   pointer value;
  length   int(10) value;
end-pr;

dcl-pr IFS_OPEN extproc('_C_IFS_open');
  pathName pointer value;
  flags    int(10) value;
  mode     int(10) value;
  options  int(10) value;
end-pr;

dcl-pr IFS_CLOSE extproc('_C_IFS_close');
  fd int(10) value;
end-pr;

dcl-s fd int(10);
dcl-s filePath pointer;

// Guardar response en archivo
filePath = %addr(vFullFile);
fd = IFS_OPEN(filePath: 577: 0: 0); // 577 = O_WRONLY+O_CREAT+O_TRUNC

if fd >= 0;
   callp IFS_WRITE(fd: %addr(response): %len(%trim(response)));
   callp IFS_CLOSE(fd);
endif;



endsr;

begsr GetApiConfig;

  dcl-s iCodArea char(85) inz('182');
  dcl-s iCodOpti char(40);
  dcl-s iCodKeys char(40);
  dcl-s iCodType char(2) inz('CH');
  dcl-s oRespons char(300);
  
  // === Obtener URL ===
  iCodOpti = 'OP-UTH-REST-GENERAL-PARAMS';
  iCodKeys = 'KEY-UTH-REST-POST-EXECUTR';
  callp PrgParameter(iCodArea: iCodOpti: iCodKeys: iCodType: oRespons);
  pUrlPost = %trim(oRespons);  // Asignar a variable global

  // === Obtener ruta para archivo ===
  iCodKeys = 'KEY-UTH-AS-LOG';
  callp PrgParameter(iCodArea: iCodOpti: iCodKeys: iCodType: oRespons);
  pFileSav = %trim(oRespons); // Asignar a variable global

endsr;


begsr SetFileName;

  dcl-s vDate date;
  dcl-s vTime time;
  dcl-s vDate8 char(8);
  dcl-s vTime6 char(6);
  dcl-s vFileName char(30);

  vDate = %date();
  vTime = %time();
  vDate8 = %char(vDate):3:2 + %char(vDate):6:2 + %char(vDate):9:2;
  vTime6 = %char(vTime):1:2 + %char(vTime):4:2 + %char(vTime):7:2;

  vFileName = 'S02_' + %trim(vDate8) + %trim(vTime6) + '.json';

  // Concatenar con ruta obtenida
  vFullFile = %trim(pFileSav) + vFileName;

endsr;



// =======================================
// BLOQUE PRINCIPAL DE EJECUCIÓN
// =======================================

// 1. Obtener la configuración
exsr GetApiConfig;

// 2. Generar nombre de archivo para guardar el response
exsr SetFileName;

// 3. Generar JSON de request
exsr GenerarRequestJson;

// 4. Enviar POST
exsr EnviarPost;

// 5. Procesar respuesta
exsr ProcesarRespuesta;

*inlr = *on;
return;

