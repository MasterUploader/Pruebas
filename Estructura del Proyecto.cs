private static decimal ToDecCuenta(string? cuenta)
{
    if (string.IsNullOrWhiteSpace(cuenta)) return 0m;
    // Deja solo dígitos
    var digits = new string(cuenta.Where(char.IsDigit).ToArray());
    if (digits.Length == 0) return 0m;

    // INT_LOTES espera 13,0 como máximo; si te pasan más, toma los 13 de la derecha
    if (digits.Length > 13) digits = digits[^13..];

    return decimal.TryParse(digits, NumberStyles.None, CultureInfo.InvariantCulture, out var dec)
        ? dec
        : 0m;
}


// ... luego de resolver auto-balance / ADQECTL / ADQCTL:
var glDec  = ToDecCuenta(glCuenta);         // GL interna (contable)
var cteDec = ToDecCuenta(numeroCuenta);     // cuenta cliente

// seguridad: no mandes 0 al RPG
if (cteDec == 0m)
    return new IntLotesParamsDto { ErrorMetodo = 1, DescripcionError = "Cuenta cliente no es numérica." };
if (glDec == 0m)
    return new IntLotesParamsDto { ErrorMetodo = 1, DescripcionError = "Cuenta GL interna no resuelta o no numérica." };

// Centro de costo para GL: si no viene de tablas, usa 198 (el que tienes en tus ejemplos)
var ccGL = glCC == 0 ? 198 : glCC;

// Descripciones finales (como acordamos)
string al1 = Trunc(nombreComercio, 40);
string al2 = Trunc($"{codigoComercio}-{terminal}", 40);
string al3Base = $"{EtiquetaConcepto(naturalezaCliente)}-{idUnico}-{(infoCta.EsAhorro ? "AHO" : infoCta.EsCheques ? "CHE" : "CTE")}";
string al3Cli  = Trunc(al3Base, 40);
string al3GL   = Trunc($"{al3Base}-{glDec}", 40);

// Construcción según naturaleza (replica tus llamadas que funcionaban)

if (naturalezaCliente == "C")
{
    // Cliente a CR  →  GL a DB
    return new IntLotesParamsDto
    {
        Perfil = perfil,
        Moneda = monedaIsoNum,
        TasaTm = 0m,

        // Mov1 = Débito GL (tipo 40)
        TipoMov1 = 40m,
        CuentaMov1 = glDec,
        DeCr1 = "D",
        CentroCosto1 = ccGL,

        // Mov2 = Crédito Cliente (tipo 1/6)
        TipoMov2 = infoCta.EsAhorro ? 1m : infoCta.EsCheques ? 6m : 1m,
        CuentaMov2 = cteDec,
        DeCr2 = "C",
        CentroCosto2 = 0m,

        // DESDB = GL  /  DESCR = Cliente
        DesDB1 = al1,
        DesDB2 = al2,
        DesDB3 = al3GL,

        DesCR1 = al1,
        DesCR2 = al2,
        DesCR3 = al3Cli,

        EsAutoBalance = auto.enabled,
        FuenteGL = fuente
    };
}
else
{
    // Cliente a DB  →  GL a CR
    return new IntLotesParamsDto
    {
        Perfil = perfil,
        Moneda = monedaIsoNum,
        TasaTm = 0m,

        // Mov1 = Débito Cliente (tipo 1/6)
        TipoMov1 = infoCta.EsAhorro ? 1m : infoCta.EsCheques ? 6m : 1m,
        CuentaMov1 = cteDec,
        DeCr1 = "D",
        CentroCosto1 = 0m,

        // Mov2 = Crédito GL (tipo 40)
        TipoMov2 = 40m,
        CuentaMov2 = glDec,
        DeCr2 = "C",
        CentroCosto2 = ccGL,

        // DESDB = Cliente  /  DESCR = GL
        DesDB1 = al1,
        DesDB2 = al2,
        DesDB3 = Trunc($"{al3Base}-AHO-{cteDec}", 40),   // igual a tu ejemplo “...-AHO-293990015” cuando aplique

        DesCR1 = al1,
        DesCR2 = al2,
        DesCR3 = Trunc($"{al3Base}-{glDec}", 40),

        EsAutoBalance = auto.enabled,
        FuenteGL = fuente
    };
}

