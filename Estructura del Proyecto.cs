No, es más o menos así, este es el código actual y en los puntos que modificare

// ===================== Movimiento 1 =====================
builder.InDecimal("PMTIPO01", tipoCuenta, precision: 2, scale: 0); //Tipo de Cuenta 1-ahorros/6-cheques/40-Contable = PMTIPO01
builder.InDecimal("PMCTAA01", naturalezaContable.Contains('D') ? numeroCuenta : 0, precision: 13, scale: 0); //Numero de cuenta a Debitar = PMCTAA01
builder.InDecimal("PMVALR01", naturalezaContable.Contains('D') ? monto : 0m, precision: 13, scale: 2); //Valor segun moneda (lps=lps, Usd=Usd Eur=Eur)
builder.InChar("PMDECR01", "D", 1); //Tipo de movimiento C=Credito D=Debito
builder.InDecimal("PMCCOS01", 0, precision: 5, scale: 0); //Centro de costos
builder.InDecimal("PMMONE01", moneda, precision: 3, scale: 0); //Moneda del movimiento

// ===================== Movimiento 2 =====================
builder.InDecimal("PMTIPO02", naturalezaContable.Contains('C') ? tipoCuenta : 0, precision: 2, scale: 0);
builder.InDecimal("PMCTAA02", naturalezaContable.Contains('C') ? numeroCuenta : 0, precision: 13, scale: 0); //Numero de cuenta a Acreditar = PMCTAA02
builder.InDecimal("PMVALR02", naturalezaContable.Contains('C') ? monto : 0m, precision: 13, scale: 2);
builder.InChar("PMDECR02", "C", 1);
builder.InDecimal("PMCCOS02", naturalezaContable.Contains('C') ? centroCosto : 0, precision: 5, scale: 0);
builder.InDecimal("PMMONE02", moneda, precision: 3, scale: 0); //Moneda del movimiento

El resto sigue igual


// ===================== Descripciones Nuevas =====================
builder.InChar("DESDB1", naturalezaContable.Contains('D') ? descripcion1 : "", 40); //Descripción 1
builder.InChar("DESDB2", naturalezaContable.Contains('D') ? descripcion2 : "", 40); //Descripción 2
builder.InChar("DESDB3", naturalezaContable.Contains('D') ? descripcion3 : "", 40); //Descripción 3

// ===================== Descripciones Originales =====================
builder.InChar("DESCR1", naturalezaContable.Contains('C') ? descripcion1 : "", 40); //Descripción 1
builder.InChar("DESCR2", naturalezaContable.Contains('C') ? descripcion2 : "", 40); //Descripción 2
builder.InChar("DESCR3", naturalezaContable.Contains('C') ? descripcion3 : "", 40); //Descripción 3

1. Entonces, si en la petición viene naturalezaContable "C", quiere decir que al cliente le acreditaremos, y a lo interno debitaremos.
Para este caso el movimiento 1 debe llevar la información de la cuenta a la que debitaremos, la interna, y en el movimiento 2 la información de la cuenta a la que acreditaremos, la del cliente.

2. Si, en la petición viene naturalezaContable "D", quiere decir que al cliente le debitaremos, y a lo interno acreditaremos.
Para este caso el movimiento 1 debe llevar la información de la cuenta que debitaremos, la del cliente, y en el movimiento 2 la información de la cuenta que acreditaremos, la interna.

>Para la descripción en la nueva, irían la descripciones de las cuenta a debitar, y en descripciones originales la descripción de la cuenta a acreditar, según corresponda el caso.

La adaptación debe ser que pueda colocar la información como corresponde asumiendo que en IntLotesParamsDto ya viene.
