namespace MS_BAN_56_ProcesamientoTransaccionesPOS.Models.Dtos
{
    /// <summary>
    /// Parámetros completos para llamar a INT_LOTES:
    /// - Arma ambos movimientos (Débito y Crédito) con cuenta, tipo, centro de costo, monto y moneda.
    /// - Incluye descripciones separadas para el lado debitado (DESDBx) y el acreditado (DESCRx).
    /// - Trae perfil, moneda y tasa.
    /// - Expone campos de trazabilidad (naturalezas, t-codes) y de salida (NomArc).
    /// </summary>
    public sealed class IntLotesParamsDto
    {
        // ------------------------- Cabecera/Generales ----------------------------

        /// <summary>Perfil Transerver (CFTSKY).</summary>
        public string Perfil { get; set; } = string.Empty;

        /// <summary>
        /// Código numérico de moneda por defecto para el posteo (ej. 340=LPS, 840=USD si aplica tu core; sino 0).
        /// Si <see cref="MonedaMov1"/> o <see cref="MonedaMov2"/> se establecen en &gt; 0, tienen prioridad en cada movimiento.
        /// </summary>
        public decimal Moneda { get; set; }

        /// <summary>Tasa TM (si tu RPG la usa; si no, 0).</summary>
        public decimal TasaTm { get; set; }

        // ------------------------- Movimiento 1 (DEBITADO) ----------------------

        /// <summary>Tipo de cuenta del movimiento 1 (1=ahorros, 6=cheques, 40=contable).</summary>
        public decimal TipoMov1 { get; set; }

        /// <summary>Número de cuenta del movimiento 1.</summary>
        public decimal CuentaMov1 { get; set; }

        /// <summary>
        /// Naturaleza del movimiento 1: 'D' o 'C'.  
        /// En el armado estándar: si el cliente es "C", Mov1 suele ser el GL en 'D'; si el cliente es "D", Mov1 suele ser el cliente en 'D'.
        /// </summary>
        public string DeCr1 { get; set; } = "D";

        /// <summary>Centro de costo del movimiento 1.</summary>
        public decimal CentroCosto1 { get; set; }

        /// <summary>
        /// Monto del movimiento 1.  
        /// Debe venir listo según la lógica de negocio (p. ej., monto total o neto según corresponda).
        /// </summary>
        public decimal MontoMov1 { get; set; }

        /// <summary>
        /// Moneda del movimiento 1 (opcional). Si es &gt; 0, INT_LOTES usará este valor; si es 0, se usa <see cref="Moneda"/>.
        /// </summary>
        public decimal MonedaMov1 { get; set; }

        // ------------------------- Movimiento 2 (ACREDITADO) --------------------

        /// <summary>Tipo de cuenta del movimiento 2 (1=ahorros, 6=cheques, 40=contable).</summary>
        public decimal TipoMov2 { get; set; }

        /// <summary>Número de cuenta del movimiento 2.</summary>
        public decimal CuentaMov2 { get; set; }

        /// <summary>
        /// Naturaleza del movimiento 2: 'D' o 'C'.  
        /// En el armado estándar: si el cliente es "C", Mov2 suele ser el cliente en 'C'; si el cliente es "D", Mov2 suele ser el GL en 'C'.
        /// </summary>
        public string DeCr2 { get; set; } = "C";

        /// <summary>Centro de costo del movimiento 2.</summary>
        public decimal CentroCosto2 { get; set; }

        /// <summary>
        /// Monto del movimiento 2.  
        /// Debe venir listo según la lógica de negocio (p. ej., monto total o neto según corresponda).
        /// </summary>
        public decimal MontoMov2 { get; set; }

        /// <summary>
        /// Moneda del movimiento 2 (opcional). Si es &gt; 0, INT_LOTES usará este valor; si es 0, se usa <see cref="Moneda"/>.
        /// </summary>
        public decimal MonedaMov2 { get; set; }

        // ------------------------- Descripciones por lado -----------------------

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

        // ------------------------- Trazabilidad (opcionales) --------------------

        /// <summary>
        /// Naturaleza del lado cliente original recibido: 'C' para acreditar o 'D' para debitar al cliente.  
        /// Útil para diagnóstico y para reconstruir la intención original.
        /// </summary>
        public string NaturalezaCliente { get; set; } = string.Empty;

        /// <summary>
        /// Naturaleza del lado GL opuesto a cliente: 'D' o 'C'.  
        /// Útil para diagnóstico; en la llamada real ya se codifica con <see cref="DeCr1"/>/<see cref="DeCr2"/>.
        /// </summary>
        public char NaturalezaGL { get; set; }

        /// <summary>T-code propuesto para el cliente (si tu flujo lo usa/traquea).</summary>
        public string TcodeCliente { get; set; } = string.Empty;

