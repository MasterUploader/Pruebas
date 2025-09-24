/// <summary>
/// Parámetros ya resueltos para construir las 2 líneas (cliente y GL)
/// que consumirá el programa RPG <c>Int_lotes</c>.
/// </summary>
public sealed class IntLotesParamsDto
{
    /// <summary>Perfil Transerver usado (CFTSKY).</summary>
    public string Perfil { get; init; } = "";

    /// <summary>Cuenta del cliente/comercio (TSTACT de la línea cliente).</summary>
    public string CuentaCliente { get; init; } = "";

    /// <summary>Cuenta GL interna de contrapartida (TSTACT de la línea GL).</summary>
    public string? CuentaGL { get; init; }

    /// <summary>Centro de costo para la línea GL (TSWSCC).</summary>
    public int CentroCostoGL { get; init; }

    /// <summary>T-code de la línea del cliente (ej. 0783 crédito / 0784 débito).</summary>
    public string TcodeCliente { get; init; } = "";

    /// <summary>T-code de la línea GL (opuesto al del cliente).</summary>
    public string TcodeGL { get; init; } = "";

    /// <summary>Naturaleza de la línea del cliente: 'C' o 'D'.</summary>
    public char NaturalezaCliente { get; init; }

    /// <summary>Naturaleza de la línea GL: 'C' o 'D' (opuesta a la del cliente).</summary>
    public char NaturalezaGL { get; init; }

    /// <summary>Descripción/leyenda 1 (AL1): normalmente nombre del comercio.</summary>
    public string Des001 { get; init; } = "";

    /// <summary>Descripción/leyenda 2 (AL2): “{Comercio}-{Terminal}”.</summary>
    public string Des002 { get; init; } = "";

    /// <summary>Descripción/leyenda 3 (AL3): “&lt;CR/DB&gt;&lt;IdUnico&gt;&lt;TipoCta&gt; / GL…”.</summary>
    public string Des003 { get; init; } = "";

    /// <summary>Descripción/leyenda 4 (AL4): libre/opcional (deja en blanco si no aplica).</summary>
    public string Des004 { get; init; } = "";

    /// <summary>Código de moneda (ISO num, si aplica para tu RPG; úsalo si Int_lotes lo pide).</summary>
    public int Moneda { get; init; }

    /// <summary>Tasa/Tipo de cambio a usar por Int_lotes si corresponde.</summary>
    public decimal TasaTm { get; init; }

    /// <summary>Indica si el perfil está en auto-balance (CFP801.CFTSGE=1).</summary>
    public bool EsAutoBalance { get; init; }

    /// <summary>Indica de dónde salió la GL: "CFP801", "ADQECTL", "ADQCTL" o "N/A".</summary>
    public string FuenteGL { get; init; } = "N/A";
}





