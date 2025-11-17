Tengo esta CLLE, crea una CLLE aparte solo para llamar a esta para realizar pruebas


             PGM        PARM(&HREQUESTID &HCHANNEL &HTERMINAL +
                          &HORGANIZA &HUSERID &HPROVIDER &HSESSION +
                          &HCLIENTIP &HTIMESTAMP &RESCODDES +
                          &RESPCODE &AUTIDRESP &RETREFNUMB +
                          &SYTRAUNUM &TRANTYPE &TILOCTRANS +
                          &DATLOCTRAN &AMOUNT &MERCHANTID &MCC +
                          &CURRCODE &PACCNUMB &TERMID &DATEEXP &CVV2)


                              /*GENERALES*/
             DCL        VAR(&HREQUESTID) TYPE(*CHAR) LEN(100)
             DCL        VAR(&HCHANNEL) TYPE(*CHAR) LEN(20)
             DCL        VAR(&HTERMINAL) TYPE(*CHAR) LEN(20)
             DCL        VAR(&HORGANIZA) TYPE(*CHAR) LEN(20)
             DCL        VAR(&HUSERID) TYPE(*CHAR) LEN(20)
             DCL        VAR(&HPROVIDER) TYPE(*CHAR) LEN(100)
             DCL        VAR(&HSESSION) TYPE(*CHAR) LEN(100)
             DCL        VAR(&HCLIENTIP) TYPE(*CHAR) LEN(20)
             DCL        VAR(&HTIMESTAMP) TYPE(*CHAR) LEN(50)

                                /*DATOS CONSULTA*/
            DCL        VAR(&ResCodDes) TYPE(*CHAR) LEN(500)
            DCL        VAR(&RespCode) TYPE(*CHAR) LEN(20)
            DCL        VAR(&AutIdResp) TYPE(*CHAR) LEN(20)
            DCL        VAR(&RetRefNumb) TYPE(*CHAR) LEN(20)
            DCL        VAR(&SyTrAuNum) TYPE(*CHAR) LEN(20)
            DCL        VAR(&TranType) TYPE(*CHAR) LEN(20)
            DCL        VAR(&TiLocTrans) TYPE(*CHAR) LEN(20)
            DCL        VAR(&DatLocTran) TYPE(*CHAR) LEN(20)
            DCL        VAR(&Amount) TYPE(*CHAR) LEN(20)
            DCL        VAR(&MerchantID) TYPE(*CHAR) LEN(20)
            DCL        VAR(&MCC) TYPE(*CHAR) LEN(20)
            DCL        VAR(&CurrCode) TYPE(*CHAR) LEN(20)
            DCL        VAR(&PAccNumb) TYPE(*CHAR) LEN(20)
            DCL        VAR(&TermID) TYPE(*CHAR) LEN(20)
            DCL        VAR(&DateExp      ) TYPE(*CHAR) LEN(20)
            DCL        VAR(&CVV2        ) TYPE(*CHAR) LEN(20)



             /*LIBRERIA*/
             ADDLIBLE   LIB(BCAH96)
                  MONMSG     MSGID(CPF2103)
             ADDLIBLE   LIB(BCAH96DTA)
                  MONMSG     MSGID(CPF2103)
             ADDLIBLE   LIB(LIBHTTP)
                  MONMSG     MSGID(CPF2103)
             ADDLIBLE   LIB(GX)
                  MONMSG     MSGID(CPF2103)
             ADDLIBLE   LIB(YAJL)
                  MONMSG     MSGID(CPF2103)

             /*ACÁ LLAMAREMOS AL POST*/
              CALL PGM(BCAH96/TNP02POST) PARM(&HREQUESTID +
                &HCHANNEL +
                &HTERMINAL +
                &HORGANIZA +
                &HUSERID +
                &HPROVIDER +
                &HSESSION +
                &HCLIENTIP +
                &HTIMESTAMP +
                &ResCodDes +
                &RespCode +
                &AutIdResp +
                &RetRefNumb +
                &SyTrAuNum +
                &TranType +
                &TiLocTrans +
                &DatLocTran +
                &Amount +
                &MerchantID +
                &MCC +
                &CurrCode +
                &PAccNumb +
                &TermID +
                &DateExp +
                &CVV2 )

     ENDPGM
