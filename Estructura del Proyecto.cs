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