/// <summary>
/// Resuelve la contrapartida GL (cuenta y CC), t-codes, naturalezas y descripciones
/// necesarias para llamar a <c>Int_lotes</c>, usando únicamente la info ya recibida
/// (perfil, cuenta, comercio, terminal, etc.).
/// </summary>
/// <param name="perfil">Perfil Transerver (CFTSKY).</param>
/// <param name="naturalezaCliente">'C' para acreditar o 'D' para debitar al cliente.</param>
/// <param name="numeroCuenta">Cuenta del cliente/comercio.</param>
/// <param name="codigoComercio">Código de comercio (numérico string).</param>
/// <param name="terminal">Terminal (para saber si es e-commerce y para AL2).</param>
/// <param name="nombreComercio">Nombre del comercio (AL1).</param>
/// <param name="idUnico">Identificador único de transacción (para AL3).</param>
/// <param name="monedaIsoNum">Código ISO numérico de moneda si tu RPG lo requiere (ej: 840=USD). Usa 0 si no aplica.</param>
/// <returns>DTO con todo lo necesario para armar las 2 líneas (cliente y GL) de Int_lotes.</returns>
private IntLotesParamsDto ResolverParametrosIntLotes(
    string perfil,
    char naturalezaCliente,
    string numeroCuenta,
    string codigoComercio,
    string terminal,
    string nombreComercio,
    string idUnico,
    int monedaIsoNum = 0)
{
    // Determinar si la terminal es virtual (e-commerce) solo para decidir fuente de GL.
    bool esEcommerce = EsTerminalVirtual(terminal);

    // T-codes estándar de cliente (y su opuesto para GL)
    string tcodeCliente = naturalezaCliente == 'C' ? "0783" : "0784";
    string tcodeGL      = naturalezaCliente == 'C' ? "0784" : "0783";
    char   naturalezaGL = naturalezaCliente == 'C' ? 'D' : 'C';

    // Para AL3 queremos incluir el tipo de cuenta (Ahorro/Cheques) como hace el RPG.
    var infoCta = VerCta(numeroCuenta); // ya lo tienes: devuelve algo con DescCorta "AHO"/"CHE", etc.
    var tipoCtaCorto = string.IsNullOrWhiteSpace(infoCta.DescCorta) ? "" : infoCta.DescCorta.Trim();

    // Descripciones equivalentes al RPG (ajusta longitudes si tu Int_lotes limita a 30/40)
    string al1 = Trunc(nombreComercio, 30);
    string al2 = Trunc($"{codigoComercio}-{terminal}", 30);
    string al3 = Trunc($"&{EtiquetaConcepto(naturalezaCliente.ToString())}&{idUnico}&{tipoCtaCorto}", 30);
    string al4 = ""; // sin uso de momento

    // === Resolver contrapartida GL ===
    // 1) CFP801 (auto-balance). Si está activo, usamos las cuentas del perfil.
    var auto = TryGetAutoBalance(perfil);
    string? glCuenta = null;
    int     glCC     = 0;
    string  fuente   = "N/A";

    if (auto.enabled)
    {
        if (naturalezaCliente == 'C')
        {
            // Cliente a CR → GL a DB
            glCuenta = auto.glDebito;   // CFTSGD
            glCC     = auto.ccDebito;   // CFTCCD
        }
        else
        {
            // Cliente a DB → GL a CR
            glCuenta = auto.glCredito;  // CFTSGC
            glCC     = auto.ccCredito;  // CFTCCC
        }
        fuente = "CFP801";
        // Nota: aunque el core balancea solo, devolvemos igual la GL para formar la descripción
        // o por si tu Int_lotes exige enviarla explícitamente.
    }
    else
    {
        // 2) ADQECTL (si e-commerce) → busca el t-code opuesto (línea GL)
        if (esEcommerce && TryGetGLFromAdqEctl(codigoComercio, tcodeGL, out var glEc, out var ccEc))
        {
            glCuenta = glEc; glCC = ccEc; fuente = "ADQECTL";
        }
        // 3) ADQCTL genérico (por control/secuencia) → t-code opuesto
        else if (TryGetGLFromAdqCtl("GL", 1, tcodeGL, out var glG, out var ccG))
        {
            glCuenta = glG; glCC = ccG; fuente = "ADQCTL";
        }
    }

    // Tasa (si tu RPG la usa). Si no aplica, queda 0.
    decimal tasa = 0m;
    try { tasa = ObtenerTasaCompraUsd(); } catch { /* opcional */ }

    return new IntLotesParamsDto
    {
        Perfil = perfil,
        CuentaCliente = numeroCuenta,
        CuentaGL = glCuenta,
        CentroCostoGL = glCC,
        TcodeCliente = tcodeCliente,
        TcodeGL = tcodeGL,
        NaturalezaCliente = naturalezaCliente,
        NaturalezaGL = naturalezaGL,
        Des001 = al1,
        Des002 = al2,
        Des003 = string.IsNullOrWhiteSpace(glCuenta) ? al3 : Trunc($"{al3}/{glCuenta}", 30),
        Des004 = al4,
        Moneda = monedaIsoNum,
        TasaTm = tasa,
        EsAutoBalance = auto.enabled,
        FuenteGL = fuente
    };
}



/// <summary>
/// Lee de CFP801 si el perfil genera asiento de balance (CFTSGE=1) y obtiene sus cuentas/CC.
/// </summary>
private (bool enabled, string glDebito, int ccDebito, string glCredito, int ccCredito) TryGetAutoBalance(string perfil)
{
    // SELECT CFTSGE, CFTSGD, CFTCCD, CFTSGC, CFTCCC FROM BNKPRD01.CFP801 WHERE CFTSBK=1 AND CFTSKY=:perfil
    var q = QueryBuilder.Core.QueryBuilder
        .From("CFP801", "BNKPRD01")
        .Select("CFTSGE", "CFTSGD", "CFTCCD", "CFTSGC", "CFTCCC")
        .Where<Cfp801>(x => x.CFTSBK == 1)
        .Where<Cfp801>(x => x.CFTSKY == perfil)
        .FetchNext(1)
        .Build();

    using var cmd = _connection.GetDbCommand(q, _contextAccessor.HttpContext!);
    using var rd  = cmd.ExecuteReader();
    if (!rd.Read()) return (false, "", 0, "", 0);

    int sge = rd.IsDBNull(0) ? 0 : Convert.ToInt32(rd.GetValue(0));
    string glDb = rd.IsDBNull(1) ? "" : rd.GetValue(1).ToString()!.Trim();
    int ccDb    = rd.IsDBNull(2) ? 0  : Convert.ToInt32(rd.GetValue(2));
    string glCr = rd.IsDBNull(3) ? "" : rd.GetValue(3).ToString()!.Trim();
    int ccCr    = rd.IsDBNull(4) ? 0  : Convert.ToInt32(rd.GetValue(4));

    return (sge == 1, glDb, ccDb, glCr, ccCr);
}

