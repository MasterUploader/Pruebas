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

El unico valor que no tengo es el ADQNUM, el cual viene de la tabla bcah96dta.ADQCTL;

Acá lo podrias obtener si es false, pero no si true:

// Si es auto-balance, usamos sus GL/CC según naturaleza del cliente
if (enabled)
{
    // Si es auto-balance, usamos sus GL/CC según naturaleza del cliente
    if (naturalezaCliente.Equals("C", StringComparison.OrdinalIgnoreCase))
    {   // Cliente CR → interno DB
        glCuenta = glDebito; glCC = ccDebito;
    }
    else
    {   // Cliente DB → interno CR
        glCuenta = glCredito; glCC = ccCredito;
    }
    fuente = "CFP801";
}
else
{
    var tcodeGL = naturalezaCliente == "C" ? "0784" : "0783";
    if (esEcommerce && TryGetGLFromAdqEctl(codigoComercio, tcodeGL, out var glEc, out var ccEc))
    {
        glCuenta = glEc; glCC = ccEc; fuente = "ADQECTL";
    }
    else if (TryGetGLFromAdqCtl("GL", 1, tcodeGL, out var glG, out var ccG))
    {
        glCuenta = glG; glCC = ccG; fuente = "ADQCTL";
    }
}


valida el codigo RPG, indicame si es un valor que recibe como parametro de entrada o consulta la tabla, si la consulta hay que ver como lo hacemos, porque sigo sin entender ese if else.

