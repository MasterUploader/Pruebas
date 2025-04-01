begsr ProcesarDataGenerales;

  FullResponseDS.data.detail.saleDt =
    %subst(SafeGetString(yajl_object_find(innerDataNode: %addr('SALE_DT'))): 1: 8);
  DDT_SALEDT = FullResponseDS.data.detail.saleDt;

  FullResponseDS.data.detail.saleTm =
    %subst(SafeGetString(yajl_object_find(innerDataNode: %addr('SALE_TM'))): 1: 6);

  FullResponseDS.data.detail.destinationAm =
    %subst(SafeGetString(yajl_object_find(innerDataNode: %addr('DESTINATION_AM'))): 1: 20);
  DDT_DSTAMT = FullResponseDS.data.detail.destinationAm;

  FullResponseDS.data.detail.serviceCd =
    %subst(SafeGetString(yajl_object_find(innerDataNode: %addr('SERVICE_CD'))): 1: 10);

  FullResponseDS.data.detail.paymentTypeCd =
    %subst(SafeGetString(yajl_object_find(innerDataNode: %addr('PAYMENT_TYPE_CD'))): 1: 10);

  FullResponseDS.data.detail.origCountryCd =
    %subst(SafeGetString(yajl_object_find(innerDataNode: %addr('ORIG_COUNTRY_CD'))): 1: 5);

  FullResponseDS.data.detail.destCurrencyCd =
    %subst(SafeGetString(yajl_object_find(innerDataNode: %addr('DEST_CURRENCY_CD'))): 1: 5);

endsr;



