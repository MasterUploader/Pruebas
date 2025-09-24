/// <summary>
/// Parámetros completos para llamar a INT_LOTES:
/// - Arma ambos movimientos (Débito y Crédito) con cuenta, tipo y centro de costo.
/// - Incluye descripciones separadas para el lado debitado (DESDBx) y el acreditado (DESCRx).
/// - Trae perfil, moneda y tasa.
/// </summary>
public sealed class IntLotesParamsDto
{
    /// <summary>Perfil Transerver (CFTSKY).</summary>
    public string Perfil { get; set; } = string.Empty;

    /// <summary>Código numérico de moneda (ej. 340=LPS, 840=USD si aplica tu core; sino 0).</summary>
    public decimal Moneda { get; set; }

    /// <summary>Tasa TM (si tu RPG la usa; si no, 0).</summary>
    public decimal TasaTm { get; set; }

    // ------------------------- Movimiento 1 (DEBITADO) -------------------------
    /// <summary>Tipo de cuenta del movimiento 1 (1=ahorros, 6=cheques, 40=contable).</summary>
    public decimal TipoMov1 { get; set; }
    /// <summary>Número de cuenta del movimiento 1.</summary>
    public decimal CuentaMov1 { get; set; }
    /// <summary>Naturaleza del movimiento 1: 'D' o 'C' (aquí siempre será Débito).</summary>
    public char DeCr1 { get; set; } = 'D';
    /// <summary>Centro de costo del movimiento 1.</summary>
    public decimal CentroCosto1 { get; set; }

    // ------------------------- Movimiento 2 (ACREDITADO) -----------------------
    /// <summary>Tipo de cuenta del movimiento 2 (1=ahorros, 6=cheques, 40=contable).</summary>
    public decimal TipoMov2 { get; set; }
    /// <summary>Número de cuenta del movimiento 2.</summary>
    public decimal CuentaMov2 { get; set; }
    /// <summary>Naturaleza del movimiento 2: 'D' o 'C' (aquí siempre será Crédito).</summary>
    public char DeCr2 { get; set; } = 'C';
    /// <summary>Centro de costo del movimiento 2.</summary>
    public decimal CentroCosto2 { get; set; }

    // ------------------------- Descripciones por lado --------------------------
    /// <summary>Descripción 1 del lado DEBITADO (DESDB1).</summary>
    public string DesDB1 { get; set; } = string.Empty;
    /// <summary>Descripción 2 del lado DEBITADO (DESDB2).</summary>
    public string DesDB2 { get; set; } = string.Empty;
    /// <summary>Descripción 3 del lado DEBITADO (DESDB3).</summary>
    public string DesDB3 { get; set; } = string.Empty;

    /// <summary>Descripción 1 del lado ACREDITADO (DESCR1).</summary>
    public string DesCR1 { get; set; } = string.Empty;
    /// <summary>Descripción 2 del lado ACREDITADO (DESCR2).</summary>
    public string DesCR2 { get; set; } = string.Empty;
    /// <summary>Descripción 3 del lado ACREDITADO (DESCR3).</summary>
    public string DesCR3 { get; set; } = string.Empty;

    // ------------------------- Diagnóstico/Info opcional -----------------------
    /// <summary>Indica si se obtuvo contrapartida por auto-balance (CFP801).</summary>
    public bool EsAutoBalance { get; set; }
    /// <summary>Fuente usada para resolver GL/CC (CFP801 / ADQECTL / ADQCTL / N/A).</summary>
    public string FuenteGL { get; set; } = string.Empty;
}




