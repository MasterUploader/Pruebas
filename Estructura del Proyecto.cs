namespace Adquirencia.Models.Db2;

/// <summary>
/// Representa la tabla BNKPRD01.CFP801.
/// Contiene la definición de perfiles de Transacción del Servidor,
/// con parámetros de balance, cuentas contables y códigos de rechazo.
/// </summary>
public class CFP801()
{
    /// <summary>Bank Number (NUMERIC 3).</summary>
    public int CFTSBK { get; set; }

    /// <summary>Profile Name (CHAR 13).</summary>
    public string CFTSKY { get; set; } = string.Empty;

    /// <summary>Profile Description (CHAR 30).</summary>
    public string CFTSPD { get; set; } = string.Empty;

    /// <summary>Profile Key - Must be unique (CHAR 6).</summary>
    public string CFTSPK { get; set; } = string.Empty;

    /// <summary>Tran Server Teller Number (NUMERIC 4).</summary>
    public int CFTSTE { get; set; }

    /// <summary>Tran Server Branch Number (NUMERIC 3).</summary>
    public int CFTSBR { get; set; }

    /// <summary>Tran Server Till Number (NUMERIC 4).</summary>
    public int CFTSTI { get; set; }

    /// <summary>Input Formatting Program Name (CHAR 10).</summary>
    public string CFTSFP { get; set; } = string.Empty;

    /// <summary>Input Source Code (NUMERIC 1).</summary>
    public int CFTSSC { get; set; }

    /// <summary>Process To First Subsystem (NUMERIC 2).</summary>
    public int CFTSS1 { get; set; }

    /// <summary>Process To Second Subsystem (NUMERIC 2).</summary>
    public int CFTSS2 { get; set; }

    /// <summary>Process To Third Subsystem (NUMERIC 2).</summary>
    public int CFTSS3 { get; set; }

    /// <summary>Process To Fourth Subsystem (NUMERIC 2).</summary>
    public int CFTSS4 { get; set; }

    /// <summary>Process To Fifth Subsystem (NUMERIC 2).</summary>
    public int CFTSS5 { get; set; }

    /// <summary>Process To Sixth Subsystem (NUMERIC 2).</summary>
    public int CFTSS6 { get; set; }

    /// <summary>Active/Inactive Status Code (NUMERIC 1).</summary>
    public int CFTSST { get; set; }

    /// <summary>Initial Override Code (NUMERIC 1).</summary>
    public int CFTSOR { get; set; }

    /// <summary>File Must Balance Code (NUMERIC 1).</summary>
    public int CFTSB { get; set; }

    /// <summary>Generate Balancing Entry Code (NUMERIC 1).</summary>
    public int CFTSGE { get; set; }

    /// <summary>GL Credit Balancing Account (DECIMAL 12).</summary>
    public decimal CFTSGC { get; set; }

    /// <summary>GL Cost Center Credit (DECIMAL 5).</summary>
    public decimal CFTCCC { get; set; }

    /// <summary>GL Debit Balancing Account (DECIMAL 12).</summary>
    public decimal CFTSGD { get; set; }

    /// <summary>GL Cost Center Debit (DECIMAL 5).</summary>
    public decimal CFTCCD { get; set; }

    /// <summary>GL Cost Center Code (NUMERIC 1).</summary>
    public int CFTSCC { get; set; }

    /// <summary>Teller Posting Credit Trancode 1 (CHAR 4).</summary>
    public string CFTPC1 { get; set; } = string.Empty;

    /// <summary>Teller Posting Debit Trancode 1 (CHAR 4).</summary>
    public string CFTPD1 { get; set; } = string.Empty;

    /// <summary>Teller Reject Credit Trancode 1 (CHAR 4).</summary>
    public string CFTRC1 { get; set; } = string.Empty;

    /// <summary>Teller Reject Debit Trancode 1 (CHAR 4).</summary>
    public string CFTRD1 { get; set; } = string.Empty;

    /// <summary>Teller Posting Credit Trancode 2 (CHAR 4).</summary>
    public string CFTPC2 { get; set; } = string.Empty;

    /// <summary>Teller Posting Debit Trancode 2 (CHAR 4).</summary>
    public string CFTPD2 { get; set; } = string.Empty;

    /// <summary>Teller Reject Credit Trancode 2 (CHAR 4).</summary>
    public string CFTRC2 { get; set; } = string.Empty;

    /// <summary>Teller Reject Debit Trancode 2 (CHAR 4).</summary>
    public string CFTRD2 { get; set; } = string.Empty;

    /// <summary>Teller Posting Credit Trancode 3 (CHAR 4).</summary>
    public string CFTPC3 { get; set; } = string.Empty;

    /// <summary>Teller Posting Debit Trancode 3 (CHAR 4).</summary>
    public string CFTPD3 { get; set; } = string.Empty;

    /// <summary>Teller Reject Credit Trancode 3 (CHAR 4).</summary>
    public string CFTRC3 { get; set; } = string.Empty;

    /// <summary>Teller Reject Debit Trancode 3 (CHAR 4).</summary>
    public string CFTRD3 { get; set; } = string.Empty;

    /// <summary>Teller Posting Credit Trancode 4 (CHAR 4).</summary>
    public string CFTPC4 { get; set; } = string.Empty;

    /// <summary>Teller Posting Debit Trancode 4 (CHAR 4).</summary>
    public string CFTPD4 { get; set; } = string.Empty;

    /// <summary>Teller Reject Credit Trancode 4 (CHAR 4).</summary>
    public string CFTRC4 { get; set; } = string.Empty;

    /// <summary>Teller Reject Debit Trancode 4 (CHAR 4).</summary>
    public string CFTRD4 { get; set; } = string.Empty;

    /// <summary>Teller Posting Credit Trancode 5 (CHAR 4).</summary>
    public string CFTPC5 { get; set; } = string.Empty;

    /// <summary>Teller Posting Debit Trancode 5 (CHAR 4).</summary>
    public string CFTPD5 { get; set; } = string.Empty;

    /// <summary>Teller Reject Credit Trancode 5 (CHAR 4).</summary>
    public string CFTRC5 { get; set; } = string.Empty;

    /// <summary>Teller Reject Debit Trancode 5 (CHAR 4).</summary>
    public string CFTRD5 { get; set; } = string.Empty;

    /// <summary>Teller Posting Credit Trancode 6 (CHAR 4).</summary>
    public string CFTPC6 { get; set; } = string.Empty;

    /// <summary>Teller Posting Debit Trancode 6 (CHAR 4).</summary>
    public string CFTPD6 { get; set; } = string.Empty;

    /// <summary>Teller Reject Credit Trancode 6 (CHAR 4).</summary>
    public string CFTRC6 { get; set; } = string.Empty;

    /// <summary>Teller Reject Debit Trancode 6 (CHAR 4).</summary>
    public string CFTRD6 { get; set; } = string.Empty;

    /// <summary>Teller Balancing CR Trancode (CHAR 4).</summary>
    public string CFTSBC { get; set; } = string.Empty;

    /// <summary>Teller Balancing DR Trancode (CHAR 4).</summary>
    public string CFTSBD { get; set; } = string.Empty;

    /// <summary>Reject Trans Description (CHAR 30).</summary>
    public string CFTSRD { get; set; } = string.Empty;
}