/// <summary>
/// Busca en ADQECTL (control e-commerce) la GL/CC cuyo T-code (ADQECTR1..15) coincida con <paramref name="tcodeBuscado"/>.
/// </summary>
private bool TryGetGLFromAdqEctl(string comercio, string tcodeBuscado, out string gl, out int cc)
{
    gl = ""; cc = 0;
    var q = QueryBuilder.Core.QueryBuilder
        .From("ADQECTL", "BCAH96DTA")
        .Select("*")
        .WhereRaw("ADQECONT = 'EC'")
        .WhereRaw($"ADQENUM = {comercio}")   // si ADQENUM es numérico
        .FetchNext(1)
        .Build();

    using var cmd = _connection.GetDbCommand(q, _contextAccessor.HttpContext!);
    using var rd  = cmd.ExecuteReader();
    if (!rd.Read()) return false;

    // Sin bucles, chequeamos 1..15 explícitos (tcode→cuenta/costo del mismo ordinal).
    string tr;
    // 1
    tr = rd["ADQECTR1"]?.ToString()?.Trim() ?? "";
    if (tr == tcodeBuscado) { gl = rd["ADQECNT1"]?.ToString()?.Trim() ?? ""; cc = SafeToInt(rd["ADQECCO1"]); return !string.IsNullOrEmpty(gl); }
    // 2
    tr = rd["ADQECTR2"]?.ToString()?.Trim() ?? "";
    if (tr == tcodeBuscado) { gl = rd["ADQECNT2"]?.ToString()?.Trim() ?? ""; cc = SafeToInt(rd["ADQECCO2"]); return !string.IsNullOrEmpty(gl); }
    // 3
    tr = rd["ADQECTR3"]?.ToString()?.Trim() ?? "";
    if (tr == tcodeBuscado) { gl = rd["ADQECNT3"]?.ToString()?.Trim() ?? ""; cc = SafeToInt(rd["ADQECCO3"]); return !string.IsNullOrEmpty(gl); }
    // 4
    tr = rd["ADQECTR4"]?.ToString()?.Trim() ?? "";
    if (tr == tcodeBuscado) { gl = rd["ADQECNT4"]?.ToString()?.Trim() ?? ""; cc = SafeToInt(rd["ADQECCO4"]); return !string.IsNullOrEmpty(gl); }
    // 5
    tr = rd["ADQECTR5"]?.ToString()?.Trim() ?? "";
    if (tr == tcodeBuscado) { gl = rd["ADQECNT5"]?.ToString()?.Trim() ?? ""; cc = SafeToInt(rd["ADQECCO5"]); return !string.IsNullOrEmpty(gl); }
    // 6
    tr = rd["ADQECTR6"]?.ToString()?.Trim() ?? "";
    if (tr == tcodeBuscado) { gl = rd["ADQECNT6"]?.ToString()?.Trim() ?? ""; cc = SafeToInt(rd["ADQECCO6"]); return !string.IsNullOrEmpty(gl); }
    // 7
    tr = rd["ADQECTR7"]?.ToString()?.Trim() ?? "";
    if (tr == tcodeBuscado) { gl = rd["ADQECNT7"]?.ToString()?.Trim() ?? ""; cc = SafeToInt(rd["ADQECCO7"]); return !string.IsNullOrEmpty(gl); }
    // 8
    tr = rd["ADQECTR8"]?.ToString()?.Trim() ?? "";
    if (tr == tcodeBuscado) { gl = rd["ADQECNT8"]?.ToString()?.Trim() ?? ""; cc = SafeToInt(rd["ADQECCO8"]); return !string.IsNullOrEmpty(gl); }
    // 9
    tr = rd["ADQECTR9"]?.ToString()?.Trim() ?? "";
    if (tr == tcodeBuscado) { gl = rd["ADQECNT9"]?.ToString()?.Trim() ?? ""; cc = SafeToInt(rd["ADQECCO9"]); return !string.IsNullOrEmpty(gl); }
    // 10
    tr = rd["ADQECTR10"]?.ToString()?.Trim() ?? "";
    if (tr == tcodeBuscado) { gl = rd["ADQECNT10"]?.ToString()?.Trim() ?? ""; cc = SafeToInt(rd["ADQECC10"]); return !string.IsNullOrEmpty(gl); }
    // 11
    tr = rd["ADQECTR11"]?.ToString()?.Trim() ?? "";
    if (tr == tcodeBuscado) { gl = rd["ADQECNT11"]?.ToString()?.Trim() ?? ""; cc = SafeToInt(rd["ADQECC11"]); return !string.IsNullOrEmpty(gl); }
    // 12
    tr = rd["ADQECTR12"]?.ToString()?.Trim() ?? "";
    if (tr == tcodeBuscado) { gl = rd["ADQECNT12"]?.ToString()?.Trim() ?? ""; cc = SafeToInt(rd["ADQECC12"]); return !string.IsNullOrEmpty(gl); }
    // 13
    tr = rd["ADQECTR13"]?.ToString()?.Trim() ?? "";
    if (tr == tcodeBuscado) { gl = rd["ADQECNT13"]?.ToString()?.Trim() ?? ""; cc = SafeToInt(rd["ADQECC13"]); return !string.IsNullOrEmpty(gl); }
    // 14
    tr = rd["ADQECTR14"]?.ToString()?.Trim() ?? "";
    if (tr == tcodeBuscado) { gl = rd["ADQECNT14"]?.ToString()?.Trim() ?? ""; cc = SafeToInt(rd["ADQECC14"]); return !string.IsNullOrEmpty(gl); }
    // 15
    tr = rd["ADQECTR15"]?.ToString()?.Trim() ?? "";
    if (tr == tcodeBuscado) { gl = rd["ADQECNT15"]?.ToString()?.Trim() ?? ""; cc = SafeToInt(rd["ADQECC15"]); return !string.IsNullOrEmpty(gl); }

    return false;
}

