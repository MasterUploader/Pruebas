/* ================================================================ */
/* CLLE: CLAPACD764                                                 */
/* Descripción: Wrapper para invocar programa RPG APACD764           */
/* Objetivo: Permitir que C# llame un CL y este ejecute el RPG con   */
/*           los parámetros requeridos.                              */
/* ================================================================ */

PGM        PARM(&PMTIPO01 &PMCTAA01 &PMVALR01 &PMDECR01 +
                &PMCCOS01 &PMMONE01 &PMTIPO02 &PMCTAA02 +
                &PMVALR02 &PMDECR02 &PMCCOS02 &PMMONE02 +
                &PMTIPO03 &PMCTAA03 &PMVALR03 &PMDECR03 +
                &PMCCOS03 &PMMONE03 &PMTIPO04 &PMCTAA04 +
                &PMVALR04 &PMDECR04 &PMCCOS04 &PMMONE04 +
                &PMPERFIL &MONEDA &DES001 &DES002 +
                &DES003 &DES004 &TASATM &CODER &DESERR)

/* Declaración de parámetros (ajustar longitudes al RPG original) */
DCL VAR(&PMTIPO01) TYPE(*DEC) LEN(2 0)
DCL VAR(&PMCTAA01) TYPE(*DEC) LEN(13 0)
DCL VAR(&PMVALR01) TYPE(*DEC) LEN(19 8)
DCL VAR(&PMDECR01) TYPE(*CHAR) LEN(1)
DCL VAR(&PMCCOS01) TYPE(*DEC) LEN(5 0)
DCL VAR(&PMMONE01) TYPE(*DEC) LEN(3 0)
/* ... repetir igual para los otros parámetros ... */
DCL VAR(&PMPERFIL) TYPE(*CHAR) LEN(13)
DCL VAR(&MONEDA)   TYPE(*DEC) LEN(3 0)
DCL VAR(&DES001)   TYPE(*CHAR) LEN(40)
DCL VAR(&DES002)   TYPE(*CHAR) LEN(40)
DCL VAR(&DES003)   TYPE(*CHAR) LEN(40)
DCL VAR(&DES004)   TYPE(*CHAR) LEN(40)
DCL VAR(&TASATM)   TYPE(*DEC) LEN(15 9)
DCL VAR(&CODER)    TYPE(*DEC) LEN(2 0)
DCL VAR(&DESERR)   TYPE(*CHAR) LEN(70)

/* Llamada al RPG */
CALL PGM(APACD764) PARM(&PMTIPO01 &PMCTAA01 &PMVALR01 &PMDECR01 +
                        &PMCCOS01 &PMMONE01 &PMTIPO02 &PMCTAA02 +
                        &PMVALR02 &PMDECR02 &PMCCOS02 &PMMONE02 +
                        &PMTIPO03 &PMCTAA03 &PMVALR03 &PMDECR03 +
                        &PMCCOS03 &PMMONE03 &PMTIPO04 &PMCTAA04 +
                        &PMVALR04 &PMDECR04 &PMCCOS04 &PMMONE04 +
                        &PMPERFIL &MONEDA &DES001 &DES002 +
                        &DES003 &DES004 &TASATM &CODER &DESERR)

ENDPGM


/* ================================================================ */
/* CLLE: CLAPACD767                                                 */
/* Descripción: Wrapper para invocar programa RPG APACD767           */
/* Objetivo: Permitir que C# llame un CL y este ejecute el RPG con   */
/*           los parámetros requeridos.                              */
/* ================================================================ */

PGM        PARM(&PMTIPO01 &PMCTAA01 &PMVALR01 &PMDECR01 +
                &PMCCOS01 &PMMONE01 &PMTIPO02 &PMCTAA02 +
                &PMVALR02 &PMDECR02 &PMCCOS02 &PMMONE02 +
                &PMTIPO03 &PMCTAA03 &PMVALR03 &PMDECR03 +
                &PMCCOS03 &PMMONE03 &PMTIPO04 &PMCTAA04 +
                &PMVALR04 &PMDECR04 &PMCCOS04 &PMMONE04 +
                &PMPERFIL &MONEDA &DES001 &DES002 +
                &DES003 &DES004 &TASATM &CODER &DESERR)

/* Declaración de parámetros (igual que en CL anterior) */
DCL VAR(&PMTIPO01) TYPE(*DEC) LEN(2 0)
DCL VAR(&PMCTAA01) TYPE(*DEC) LEN(13 0)
DCL VAR(&PMVALR01) TYPE(*DEC) LEN(19 8)
DCL VAR(&PMDECR01) TYPE(*CHAR) LEN(1)
DCL VAR(&PMCCOS01) TYPE(*DEC) LEN(5 0)
DCL VAR(&PMMONE01) TYPE(*DEC) LEN(3 0)
/* ... repetir igual para los otros parámetros ... */
DCL VAR(&PMPERFIL) TYPE(*CHAR) LEN(13)
DCL VAR(&MONEDA)   TYPE(*DEC) LEN(3 0)
DCL VAR(&DES001)   TYPE(*CHAR) LEN(40)
DCL VAR(&DES002)   TYPE(*CHAR) LEN(40)
DCL VAR(&DES003)   TYPE(*CHAR) LEN(40)
DCL VAR(&DES004)   TYPE(*CHAR) LEN(40)
DCL VAR(&TASATM)   TYPE(*DEC) LEN(15 9)
DCL VAR(&CODER)    TYPE(*DEC) LEN(2 0)
DCL VAR(&DESERR)   TYPE(*CHAR) LEN(70)

/* Llamada al RPG */
CALL PGM(APACD767) PARM(&PMTIPO01 &PMCTAA01 &PMVALR01 &PMDECR01 +
                        &PMCCOS01 &PMMONE01 &PMTIPO02 &PMCTAA02 +
                        &PMVALR02 &PMDECR02 &PMCCOS02 &PMMONE02 +
                        &PMTIPO03 &PMCTAA03 &PMVALR03 &PMDECR03 +
                        &PMCCOS03 &PMMONE03 &PMTIPO04 &PMCTAA04 +
                        &PMVALR04 &PMDECR04 &PMCCOS04 &PMMONE04 +
                        &PMPERFIL &MONEDA &DES001 &DES002 +
                        &DES003 &DES004 &TASATM &CODER &DESERR)

ENDPGM
