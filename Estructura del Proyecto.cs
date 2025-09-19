/* ================================================================ */
/* CLLE: POSRE800                                                  */
/* Descripción: Wrapper para invocar programa RPG APACD764         */
/* Objetivo   : Recibir parámetros desde C# y llamar al RPG con    */
/*              la misma firma; devuelve CODER/DESERR como OUT.     */
/* Notas:                                                          */
/* - Ajusta la librería del CALL si aplica (ej. BCAH96DTA/APACD764)*/
/* - Todos los parámetros son posicionales y deben coincidir       */
/*   exactamente con el prototipo del RPG.                         */
/* ================================================================ */

PGM        PARM(&PMTIPO01 &PMCTAA01 &PMVALR01 &PMDECR01 +
                &PMCCOS01 &PMMONE01 &PMTIPO02 &PMCTAA02 +
                &PMVALR02 &PMDECR02 &PMCCOS02 &PMMONE02 +
                &PMTIPO03 &PMCTAA03 &PMVALR03 &PMDECR03 +
                &PMCCOS03 &PMMONE03 &PMTIPO04 &PMCTAA04 +
                &PMVALR04 &PMDECR04 &PMCCOS04 &PMMONE04 +
                &PMPERFIL &MONEDA   &DES001  &DES002  +
                &DES003   &DES004   &TASATM  &CODER   &DESERR)

/* --------------------------- Línea 01 --------------------------- */
/* PMTIPO01 : Tipo de transacción (código/TCODE).                  */
DCL        VAR(&PMTIPO01) TYPE(*DEC)  LEN(2 0)
/* PMCTAA01 : Cuenta contable / financiera (número de cuenta).     */
DCL        VAR(&PMCTAA01) TYPE(*DEC)  LEN(13 0)
/* PMVALR01 : Importe de la línea 01 (19,8).                       */
DCL        VAR(&PMVALR01) TYPE(*DEC)  LEN(19 8)
/* PMDECR01 : Naturaleza 'D' débito / 'C' crédito.                 */
DCL        VAR(&PMDECR01) TYPE(*CHAR) LEN(1)
/* PMCCOS01 : Centro de costo (si aplica).                         */
DCL        VAR(&PMCCOS01) TYPE(*DEC)  LEN(5 0)
/* PMMONE01 : Moneda de la línea (código).                         */
DCL        VAR(&PMMONE01) TYPE(*DEC)  LEN(3 0)

/* --------------------------- Línea 02 --------------------------- */
DCL        VAR(&PMTIPO02) TYPE(*DEC)  LEN(2 0)
DCL        VAR(&PMCTAA02) TYPE(*DEC)  LEN(13 0)
DCL        VAR(&PMVALR02) TYPE(*DEC)  LEN(19 8)
DCL        VAR(&PMDECR02) TYPE(*CHAR) LEN(1)
DCL        VAR(&PMCCOS02) TYPE(*DEC)  LEN(5 0)
DCL        VAR(&PMMONE02) TYPE(*DEC)  LEN(3 0)

/* --------------------------- Línea 03 --------------------------- */
DCL        VAR(&PMTIPO03) TYPE(*DEC)  LEN(2 0)
DCL        VAR(&PMCTAA03) TYPE(*DEC)  LEN(13 0)
DCL        VAR(&PMVALR03) TYPE(*DEC)  LEN(19 8)
DCL        VAR(&PMDECR03) TYPE(*CHAR) LEN(1)
DCL        VAR(&PMCCOS03) TYPE(*DEC)  LEN(5 0)
DCL        VAR(&PMMONE03) TYPE(*DEC)  LEN(3 0)

/* --------------------------- Línea 04 --------------------------- */
DCL        VAR(&PMTIPO04) TYPE(*DEC)  LEN(2 0)
DCL        VAR(&PMCTAA04) TYPE(*DEC)  LEN(13 0)
DCL        VAR(&PMVALR04) TYPE(*DEC)  LEN(19 8)
DCL        VAR(&PMDECR04) TYPE(*CHAR) LEN(1)
DCL        VAR(&PMCCOS04) TYPE(*DEC)  LEN(5 0)
DCL        VAR(&PMMONE04) TYPE(*DEC)  LEN(3 0)

