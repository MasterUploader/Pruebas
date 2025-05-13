dcl-proc GetSafeString export;
  dcl-pi *n char(100);
    node pointer value;
    name varchar(50) const;
    len int(5) const;
  end-pi;

  dcl-s result char(100);
  dcl-s tmpPtr pointer;
  dcl-s tmpStr char(500) based(tmpPtr);

  result = *blanks;

  tmpPtr = yajl_object_find(node: %trim(name));

  if tmpPtr <> *null;
    result = %subst(%str(%addr(tmpStr)): 1: len);
    if %trim(result) = '';
      result = ' '; // Asigna un espacio si viene vacío
    endif;
  else;
    result = ' ';
  endif;

  return result;
end-proc;




dcl-proc ProcesarRespuestaExtendida;
  dcl-s root pointer;
  dcl-s header pointer;
  dcl-s reqHeader pointer;
  dcl-s data pointer;
  dcl-s dataBody pointer;
  dcl-s sender pointer;
  dcl-s recipient pointer;
  dcl-s identRec pointer;
  dcl-s identSend pointer;
  dcl-s addr pointer;
  dcl-s fName pointer;
  dcl-s msg varchar(500);

  root = yajl_stmf_load_tree(%trim(vFullFileR): msg);

  header = yajl_object_find(root: 'Header');
  if header <> *null;
    FullResponseDS.header.responseId =
      GetSafeString(header: 'ReponseId': 40);
    FullResponseDS.header.timestamp =
      GetSafeString(header: 'Timestamp': 30);
    FullResponseDS.header.processingTime =
      GetSafeString(header: 'ProcessingTime': 20);
    FullResponseDS.header.statusCode =
      GetSafeString(header: 'StatusCode': 10);
    FullResponseDS.header.message =
      GetSafeString(header: 'Message': 100);

    reqHeader = yajl_object_find(header: 'RequestHeader');
    if reqHeader <> *null;
      // Si deseas mapear esto, hazlo aquí
    endif;
  endif;

  data = yajl_object_find(root: 'Data');
  if data <> *null;
    FullResponseDS.data.opCode =
      GetSafeString(data: 'OpCode': 4);
    FullResponseDS.data.processMsg =
      GetSafeString(data: 'ProcessMsg': 5000);
    FullResponseDS.data.errorParamFullName =
      GetSafeString(data: 'ErrorParamFullName': 255);
    FullResponseDS.data.transStatusCd =
      GetSafeString(data: 'TransStatusCd': 3);
    FullResponseDS.data.transStatusDt =
      GetSafeString(data: 'TransStatusDt': 8);
    FullResponseDS.data.processDt =
      GetSafeString(data: 'ProcessDt': 8);
    FullResponseDS.data.processTm =
      GetSafeString(data: 'ProcessTm': 6);

    dataBody = yajl_object_find(data: 'Data');
    if dataBody <> *null;

      FullResponseDS.data.detail.saleDt =
        GetSafeString(dataBody: 'SaleDt': 8);
      FullResponseDS.data.detail.saleTm =
        GetSafeString(dataBody: 'SaleTm': 6);
      FullResponseDS.data.detail.serviceCd =
        GetSafeString(dataBody: 'ServiceCd': 10);
      FullResponseDS.data.detail.paymentTypeCd =
        GetSafeString(dataBody: 'PaymentTypeCd': 10);
      FullResponseDS.data.detail.origCountryCd =
        GetSafeString(dataBody: 'OrigCountryCd': 5);
      FullResponseDS.data.detail.destCountryCd =
        GetSafeString(dataBody: 'DestCountryCd': 5);
      FullResponseDS.data.detail.destCurrencyCd =
        GetSafeString(dataBody: 'DestCurrencyCd': 5);
      FullResponseDS.data.detail.destinationAm =
        GetSafeString(dataBody: 'DestAmount': 20);
      FullResponseDS.data.detail.originAm =
        GetSafeString(dataBody: 'OrigAmount': 20);

      // Sender
      sender = yajl_object_find(dataBody: 'Sender');
      if sender <> *null;
        FullResponseDS.data.detail.sender.firstName =
          GetSafeString(sender: 'FirstName': 40);
        FullResponseDS.data.detail.sender.middleName =
          GetSafeString(sender: 'MiddleName': 40);
        FullResponseDS.data.detail.sender.lastName =
          GetSafeString(sender: 'LastName': 40);
        FullResponseDS.data.detail.sender.motherMName =
          GetSafeString(sender: 'MotherMName': 40);

        addr = yajl_object_find(sender: 'Address');
        if addr <> *null;
          FullResponseDS.data.detail.sender.address.address =
            GetSafeString(addr: 'Address': 65);
          FullResponseDS.data.detail.sender.address.city =
            GetSafeString(addr: 'City': 30);
          FullResponseDS.data.detail.sender.address.stateCd =
            GetSafeString(addr: 'StateCd': 3);
          FullResponseDS.data.detail.sender.address.countryCd =
            GetSafeString(addr: 'CountryCd': 3);
          FullResponseDS.data.detail.sender.address.zipCode =
            GetSafeString(addr: 'ZipCode': 10);
          FullResponseDS.data.detail.sender.address.phone =
            GetSafeString(addr: 'Phone': 15);
        endif;
      endif;

      // Recipient
      recipient = yajl_object_find(dataBody: 'Recipient');
      if recipient <> *null;
        FullResponseDS.data.detail.recipient.firstName =
          GetSafeString(recipient: 'FirstName': 40);
        FullResponseDS.data.detail.recipient.middleName =
          GetSafeString(recipient: 'MiddleName': 40);
        FullResponseDS.data.detail.recipient.lastName =
          GetSafeString(recipient: 'LastName': 40);
        FullResponseDS.data.detail.recipient.motherMName =
          GetSafeString(recipient: 'MotherMName': 40);

        addr = yajl_object_find(recipient: 'Address');
        if addr <> *null;
          FullResponseDS.data.detail.recipient.address.address =
            GetSafeString(addr: 'Address': 65);
          FullResponseDS.data.detail.recipient.address.city =
            GetSafeString(addr: 'City': 30);
          FullResponseDS.data.detail.recipient.address.stateCd =
            GetSafeString(addr: 'StateCd': 3);
          FullResponseDS.data.detail.recipient.address.countryCd =
            GetSafeString(addr: 'CountryCd': 3);
          FullResponseDS.data.detail.recipient.address.zipCode =
            GetSafeString(addr: 'ZipCode': 10);
          FullResponseDS.data.detail.recipient.address.phone =
            GetSafeString(addr: 'Phone': 15);
        endif;
      endif;

      identSend = yajl_object_find(dataBody: 'SenderIdentification');
      if identSend <> *null;
        FullResponseDS.data.detail.senderIdent.typeCd =
          GetSafeString(identSend: 'TypeCd': 10);
      endif;

      identRec = yajl_object_find(dataBody: 'RecipientIdentification');
      if identRec <> *null;
        FullResponseDS.data.detail.recipientIdent.typeCd =
          GetSafeString(identRec: 'TypeCd': 10);
      endif;

    endif;
  endif;

  yajl_tree_free(root);
end-proc;