/// <summary>
/// Resuelve contrapartida interna (GL+CC), tipo de cuenta de cliente (Ahorro/Cheques),
/// naturalezas y descripciones ya mapeadas a los dos lados (Débito/Crédito),
/// para llamar directamente a INT_LOTES.
/// 
/// Regla:
/// - Si <paramref name="naturalezaCliente"/> = "C": al cliente se le acredita, internamente se debita.
///   → Mov1 = Interno (D) / Mov2 = Cliente (C)
/// - Si <paramref name="naturalezaCliente"/> = "D": al cliente se le debita, internamente se acredita.
///   → Mov1 = Cliente (D) / Mov2 = Interno (C)
/// </summary>
private IntLotesParamsDto ResolverParametrosIntLotes(
    bool esEcommerce,
    string perfil,
    string naturalezaCliente,          // 'C' o 'D'
    string numeroCuentaCliente,        // cuenta del comercio/cliente
    string codigoComercio,             // para AL2
    string terminal,                   // para AL2 / tipo e-commerce
    string nombreComercio,             // AL1
    string idUnico,                    // AL3
    int monedaIsoNum = 0,              // 0 si tu RPG no lo usa
    int centroCostoCliente = 0         // CC para lado cliente si aplica (suele ser 0)
)
{
    // 1) Tipo de cuenta del cliente (Ahorros/Cheques) via VerCta(TAP002)
    var infoCta = VerCta(numeroCuentaCliente);
    var tipoCliente = infoCta.EsAhorro ? 1m : infoCta.EsCheques ? 6m : 40m;

    // 2) Contrapartida GL y CC:
    var auto = TryGetAutoBalance(perfil);
    string? glCuentaTxt = null;
    int glCC = 0;
    string fuente = "N/A";

    if (auto.enabled)
    {
        if (naturalezaCliente.Equals("C", StringComparison.OrdinalIgnoreCase))
        {   // Cliente CR → interno DB
            glCuentaTxt = auto.glDebito;   glCC = auto.ccDebito;  fuente = "CFP801";
        }
        else
        {   // Cliente DB → interno CR
            glCuentaTxt = auto.glCredito;  glCC = auto.ccCredito; fuente = "CFP801";
        }
    }
    else
    {
        var tcodeGL = naturalezaCliente.Equals("C", StringComparison.OrdinalIgnoreCase) ? "0784" : "0783";
        if (esEcommerce && TryGetGLFromAdqEctl(codigoComercio, tcodeGL, out var glEc, out var ccEc))
        { glCuentaTxt = glEc; glCC = ccEc; fuente = "ADQECTL"; }
        else if (TryGetGLFromAdqCtl("GL", 1, tcodeGL, out var glG, out var ccG))
        { glCuentaTxt = glG; glCC = ccG; fuente = "ADQCTL"; }
    }

    // Si no hay GL legible, úsala como 0 (el RPG puede rechazar si es obligatorio)
    if (!decimal.TryParse(glCuentaTxt ?? "0", NumberStyles.Any, CultureInfo.InvariantCulture, out var glCuentaNum))
        glCuentaNum = 0m;

    // 3) Descripciones (ALx), separadas por lado:
    string al1 = Trunc(nombreComercio, 40);
    string al2 = Trunc($"{codigoComercio}-{terminal}", 40);
    var tag = EtiquetaConcepto(naturalezaCliente);
    string tipoCorto = string.IsNullOrWhiteSpace(infoCta.DescCorta) ? "" : infoCta.DescCorta.Trim();
    string al3Base = Trunc($"{tag}-{idUnico}-{tipoCorto}", 40);

    // Si conocemos GL, agregar al final de la tercera leyenda para trazabilidad
    string al3ConGL = string.IsNullOrWhiteSpace(glCuentaTxt) ? al3Base
                        : Trunc($"{al3Base}-{glCuentaTxt}", 40);

    // 4) Armar DTO final (ambos movimientos fijados)
    var dto = new IntLotesParamsDto
    {
        Perfil = perfil,
        Moneda = monedaIsoNum,
        TasaTm = SafeObtenerTasa(),

        EsAutoBalance = auto.enabled,
        FuenteGL = fuente
    };

    bool clienteEsCredito = naturalezaCliente.Equals("C", StringComparison.OrdinalIgnoreCase);

    if (clienteEsCredito)
    {
        // Mov1 = Interno (Débito)
        dto.TipoMov1     = 40m;
        dto.CuentaMov1   = glCuentaNum;
        dto.DeCr1        = 'D';
        dto.CentroCosto1 = glCC;

        // Mov2 = Cliente (Crédito)
        dto.TipoMov2     = tipoCliente;
        dto.CuentaMov2   = decimal.TryParse(numeroCuentaCliente, out var ctaCli) ? ctaCli : 0m;
        dto.DeCr2        = 'C';
        dto.CentroCosto2 = centroCostoCliente;

        // Descripciones: DESDB* = lado debitado (interno) / DESCR* = lado acreditado (cliente)
        dto.DesDB1 = al1; dto.DesDB2 = al2; dto.DesDB3 = al3ConGL;
        dto.DesCR1 = al1; dto.DesCR2 = al2; dto.DesCR3 = al3Base;
    }
    else
    {
        // Mov1 = Cliente (Débito)
        dto.TipoMov1     = tipoCliente;
        dto.CuentaMov1   = decimal.TryParse(numeroCuentaCliente, out var ctaCli) ? ctaCli : 0m;
        dto.DeCr1        = 'D';
        dto.CentroCosto1 = centroCostoCliente;

        // Mov2 = Interno (Crédito)
        dto.TipoMov2     = 40m;
        dto.CuentaMov2   = glCuentaNum;
        dto.DeCr2        = 'C';
        dto.CentroCosto2 = glCC;

        // Descripciones: DESDB* = lado debitado (cliente) / DESCR* = lado acreditado (interno)
        dto.DesDB1 = al1; dto.DesDB2 = al2; dto.DesDB3 = al3Base;
        dto.DesCR1 = al1; dto.DesCR2 = al2; dto.DesCR3 = al3ConGL;
    }

    return dto;

    // --- local ---
    decimal SafeObtenerTasa()
    {
        try { return ObtenerTasaCompraUsd(); }
        catch { return 0m; }
    }
}