/* -------------------------- Cabecera --------------------------- */
/* PMPERFIL : Perfil Transerver (CFTSKY) usado para el posteo.     */
DCL        VAR(&PMPERFIL) TYPE(*CHAR) LEN(13)
/* MONEDA   : Moneda del asiento (cabecera).                       */
DCL        VAR(&MONEDA)   TYPE(*DEC)  LEN(3 0)
/* DES001..DES004 : Descripciones/leyendas del asiento.            */
DCL        VAR(&DES001)   TYPE(*CHAR) LEN(40)
DCL        VAR(&DES002)   TYPE(*CHAR) LEN(40)
DCL        VAR(&DES003)   TYPE(*CHAR) LEN(40)
DCL        VAR(&DES004)   TYPE(*CHAR) LEN(40)
/* TASATM   : Tasa de cambio / tasa TM (15,9).                     */
DCL        VAR(&TASATM)   TYPE(*DEC)  LEN(15 9)
/* CODER    : Código de error devuelto por el RPG (OUT).           */
DCL        VAR(&CODER)    TYPE(*DEC)  LEN(2 0)
/* DESERR   : Descripción de error devuelta por el RPG (OUT).      */
DCL        VAR(&DESERR)   TYPE(*CHAR) LEN(70)

/* --------------------------- Llamada ---------------------------- */
/* Califica la librería si corresponde: LIB/PGM                    */
/* CALL PGM(BCAH96DTA/APACD764) PARM(...                           */
CALL       PGM(APACD764) PARM(&PMTIPO01 &PMCTAA01 &PMVALR01 &PMDECR01 +
                              &PMCCOS01 &PMMONE01 &PMTIPO02 &PMCTAA02 +
                              &PMVALR02 &PMDECR02 &PMCCOS02 &PMMONE02 +
                              &PMTIPO03 &PMCTAA03 &PMVALR03 &PMDECR03 +
                              &PMCCOS03 &PMMONE03 &PMTIPO04 &PMCTAA04 +
                              &PMVALR04 &PMDECR04 &PMCCOS04 &PMMONE04 +
                              &PMPERFIL &MONEDA   &DES001   &DES002   +
                              &DES003   &DES004   &TASATM   &CODER    &DESERR)

ENDPGM

/* ================================================================ */
/* CLLE: POSRE700                                                  */
/* Descripción: Wrapper para invocar programa RPG APACD767         */
/* Objetivo   : Recibir parámetros desde C# y llamar al RPG con    */
/*              la misma firma; devuelve CODER/DESERR como OUT.     */
/* Notas:                                                          */
/* - Ajusta la librería del CALL si aplica (ej. BCAH96DTA/APACD767)*/
/* - Estructura de parámetros idéntica a POSRE800.                 */
/* ================================================================ */

PGM        PARM(&PMTIPO01 &PMCTAA01 &PMVALR01 &PMDECR01 +
                &PMCCOS01 &PMMONE01 &PMTIPO02 &PMCTAA02 +
                &PMVALR02 &PMDECR02 &PMCCOS02 &PMMONE02 +
                &PMTIPO03 &PMCTAA03 &PMVALR03 &PMDECR03 +
                &PMCCOS03 &PMMONE03 &PMTIPO04 &PMCTAA04 +
                &PMVALR04 &PMDECR04 &PMCCOS04 &PMMONE04 +
                &PMPERFIL &MONEDA   &DES001  &DES002  +
                &DES003   &DES004   &TASATM  &CODER   &DESERR)

/* --------------------------- Línea 01 --------------------------- */
DCL        VAR(&PMTIPO01) TYPE(*DEC)  LEN(2 0)   /* Tipo transacción */
DCL        VAR(&PMCTAA01) TYPE(*DEC)  LEN(13 0)  /* Cuenta           */
DCL        VAR(&PMVALR01) TYPE(*DEC)  LEN(19 8)  /* Importe          */
DCL        VAR(&PMDECR01) TYPE(*CHAR) LEN(1)     /* 'D'/'C'          */
DCL        VAR(&PMCCOS01) TYPE(*DEC)  LEN(5 0)   /* Centro costo     */
DCL        VAR(&PMMONE01) TYPE(*DEC)  LEN(3 0)   /* Moneda línea     */

