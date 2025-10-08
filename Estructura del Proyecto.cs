/// <summary>
/// Resuelve todos los parámetros necesarios para INT_LOTES:
/// - Calcula t-codes, obtiene ADQNUM y, si aplica, GL/CC en UNA sola consulta (ADQECTL o ADQCTL).
/// - Aplica auto-balance (CFP801) si está activo para el perfil.
/// - Arma las descripciones EXACTAS como en el RPG (40 chars).
/// - Construye ambos movimientos: Mov1 = Débito, Mov2 = Crédito, según la naturaleza del cliente.
/// </summary>
private IntLotesParamsDto ResolverParametrosIntLotes(
    bool esEcommerce,
    string perfil,
    string naturalezaCliente, // 'C' (acreditamos cliente) o 'D' (debitamos cliente)
    string numeroCuenta,      // cuenta del cliente
    string codigoComercio,
    string terminal,
    string nombreComercio,
    string idUnico,
    int monedaIsoNum = 0
)
{
    var dto = new IntLotesParamsDto
    {
        Perfil = perfil,
        Moneda = monedaIsoNum,
        TasaTm = 0m,
        ErrorMetodo = 0
    };

    // ---------------------------------------------------------
    // 1) Determinar t-codes (cliente vs GL) según naturaleza
    // ---------------------------------------------------------
    string tcodeCliente = naturalezaCliente.Equals("C", StringComparison.OrdinalIgnoreCase) ? "0783" : "0784";
    string tcodeGL      = naturalezaCliente.Equals("C", StringComparison.OrdinalIgnoreCase) ? "0784" : "0783";

    // ---------------------------------------------------------
    // 2) Obtener ADQNUM + (GL/CC) en UNA consulta (control EC/GL)
    // ---------------------------------------------------------
    var (adqNumCtl, glCtl, ccCtl) = ObtenerAdqNumYGL(esEcommerce, tcodeGL);

    // ---------------------------------------------------------
    // 3) Auto-balance (CFP801) o control (ADQECTL/ADQCTL)
    // ---------------------------------------------------------
    var (enabled, glDeb, ccDeb, glCre, ccCre) = TryGetAutoBalance(perfil);
    string? glCuenta = null;
    int glCC = 0;
    dto.EsAutoBalance = enabled;

    if (enabled)
    {
        // Cliente 'C' → interno debe ir a Débito; Cliente 'D' → interno a Crédito
        if (naturalezaCliente.Equals("C", StringComparison.OrdinalIgnoreCase))
        {
            glCuenta = glDeb;  glCC = ccDeb;  dto.FuenteGL = "CFP801";
        }
        else
        {
            glCuenta = glCre;  glCC = ccCre;  dto.FuenteGL = "CFP801";
        }
    }
    else
    {
        glCuenta = glCtl; glCC = ccCtl; dto.FuenteGL = esEcommerce ? "ADQECTL" : "ADQCTL";
    }

    // ---------------------------------------------------------
    // 4) Tipo de cuenta del cliente (Ahorros/Cheques) vía Ver_cta
    // ---------------------------------------------------------
    var infoCta = VerCta(numeroCuenta);
    decimal tipoClienteDec = infoCta.EsAhorro ? 1 : infoCta.EsCheques ? 6 : 40;
    string tipoClienteTxt  = infoCta.EsAhorro ? "AHO" : infoCta.EsCheques ? "CHE" : "CTE";

    // ---------------------------------------------------------
    // 5) Tasa (si tu RPG la usa). De lo contrario, queda 0.
    // ---------------------------------------------------------
    try { dto.TasaTm = ObtenerTasaCompraUsd(); } catch { dto.TasaTm = 0m; }

    // ---------------------------------------------------------
    // 6) Descripciones EXACTAS del RPG (40 chars; usa ADQNUM)
    // ---------------------------------------------------------
    string concepto = "VTA";
    string fechaFormateada = DateTime.Now.ToString("yyyyMMdd");
    string ochoEspacios = "        ";
    string adqNumPadded = (adqNumCtl ?? "0").PadRight(10); // anchura usada por el RPG

    string desDb1  = Trunc("Total Neto Db liquidacion come", 40);
    string desCr1  = Trunc("Total Neto Cr liquidacion come", 40);
    string descDb2 = Trunc($"{codigoComercio}{ochoEspacios}-{fechaFormateada}-{tipoClienteTxt}-{numeroCuenta}", 40);
    string descCr2 = Trunc($"{codigoComercio}{ochoEspacios}-{fechaFormateada}", 40);
    string descDb3 = Trunc($"&{concepto}&{adqNumPadded}Db Net.Liq1  ||", 40);
    string descCr3 = Trunc($"&{concepto}&{adqNumPadded}Cr Net.Liq2  ||", 40);

    // ---------------------------------------------------------
    // 7) Construir ambos movimientos (Mov1 = Débito, Mov2 = Crédito)
    //    Reglas:
    //    - Si naturalezaCliente = 'C' (acreditamos cliente):
    //        Mov1: Débito interno (GL)
    //        Mov2: Crédito cliente
    //    - Si naturalezaCliente = 'D' (debitamos cliente):
    //        Mov1: Débito cliente
    //        Mov2: Crédito interno (GL)
    // ---------------------------------------------------------
    static decimal ToDecOrZero(string? s)
        => decimal.TryParse((s ?? "").Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out var d) ? d : 0m;

    if (naturalezaCliente.Equals("C", StringComparison.OrdinalIgnoreCase))
    {
        // Mov1 = DB interno (GL)
        dto.TipoMov1     = 40;
        dto.CuentaMov1   = ToDecOrZero(glCuenta);
        dto.DeCr1        = "D";
        dto.CentroCosto1 = glCC;

        // Mov2 = CR cliente
        dto.TipoMov2     = tipoClienteDec;
        dto.CuentaMov2   = ToDecOrZero(numeroCuenta);
        dto.DeCr2        = "C";
        dto.CentroCosto2 = 0;

        // Descripciones: DESDB* (lado debitado = interno), DESCR* (lado acreditado = cliente)
        dto.DesDB1 = desDb1;  dto.DesDB2 = descDb2; dto.DesDB3 = descDb3;
        dto.DesCR1 = desCr1;  dto.DesCR2 = descCr2; dto.DesCR3 = descCr3;
    }
    else
    {
        // Mov1 = DB cliente
        dto.TipoMov1     = tipoClienteDec;
        dto.CuentaMov1   = ToDecOrZero(numeroCuenta);
        dto.DeCr1        = "D";
        dto.CentroCosto1 = 0;

        // Mov2 = CR interno (GL)
        dto.TipoMov2     = 40;
        dto.CuentaMov2   = ToDecOrZero(glCuenta);
        dto.DeCr2        = "C";
        dto.CentroCosto2 = glCC;

        // Descripciones: DESDB* (lado debitado = cliente), DESCR* (lado acreditado = interno)
        dto.DesDB1 = desDb1;  dto.DesDB2 = descDb2; dto.DesDB3 = descDb3;
        dto.DesCR1 = desCr1;  dto.DesCR2 = descCr2; dto.DesCR3 = descCr3;
    }

    // ---------------------------------------------------------
    // 8) Validación mínima: GL obligatoria para el lado interno
    // ---------------------------------------------------------
    bool glFaltante =
        (naturalezaCliente.Equals("C", StringComparison.OrdinalIgnoreCase) && dto.CuentaMov1 == 0m)
     || (naturalezaCliente.Equals("D", StringComparison.OrdinalIgnoreCase) && dto.CuentaMov2 == 0m);

    if (glFaltante)
    {
        dto.ErrorMetodo = 1;
        dto.DescripcionError = $"No se encontró cuenta interna (GL) para tcode {tcodeGL} en {(dto.FuenteGL == "CFP801" ? "CFP801" : (esEcommerce ? "ADQECTL" : "ADQCTL"))}.";
    }

    return dto;
}
