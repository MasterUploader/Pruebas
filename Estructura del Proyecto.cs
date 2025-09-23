-- BCAH96.INT_LOTES: firma alineada al *ENTRY PLIST de INT_LOTES
CREATE OR REPLACE PROCEDURE BCAH96.INT_LOTES (
    IN PMTIPO01  DECIMAL(2,0),
    IN PMCTAA01  DECIMAL(13,0),
    IN PMVALR01  DECIMAL(13,2),
    IN PMDECR01  CHAR(1),
    IN PMCCOS01  DECIMAL(5,0),
    IN PMMONE01  DECIMAL(3,0),

    IN PMTIPO02  DECIMAL(2,0),
    IN PMCTAA02  DECIMAL(13,0),
    IN PMVALR02  DECIMAL(13,2),
    IN PMDECR02  CHAR(1),
    IN PMCCOS02  DECIMAL(5,0),
    IN PMMONE02  DECIMAL(3,0),

    IN PMTIPO03  DECIMAL(2,0),
    IN PMCTAA03  DECIMAL(13,0),
    IN PMVALR03  DECIMAL(13,2),
    IN PMDECR03  CHAR(1),
    IN PMCCOS03  DECIMAL(5,0),
    IN PMMONE03  DECIMAL(3,0),

    IN PMTIPO04  DECIMAL(2,0),
    IN PMCTAA04  DECIMAL(13,0),
    IN PMVALR04  DECIMAL(13,2),
    IN PMDECR04  CHAR(1),
    IN PMCCOS04  DECIMAL(5,0),
    IN PMMONE04  DECIMAL(3,0),

    IN PMPERFIL  CHAR(13),
    IN MONEDA    DECIMAL(3,0),

    IN DESDB1    CHAR(40),
    IN DESDB2    CHAR(40),
    IN DESDB3    CHAR(40),

    IN DESCR1    CHAR(40),
    IN DESCR2    CHAR(40),
    IN DESCR3    CHAR(40),

    OUT CODER    DECIMAL(2,0),
    OUT DESERR   CHAR(70),
    OUT NOMARC   CHAR(10)
)
LANGUAGE RPGLE
SPECIFIC BCAH96.INT_LOTES
DETERMINISTIC
MODIFIES SQL DATA
CALLED ON NULL INPUT
EXTERNAL NAME 'BCAH96/INT_LOTES'
PARAMETER STYLE GENERAL;



// Monto movimiento 1
builder.InDecimal("PMVALR01", naturalezaContable.Contains('D') ? monto : 0m, precision: 13, scale: 2);
// ...
builder.InDecimal("PMVALR02", naturalezaContable.Contains('C') ? monto : 0m, precision: 13, scale: 2);
// ...
builder.InDecimal("PMVALR03", 0m, precision: 13, scale: 2);
builder.InDecimal("PMVALR04", 0m, precision: 13, scale: 2);

// OUTs
builder.OutDecimal("CODER", 2, 0);
builder.OutChar("DESERR", 70);
builder.OutChar("NomArc", 10);


