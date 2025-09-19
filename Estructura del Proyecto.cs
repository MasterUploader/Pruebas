CREATE OR REPLACE PROCEDURE BCAH96DTA.POSRE800
(
    -- ================== Movimiento 1 ==================
    IN  PMTIPO01  DECIMAL(2,0),
    IN  PMCTAA01  DECIMAL(13,0),
    IN  PMVALR01  DECIMAL(19,8),
    IN  PMDECR01  CHAR(1),
    IN  PMCCOS01  DECIMAL(5,0),
    IN  PMMONE01  DECIMAL(3,0),

    -- ================== Movimiento 2 ==================
    IN  PMTIPO02  DECIMAL(2,0),
    IN  PMCTAA02  DECIMAL(13,0),
    IN  PMVALR02  DECIMAL(19,8),
    IN  PMDECR02  CHAR(1),
    IN  PMCCOS02  DECIMAL(5,0),
    IN  PMMONE02  DECIMAL(3,0),

    -- ================== Movimiento 3 ==================
    IN  PMTIPO03  DECIMAL(2,0),
    IN  PMCTAA03  DECIMAL(13,0),
    IN  PMVALR03  DECIMAL(19,8),
    IN  PMDECR03  CHAR(1),
    IN  PMCCOS03  DECIMAL(5,0),
    IN  PMMONE03  DECIMAL(3,0),

    -- ================== Movimiento 4 ==================
    IN  PMTIPO04  DECIMAL(2,0),
    IN  PMCTAA04  DECIMAL(13,0),
    IN  PMVALR04  DECIMAL(19,8),
    IN  PMDECR04  CHAR(1),
    IN  PMCCOS04  DECIMAL(5,0),
    IN  PMMONE04  DECIMAL(3,0),

    -- ================== Generales ==================
    IN  PMPERFIL  CHAR(13),
    IN  MONEDA    DECIMAL(3,0),
    IN  DES001    CHAR(40),
    IN  DES002    CHAR(40),
    IN  DES003    CHAR(40),
    IN  DES004    CHAR(40),
    IN  TASATM    DECIMAL(15,9),

    -- ================== OUT ==================
    OUT CODER     DECIMAL(2,0),
    OUT DESERR    CHAR(70)
)
LANGUAGE RPGLE
PARAMETER STYLE GENERAL
DETERMINISTIC
EXTERNAL NAME 'BCAH96DTA/APACD764';



CREATE OR REPLACE PROCEDURE BCAH96DTA.POSRE700
(
    -- ================== Movimiento 1 ==================
    IN  PMTIPO01  DECIMAL(2,0),
    IN  PMCTAA01  DECIMAL(13,0),
    IN  PMVALR01  DECIMAL(19,8),
    IN  PMDECR01  CHAR(1),
    IN  PMCCOS01  DECIMAL(5,0),
    IN  PMMONE01  DECIMAL(3,0),

    -- ================== Movimiento 2 ==================
    IN  PMTIPO02  DECIMAL(2,0),
    IN  PMCTAA02  DECIMAL(13,0),
    IN  PMVALR02  DECIMAL(19,8),
    IN  PMDECR02  CHAR(1),
    IN  PMCCOS02  DECIMAL(5,0),
    IN  PMMONE02  DECIMAL(3,0),

    -- ================== Movimiento 3 ==================
    IN  PMTIPO03  DECIMAL(2,0),
    IN  PMCTAA03  DECIMAL(13,0),
    IN  PMVALR03  DECIMAL(19,8),
    IN  PMDECR03  CHAR(1),
    IN  PMCCOS03  DECIMAL(5,0),
    IN  PMMONE03  DECIMAL(3,0),

    -- ================== Movimiento 4 ==================
    IN  PMTIPO04  DECIMAL(2,0),
    IN  PMCTAA04  DECIMAL(13,0),
    IN  PMVALR04  DECIMAL(19,8),
    IN  PMDECR04  CHAR(1),
    IN  PMCCOS04  DECIMAL(5,0),
    IN  PMMONE04  DECIMAL(3,0),

    -- ================== Generales ==================
    IN  PMPERFIL  CHAR(13),
    IN  MONEDA    DECIMAL(3,0),
    IN  DES001    CHAR(40),
    IN  DES002    CHAR(40),
    IN  DES003    CHAR(40),
    IN  DES004    CHAR(40),
    IN  TASATM    DECIMAL(15,9),

    -- ================== OUT ==================
    OUT CODER     DECIMAL(2,0),
    OUT DESERR    CHAR(70)
)
LANGUAGE RPGLE
PARAMETER STYLE GENERAL
DETERMINISTIC
EXTERNAL NAME 'BCAH96DTA/APACD767';
