dcl-proc ProcesarDataGeneral;
  dcl-pi *n;
    dataNode pointer value;
  end-pi;

  FullResponseDS.data.detail.saleDt =
    GetStringFromJson(dataNode: 'saleDt': 8);

  FullResponseDS.data.detail.saleTm =
    GetStringFromJson(dataNode: 'saleTm': 6);

  FullResponseDS.data.detail.serviceCd =
    GetStringFromJson(dataNode: 'serviceCd': 10);

  FullResponseDS.data.detail.paymentTypeCd =
    GetStringFromJson(dataNode: 'paymentTypeCd': 10);

  FullResponseDS.data.detail.origCountryCd =
    GetStringFromJson(dataNode: 'origCountryCd': 5);

  FullResponseDS.data.detail.origCurrencyCd =
    GetStringFromJson(dataNode: 'origCurrencyCd': 5);

  FullResponseDS.data.detail.destCountryCd =
    GetStringFromJson(dataNode: 'destCountryCd': 5);

  FullResponseDS.data.detail.destCurrencyCd =
    GetStringFromJson(dataNode: 'DestCurrencyCd': 5);

  FullResponseDS.data.detail.originAm =
    GetStringFromJson(dataNode: 'origAmount': 20);

  FullResponseDS.data.detail.destinationAm =
    GetStringFromJson(dataNode: 'destAmount': 20);

  FullResponseDS.data.detail.exchRateFx =
    GetStringFromJson(dataNode: 'exchangeRateFx': 20);

  FullResponseDS.data.detail.marketRefCurrencyCd =
    GetStringFromJson(dataNode: 'marketRefCurrencyCd': 5);

  FullResponseDS.data.detail.marketRefCurrencyAm =
    GetStringFromJson(dataNode: 'marketRefCurrencyAm': 20);
end-proc;


dcl-proc ProcesarSender;
  dcl-pi *n;
    dataNode pointer value;
  end-pi;

  dcl-s sender pointer;
  dcl-s addr pointer;

  sender = yajl_object_find(dataNode: 'sender');
  if sender <> *null;
    FullResponseDS.data.detail.sender.firstName =
      GetStringFromJson(sender: 'firstName': 40);
    FullResponseDS.data.detail.sender.middleName =
      GetStringFromJson(sender: 'middleName': 40);
    FullResponseDS.data.detail.sender.lastName =
      GetStringFromJson(sender: 'lastName': 40);
    FullResponseDS.data.detail.sender.motherMName =
      GetStringFromJson(sender: 'motherMName': 40);

    addr = yajl_object_find(sender: 'address');
    if addr <> *null;
      FullResponseDS.data.detail.sender.address.address =
        GetStringFromJson(addr: 'address': 65);
      FullResponseDS.data.detail.sender.address.city =
        GetStringFromJson(addr: 'city': 30);
      FullResponseDS.data.detail.sender.address.stateCd =
        GetStringFromJson(addr: 'stateCd': 3);
      FullResponseDS.data.detail.sender.address.countryCd =
        GetStringFromJson(addr: 'countryCd': 3);
      FullResponseDS.data.detail.sender.address.zipCode =
        GetStringFromJson(addr: 'zipCode': 10);
      FullResponseDS.data.detail.sender.address.phone =
        GetStringFromJson(addr: 'phone': 15);
    endif;
  endif;
end-proc;