begsr ProcesarSender;

  dcl-s senderNode pointer;
  dcl-s senderAddr pointer;

  senderNode = yajl_object_find(innerDataNode: %addr('SENDER'));

  if senderNode <> *null;

    FullResponseDS.data.detail.sender.firstName =
      %subst(SafeGetString(yajl_object_find(senderNode: %addr('FIRST_NAME'))): 1: 20);
    SND_FNAME = FullResponseDS.data.detail.sender.firstName;

    FullResponseDS.data.detail.sender.middleName =
      %subst(SafeGetString(yajl_object_find(senderNode: %addr('MIDDLE_NAME'))): 1: 20);
    SND_MNAME = FullResponseDS.data.detail.sender.middleName;

    FullResponseDS.data.detail.sender.lastName =
      %subst(SafeGetString(yajl_object_find(senderNode: %addr('LAST_NAME'))): 1: 20);
    SND_LNAME = FullResponseDS.data.detail.sender.lastName;

    FullResponseDS.data.detail.sender.motherMName =
      %subst(SafeGetString(yajl_object_find(senderNode: %addr('MOTHER_M_NAME'))): 1: 20);
    SND_MOMNM = FullResponseDS.data.detail.sender.motherMName;

    senderAddr = yajl_object_find(senderNode: %addr('ADDRESS'));
    if senderAddr <> *null;

      FullResponseDS.data.detail.sender.address.address =
        %subst(SafeGetString(yajl_object_find(senderAddr: %addr('ADDRESS'))): 1: 50);
      SND_ADDR = FullResponseDS.data.detail.sender.address.address;

      FullResponseDS.data.detail.sender.address.city =
        %subst(SafeGetString(yajl_object_find(senderAddr: %addr('CITY'))): 1: 30);
      SND_CITY = FullResponseDS.data.detail.sender.address.city;

      FullResponseDS.data.detail.sender.address.stateCd =
        %subst(SafeGetString(yajl_object_find(senderAddr: %addr('STATE_CD'))): 1: 5);
      SND_STCD = FullResponseDS.data.detail.sender.address.stateCd;

      FullResponseDS.data.detail.sender.address.countryCd =
        %subst(SafeGetString(yajl_object_find(senderAddr: %addr('COUNTRY_CD'))): 1: 5);
      SND_CTRY = FullResponseDS.data.detail.sender.address.countryCd;

      FullResponseDS.data.detail.sender.address.zipCode =
        %subst(SafeGetString(yajl_object_find(senderAddr: %addr('ZIP_CODE'))): 1: 10);
      SND_ZIP = FullResponseDS.data.detail.sender.address.zipCode;

      FullResponseDS.data.detail.sender.address.phone =
        %subst(SafeGetString(yajl_object_find(senderAddr: %addr('PHONE'))): 1: 20);
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

    FullResponseDS.data.detail.recipient.firstName =
      %subst(SafeGetString(yajl_object_find(recipientNode: %addr('FIRST_NAME'))): 1: 20);
    REC_FNAME = FullResponseDS.data.detail.recipient.firstName;

    FullResponseDS.data.detail.recipient.middleName =
      %subst(SafeGetString(yajl_object_find(recipientNode: %addr('MIDDLE_NAME'))): 1: 20);
    REC_MNAME = FullResponseDS.data.detail.recipient.middleName;

    FullResponseDS.data.detail.recipient.lastName =
      %subst(SafeGetString(yajl_object_find(recipientNode: %addr('LAST_NAME'))): 1: 20);
    REC_LNAME = FullResponseDS.data.detail.recipient.lastName;

    FullResponseDS.data.detail.recipient.motherMName =
      %subst(SafeGetString(yajl_object_find(recipientNode: %addr('MOTHER_M_NAME'))): 1: 20);
    REC_MOMNM = FullResponseDS.data.detail.recipient.motherMName;

    FullResponseDS.data.detail.recipient.identifTypeCd =
      %subst(SafeGetString(yajl_object_find(recipientNode: %addr('IDENTIF_TYPE_CD'))): 1: 10);
    REC_IDTYP = FullResponseDS.data.detail.recipient.identifTypeCd;

    FullResponseDS.data.detail.recipient.identifNm =
      %subst(SafeGetString(yajl_object_find(recipientNode: %addr('IDENTIF_NM'))): 1: 30);
    REC_IDNM = FullResponseDS.data.detail.recipient.identifNm;

    foreignNode = yajl_object_find(recipientNode: %addr('FOREIGN_NAME'));
    if foreignNode <> *null;

      FullResponseDS.data.detail.recipient.foreignName.firstName =
        %subst(SafeGetString(yajl_object_find(foreignNode: %addr('FIRST_NAME'))): 1: 20);
      RFC_FNAME = FullResponseDS.data.detail.recipient.foreignName.firstName;

      FullResponseDS.data.detail.recipient.foreignName.middleName =
        %subst(SafeGetString(yajl_object_find(foreignNode: %addr('MIDDLE_NAME'))): 1: 20);
      RFC_MNAME = FullResponseDS.data.detail.recipient.foreignName.middleName;

      FullResponseDS.data.detail.recipient.foreignName.lastName =
        %subst(SafeGetString(yajl_object_find(foreignNode: %addr('LAST_NAME'))): 1: 20);
      RFC_LNAME = FullResponseDS.data.detail.recipient.foreignName.lastName;

      FullResponseDS.data.detail.recipient.foreignName.motherMName =
        %subst(SafeGetString(yajl_object_find(foreignNode: %addr('MOTHER_M_NAME'))): 1: 20);
      RFC_MOMNM = FullResponseDS.data.detail.recipient.foreignName.motherMName;
    endif;

    recipientAddr = yajl_object_find(recipientNode: %addr('ADDRESS'));
    if recipientAddr <> *null;

      FullResponseDS.data.detail.recipient.address.address =
        %subst(SafeGetString(yajl_object_find(recipientAddr: %addr('ADDRESS'))): 1: 50);
      REC_ADDR = FullResponseDS.data.detail.recipient.address.address;

      FullResponseDS.data.detail.recipient.address.city =
        %subst(SafeGetString(yajl_object_find(recipientAddr: %addr('CITY'))): 1: 30);
      REC_CITY = FullResponseDS.data.detail.recipient.address.city;

      FullResponseDS.data.detail.recipient.address.stateCd =
        %subst(SafeGetString(yajl_object_find(recipientAddr: %addr('STATE_CD'))): 1: 5);
      REC_STCD = FullResponseDS.data.detail.recipient.address.stateCd;

      FullResponseDS.data.detail.recipient.address.countryCd =
        %subst(SafeGetString(yajl_object_find(recipientAddr: %addr('COUNTRY_CD'))): 1: 5);
      REC_CTRY = FullResponseDS.data.detail.recipient.address.countryCd;

      FullResponseDS.data.detail.recipient.address.zipCode =
        %subst(SafeGetString(yajl_object_find(recipientAddr: %addr('ZIP_CODE'))): 1: 10);
      REC_ZIP = FullResponseDS.data.detail.recipient.address.zipCode;

      FullResponseDS.data.detail.recipient.address.phone =
        %subst(SafeGetString(yajl_object_find(recipientAddr: %addr('PHONE'))): 1: 20);
      REC_PHONE = FullResponseDS.data.detail.recipient.address.phone;

    endif;
  endif;

