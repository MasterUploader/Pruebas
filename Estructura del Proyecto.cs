Estos son los campos agrega los que hacen falta y adapta:

namespace MS_BAN_56_ProcesamientoTransaccionesPOS.Models.Dtos;

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
    public string DeCr1 { get; set; } = "D";
    /// <summary>Centro de costo del movimiento 1.</summary>
    public decimal CentroCosto1 { get; set; }

    // ------------------------- Movimiento 2 (ACREDITADO) -----------------------
    /// <summary>Tipo de cuenta del movimiento 2 (1=ahorros, 6=cheques, 40=contable).</summary>
    public decimal TipoMov2 { get; set; }
    /// <summary>Número de cuenta del movimiento 2.</summary>
    public decimal CuentaMov2 { get; set; }
    /// <summary>Naturaleza del movimiento 2: 'D' o 'C' (aquí siempre será Crédito).</summary>
    public string DeCr2 { get; set; } = "C";
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

    /// <summary>
    /// error al llamar a INT_LOTES (0=OK, 1=error de sistema, 2=error de negocio).
    /// </summary>
    public int ErrorMetodo { get; set; }

    /// <summary>Código de error devuelto por INT_LOTES (si aplica).</summary>
    public string DescripcionError { get; set; } = string.Empty;
}