/* --------------------------- Línea 02 --------------------------- */
DCL        VAR(&PMTIPO02) TYPE(*DEC)  LEN(2 0)
DCL        VAR(&PMCTAA02) TYPE(*DEC)  LEN(13 0)
DCL        VAR(&PMVALR02) TYPE(*DEC)  LEN(19 8)
DCL        VAR(&PMDECR02) TYPE(*CHAR) LEN(1)
DCL        VAR(&PMCCOS02) TYPE(*DEC)  LEN(5 0)
DCL        VAR(&PMMONE02) TYPE(*DEC)  LEN(3 0)

/* --------------------------- Línea 03 --------------------------- */
DCL        VAR(&PMTIPO03) TYPE(*DEC)  LEN(2 0)
DCL        VAR(&PMCTAA03) TYPE(*DEC)  LEN(13 0)
DCL        VAR(&PMVALR03) TYPE(*DEC)  LEN(19 8)
DCL        VAR(&PMDECR03) TYPE(*CHAR) LEN(1)
DCL        VAR(&PMCCOS03) TYPE(*DEC)  LEN(5 0)
DCL        VAR(&PMMONE03) TYPE(*DEC)  LEN(3 0)

/* --------------------------- Línea 04 --------------------------- */
DCL        VAR(&PMTIPO04) TYPE(*DEC)  LEN(2 0)
DCL        VAR(&PMCTAA04) TYPE(*DEC)  LEN(13 0)
DCL        VAR(&PMVALR04) TYPE(*DEC)  LEN(19 8)
DCL        VAR(&PMDECR04) TYPE(*CHAR) LEN(1)
DCL        VAR(&PMCCOS04) TYPE(*DEC)  LEN(5 0)
DCL        VAR(&PMMONE04) TYPE(*DEC)  LEN(3 0)

/* -------------------------- Cabecera --------------------------- */
DCL        VAR(&PMPERFIL) TYPE(*CHAR) LEN(13)    /* Perfil TS       */
DCL        VAR(&MONEDA)   TYPE(*DEC)  LEN(3 0)   /* Moneda cabecera */
DCL        VAR(&DES001)   TYPE(*CHAR) LEN(40)    /* Descripción 1   */
DCL        VAR(&DES002)   TYPE(*CHAR) LEN(40)    /* Descripción 2   */
DCL        VAR(&DES003)   TYPE(*CHAR) LEN(40)    /* Descripción 3   */
DCL        VAR(&DES004)   TYPE(*CHAR) LEN(40)    /* Descripción 4   */
DCL        VAR(&TASATM)   TYPE(*DEC)  LEN(15 9)  /* Tasa TM         */
DCL        VAR(&CODER)    TYPE(*DEC)  LEN(2 0)   /* OUT: código err */
DCL        VAR(&DESERR)   TYPE(*CHAR) LEN(70)    /* OUT: desc. err  */

/* --------------------------- Llamada ---------------------------- */
/* Califica la librería si corresponde: LIB/PGM                    */
/* CALL PGM(BCAH96DTA/APACD767) PARM(...                           */
CALL       PGM(APACD767) PARM(&PMTIPO01 &PMCTAA01 &PMVALR01 &PMDECR01 +
                              &PMCCOS01 &PMMONE01 &PMTIPO02 &PMCTAA02 +
                              &PMVALR02 &PMDECR02 &PMCCOS02 &PMMONE02 +
                              &PMTIPO03 &PMCTAA03 &PMVALR03 &PMDECR03 +
                              &PMCCOS03 &PMMONE03 &PMTIPO04 &PMCTAA04 +
                              &PMVALR04 &PMDECR04 &PMCCOS04 &PMMONE04 +
                              &PMPERFIL &MONEDA   &DES001   &DES002   +
                              &DES003   &DES004   &TASATM   &CODER    &DESERR)

ENDPGM