/// <summary>
/// Ejecuta INT_LOTES con dos movimientos completos (Débito y Crédito) usando un DTO ya resuelto.
/// - Mov1 = lado DEBITADO (PMTIPO01..PMMONE01 + DESDB1..3)
/// - Mov2 = lado ACREDITADO (PMTIPO02..PMMONE02 + DESCR1..3)
/// OUT: CODER, DESERR, NomArc
/// </summary>
/// <param name="p">Parámetros ya armados para INT_LOTES (ambos movimientos, descripciones y generales).</param>
/// <param name="monto">Importe a postear (se usa idéntico en ambos lados).</param>
public async Task<(int CodigoErrorPosteo, string? DescripcionErrorPosteo, string? NomArc)> PosteoLoteAsync(
    IntLotesParamsDto p,
    decimal monto)
{
    try
    {
        var builder = ProgramCallBuilder.For(_connection, "BCAH96", "INT_LOTES")
            .UseSqlNaming()
            .WrapCallWithBraces();

        // ===================== Movimiento 1 (DEBITADO) =====================
        builder.InDecimal("PMTIPO01", p.TipoMov1, precision: 2, scale: 0);
        builder.InDecimal("PMCTAA01", p.CuentaMov1, precision: 13, scale: 0);
        builder.InDecimal("PMVALR01", monto,      precision: 13, scale: 2);
        builder.InChar   ("PMDECR01", p.DeCr1.ToString(), 1);
        builder.InDecimal("PMCCOS01", p.CentroCosto1, precision: 5, scale: 0);
        builder.InDecimal("PMMONE01", p.Moneda, precision: 3, scale: 0);

        // ===================== Movimiento 2 (ACREDITADO) ====================
        builder.InDecimal("PMTIPO02", p.TipoMov2, precision: 2, scale: 0);
        builder.InDecimal("PMCTAA02", p.CuentaMov2, precision: 13, scale: 0);
        builder.InDecimal("PMVALR02", monto,      precision: 13, scale: 2);
        builder.InChar   ("PMDECR02", p.DeCr2.ToString(), 1);
        builder.InDecimal("PMCCOS02", p.CentroCosto2, precision: 5, scale: 0);
        builder.InDecimal("PMMONE02", p.Moneda, precision: 3, scale: 0);

        // ===================== Movimiento 3 (no usado) ======================
        builder.InDecimal("PMTIPO03", 0m, precision: 2, scale: 0);
        builder.InDecimal("PMCTAA03", 0m, precision: 13, scale: 0);
        builder.InDecimal("PMVALR03", 0m, precision: 13, scale: 2);
        builder.InChar   ("PMDECR03", "",  1);
        builder.InDecimal("PMCCOS03", 0m, precision: 5, scale: 0);
        builder.InDecimal("PMMONE03", 0m, precision: 3, scale: 0);

        // ===================== Movimiento 4 (no usado) ======================
        builder.InDecimal("PMTIPO04", 0m, precision: 2, scale: 0);
        builder.InDecimal("PMCTAA04", 0m, precision: 13, scale: 0);
        builder.InDecimal("PMVALR04", 0m, precision: 13, scale: 2);
        builder.InChar   ("PMDECR04", "",  1);
        builder.InDecimal("PMCCOS04", 0m, precision: 5, scale: 0);
        builder.InDecimal("PMMONE04", 0m, precision: 3, scale: 0);

        // ===================== Generales / Perfil / Moneda ==================
        builder.InChar   ("PMPERFIL", p.Perfil, 13);
        builder.InDecimal("MONEDA",   p.Moneda, precision: 3, scale: 0);

        // ===================== Descripciones por lado =======================
        // Lado debitado
        builder.InChar("DESDB1", p.DesDB1, 40);
        builder.InChar("DESDB2", p.DesDB2, 40);
        builder.InChar("DESDB3", p.DesDB3, 40);

        // Lado acreditado
        builder.InChar("DESCR1", p.DesCR1, 40);
        builder.InChar("DESCR2", p.DesCR2, 40);
        builder.InChar("DESCR3", p.DesCR3, 40);

        // ===================== OUT =====================
        builder.OutDecimal("CODER", 2, 0);
        builder.OutChar   ("DESERR", 70);
        builder.OutChar   ("NomArc", 10);

        var result = await builder.CallAsync(_contextAccessor.HttpContext);

        result.TryGet("CODER",  out int codigoError);
        result.TryGet("DESERR", out string? descripcionError);
        result.TryGet("NomArc", out string? nomArc);

        return (codigoError, descripcionError, nomArc);
    }
    catch (Exception ex)
    {
        return (-1, "Error general en PosteoLoteAsync: " + ex.Message, "");
    }
}