/// <summary>
/// Busca en ADQCTL (control/secuencia) la GL/CC cuyo T-code (ADQCTR1..15) coincida con <paramref name="tcodeBuscado"/>.
/// </summary>
private bool TryGetGLFromAdqCtl(string control, int secuencia, string tcodeBuscado, out string gl, out int cc)
{
    gl = ""; cc = 0;
    var q = QueryBuilder.Core.QueryBuilder
        .From("ADQCTL", "BCAH96DTA")
        .Select("*")
        .WhereRaw($"ADQCONT = '{control}'")
        .WhereRaw($"ADQNUM = {secuencia}")
        .FetchNext(1)
        .Build();

    using var cmd = _connection.GetDbCommand(q, _contextAccessor.HttpContext!);
    using var rd  = cmd.ExecuteReader();
    if (!rd.Read()) return false;

    string tr;
    // mismo patrón que ADQECTL, sin bucles
    tr = rd["ADQCTR1"]?.ToString()?.Trim() ?? "";
    if (tr == tcodeBuscado) { gl = rd["ADQCNT1"]?.ToString()?.Trim() ?? ""; cc = SafeToInt(rd["ADQCCO1"]); return !string.IsNullOrEmpty(gl); }

    tr = rd["ADQCTR2"]?.ToString()?.Trim() ?? "";
    if (tr == tcodeBuscado) { gl = rd["ADQCNT2"]?.ToString()?.Trim() ?? ""; cc = SafeToInt(rd["ADQCCO2"]); return !string.IsNullOrEmpty(gl); }

    tr = rd["ADQCTR3"]?.ToString()?.Trim() ?? "";
    if (tr == tcodeBuscado) { gl = rd["ADQCNT3"]?.ToString()?.Trim() ?? ""; cc = SafeToInt(rd["ADQCCO3"]); return !string.IsNullOrEmpty(gl); }

    tr = rd["ADQCTR4"]?.ToString()?.Trim() ?? "";
    if (tr == tcodeBuscado) { gl = rd["ADQCNT4"]?.ToString()?.Trim() ?? ""; cc = SafeToInt(rd["ADQCCO4"]); return !string.IsNullOrEmpty(gl); }

    tr = rd["ADQCTR5"]?.ToString()?.Trim() ?? "";
    if (tr == tcodeBuscado) { gl = rd["ADQCNT5"]?.ToString()?.Trim() ?? ""; cc = SafeToInt(rd["ADQCCO5"]); return !string.IsNullOrEmpty(gl); }

    tr = rd["ADQCTR6"]?.ToString()?.Trim() ?? "";
    if (tr == tcodeBuscado) { gl = rd["ADQCNT6"]?.ToString()?.Trim() ?? ""; cc = SafeToInt(rd["ADQCCO6"]); return !string.IsNullOrEmpty(gl); }

    tr = rd["ADQCTR7"]?.ToString()?.Trim() ?? "";
    if (tr == tcodeBuscado) { gl = rd["ADQCNT7"]?.ToString()?.Trim() ?? ""; cc = SafeToInt(rd["ADQCCO7"]); return !string.IsNullOrEmpty(gl); }

    tr = rd["ADQCTR8"]?.ToString()?.Trim() ?? "";
    if (tr == tcodeBuscado) { gl = rd["ADQCNT8"]?.ToString()?.Trim() ?? ""; cc = SafeToInt(rd["ADQCCO8"]); return !string.IsNullOrEmpty(gl); }

    tr = rd["ADQCTR9"]?.ToString()?.Trim() ?? "";
    if (tr == tcodeBuscado) { gl = rd["ADQCNT9"]?.ToString()?.Trim() ?? ""; cc = SafeToInt(rd["ADQCCO9"]); return !string.IsNullOrEmpty(gl); }

    tr = rd["ADQCTR10"]?.ToString()?.Trim() ?? "";
    if (tr == tcodeBuscado) { gl = rd["ADQCNT10"]?.ToString()?.Trim() ?? ""; cc = SafeToInt(rd["ADQCC10"]); return !string.IsNullOrEmpty(gl); }

    tr = rd["ADQCTR11"]?.ToString()?.Trim() ?? "";
    if (tr == tcodeBuscado) { gl = rd["ADQCNT11"]?.ToString()?.Trim() ?? ""; cc = SafeToInt(rd["ADQCC11"]); return !string.IsNullOrEmpty(gl); }

    tr = rd["ADQCTR12"]?.ToString()?.Trim() ?? "";
    if (tr == tcodeBuscado) { gl = rd["ADQCNT12"]?.ToString()?.Trim() ?? ""; cc = SafeToInt(rd["ADQCC12"]); return !string.IsNullOrEmpty(gl); }

    tr = rd["ADQCTR13"]?.ToString()?.Trim() ?? "";
    if (tr == tcodeBuscado) { gl = rd["ADQCNT13"]?.ToString()?.Trim() ?? ""; cc = SafeToInt(rd["ADQCC13"]); return !string.IsNullOrEmpty(gl); }

    tr = rd["ADQCTR14"]?.ToString()?.Trim() ?? "";
    if (tr == tcodeBuscado) { gl = rd["ADQCNT14"]?.ToString()?.Trim() ?? ""; cc = SafeToInt(rd["ADQCC14"]); return !string.IsNullOrEmpty(gl); }

    tr = rd["ADQCTR15"]?.ToString()?.Trim() ?? "";
    if (tr == tcodeBuscado) { gl = rd["ADQCNT15"]?.ToString()?.Trim() ?? ""; cc = SafeToInt(rd["ADQCC15"]); return !string.IsNullOrEmpty(gl); }

    return false;
}

private static int SafeToInt(object? o)
    => o is null || o is DBNull ? 0 : Convert.ToInt32(o, CultureInfo.InvariantCulture);


var p = ResolverParametrosIntLotes(
    perfil: perfilTranserver,
    naturalezaCliente: nat[0],                 // 'C' o 'D'
    numeroCuenta: guardarTransaccionesDto.NumeroCuenta,
    codigoComercio: guardarTransaccionesDto.CodigoComercio,
    terminal: guardarTransaccionesDto.Terminal,
    nombreComercio: guardarTransaccionesDto.NombreComercio,
    idUnico: guardarTransaccionesDto.IdTransaccionUnico,
    monedaIsoNum: 0                            // si tu RPG lo espera, envía el ISO num correspondiente
);

// Con 'p' ya tienes:
// p.CuentaGL, p.CentroCostoGL, p.Des001..p.Des004, p.TcodeCliente, p.TcodeGL, etc.
// Si tu Int_lotes pide 4 renglones, usarás 2 (cliente y GL) y dejas los otros en 0/blank.




