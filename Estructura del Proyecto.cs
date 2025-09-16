namespace Adquirencia.Models.Db2;

/// <summary>
/// Representa la tabla BNKPRD01.CFP102.
/// Contiene información de sucursales bancarias, incluyendo nombre, dirección,
/// códigos regionales, feriados y parámetros operativos.
/// </summary>
public class CFP102()
{
    /// <summary>Bank Number (NUMERIC 3).</summary>
    public int CFBANK { get; set; }

    /// <summary>Branch Number (NUMERIC 3).</summary>
    public int CFBRCH { get; set; }

    /// <summary>Branch Name (CHAR 40).</summary>
    public string CFBRNM { get; set; } = string.Empty;

    /// <summary>Branch Address 1 (CHAR 30).</summary>
    public string CFBRA1 { get; set; } = string.Empty;

    /// <summary>Branch Address 2 (CHAR 30).</summary>
    public string CFBRA2 { get; set; } = string.Empty;

    /// <summary>Zip Code (NUMERIC 5).</summary>
    public int CFBZIP { get; set; }

    /// <summary>Postal Code (CHAR 10).</summary>
    public string CFBRPC { get; set; } = string.Empty;

    /// <summary>State Code for Branches (NUMERIC 2).</summary>
    public int CFBRST { get; set; }

    /// <summary>Region Number (DECIMAL 3).</summary>
    public decimal CFBRRG { get; set; }

    /// <summary>Bank State Branch (NUMERIC 6).</summary>
    public int CFBSB { get; set; }

    /// <summary>Branch Manager Name (CHAR 30).</summary>
    public string CFBMGN { get; set; } = string.Empty;

    /// <summary>Holiday 1 (DECIMAL 7).</summary>
    public decimal CFHB01 { get; set; }

    /// <summary>Holiday 2 (DECIMAL 7).</summary>
    public decimal CFHB02 { get; set; }

    /// <summary>Holiday 3 (DECIMAL 7).</summary>
    public decimal CFHB03 { get; set; }

    /// <summary>Holiday 4 (DECIMAL 7).</summary>
    public decimal CFHB04 { get; set; }

    /// <summary>Holiday 5 (DECIMAL 7).</summary>
    public decimal CFHB05 { get; set; }

    /// <summary>Holiday 6 (DECIMAL 7).</summary>
    public decimal CFHB06 { get; set; }

    /// <summary>Holiday 7 (DECIMAL 7).</summary>
    public decimal CFHB07 { get; set; }

    /// <summary>Holiday 8 (DECIMAL 7).</summary>
    public decimal CFHB08 { get; set; }

    /// <summary>Holiday 9 (DECIMAL 7).</summary>
    public decimal CFHB09 { get; set; }

    /// <summary>Holiday 10 (DECIMAL 7).</summary>
    public decimal CFHB10 { get; set; }

    /// <summary>Holiday 11 (DECIMAL 7).</summary>
    public decimal CFHB11 { get; set; }

    /// <summary>Holiday 12 (DECIMAL 7).</summary>
    public decimal CFHB12 { get; set; }

    /// <summary>Holiday 13 (DECIMAL 7).</summary>
    public decimal CFHB13 { get; set; }

    /// <summary>Holiday 14 (DECIMAL 7).</summary>
    public decimal CFHB14 { get; set; }

    /// <summary>Holiday 15 (DECIMAL 7).</summary>
    public decimal CFHB15 { get; set; }

    /// <summary>Holiday 16 (DECIMAL 7).</summary>
    public decimal CFHB16 { get; set; }

    /// <summary>Holiday 17 (DECIMAL 7).</summary>
    public decimal CFHB17 { get; set; }

    /// <summary>Holiday 18 (DECIMAL 7).</summary>
    public decimal CFHB18 { get; set; }

    /// <summary>Holiday 19 (DECIMAL 7).</summary>
    public decimal CFHB19 { get; set; }

    /// <summary>Holiday 20 (DECIMAL 7).</summary>
    public decimal CFHB20 { get; set; }

    /// <summary>Holiday 21 (DECIMAL 7).</summary>
    public decimal CFHB21 { get; set; }

    /// <summary>Holiday 22 (DECIMAL 7).</summary>
    public decimal CFHB22 { get; set; }

    /// <summary>Holiday 23 (DECIMAL 7).</summary>
    public decimal CFHB23 { get; set; }

    /// <summary>Holiday 24 (DECIMAL 7).</summary>
    public decimal CFHB24 { get; set; }

    /// <summary>Branch Week Definition (CHAR 7).</summary>
    public string CFBWK { get; set; } = string.Empty;

    /// <summary>Time of funds release (DECIMAL 6).</summary>
    public decimal CFBRTM { get; set; }
}