endsr;


begsr ProcesarSenderIdentification;

  dcl-s senderIdentNode pointer;

  senderIdentNode = yajl_object_find(innerDataNode: %addr('SENDER_IDENTIFICATION'));

  if senderIdentNode <> *null;

    FullResponseDS.data.detail.senderIdent.typeCd =
      %subst(SafeGetString(yajl_object_find(senderIdentNode: %addr('TYPE_CD'))): 1: 10);
    SID_TYPCD = FullResponseDS.data.detail.senderIdent.typeCd;

    FullResponseDS.data.detail.senderIdent.issuerCd =
      %subst(SafeGetString(yajl_object_find(senderIdentNode: %addr('ISSUER_CD'))): 1: 10);
    SID_ISSCD = FullResponseDS.data.detail.senderIdent.issuerCd;

    FullResponseDS.data.detail.senderIdent.issuerStateCd =
      %subst(SafeGetString(yajl_object_find(senderIdentNode: %addr('ISSUER_STATE_CD'))): 1: 10);
    SID_ISSST = FullResponseDS.data.detail.senderIdent.issuerStateCd;

    FullResponseDS.data.detail.senderIdent.issuerCountryCd =
      %subst(SafeGetString(yajl_object_find(senderIdentNode: %addr('ISSUER_COUNTRY_CD'))): 1: 10);
    SID_ISSCT = FullResponseDS.data.detail.senderIdent.issuerCountryCd;

    FullResponseDS.data.detail.senderIdent.identifNm =
      %subst(SafeGetString(yajl_object_find(senderIdentNode: %addr('IDENTIF_NM'))): 1: 30);
    SID_IDNM = FullResponseDS.data.detail.senderIdent.identifNm;

    FullResponseDS.data.detail.senderIdent.expirationDt =
      %subst(SafeGetString(yajl_object_find(senderIdentNode: %addr('EXPIRATION_DT'))): 1: 8);
    SID_EXPDT = FullResponseDS.data.detail.senderIdent.expirationDt;

  endif;

endsr;


begsr ProcesarRecipientIdentification;

  dcl-s recipientIdentNode pointer;

  recipientIdentNode = yajl_object_find(innerDataNode: %addr('RECIPIENT_IDENTIFICATION'));

  if recipientIdentNode <> *null;

    FullResponseDS.data.detail.recipientIdent.typeCd =
      %subst(SafeGetString(yajl_object_find(recipientIdentNode: %addr('TYPE_CD'))): 1: 10);
    RID_TYPCD = FullResponseDS.data.detail.recipientIdent.typeCd;

    FullResponseDS.data.detail.recipientIdent.issuerCd =
      %subst(SafeGetString(yajl_object_find(recipientIdentNode: %addr('ISSUER_CD'))): 1: 10);
    RID_ISSCD = FullResponseDS.data.detail.recipientIdent.issuerCd;

    FullResponseDS.data.detail.recipientIdent.issuerStateCd =
      %subst(SafeGetString(yajl_object_find(recipientIdentNode: %addr('ISSUER_STATE_CD'))): 1: 10);
    RID_ISSST = FullResponseDS.data.detail.recipientIdent.issuerStateCd;

    FullResponseDS.data.detail.recipientIdent.issuerCountryCd =
      %subst(SafeGetString(yajl_object_find(recipientIdentNode: %addr('ISSUER_COUNTRY_CD'))): 1: 10);
    RID_ISSCT = FullResponseDS.data.detail.recipientIdent.issuerCountryCd;

    FullResponseDS.data.detail.recipientIdent.identifNm =
      %subst(SafeGetString(yajl_object_find(recipientIdentNode: %addr('IDENTIF_NM'))): 1: 30);
    RID_IDNM = FullResponseDS.data.detail.recipientIdent.identifNm;

    FullResponseDS.data.detail.recipientIdent.expirationDt =
      %subst(SafeGetString(yajl_object_find(recipientIdentNode: %addr('EXPIRATION_DT'))): 1: 8);
    RID_EXPDT = FullResponseDS.data.detail.recipientIdent.expirationDt;

  endif;

endsr;
