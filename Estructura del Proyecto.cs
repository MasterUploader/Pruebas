public sealed class IntLotesParamsDto
{
    public string Perfil { get; set; } = "";
    public string CuentaCliente { get; set; } = "";
    public int TipoCuentaCliente { get; set; }   // 1=Ahorro, 6=Cheques, 40=Contable
    public string? CuentaGL { get; set; }        // texto, porque GL contable puede tener ceros a la izquierda
    public int CentroCostoGL { get; set; }
    public string TcodeCliente { get; set; } = "";
    public string TcodeGL { get; set; } = "";
    public string NaturalezaCliente { get; set; } = ""; // 'C' o 'D'
    public char   NaturalezaGL { get; set; }            // 'D' o 'C'
    public int Moneda { get; set; }
    public decimal TasaTm { get; set; }
    public bool EsAutoBalance { get; set; }
    public string FuenteGL { get; set; } = "";

    // ➜ Descripciones exactas para INT_LOTES (ya listas):
    public string DesDb1 { get; set; } = "";  // DESDB1 (para la línea Débito del llamado)
    public string DesDb2 { get; set; } = "";  // DESDB2
    public string DesDb3 { get; set; } = "";  // DESDB3
    public string DesCr1 { get; set; } = "";  // DESCR1 (para la línea Crédito del llamado)
    public string DesCr2 { get; set; } = "";  // DESCR2
    public string DesCr3 { get; set; } = "";  // DESCR3
}


