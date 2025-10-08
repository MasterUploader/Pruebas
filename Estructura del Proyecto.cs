// Normaliza cuentas numéricas para usarlas en los returns
decimal glAsDec = 0m;
_ = !string.IsNullOrWhiteSpace(glCuenta) && decimal.TryParse(glCuenta, out glAsDec);

decimal cteAsDec = 0m;
_ = decimal.TryParse(numeroCuenta, out cteAsDec);


// --- Caso naturalezaCliente == "C": Débito GL / Crédito Cliente ---
return new IntLotesParamsDto
{
    Perfil = perfil,
    Moneda = monedaIsoNum,
    TasaTm = 0m,

    // Mov1 = Débito GL
    TipoMov1   = tipoGL,
    CuentaMov1 = glAsDec,            // ← ya es 0 si no pudo parsearse
    DeCr1      = "D",
    CentroCosto1 = glCC,
    MontoMov1  = monto,
    MonedaMov1 = monedaIsoNum,

    // Mov2 = Crédito Cliente
    TipoMov2   = tipoCliente,
    CuentaMov2 = cteAsDec,           // ← ya es 0 si no pudo parsearse
    DeCr2      = "C",
    CentroCosto2 = 0m,
    MontoMov2  = monto,
    MonedaMov2 = monedaIsoNum,

    // Descripciones…
    DesDB1 = desBase1,
    DesDB2 = desBase2,
    DesDB3 = Trunc($"{desBase3}-{glCuenta}", 40),

    DesCR1 = desBase1,
    DesCR2 = desBase2,
    DesCR3 = desBase3,

    NaturalezaCliente = naturalezaCliente,
    NaturalezaGL = 'D',
    TcodeCliente = "0783",
    TcodeGL = "0784",
    EsAutoBalance = auto.enabled,
    FuenteGL = fuente,
    CuentaClienteOriginal = numeroCuenta,
    CuentaGLResuelta = glCuenta ?? string.Empty
};


// --- Caso naturalezaCliente == "D": Débito Cliente / Crédito GL ---
return new IntLotesParamsDto
{
    Perfil = perfil,
    Moneda = monedaIsoNum,
    TasaTm = 0m,

    // Mov1 = Débito Cliente
    TipoMov1   = tipoCliente,
    CuentaMov1 = cteAsDec,     // ← ya seguro
    DeCr1      = "D",
    CentroCosto1 = 0m,
    MontoMov1  = monto,
    MonedaMov1 = monedaIsoNum,

    // Mov2 = Crédito GL
    TipoMov2   = tipoGL,
    CuentaMov2 = glAsDec,      // ← ya seguro
    DeCr2      = "C",
    CentroCosto2 = glCC,
    MontoMov2  = monto,
    MonedaMov2 = monedaIsoNum,

    // Descripciones…
    DesDB1 = desBase1,
    DesDB2 = desBase2,
    DesDB3 = desBase3,

    DesCR1 = desBase1,
    DesCR2 = desBase2,
    DesCR3 = Trunc($"{desBase3}-{glCuenta}", 40),

    NaturalezaCliente = naturalezaCliente,
    NaturalezaGL = 'C',
    TcodeCliente = "0784",
    TcodeGL = "0783",
    EsAutoBalance = auto.enabled,
    FuenteGL = fuente,
    CuentaClienteOriginal = numeroCuenta,
    CuentaGLResuelta = glCuenta ?? string.Empty
};
