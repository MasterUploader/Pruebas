    //*DFTACTGRP(*NO) ACTGRP(*NEW) BNDDIR('HTTPAPI':'QC2LE')
    //*BNDDIR('HTTPAPI':'QC2LE')
    //* BNDDIR('YAJL') DECEDIT('0.')
       // ===============================================================
       //Includes y prototipos externos
       // ===============================================================
      /copy qrpglesrc,httpapi_h
      /copy qrpglesrc,ifsio_h
      /include yajl_h

       dcl-pr BTS02POST;
            HRequestId           char(100);
            HChannel             char(20);
            HTerminal            char(20);
            HOrganization        char(20);
            HUserId              char(20);
            HProvider            char(20);
            HSessionId           char(100);
            HClientIp            char(20);
            HTimestamp           char(50);
            ConfirmationNm       char(11);
            RegionSd             char(15);
            BranchSd             char(15);
            StateCd              char(3);
            CountryCd            char(3);
            Username             char(20);
            Terminal             char(15);
            AgentDt              char(8);
            AgentTm              char(6);
            HDR_RSPID            char(100);
            HDR_TMSTMP           char(50);
            HDR_PRTIME           char(20);
            HDR_STSCD            char(20);
            HDR_MSG              char(5000);
            SPRIMNAME          char(40);
            SSECUNAME          char(40);
            SAPELLIDO          char(40);
            SSEGUAPE           char(40);
            SDIRECCIO          char(65);
            SCIUDAD            char(30);
            SESTADO            char(3);
            SPAIS              char(3);
            SCODPOST           char(10);
            STELEFONO          char(15);
            ZPRIMNAME          char(40);
            ZSECUNAME          char(40);
            ZAPELLIDO          char(40);
            ZSEGUAPE           char(40);
            ZDIRECCIO          char(65);
            ZCIUDAD            char(30);
            ZESTADO            char(3);
            ZPAIS              char(3);
            ZCODPOST           char(10);
            ZTELEFONO          char(15);
            MCODPAIS          char(3);
            MCODMONED         char(3);
            MMTOENVIA         char(20);
            MMTOCALCU         char(20);
            MFACTCAMB         char(21);
            XESTATUS          char(3);
            IDENTIDAD         char(20);
            MSALEDT           char(8);
            MMONREFER         char(3);
            MTASAREFE         char(21);
            MMTOREF           char(20);
            TIPPAG            char(3);
            OPCODE              char(4);
            ProcessMsg          char(70);
            ErrorParamFullName  char(255);
            TransStatusCd       char(3);
            TransStatusDt       char(8);
            ProcessDt           char(8);
            ProcessTm           char(6);
       end-pr;
       // ===============================================================
       //PI: Párametros de entrada
       // ===============================================================
          dcl-pi BTS02POST;
            HRequestId           char(100);
            HChannel             char(20);
            HTerminal            char(20);
            HOrganization        char(20);
            HUserId              char(20);
            HProvider            char(20);
            HSessionId           char(100);
            HClientIp            char(20);
            HTimestamp           char(50);
            ConfirmationNm       char(11);
            RegionSd             char(15);
            BranchSd             char(15);
            StateCd              char(3);
            CountryCd            char(3);
            Username             char(20);
            Terminal             char(15);
            AgentDt              char(8);
            AgentTm              char(6);
            HDR_RSPID            char(100);
            HDR_TMSTMP           char(50);
            HDR_PRTIME           char(20);
            HDR_STSCD            char(20);
            HDR_MSG              char(5000);
            SPRIMNAME          char(40);
            SSECUNAME          char(40);
            SAPELLIDO          char(40);
            SSEGUAPE           char(40);
            SDIRECCIO          char(65);
            SCIUDAD            char(30);
            SESTADO            char(3);
            SPAIS              char(3);
            SCODPOST           char(10);
            STELEFONO          char(15);
            ZPRIMNAME          char(40);
            ZSECUNAME          char(40);
            ZAPELLIDO          char(40);
            ZSEGUAPE           char(40);
            ZDIRECCIO          char(65);
            ZCIUDAD            char(30);
            ZESTADO            char(3);
            ZPAIS              char(3);
            ZCODPOST           char(10);
            ZTELEFONO          char(15);
            MCODPAIS          char(3);
            MCODMONED         char(3);
            MMTOENVIA         char(20);
            MMTOCALCU         char(20);
            MFACTCAMB         char(21);
            XESTATUS          char(3);
            IDENTIDAD         char(20);
            MSALEDT           char(8);
            MMONREFER         char(3);
            MTASAREFE         char(21);
            MMTOREF           char(20);
            TIPPAG            char(3);
            OPCODE              char(4);
            ProcessMsg          char(70);
            ErrorParamFullName  char(255);
            TransStatusCd       char(3);
            TransStatusDt       char(8);
            ProcessDt           char(8);
            ProcessTm           char(6);
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
          // ===============================================================
          // Variables YAJL para generación del request JSON
          // ===============================================================
          dcl-s jsonBuffer varchar(32700);
          // ===============================================================
          // Variables para uso de libhttp_post
          // ===============================================================
          dcl-s reqPtr pointer;
          dcl-s resPtr pointer;
          dcl-s hdrPtr pointer;
          dcl-s urlPtr pointer;
          dcl-s responseLen int(10);
          dcl-s url varchar(200);
          dcl-s contentType varchar(50);
          // ===============================================================
          // Buffers auxiliares
          // ===============================================================
          dcl-s headers varchar(200);
          dcl-s innerDataNode pointer;
          dcl-s rc int(10);
          // ===============================================================
          // ----------- HEADER -------------------ojo
          // ===============================================================
          dcl-ds HeaderDS qualified;
            responseId      char(40);
            timestamp       char(30);
            processingTime  char(20);
            statusCode      char(10);
            message         char(100);
          end-ds;

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
            ConfirmationNm char(11);
            RegionSd       char(15);
            BranchSd       char(15);
            StateCd        char(3);
            CountryCd      char(3);
            Username       char(20);
            Terminal       char(15);
            AgentDt        char(8);
            AgentTm        char(6);
          end-ds;
           // ===============================================================
           // ----------- DATA (nivel 1) -----------
           // ===============================================================
          dcl-ds DataDS qualified;
            opCode         char(4);
            processMsg     char(5000);
            errorParamFullName char(255);
            transStatusCd  char(3);
            transStatusDt  char(8);
            processDt      char(8);
            processTm      char(6);
            detail       likeds(DataDetailDS);
          end-ds;
          // ===============================================================
          // ---------- DATA.DATA (detalle) -------
          // ===============================================================
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
          // ===============================================================
          // ---------- PERSONA BASE (Sender, ForeignName) ----------
          // ===============================================================
          dcl-ds PersonDS qualified;
            firstName     char(40);
            middleName    char(40);
            lastName      char(40);
            motherMName   char(40);
            address       likeds(AddressDS);
          end-ds;
          // ===============================================================
          // ---------- RECIPIENT (con ForeignName) ----------
          // ===============================================================
          dcl-ds RecipientDS qualified;
            firstName     char(40);
            middleName    char(40);
            lastName      char(40);
            motherMName   char(40);
            identifTypeCd char(10);
            identifNm     char(30);
            foreignName   likeds(PersonDS);
            address       likeds(AddressDS);
          end-ds;
          // ===============================================================
          // ---------- ADDRESS ----------
          // ===============================================================
          dcl-ds AddressDS qualified;
            address    char(65);
            city       char(30);
            stateCd    char(3);
            countryCd  char(3);
            zipCode    char(10);
            phone      char(15);
          end-ds;
          // ===============================================================
          // Estructura principal de respuesta JSON
          // ===============================================================
          dcl-ds FullResponseDS qualified;
            header likeds(HeaderDS);
            data   likeds(DataDS);
          end-ds;
          // ===============================================================
          // ---------- IDENTIFICACIONES ----------
          // ===============================================================
          dcl-ds IdentificationDS qualified;
            typeCd           char(10);
            issuerCd         char(10);
            issuerStateCd    char(10);
            issuerCountryCd  char(10);
            identifNm        char(30);
            expirationDt     char(8);
          end-ds;
          // ===============================================================
          // ---------- SBR100 ----------
          // ===============================================================
          dcl-pr sbr100 ;
                    input likeDS(HeaderInputDS);
          end-pr;
        // ===============================================================
        // ---------- Inicio ----------
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
            HeaderInputDS.ConfirmationNm = ConfirmationNm;
            HeaderInputDS.RegionSd       = RegionSd;
            HeaderInputDS.BranchSd       = BranchSd;
            HeaderInputDS.StateCd        = StateCd;
            HeaderInputDS.CountryCd      = CountryCd;
            HeaderInputDS.Username       = Username;
            HeaderInputDS.Terminal       = Terminal;
            HeaderInputDS.AgentDt        = AgentDt;
            HeaderInputDS.AgentTm        = AgentTm;

            sbr100(HeaderInputDS);
          *inlr = *on;
          return;
        // ===============================================================
        // ------------------- PROCEDIMIENTOS --------------------
        // ===============================================================
        // ===============================================================
        //Procedimiento Procesar Respuesta
        // ===============================================================
        // dcl-pr  GetApiConfig;
        //   dcl-s iCodArea    zoned(8:0);
        //   dcl-s iCodOpti    char(40);
        //   dcl-s iCodKeys    char(40);
        //   dcl-s iCodType    char(2);
        //   dcl-s oRespons    char(300);
        // end-pr;
        dcl-proc GetStringFromJson;
          dcl-pi *n char(100);
            parentNode pointer value;
            fieldName varchar(50) const;
            maxLength int(5) const;
          end-pi;

          dcl-s result char(100);
          dcl-s tempPtr pointer;
          dcl-s tempStr char(500) based(tempPtr); // Puede ajustar tamaño

          result = *blanks;

          tempPtr = yajl_object_find(parentNode: %trim(fieldName));

          if tempPtr <> *null;
            result = %subst(%str(%addr(tempStr)): 1: maxLength);
          endif;

          return result;
        end-proc;

          // ===============================================================
          //Procedimiento Procesar Respuesta
          // ===============================================================
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

        // ===============================================================
        //Procedimiento encargada de Generar el JsonRequest
        //para la petición al API
        // ===============================================================
        dcl-proc GenerarRequestJson export;
            dcl-pi *n;
            input likeDs(HeaderInputDS);      
            end-pi;

          dcl-s jsonPtr pointer;
          dcl-s jsonLen int(10);
          dcl-s jsonGen int(10);
          dcl-s errMsg varchar(500);

          // Abrir el generador JSON (N o Y, *OFF o *ON)
           jsonGen = yajl_genOpen(*ON);

          // Comenzar el objeto JSON principal
          callp yajl_beginObj();

          // --- HEADER ---
          callp yajl_addChar('header');
          callp yajl_beginObj();
          callp yajl_addChar('h-request-id': %trim(HRequestId));
          callp yajl_addChar('h-channel': %trim(HChannel));
          callp yajl_addChar('h-session-id': %trim(HSessionId));
          callp yajl_addChar('h-client-ip': %trim(HClientIp));
          callp yajl_addChar('h-user-id': %trim(HUserId));
          callp yajl_addChar('h-provider': %trim(HProvider));
          callp yajl_addChar('h-organization': %trim(HOrganization));
          callp yajl_addChar('h-terminal': %trim(HTerminal));
          callp yajl_addChar('h-timestamp': %trim(HTimestamp));
          callp yajl_endObj();
          // --- HEADER ---

          // --- Body ---//
          callp yajl_addChar('body');
          callp yajl_beginObj();

          //--- ExecTR ---//
          callp yajl_addChar('execTR');
          callp yajl_beginObj();

          // --- REQUEST --- //
          callp yajl_addChar('request');
          callp yajl_beginObj();
          // callp yajl_addChar('AGENT_CD': %trim(AgentCode));
          // callp yajl_addChar('AGENT_TRANS_TYPE_CODE'
          // : %trim(AgentTransactionTypeCode));

          // --- Data --- //
          callp yajl_addChar('data');
          callp yajl_beginObj();
          callp yajl_addChar('confirmationNm': %trim(ConfirmationNm));

          // --- agent --- //
          callp yajl_addChar('agent');
          callp yajl_beginObj();
          callp yajl_addChar('regionSd': %trim(RegionSd));
          callp yajl_addChar('branchSd': %trim(BranchSd));
          callp yajl_addChar('stateCd': %trim(StateCd));
          callp yajl_addChar('countryCd': %trim(CountryCd));
          callp yajl_addChar('username': %trim(Username));
          callp yajl_addChar('terminal': %trim(Terminal));
          callp yajl_addChar('agentDt': %trim(AgentDt));
          callp yajl_addChar('agentTm': %trim(AgentTm));

          callp yajl_endObj();
          // --- agent --- //

          callp yajl_endObj();
          // --- Data --- //

          callp yajl_endObj();
          // --- REQUEST --- //

          callp yajl_endObj();
          //--- ExecTR ---//

          callp yajl_endObj();
          // --- Body ---//

          callp yajl_endObj();
          // --- Comenzar el objeto JSON principal ---//

          // Obtener el buffer JSON generado
                yajl_saveBuf(vFullFileC: errMsg);

           if errMsg <> '';
          // handle error
          endif;

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

          // Preparar punteros dinámicos con datos de GetApiConfig
          headers = 'Content-Type: application/json';

          // ----------------------------------------
          // Realiza el POST, guarda en archivo IFS
          // ----------------------------------------

          //http_degug(*on);

          rc = HTTP_POST(%trim(pUrlPost)
                     : %addr( vFullFileC)
                     : %len(vFullFileC)
                     : %Trim(vFullFileR)
                     : HTTP_TIMEOUT
                     : HTTP_USERAGENT
                     : headers );

          // Validaciones del resultado
          if rc < 0;
              // Error grave de conexión
              response = '{ "error": "Fallo de conexión o red." }';
          elseif rc > 0;
              // Error HTTP
              response = '{ "error": "Error HTTP. Código RC=' +
              %char(rc) + '" }';
          elseif %trim(response) = *blanks;
              // Respuesta vacía
              response = '{ "error": "Respuesta vacía de la API. RC=0" }';
          elseif %scan('error': %xlate('":,{}[]' : '        ' :
                  %trim(response))) > 0;
              // Contenido contiene palabra error
              response = '{ "warning": "La API respondió con posible error" }';
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
            iCodOpti = 'OP-REMESADORA-BTS';
            iCodKeys = 'KEY-API-V1-BTS-CONSULTA';
            callp PrgParameter(iCodArea:
                         iCodOpti:
                         iCodKeys:
                         iCodType:
                         oRespons);
            pUrlPost = %trim(oRespons);  // Asignar a variable global

            // === Obtener ruta para archivo ===
            iCodKeys = 'KEY-BTS-AS-LOG';
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

            dcl-s vDate date;
            dcl-s vTime time;
            dcl-s vDate8 char(8);
            dcl-s vTime6 char(6);
            dcl-s vFileNameC char(30);
            dcl-s vFileNameR char(30);

            vDate = %date();
            vTime = %time();
            vDate8 = %subst(%char(vDate):3:2) +
            %subst(%char(vDate):6:2) +
            %subst(%char(vDate):9:2);

            vTime6 = %subst(%char(vTime):1:2) +
            %subst(%char(vTime):4:2) +
            %subst(%char(vTime):7:2);

            vFileNameC = 'CBTS02_' + %trim(vDate8) + %trim(vTime6) + '.json';
            vFileNameR = 'RBTS02_' + %trim(vDate8) + %trim(vTime6) + '.json';

            // Concatenar con ruta obtenida
            vFullFileC = %trim(pFileSav) + vFileNameC;
            vFullFileR = %trim(pFileSav) + vFileNameR;
        end-proc;

        // =======================================================
        // Procedimiento que guarda la respuesta JSON en el IFS
        // =======================================================
        dcl-proc GuardarResponseJson;

          dcl-s fd int(10);
          dcl-s filePath varchar(300);

          // Se asume que vFullFileC ya tiene el path completo
        //filePath = %addr(vFullFileC);

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
           ProcesarRespuesta();

          // 6. Guardamos la Respuesta
           GuardarResponseJson();

        end-proc;