/// <summary>
/// Resuelve contrapartida GL y ARMA las descripciones AL1/AL2/AL3 con el MISMO estilo del RPG:
/// AL1 = nombre comercio (W02NACO)
/// AL2 = codigoComercio + '-' + A02FEDA (+ "-{LUTCZch}-{LUCAZch}" si aplica en débito GL)
/// AL3 = "&" + Concepto3 + "&" + <control10> + <DescConcepto> + "||"  (truncado a 40)
/// </summary>
private IntLotesParamsDto ResolverParametrosIntLotes(
    bool esEcommerce,
    string perfil,
    string naturalezaCliente,     // 'C' acredita cliente, 'D' debita cliente
    string numeroCuenta,          // cuenta cliente
    string codigoComercio,
    string terminal,
    string nombreComercio,
    string idUnico,
    int monedaIsoNum = 0
)
{
    // === 1) Naturalezas y T-Codes como antes ===
    string tcodeCliente = naturalezaCliente == "C" ? "0783" : "0784";
    string tcodeGL      = naturalezaCliente == "C" ? "0784" : "0783";
    char   naturalezaGL = naturalezaCliente == "C" ? 'D' : 'C';

    // === 2) Tipo de cuenta cliente (Ver_cta) para armar "LUTCZch" y etiqueta corta ===
    var infoCta = VerCta(numeroCuenta);
    string tipoCorto = string.IsNullOrWhiteSpace(infoCta.DescCorta) ? "" : infoCta.DescCorta.Trim();
    // LUTCZch: 1 si Ahorro, 6 si Cheques, 6 por defecto (contable)
    string LUTCZch = infoCta.EsAhorro ? "1" : infoCta.EsCheques ? "6" : "6";
    // LUCAZch = cuenta cliente (10 chars, como en RPG usaban wdmacctch10)
    string LUCAZch = Trunc(numeroCuenta?.Trim() ?? "", 10);

    // === 3) A02FEDA (lo que el RPG pone en AL2). Si no se consigue, usar Terminal como fallback ===
    string a02Feda = ObtenerA02FEDA(codigoComercio) ?? terminal ?? "";
    a02Feda = Trunc(a02Feda, 10);

    // === 4) Contrapartida GL: CFP801 / ADQECTL / ADQCTL (igual a lo que ya tenías) ===
    var auto = TryGetAutoBalance(perfil);
    string? glCuentaTxt = null;
    int glCC = 0;
    string fuente = "N/A";

    if (auto.enabled)
    {
        if (naturalezaCliente == "C") { glCuentaTxt = auto.glDebito;  glCC = auto.ccDebito;  } // GL Debe
        else                          { glCuentaTxt = auto.glCredito; glCC = auto.ccCredito; } // GL Haber
        fuente = "CFP801";
    }
    else
    {
        if ( esEcommerce && TryGetGLFromAdqEctl(codigoComercio, tcodeGL, out var gl1, out var cc1) )
        { glCuentaTxt = gl1; glCC = cc1; fuente = "ADQECTL"; }
        else if ( TryGetGLFromAdqCtl("GL", 1, tcodeGL, out var gl2, out var cc2) )
        { glCuentaTxt = gl2; glCC = cc2; fuente = "ADQCTL"; }
    }

    // === 5) Concepto y descripción corta para AL3 (RPG usaba 'VTA','AFI','ISR','AVA','MEM'...):
    // Como transacción genérica POS, usamos 'VTA'. Ajusta si necesitas otro.
    string concepto3 = "VTA";
    string descCr    = "Cr Comercio";  // lo que ponía el RPG en casos de crédito al comercio
    string descDb    = "Db Comercio";  // lo que ponía el RPG en casos de débito al comercio

    // === 6) "Número de control" de 10 (W_ADQNUM_ch en RPG). Si no tienes ADQCTL, usamos el IdUnico (solo dígitos) con pad:
    string control10 = BuildControl10(idUnico); // 10 chars (solo dígitos, left-pad con '0')

    // === 7) AL1/AL2/AL3 con el MISMO estilo ===
    // AL1: nombre comercio (hasta 40)
    string al1 = Trunc(nombreComercio, 40);
    // AL2: codigoComercio + '-' + A02FEDA
    string al2Base = Trunc($"{codigoComercio}-{a02Feda}", 40);
    // AL2 para débito GL (en el RPG Debito incluía “-LUTCZch-LUCAZch” si existían)
    string al2DebGl = string.IsNullOrWhiteSpace(LUTCZch) || string.IsNullOrWhiteSpace(LUCAZch)
        ? al2Base
        : Trunc($"{codigoComercio}-{a02Feda}-{LUTCZch}-{LUCAZch}", 40);

    // AL3: "&" + Concepto + "&" + control10 + DesConcepto + "||"
    string Al3(string conceptoTres, string control10Num, string desConcepto)
        => Trunc($"&{conceptoTres}&{control10Num}{desConcepto}||", 40);

    // === 8) Descripciones por lado, según lo que pediste:
    // Regla: si naturalezaCliente = "C": cliente CR (DESCRx) y GL DB (DESDBx)
    //        si naturalezaCliente = "D": cliente DB (DESDBx) y GL CR (DESCRx)
    string desDb1, desDb2, desDb3, desCr1, desCr2, desCr3;

    if (naturalezaCliente == "C")
    {
        // Débito (interno GL) -> DESDBx
        desDb1 = al1;
        desDb2 = al2DebGl;                      // incluye LUTCZch/LUCAZch como en Debito
        desDb3 = Al3(concepto3, control10, "Db Comer");

        // Crédito (cliente) -> DESCRx
        desCr1 = al1;
        desCr2 = al2Base;                       // en CrCliVta no se agregaban LUTCZch/LUCAZch
        desCr3 = Al3(concepto3, control10, "Cr Comer");
    }
    else // "D"
    {
        // Débito (cliente) -> DESDBx   (DbCliVta en RPG no agrega LUTCZch/LUCAZch en AL2)
        desDb1 = al1;
        desDb2 = al2Base;
        desDb3 = Al3(concepto3, control10, "Db Comer");

        // Crédito (interno GL) -> DESCRx   (en Credito GL tampoco agregaba LUTCZch/LUCAZch)
        desCr1 = al1;
        desCr2 = al2Base;
        desCr3 = Al3(concepto3, control10, "Cr Comer");
    }

    // === 9) Retorno completo para usar directo en INT_LOTES ===
    return new IntLotesParamsDto
    {
        Perfil = perfil,
        CuentaCliente = numeroCuenta,
        TipoCuentaCliente = infoCta.EsAhorro ? 1 : infoCta.EsCheques ? 6 : 40,
        CuentaGL = glCuentaTxt,
        CentroCostoGL = glCC,
        TcodeCliente = tcodeCliente,
        TcodeGL = tcodeGL,
        NaturalezaCliente = naturalezaCliente,
        NaturalezaGL = naturalezaGL,
        Moneda = monedaIsoNum,
        TasaTm = 0m,            // si luego necesitas usar tasa, la colocas
        EsAutoBalance = auto.enabled,
        FuenteGL = fuente,

        DesDb1 = desDb1,
        DesDb2 = desDb2,
        DesDb3 = desDb3,
        DesCr1 = desCr1,
        DesCr2 = desCr2,
        DesCr3 = desCr3
    };
}
private static string BuildControl10(string id)
{
    if (string.IsNullOrWhiteSpace(id)) return "0000000000";
    var digits = new string(id.Where(char.IsDigit).ToArray());
    if (digits.Length == 0) digits = "0";
    digits = digits.Length > 10 ? digits[^10..] : digits.PadLeft(10, '0');
    return digits;
}

private string? ObtenerA02FEDA(string codComercio)
{
    try
    {
        var q = QueryBuilder.Core.QueryBuilder
            .From("ADQ02COM", "BCAH96DTA")
            .Select("A02FEDA")
            .WhereRaw($"A02COME = {codComercio}")
            .FetchNext(1)
            .Build();

        using var cmd = _connection.GetDbCommand(q, _contextAccessor.HttpContext!);
        using var rd = cmd.ExecuteReader();
        if (rd.Read())
        {
            var v = rd[0];
            return v is DBNull ? null : Convert.ToString(v)?.Trim();
        }
        return null;
    }
    catch { return null; }
}


// ===================== Descripciones Débito (nuevas) =====================
builder.InChar("DESDB1", p.DesDb1, 40);
builder.InChar("DESDB2", p.DesDb2, 40);
builder.InChar("DESDB3", p.DesDb3, 40);

// ===================== Descripciones Crédito (originales) =====================
builder.InChar("DESCR1", p.DesCr1, 40);
builder.InChar("DESCR2", p.DesCr2, 40);
builder.InChar("DESCR3", p.DesCr3, 40);
