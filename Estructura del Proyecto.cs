             PGM
             /* ------------------------------------------------------------ */
             /*  Programa de prueba: ejecuta TNP02POST con datos simulados   */
             /*  Autor: Brayan Banegas                                       */
             /*  Fecha: 2025-11-17                                           */
             /* ------------------------------------------------------------ */

             DCL        VAR(&HREQUESTID) TYPE(*CHAR) LEN(100) VALUE('REQ1234567890')
             DCL        VAR(&HCHANNEL)   TYPE(*CHAR) LEN(20)  VALUE('WEB')
             DCL        VAR(&HTERMINAL)  TYPE(*CHAR) LEN(20)  VALUE('P0055468')
             DCL        VAR(&HORGANIZA)  TYPE(*CHAR) LEN(20)  VALUE('ORG01')
             DCL        VAR(&HUSERID)    TYPE(*CHAR) LEN(20)  VALUE('USRTEST')
             DCL        VAR(&HPROVIDER)  TYPE(*CHAR) LEN(100) VALUE('DaviviendaTNP')
             DCL        VAR(&HSESSION)   TYPE(*CHAR) LEN(100) VALUE('SESSION123')
             DCL        VAR(&HCLIENTIP)  TYPE(*CHAR) LEN(20)  VALUE('192.168.1.50')
             DCL        VAR(&HTIMESTAMP) TYPE(*CHAR) LEN(50)  VALUE('2025-11-17T21:00:00Z')

             DCL        VAR(&RESCODDES)  TYPE(*CHAR) LEN(500)
             DCL        VAR(&RESPCODE)   TYPE(*CHAR) LEN(20)
             DCL        VAR(&AUTIDRESP)  TYPE(*CHAR) LEN(20)
             DCL        VAR(&RETREFNUMB) TYPE(*CHAR) LEN(20)
             DCL        VAR(&SYTRAUNUM)  TYPE(*CHAR) LEN(20)
             DCL        VAR(&TRANTYPE)   TYPE(*CHAR) LEN(20)
             DCL        VAR(&TILOCTRANS) TYPE(*CHAR) LEN(20)
             DCL        VAR(&DATLOCTRAN) TYPE(*CHAR) LEN(20)
             DCL        VAR(&AMOUNT)     TYPE(*CHAR) LEN(20) VALUE('10000')
             DCL        VAR(&MERCHANTID) TYPE(*CHAR) LEN(20) VALUE('4001021')
             DCL        VAR(&MCC)        TYPE(*CHAR) LEN(20) VALUE('5999')
             DCL        VAR(&CURRCODE)   TYPE(*CHAR) LEN(20) VALUE('340')
             DCL        VAR(&PACCNUMB)   TYPE(*CHAR) LEN(20) VALUE('5413330057004039')
             DCL        VAR(&TERMID)     TYPE(*CHAR) LEN(20) VALUE('P0055468')
             DCL        VAR(&DATEEXP)    TYPE(*CHAR) LEN(20) VALUE('2512')
             DCL        VAR(&CVV2)       TYPE(*CHAR) LEN(20) VALUE('000')

             /* --- Bibliotecas requeridas --- */
             ADDLIBLE   LIB(BCAH96)     MONMSG CPF2103
             ADDLIBLE   LIB(BCAH96DTA)  MONMSG CPF2103
             ADDLIBLE   LIB(LIBHTTP)    MONMSG CPF2103
             ADDLIBLE   LIB(GX)         MONMSG CPF2103
             ADDLIBLE   LIB(YAJL)       MONMSG CPF2103

             /* --- Llamada al programa principal --- */
             CALL       PGM(BCAH96/TNP02POST) PARM(&HREQUESTID +
                          &HCHANNEL +
                          &HTERMINAL +
                          &HORGANIZA +
                          &HUSERID +
                          &HPROVIDER +
                          &HSESSION +
                          &HCLIENTIP +
                          &HTIMESTAMP +
                          &RESCODDES +
                          &RESPCODE +
                          &AUTIDRESP +
                          &RETREFNUMB +
                          &SYTRAUNUM +
                          &TRANTYPE +
                          &TILOCTRANS +
                          &DATLOCTRAN +
                          &AMOUNT +
                          &MERCHANTID +
                          &MCC +
                          &CURRCODE +
                          &PACCNUMB +
                          &TERMID +
                          &DATEEXP +
                          &CVV2)

             /* --- Muestra en joblog los valores resultantes --- */
             SNDPGMMSG  MSGID(CPF9898) MSGF(QCPFMSG) MSGDTA('TNP02POST ejecutado. Código=' *CAT &RESPCODE *BCAT +
                          ' Descripción=' *CAT &RESCODDES)

             ENDPGM