dcl-proc ProcesarRecipient;
  dcl-pi *n;
    dataNode pointer value;
  end-pi;

  dcl-s rec pointer;
  dcl-s addr pointer;
  dcl-s fName pointer;

  rec = yajl_object_find(dataNode: 'recipient');
  if rec <> *null;
    FullResponseDS.data.detail.recipient.firstName =
      GetStringFromJson(rec: 'firstName': 40);
    FullResponseDS.data.detail.recipient.middleName =
      GetStringFromJson(rec: 'middleName': 40);
    FullResponseDS.data.detail.recipient.lastName =
      GetStringFromJson(rec: 'lastName': 40);
    FullResponseDS.data.detail.recipient.motherMName =
      GetStringFromJson(rec: 'motherMName': 40);
    FullResponseDS.data.detail.recipient.identifTypeCd =
      GetStringFromJson(rec: 'identif_Type_Cd': 10);
    FullResponseDS.data.detail.recipient.identifNm =
      GetStringFromJson(rec: 'identif_Nm': 30);

    fName = yajl_object_find(rec: 'foreing_Name');
    if fName <> *null;
      FullResponseDS.data.detail.recipient.foreignName.firstName =
        GetStringFromJson(fName: 'firstName': 40);
      FullResponseDS.data.detail.recipient.foreignName.middleName =
        GetStringFromJson(fName: 'middleName': 40);
      FullResponseDS.data.detail.recipient.foreignName.lastName =
        GetStringFromJson(fName: 'lastName': 40);
      FullResponseDS.data.detail.recipient.foreignName.motherMName =
        GetStringFromJson(fName: 'motherMName': 40);
    endif;

    addr = yajl_object_find(rec: 'address');
    if addr <> *null;
      FullResponseDS.data.detail.recipient.address.address =
        GetStringFromJson(addr: 'address': 65);
      FullResponseDS.data.detail.recipient.address.city =
        GetStringFromJson(addr: 'city': 30);
      FullResponseDS.data.detail.recipient.address.stateCd =
        GetStringFromJson(addr: 'stateCd': 3);
      FullResponseDS.data.detail.recipient.address.countryCd =
        GetStringFromJson(addr: 'countryCd': 3);
      FullResponseDS.data.detail.recipient.address.zipCode =
        GetStringFromJson(addr: 'zipCode': 10);
      FullResponseDS.data.detail.recipient.address.phone =
        GetStringFromJson(addr: 'phone': 15);
    endif;
  endif;
end-proc;

dcl-proc ProcesarSenderIdent;
  dcl-pi *n;
    dataNode pointer value;
  end-pi;

  dcl-s ident pointer;

  ident = yajl_object_find(dataNode: 'senderIdentification');
  if ident <> *null;
    FullResponseDS.data.detail.senderIdent.typeCd =
      GetStringFromJson(ident: 'typeCd': 10);
    FullResponseDS.data.detail.senderIdent.issuerCd =
      GetStringFromJson(ident: 'issuerCd': 10);
    FullResponseDS.data.detail.senderIdent.issuerStateCd =
      GetStringFromJson(ident: 'issuerStateCd': 10);
    FullResponseDS.data.detail.senderIdent.issuerCountryCd =
      GetStringFromJson(ident: 'issuerCountryCd': 10);
    FullResponseDS.data.detail.senderIdent.identifNm =
      GetStringFromJson(ident: 'identFnum': 30);
    FullResponseDS.data.detail.senderIdent.expirationDt =
      GetStringFromJson(ident: 'expirationDt': 8);
  endif;
end-proc;

dcl-proc ProcesarRecipientIdent;
  dcl-pi *n;
    dataNode pointer value;
  end-pi;

  dcl-s ident pointer;

  ident = yajl_object_find(dataNode: 'recipientIdentification');
  if ident <> *null;
    FullResponseDS.data.detail.recipientIdent.typeCd =
      GetStringFromJson(ident: 'typeCd': 10);
    FullResponseDS.data.detail.recipientIdent.issuerCd =
      GetStringFromJson(ident: 'issuerCd': 10);
    FullResponseDS.data.detail.recipientIdent.issuerStateCd =
      GetStringFromJson(ident: 'issuerStateCd': 10);
    FullResponseDS.data.detail.recipientIdent.issuerCountryCd =
      GetStringFromJson(ident: 'issuerCountryCd': 10);
    FullResponseDS.data.detail.recipientIdent.identifNm =
      GetStringFromJson(ident: 'identFnum': 30);
    FullResponseDS.data.detail.recipientIdent.expirationDt =
      GetStringFromJson(ident: 'expirationDt': 8);
  endif;
end-proc;
