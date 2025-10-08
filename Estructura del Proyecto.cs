La descripcion debe de ser así:
// Descripciones base (40 chars; INT_LOTES espera 40 en DESxx)

string concepto = "VTA";

// Obtener la fecha actual
DateTime fechaActual = DateTime.Now;

// Formatear la fecha como YYYYMMDD
string fechaFormateada = fechaActual.ToString("yyyyMMdd");

string desDb1 = Trunc("Total Neto Db liquidacion come", 40);
string desCr1 = Trunc("Total Neto Cr liquidacion come", 40);

string descDb2 = Trunc($"{codigoComercio}        -{fechaFormateada}-{tipoCliente}-{numeroCuenta}", 40);
string descCr2 = Trunc($"{codigoComercio}        -{fechaFormateada}", 40);

string descDb3 = Trunc($"&{concepto}&ADQNUM    Db Net.Liq1  ||", 40);
string descCr3 = Trunc($"&{concepto}&ADQNUM    Cr Net.Liq2  ||", 40);

El unico valor que no tengo es el ADQNUM, el cual viene de la tabla bcah96dta.ADQCTL 