        /// <summary>T-code propuesto para el GL (si tu flujo lo usa/traquea).</summary>
        public string TcodeGL { get; set; } = string.Empty;

        /// <summary>Indica si se obtuvo contrapartida por auto-balance (CFP801).</summary>
        public bool EsAutoBalance { get; set; }

        /// <summary>Fuente usada para resolver GL/CC (CFP801 / ADQECTL / ADQCTL / N/A).</summary>
        public string FuenteGL { get; set; } = string.Empty;

        /// <summary>Cuenta cliente “cruda” (para trazabilidad, si te interesa conservarla).</summary>
        public string CuentaClienteOriginal { get; set; } = string.Empty;

        /// <summary>Cuenta GL resuelta (texto tal cual se obtuvo; útil para auditoría).</summary>
        public string CuentaGLResuelta { get; set; } = string.Empty;

        // ------------------------- Salidas de INT_LOTES -------------------------

        /// <summary>Nombre de archivo generado por INT_LOTES (NomArc) si aplica.</summary>
        public string NomArc { get; set; } = string.Empty;

        /// <summary>
        /// Código de error devuelto por INT_LOTES (0=OK; distinto de 0 indica error).  
        /// No confundir con <see cref="ErrorMetodo"/>, que es error de la lógica local.
        /// </summary>
        public int CodigoErrorPosteo { get; set; }

        /// <summary>Descripción de error devuelta por INT_LOTES (si aplica).</summary>
        public string DescripcionError { get; set; } = string.Empty;

        // ------------------------- Error local (no de INT_LOTES) ----------------

        /// <summary>Error del método local que construye/llama (0=OK, &gt;0 error local).</summary>
        public int ErrorMetodo { get; set; }
    }
}


