dcl-proc ProcesarRespuesta;

          dcl-s rootNode pointer;
          dcl-s headerNode pointer;
          dcl-s dataNode pointer;

          dcl-s errmsg   varchar(500);
          // Cargar el árbol JSON desde la variable de respuesta
          rootNode = yajl_buf_load_tree(%addr(vFullFileR) :
          %len(%trimr(vFullFileR)):errmsg);

          // ===============================================================
          //  HEADER
          // ===============================================================
          headerNode = yajl_object_find(rootNode: 'Header');
          if headerNode <> *null;
            FullResponseDS.header.responseId =
              GetStringFromJson(headerNode: 'responseId ':40);

            FullResponseDS.header.timestamp =
              GetStringFromJson(headerNode:'timestamp': 30);

            FullResponseDS.header.processingTime =
              GetStringFromJson(headerNode:'ProcessingTime': 20);

            FullResponseDS.header.statusCode =
              GetStringFromJson(headerNode:'StatusCode':10);

            FullResponseDS.header.message =
              GetStringFromJson(headerNode:'Message': 100);
          endif;

          // Mapeo a parámetros de salida
          HDR_RSPID  = FullResponseDS.header.responseId;
          HDR_TMSTMP = FullResponseDS.header.timestamp;
          HDR_PRTIME = FullResponseDS.header.processingTime;
          HDR_STSCD  = FullResponseDS.header.statusCode;
          HDR_MSG    = FullResponseDS.header.message;

          // ===============================================================
          //  DATA
          // ===============================================================
          dataNode = yajl_object_find(rootNode: 'Data');
          if dataNode <> *null;
            FullResponseDS.data.opCode =
              GetStringFromJson(dataNode: 'OPCODE': 4);

            FullResponseDS.data.processMsg =
              GetStringFromJson(dataNode:'PROCESS_MSG': 5000);

            FullResponseDS.data.transStatusCd =
              GetStringFromJson(dataNode:'TRANS_STATUS_CD': 3);

            FullResponseDS.data.transStatusDt =
              GetStringFromJson(dataNode:'TRANS_STATUS_DT': 8);

            FullResponseDS.data.processDt =
              GetStringFromJson(dataNode: 'PROCESS_DT': 8);

            FullResponseDS.data.processTm =
              GetStringFromJson(dataNode:'PROCESS_TM': 6);
            // ===============================================================
            //  DATA.DATA
            // ===============================================================
            innerDataNode = yajl_object_find(dataNode: 'DATA');
            if innerDataNode <> *null;
                 ProcesarDataGenerales();
                 ProcesarSender();
                 ProcesarRecipient();
                 ProcesarSenderIdentification();
                 ProcesarRecipientIdentification();
            endif;
          endif;

          // Liberar recursos de YAJL
          yajl_tree_free(rootNode);
        end-proc;

        // ===============================================================
        //  Procedimiento ProcesarDataGeneral
        // ===============================================================
        dcl-proc ProcesarDataGenerales;

          FullResponseDS.data.detail.saleDt =
            GetStringFromJson(innerDataNode:'SALE_DT': 8);

         // DDT_SALEDT = FullResponseDS.data.detail.saleDt;

          FullResponseDS.data.detail.saleTm =
            GetStringFromJson(innerDataNode: 'SALE_TM': 6);

          FullResponseDS.data.detail.destinationAm =
            GetStringFromJson(innerDataNode:'DESTINATION_AM': 20);

        //  DDT_DSTAMT = FullResponseDS.data.detail.destinationAm;

          FullResponseDS.data.detail.serviceCd =
            GetStringFromJson(innerDataNode:'SERVICE_CD': 10);

          FullResponseDS.data.detail.paymentTypeCd =
            GetStringFromJson(innerDataNode:'PAYMENT_TYPE_CD': 10);

          FullResponseDS.data.detail.origCountryCd =
            GetStringFromJson(innerDataNode:'ORIG_COUNTRY_CD': 5);

          FullResponseDS.data.detail.destCurrencyCd =
            GetStringFromJson(innerDataNode:'DEST_CURRENCY_CD': 5);

        end-proc;

        // ===============================================================
        //  Procedimiento ProcesarSender
        // ===============================================================
        dcl-proc ProcesarSender;
          dcl-s senderNode pointer;
          dcl-s senderAddr pointer;

          senderNode = yajl_object_find(innerDataNode: 'SENDER');

          if senderNode <> *null;

            FullResponseDS.data.detail.sender.firstName =
              GetStringFromJson(senderNode:'FIRST_NAME': 20);

          //  SND_FNAME = FullResponseDS.data.detail.sender.firstName;

            FullResponseDS.data.detail.sender.middleName =
              GetStringFromJson(senderNode:'MIDDLE_NAME': 20);

           // SND_MNAME = FullResponseDS.data.detail.sender.middleName;

            FullResponseDS.data.detail.sender.lastName =
              GetStringFromJson(senderNode:'LAST_NAME': 20);

           // SND_LNAME = FullResponseDS.data.detail.sender.lastName;

            FullResponseDS.data.detail.sender.motherMName =
              GetStringFromJson(senderNode:'MOTHER_M_NAME': 20);

           // SND_MOMNM = FullResponseDS.data.detail.sender.motherMName;

            senderAddr = yajl_object_find(senderNode: 'ADDRESS');
            if senderAddr <> *null;

              FullResponseDS.data.detail.sender.address.address =
                GetStringFromJson(senderAddr:'ADDRESS': 65);

            //  SND_ADDR = FullResponseDS.data.detail.sender.address.address;

              FullResponseDS.data.detail.sender.address.city =
                GetStringFromJson(senderAddr:'CITY': 30);

             // SND_CITY = FullResponseDS.data.detail.sender.address.city;

              FullResponseDS.data.detail.sender.address.stateCd =
                GetStringFromJson(senderAddr:'STATE_CD': 3);

             // SND_STCD = FullResponseDS.data.detail.sender.address.stateCd;

              FullResponseDS.data.detail.sender.address.countryCd =
                GetStringFromJson(senderAddr:'COUNTRY_CD': 3);

            //  SND_CTRY = FullResponseDS.data.detail.sender.address.countryCd;

              FullResponseDS.data.detail.sender.address.zipCode =
                GetStringFromJson(senderAddr:'ZIP_CODE': 10);

            //  SND_ZIP = FullResponseDS.data.detail.sender.address.zipCode;

              FullResponseDS.data.detail.sender.address.phone =
                GetStringFromJson(senderAddr:'PHONE': 15);

            //  SND_PHONE = FullResponseDS.data.detail.sender.address.phone;

            endif;
          endif;

        end-proc;

        // ===============================================================
        //  Procedimiento ProcesarRecipient
        // ===============================================================
        dcl-proc ProcesarRecipient;

          dcl-s recipientNode pointer;
          dcl-s foreignNode pointer;
          dcl-s recipientAddr pointer;

          recipientNode = yajl_object_find(innerDataNode: 'RECIPIENT');

          if recipientNode <> *null;

            FullResponseDS.data.detail.recipient.firstName =
              GetStringFromJson(recipientNode:'FIRST_NAME': 40);

         //   REC_FNAME = FullResponseDS.data.detail.recipient.firstName;

            FullResponseDS.data.detail.recipient.middleName =
              GetStringFromJson(recipientNode:'MIDDLE_NAME': 40);

          //  REC_MNAME = FullResponseDS.data.detail.recipient.middleName;

            FullResponseDS.data.detail.recipient.lastName =
              GetStringFromJson(recipientNode: 'LAST_NAME': 40);

          //  REC_LNAME = FullResponseDS.data.detail.recipient.lastName;

            FullResponseDS.data.detail.recipient.motherMName =
              GetStringFromJson(recipientNode:'MOTHER_M_NAME': 40);

          //  REC_MOMNM = FullResponseDS.data.detail.recipient.motherMName;

            FullResponseDS.data.detail.recipient.identifTypeCd =
              GetStringFromJson(recipientNode:'IDENTIF_TYPE_CD': 10);

          //  REC_IDTYP = FullResponseDS.data.detail.recipient.identifTypeCd;

            FullResponseDS.data.detail.recipient.identifNm =
              GetStringFromJson(recipientNode:'IDENTIF_NM': 30);

         //   REC_IDNM = FullResponseDS.data.detail.recipient.identifNm;

            foreignNode = yajl_object_find(recipientNode:'FOREIGN_NAME');

            if foreignNode <> *null;

              FullResponseDS.data.detail.recipient.foreignName.firstName =
                GetStringFromJson(foreignNode:'FIRST_NAME': 40);

           //   RFC_FNAME = FullResponseDS.data.detail.recipient.foreignName
           //   .firstName;

           //   FullResponseDS.data.detail.recipient.foreignName.middleName =
              //  GetStringFromJson(foreignNode:'MIDDLE_NAME': 20);

            //  RFC_MNAME = FullResponseDS.data.detail.recipient.foreignName
            //  .middleName;

              FullResponseDS.data.detail.recipient.foreignName.lastName =
                GetStringFromJson(foreignNode:'LAST_NAME': 40);
           //   RFC_LNAME = FullResponseDS.data.detail.recipient.foreignName
            //  .lastName;

              FullResponseDS.data.detail.recipient.foreignName.motherMName =
                GetStringFromJson(foreignNode:'MOTHER_M_NAME': 40);
           //   RFC_MOMNM = FullResponseDS.data.detail.recipient.foreignName
            //  .motherMName;

            endif;

            recipientAddr = yajl_object_find(recipientNode: 'ADDRESS');
            if recipientAddr <> *null;

              FullResponseDS.data.detail.recipient.address.address =
                GetStringFromJson(recipientAddr:'ADDRESS': 65);

           //   REC_ADDR = FullResponseDS.data.detail.recipient.address.address;

              FullResponseDS.data.detail.recipient.address.city =
                GetStringFromJson(recipientAddr:'CITY': 30);

             // REC_CITY = FullResponseDS.data.detail.recipient.address.city;

              FullResponseDS.data.detail.recipient.address.stateCd =
                GetStringFromJson(recipientAddr:'STATE_CD': 3);

           //   REC_STCD = FullResponseDS.data.detail.recipient.address.stateCd;

              FullResponseDS.data.detail.recipient.address.countryCd =
                GetStringFromJson(recipientAddr:'COUNTRY_CD': 3);
          // REC_CTRY = FullResponseDS.data.detail.recipient.address.countryCd;

              FullResponseDS.data.detail.recipient.address.zipCode =
                GetStringFromJson(recipientAddr:'ZIP_CODE': 10);

            //  REC_ZIP = FullResponseDS.data.detail.recipient.address.zipCode;

              FullResponseDS.data.detail.recipient.address.phone =
                GetStringFromJson(recipientAddr:'PHONE': 15);

             // REC_PHONE = FullResponseDS.data.detail.recipient.address.phone;

            endif;
          endif;

        end-proc;

        // ===============================================================
        //  Procedimiento ProcesarSenderIdentification
        // ===============================================================
        dcl-proc ProcesarSenderIdentification;

          dcl-s senderIdentNode pointer;

          senderIdentNode = yajl_object_find(innerDataNode:
          'SENDER_IDENTIFICATION');

          if senderIdentNode <> *null;

            FullResponseDS.data.detail.senderIdent.typeCd =
              GetStringFromJson(senderIdentNode:'TYPE_CD': 10);

          //  SID_TYPCD = FullResponseDS.data.detail.senderIdent.typeCd;

            FullResponseDS.data.detail.senderIdent.issuerCd =
              GetStringFromJson(senderIdentNode:'ISSUER_CD': 10);

          //  SID_ISSCD = FullResponseDS.data.detail.senderIdent.issuerCd;

            FullResponseDS.data.detail.senderIdent.issuerStateCd =
              GetStringFromJson(senderIdentNode:'ISSUER_STATE_CD': 10);

           // SID_ISSST = FullResponseDS.data.detail.senderIdent.issuerStateCd;

            FullResponseDS.data.detail.senderIdent.issuerCountryCd =
              GetStringFromJson(senderIdentNode:'ISSUER_COUNTRY_CD': 10);

          // SID_ISSCT = FullResponseDS.data.detail.senderIdent.issuerCountryCd;

            FullResponseDS.data.detail.senderIdent.identifNm =
              GetStringFromJson(senderIdentNode:'IDENTIF_NM': 30);

         //   SID_IDNM = FullResponseDS.data.detail.senderIdent.identifNm;

            FullResponseDS.data.detail.senderIdent.expirationDt =
              GetStringFromJson(senderIdentNode:'EXPIRATION_DT': 8);

         //   SID_EXPDT = FullResponseDS.data.detail.senderIdent.expirationDt;

          endif;

        end-proc;

        // ===============================================================
        //  Procedimiento ProcesarRecipientIdentification
        // ===============================================================
        dcl-proc ProcesarRecipientIdentification;

          dcl-s recipientIdentNode pointer;

          recipientIdentNode = yajl_object_find(innerDataNode:
          'RECIPIENT_IDENTIFICATION');

          if recipientIdentNode <> *null;

            FullResponseDS.data.detail.recipientIdent.typeCd =
              GetStringFromJson(recipientIdentNode:'TYPE_CD': 10);

          //  RID_TYPCD = FullResponseDS.data.detail.recipientIdent.typeCd;

            FullResponseDS.data.detail.recipientIdent.issuerCd =
              GetStringFromJson(recipientIdentNode:'ISSUER_CD': 10);

          //  RID_ISSCD = FullResponseDS.data.detail.recipientIdent.issuerCd;

            FullResponseDS.data.detail.recipientIdent.issuerStateCd =
              GetStringFromJson(recipientIdentNode:'ISSUER_STATE_CD': 10);

        // RID_ISSST = FullResponseDS.data.detail.recipientIdent.issuerStateCd;

            FullResponseDS.data.detail.recipientIdent.issuerCountryCd =
              GetStringFromJson(recipientIdentNode:'ISSUER_COUNTRY_CD': 10);

        //    RID_ISSCT = FullResponseDS.data.detail.recipientIdent.
           // issuerCountryCd;

            FullResponseDS.data.detail.recipientIdent.identifNm =
              GetStringFromJson(recipientIdentNode:'IDENTIF_NM': 30);

          //  RID_IDNM = FullResponseDS.data.detail.recipientIdent.identifNm;

            FullResponseDS.data.detail.recipientIdent.expirationDt =
              GetStringFromJson(recipientIdentNode:'EXPIRATION_DT': 8);

          //RID_EXPDT = FullResponseDS.data.detail.recipientIdent.expirationDt;

          endif;

        end-proc;
