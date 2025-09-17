namespace Adquirencia.Models.Db2;

/// <summary>
/// Parámetros y fechas operativas del core (BNKPRD01.TAP001).
/// Provee fechas en calendario y julianas, banderas de proceso y perfiles por defecto
/// para crédito/débito a nivel de Transaction Server.
/// </summary>
/// <remarks>
/// - Campos DEC/NUM sin escala → <see cref="int"/> para conservar exactitud.
/// - Campos de texto (CHAR) → <see cref="string"/> con inicialización a Empty.
/// - DSCDT/DSCNDT suelen estar en formato calendario (CYYMMDD o YYYYMMDD según instalación).
/// - Varios campos *julianos* (DSDT, DSNDT, DSFDY, etc.) se entregan como enteros para conversión externa.
/// </remarks>
public class TAP001()
{
    /// <summary>Bank Number (NUM 3).</summary>
    public int DSBK { get; set; }

    /// <summary>Current Date/Calendar (DEC 9). Calendario (CYYMMDD/YYYMMDD según site).</summary>
    public int DSCDT { get; set; }

    /// <summary>Process Thru Date/Calendar (DEC 9).</summary>
    public int DSCNDT { get; set; }

    /// <summary>Current Date - Julian (DEC 7).</summary>
    public int DSDT { get; set; }

    /// <summary>Process Thru Date - Julian (DEC 7).</summary>
    public int DSNDT { get; set; }

    /// <summary>Next Processing Date - Julian (DEC 7).</summary>
    public int DSNPDT { get; set; }

    /// <summary>First Day of Year - Julian (DEC 7).</summary>
    public int DSFDY { get; set; }

    /// <summary>Last Day of Year - Julian (DEC 7).</summary>
    public int DSLDY { get; set; }

    /// <summary>First Day of Month - Julian (DEC 7).</summary>
    public int DSFDM { get; set; }

    /// <summary>Last Day of Month - Julian (DEC 7).</summary>
    public int DSLDM { get; set; }

    /// <summary>First Day of Week - Julian (DEC 7).</summary>
    public int DSFDW { get; set; }

    /// <summary>Last Day of Week - Julian (DEC 7).</summary>
    public int D SLDW { get; set; }  // Nota funcional: usado para cierres semanales.

    /// <summary>Week Process Flag (CHAR 1). Banderín de proceso semanal.</summary>
    public string DSWKFG { get; set; } = string.Empty;

    /// <summary>Month Process Flag (CHAR 1). Banderín de proceso mensual.</summary>
    public string DSMOFG { get; set; } = string.Empty;

    /// <summary>Year Process Flag (CHAR 1). Banderín de proceso anual.</summary>
    public string DSYRFG { get; set; } = string.Empty;

    /// <summary>Process Week Definition (CHAR 7). Definición de semana operativa.</summary>
    public string DSWK { get; set; } = string.Empty;

    /// <summary>Process Today Flag (CHAR 1). Indica si se procesa hoy.</summary>
    public string DSPROC { get; set; } = string.Empty;

    /// <summary>Day of Week (NUM 1). 1..7 según configuración del core.</summary>
    public int DSDOW { get; set; }

    /// <summary>Bank Name (CHAR 40).</summary>
    public string DSBKNM { get; set; } = string.Empty;

    /// <summary>Withholding Percentage (DEC 7,6). Porcentaje de retención.</summary>
    public decimal DSWHPC { get; set; }

    /// <summary>Last Date Processed/Julian (DEC 7).</summary>
    public int DSLPRC { get; set; }

    /// <summary>Time Print Flag (CHAR 1). Control de impresión de hora.</summary>
    public string DSTMPF { get; set; } = string.Empty;

    /// <summary>Tran Print Flag (CHAR 1). Control de impresión de transacciones.</summary>
    public string DSTAPF { get; set; } = string.Empty;

    /// <summary>Last cheque number printed (DEC 7).</summary>
    public int DSCKNR { get; set; }

    /// <summary>Tax year start date - Julian (DEC 7).</summary>
    public int DSFYMO { get; set; }

    /// <summary>Withholding Control Option (NUM 1).</summary>
    public int DSWHFG { get; set; }

    /// <summary>Withholding Adjustment Option (NUM 1).</summary>
    public int DSWHRS { get; set; }

    /// <summary>Tran server profile for CR (CHAR 13). Perfil por defecto para crédito.</summary>
    public string DSCRPF { get; set; } = string.Empty;

    /// <summary>Tran server profile for DR (CHAR 13). Perfil por defecto para débito.</summary>
    public string DSDRPF { get; set; } = string.Empty;

    /// <summary>Institution reference (CHAR 20). Referencia institucional.</summary>
    public string DSIREF { get; set; } = string.Empty;
}
