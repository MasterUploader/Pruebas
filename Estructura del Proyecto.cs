namespace Adquirencia.Models.Db2;

/// <summary>
/// Representa la tabla BNKPRD01.POP801 (Batch Header).
/// Contiene información de control de lotes: perfil, fecha, totales y estados.
/// </summary>
public class POP801
{
    /// <summary>
    /// Bank Number (NUMERIC 3).
    /// </summary>
    public int FTTSBK { get; set; }

    /// <summary>
    /// Transaction Server Profile (CHAR 13).
    /// </summary>
    public string FTTSKY { get; set; } = string.Empty;

    /// <summary>
    /// Processing Date - Effective (DECIMAL 7, formato CYYMMDD).
    /// </summary>
    public int FTTSDT { get; set; }

    /// <summary>
    /// Batch Number (001-999) (NUMERIC 3).
    /// </summary>
    public int FTSBT { get; set; }

    /// <summary>
    /// Originated By (CHAR 10).
    /// </summary>
    public string FTTSOR { get; set; } = string.Empty;

    /// <summary>
    /// File Status (NUMERIC 2).
    /// </summary>
    public int FTTSST { get; set; }

    /// <summary>
    /// Total Debit Items Count (NUMERIC 5).
    /// </summary>
    public int FTTSDI { get; set; }

    /// <summary>
    /// Total Credit Items Count (NUMERIC 5).
    /// </summary>
    public int FTTSCI { get; set; }

    /// <summary>
    /// Total Debit Amount - LCYE (NUMERIC 15,2).
    /// </summary>
    public decimal FTTSID { get; set; }

    /// <summary>
    /// Total Credit Amount - LCYE (NUMERIC 15,2).
    /// </summary>
    public decimal FTTSIC { get; set; }

    /// <summary>
    /// Total Debit Items Posted (NUMERIC 5).
    /// </summary>
    public int FTTSDP { get; set; }

    /// <summary>
    /// Total Credit Items Posted (NUMERIC 5).
    /// </summary>
    public int FTTSCP { get; set; }

    /// <summary>
    /// Total Debit Amount Posted (NUMERIC 15,2).
    /// </summary>
    public decimal FTTSPD { get; set; }

    /// <summary>
    /// Total Credit Amount Posted (NUMERIC 15,2).
    /// </summary>
    public decimal FTTSPC { get; set; }

    /// <summary>
    /// FCYE Debit Balancing Entry (NUMERIC 15,2).
    /// </summary>
    public decimal FTTSBD { get; set; }

    /// <summary>
    /// LCYE Debit Balancing Entry (NUMERIC 15,2).
    /// </summary>
    public decimal FTTSLD { get; set; }

    /// <summary>
    /// FCYE Credit Balancing Entry (NUMERIC 15,2).
    /// </summary>
    public decimal FTTSBC { get; set; }

    /// <summary>
    /// LCYE Credit Balancing Entry (NUMERIC 15,2).
    /// </summary>
    public decimal FTTSLC { get; set; }
}