/// <summary>
/// Resuelve TODO lo necesario para llamar INT_LOTES y devuelve el DTO final:
/// - Según naturaleza del cliente ('C' o 'D') arma Mov1 (Débito) y Mov2 (Crédito)
/// - Asigna cuentas/tipos/CC para lado cliente y lado GL
/// - Copia el monto en ambos movimientos (se debita y se acredita a la vez)
/// - Llena DESDBx (lado debitado) y DESCRx (lado acreditado)
/// </summary>
private IntLotesParamsDto ResolverParametrosIntLotes(
    bool esEcommerce,
    string perfil,
    string naturalezaCliente,             // 'C' (acreditar cliente) o 'D' (debitar cliente)
    string numeroCuenta,                  // cuenta del cliente (texto)
    string codigoComercio,
    string terminal,
    string nombreComercio,
    string idUnico,
    int monedaIsoNum,                     // 0 si no aplica ISO; de lo contrario ej. 340/840
    decimal monto                         // monto a postear (mismo en ambos lados)
)
{
    // Naturaleza opuesta (lado GL) solo para trazabilidad
    char naturalezaGL = naturalezaCliente == "C" ? 'D' : 'C';

    // Determinar tipo de la cuenta cliente (ahorros/cheques)
    var infoCta = VerCta(numeroCuenta);
    var tipoCliente = infoCta.EsAhorro ? 1m : infoCta.EsCheques ? 6m : 40m;

    // Resolver GL/CC (CFP801 -> ADQECTL -> ADQCTL)
    var auto = TryGetAutoBalance(perfil);
    string? glCuenta = null;
    int glCC = 0;
    string fuente = "N/A";

    if (auto.enabled)
    {
        if (naturalezaCliente == "C")
        {
            // Cliente en CR -> GL en DB
            glCuenta = auto.glDebito;
            glCC = auto.ccDebito;
        }
        else
        {
            // Cliente en DB -> GL en CR
            glCuenta = auto.glCredito;
            glCC = auto.ccCredito;
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

    // Si no hubo GL, caemos en placeholders neutros
    var glEsNumerica = !string.IsNullOrWhiteSpace(glCuenta) && decimal.TryParse(glCuenta, out var glAsDec);
    var tipoGL = 40m; // contable

    // Descripciones base (40 chars; INT_LOTES espera 40 en DESxx)
    string desBase1 = Trunc(nombreComercio, 40);
    string desBase2 = Trunc($"{codigoComercio}-{terminal}", 40);
    string desBase3 = Trunc($"{EtiquetaConcepto(naturalezaCliente)}-{idUnico}-{infoCta.DescCorta}", 40);

    // Armar DTO según reglas:
    // Caso A: naturalezaCliente == "C" (ACREDITAR CLIENTE; DEBITAR GL)
    //   - Mov1 (Débito)  -> GL
    //   - Mov2 (Crédito) -> Cliente
    //   - DESDBx = GL, DESCRx = Cliente
    if (naturalezaCliente == "C")
    {
        return new IntLotesParamsDto
        {
            Perfil = perfil,
            Moneda = monedaIsoNum,
            TasaTm = 0m, // si no aplica, 0

            // Mov1 = Débito GL
            TipoMov1 = tipoGL,
            CuentaMov1 = glEsNumerica ? glAsDec : 0m,
            DeCr1 = "D",
            CentroCosto1 = glCC,
            MontoMov1 = monto,
            MonedaMov1 = monedaIsoNum,

            // Mov2 = Crédito Cliente
            TipoMov2 = tipoCliente,
            CuentaMov2 = decimal.TryParse(numeroCuenta, out var cteDec1) ? cteDec1 : 0m,
            DeCr2 = "C",
            CentroCosto2 = 0m, // para cliente usualmente 0
            MontoMov2 = monto,
            MonedaMov2 = monedaIsoNum,

            // Descripciones: DB=GL, CR=Cliente
            DesDB1 = desBase1,
            DesDB2 = desBase2,
            DesDB3 = Trunc($"{desBase3}-{glCuenta}", 40),

            DesCR1 = desBase1,
            DesCR2 = desBase2,
            DesCR3 = desBase3,

            // Trazabilidad
            NaturalezaCliente = naturalezaCliente,
            NaturalezaGL = naturalezaGL,
            TcodeCliente = "0783",
            TcodeGL = "0784",
            EsAutoBalance = auto.enabled,
            FuenteGL = fuente,
            CuentaClienteOriginal = numeroCuenta,
            CuentaGLResuelta = glCuenta ?? string.Empty
        };
    }

    // Caso B: naturalezaCliente == "D" (DEBITAR CLIENTE; ACREDITAR GL)
    //   - Mov1 (Débito)  -> Cliente
    //   - Mov2 (Crédito) -> GL
    //   - DESDBx = Cliente, DESCRx = GL
    return new IntLotesParamsDto
    {
        Perfil = perfil,
        Moneda = monedaIsoNum,
        TasaTm = 0m,

        // Mov1 = Débito Cliente
        TipoMov1 = tipoCliente,
        CuentaMov1 = decimal.TryParse(numeroCuenta, out var cteDec2) ? cteDec2 : 0m,
        DeCr1 = "D",
        CentroCosto1 = 0m,
        MontoMov1 = monto,
        MonedaMov1 = monedaIsoNum,

        // Mov2 = Crédito GL
        TipoMov2 = tipoGL,
        CuentaMov2 = glEsNumerica ? glAsDec : 0m,
        DeCr2 = "C",
        CentroCosto2 = glCC,
        MontoMov2 = monto,
        MonedaMov2 = monedaIsoNum,

        // Descripciones: DB=Cliente, CR=GL
        DesDB1 = desBase1,
        DesDB2 = desBase2,
        DesDB3 = desBase3,

        DesCR1 = desBase1,
        DesCR2 = desBase2,
        DesCR3 = Trunc($"{desBase3}-{glCuenta}", 40),

        // Trazabilidad
        NaturalezaCliente = naturalezaCliente,
        NaturalezaGL = naturalezaGL,
        TcodeCliente = "0784",
        TcodeGL = "0783",
        EsAutoBalance = auto.enabled,
        FuenteGL = fuente,
        CuentaClienteOriginal = numeroCuenta,
        CuentaGLResuelta = glCuenta ?? string.Empty
    };
}



/// <summary>
/// Ejecuta INT_LOTES con los 35 parámetros exactos usando <see cref="ProgramCallBuilder"/>.
/// Toma todos los valores de <paramref name="p"/> y captura (CODER, DESERR, NomArc).
/// </summary>
public async Task<(int CodigoErrorPosteo, string? DescripcionErrorPosteo, string? NomArc)> PosteoLoteAsync(IntLotesParamsDto p)
{
    try
    {
        var builder = ProgramCallBuilder.For(_connection, "BCAH96", "INT_LOTES")
            .UseSqlNaming()
            .WrapCallWithBraces();

        // ===================== Movimiento 1 (Débito) =====================
        builder.InDecimal("PMTIPO01",  p.TipoMov1,   precision: 2,  scale: 0);
        builder.InDecimal("PMCTAA01",  p.CuentaMov1, precision: 13, scale: 0);
        builder.InDecimal("PMVALR01",  p.MontoMov1,  precision: 13, scale: 2);
        builder.InChar   ("PMDECR01",  string.IsNullOrEmpty(p.DeCr1) ? "D" : p.DeCr1, 1);
        builder.InDecimal("PMCCOS01",  p.CentroCosto1, precision: 5, scale: 0);
        builder.InDecimal("PMMONE01",  p.MonedaMov1 > 0 ? p.MonedaMov1 : p.Moneda, precision: 3, scale: 0);

        // ===================== Movimiento 2 (Crédito) ====================
        builder.InDecimal("PMTIPO02",  p.TipoMov2,   precision: 2,  scale: 0);
        builder.InDecimal("PMCTAA02",  p.CuentaMov2, precision: 13, scale: 0);
        builder.InDecimal("PMVALR02",  p.MontoMov2,  precision: 13, scale: 2);
        builder.InChar   ("PMDECR02",  string.IsNullOrEmpty(p.DeCr2) ? "C" : p.DeCr2, 1);
        builder.InDecimal("PMCCOS02",  p.CentroCosto2, precision: 5, scale: 0);
        builder.InDecimal("PMMONE02",  p.MonedaMov2 > 0 ? p.MonedaMov2 : p.Moneda, precision: 3, scale: 0);

        // ===================== Movimiento 3 (vacío) ======================
        builder.InDecimal("PMTIPO03",  0m, precision: 2, scale: 0);
        builder.InDecimal("PMCTAA03",  0m, precision: 13, scale: 0);
        builder.InDecimal("PMVALR03",  0m, precision: 13, scale: 2);
        builder.InChar   ("PMDECR03",  "", 1);
        builder.InDecimal("PMCCOS03",  0m, precision: 5, scale: 0);
        builder.InDecimal("PMMONE03",  0m, precision: 3, scale: 0);

        // ===================== Movimiento 4 (vacío) ======================
        builder.InDecimal("PMTIPO04",  0m, precision: 2, scale: 0);
        builder.InDecimal("PMCTAA04",  0m, precision: 13, scale: 0);
        builder.InDecimal("PMVALR04",  0m, precision: 13, scale: 2);
        builder.InChar   ("PMDECR04",  "", 1);
        builder.InDecimal("PMCCOS04",  0m, precision: 5, scale: 0);
        builder.InDecimal("PMMONE04",  0m, precision: 3, scale: 0);

        // ===================== Generales/Descripciones ===================
        builder.InChar   ("PMPERFIL",  p.Perfil, 13);
        builder.InDecimal("MONEDA",    p.Moneda, precision: 3, scale: 0);

        // Lado DEBITADO → DESDBx
        builder.InChar("DESDB1", p.DesDB1 ?? "", 40);
        builder.InChar("DESDB2", p.DesDB2 ?? "", 40);
        builder.InChar("DESDB3", p.DesDB3 ?? "", 40);

        // Lado ACREDITADO → DESCRx
        builder.InChar("DESCR1", p.DesCR1 ?? "", 40);
        builder.InChar("DESCR2", p.DesCR2 ?? "", 40);
        builder.InChar("DESCR3", p.DesCR3 ?? "", 40);

        // ===================== OUT =====================
        builder.OutDecimal("CODER", 2, 0);
        builder.OutChar   ("DESERR", 70);
        builder.OutChar   ("NomArc", 10);

        var result = await builder.CallAsync(_contextAccessor.HttpContext);

        result.TryGet("CODER",  out int    codigoError);
        result.TryGet("DESERR", out string? descripcionError);
        result.TryGet("NomArc", out string? nomArc);

        return (codigoError, descripcionError, nomArc);
    }
    catch (Exception ex)
    {
        return (-1, "Error general en PosteoLoteAsync: " + ex.Message, "");
    }
}


// 1) Librerías (si aplica en tu ambiente)
var (agregoLibrerias, errLibs) = CargaLibrerias();
if (!agregoLibrerias) return BuildError("500", errLibs);

// 2) Armar DTO completo listo para INT_LOTES
var p = ResolverParametrosIntLotes(
    esEcommerce: esTerminalVirtual,
    perfil: perfilTranserver,
    naturalezaCliente: guardarTransaccionesDto.NaturalezaContable,  // 'C' o 'D'
    numeroCuenta: guardarTransaccionesDto.NumeroCuenta,
    codigoComercio: guardarTransaccionesDto.CodigoComercio,
    terminal: guardarTransaccionesDto.Terminal,
    nombreComercio: guardarTransaccionesDto.NombreComercio,
    idUnico: guardarTransaccionesDto.IdTransaccionUnico,
    monedaIsoNum: 0,            // ajusta si tu core exige ISO numérico
    monto: deb > 0m ? deb : cre // mismo monto en ambos lados
);

// 3) Ejecutar INT_LOTES
var r = await PosteoLoteAsync(p);
if (r.CodigoErrorPosteo != 0)
    return BuildError(r.CodigoErrorPosteo.ToString(), r.DescripcionErrorPosteo ?? "Error desconocido en INT_LOTES.");

// OK
return BuildError("200", "Transacción procesada correctamente.");






