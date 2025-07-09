using QueryBuilder.Attributes;

namespace API_1_TERCEROS_REMESADORAS.Models.BDD;

/// <summary>
/// Representa la tabla BTSACTA para registrar información de transferencias.
/// </summary>
public class BtsaCtaModel
{
    [SqlColumnDefinition("INOCONFIR", SqlDataType.Char, 20)]
    public string INOCONFIR { get; set; } = string.Empty;

    [SqlColumnDefinition("IDATRECI", SqlDataType.Char, 8)]
    public string IDATRECI { get; set; } = string.Empty;

    [SqlColumnDefinition("IHORRECI", SqlDataType.Char, 9)]
    public string IHORRECI { get; set; } = string.Empty;

    [SqlColumnDefinition("IDATCONF", SqlDataType.Char, 8)]
    public string IDATCONF { get; set; } = string.Empty;

    [SqlColumnDefinition("IHORCONF", SqlDataType.Char, 9)]
    public string IHORCONF { get; set; } = string.Empty;

    [SqlColumnDefinition("IDATVAL", SqlDataType.Char, 8)]
    public string IDATVAL { get; set; } = string.Empty;

    [SqlColumnDefinition("IHORVAL", SqlDataType.Char, 9)]
    public string IHORVAL { get; set; } = string.Empty;

    [SqlColumnDefinition("IDATPAGO", SqlDataType.Char, 8)]
    public string IDATPAGO { get; set; } = string.Empty;

    [SqlColumnDefinition("IHORPAGO", SqlDataType.Char, 9)]
    public string IHORPAGO { get; set; } = string.Empty;

    [SqlColumnDefinition("IDATACRE", SqlDataType.Char, 8)]
    public string IDATACRE { get; set; } = string.Empty;

    [SqlColumnDefinition("IHORACRE", SqlDataType.Char, 9)]
    public string IHORACRE { get; set; } = string.Empty;

    [SqlColumnDefinition("IDATRECH", SqlDataType.Char, 8)]
    public string IDATRECH { get; set; } = string.Empty;

    [SqlColumnDefinition("IHORRECH", SqlDataType.Char, 9)]
    public string IHORRECH { get; set; } = string.Empty;

    [SqlColumnDefinition("ITIPPAGO", SqlDataType.Char, 10)]
    public string ITIPPAGO { get; set; } = string.Empty;

    [SqlColumnDefinition("ISERVICD", SqlDataType.Char, 10)]
    public string ISERVICD { get; set; } = string.Empty;

    [SqlColumnDefinition("IDESPAIS", SqlDataType.Char, 10)]
    public string IDESPAIS { get; set; } = string.Empty;

    [SqlColumnDefinition("IDESMONE", SqlDataType.Char, 10)]
    public string IDESMONE { get; set; } = string.Empty;

    [SqlColumnDefinition("ISAGENCD", SqlDataType.Char, 10)]
    public string ISAGENCD { get; set; } = string.Empty;

    [SqlColumnDefinition("ISPAISCD", SqlDataType.Char, 10)]
    public string ISPAISCD { get; set; } = string.Empty;

    [SqlColumnDefinition("ISTATECD", SqlDataType.Char, 10)]
    public string ISTATECD { get; set; } = string.Empty;

    [SqlColumnDefinition("IRAGENCD", SqlDataType.Char, 10)]
    public string IRAGENCD { get; set; } = string.Empty;

    [SqlColumnDefinition("ITICUENTA", SqlDataType.Char, 10)]
    public string ITICUENTA { get; set; } = string.Empty;

    [SqlColumnDefinition("INOCUENTA", SqlDataType.Char, 20)]
    public string INOCUENTA { get; set; } = string.Empty;

    [SqlColumnDefinition("INUMREFER", SqlDataType.Char, 20)]
    public string INUMREFER { get; set; } = string.Empty;

    [SqlColumnDefinition("ISTSREM", SqlDataType.Char, 10)]
    public string ISTSREM { get; set; } = string.Empty;

    [SqlColumnDefinition("ISTSPRO", SqlDataType.Char, 10)]
    public string ISTSPRO { get; set; } = string.Empty;

    [SqlColumnDefinition("IERR", SqlDataType.Char, 10)]
    public string IERR { get; set; } = string.Empty;

    [SqlColumnDefinition("IERRDSC", SqlDataType.Char, 100)]
    public string IERRDSC { get; set; } = string.Empty;

    [SqlColumnDefinition("IDSCRECH", SqlDataType.Char, 100)]
    public string IDSCRECH { get; set; } = string.Empty;

    [SqlColumnDefinition("ACODPAIS", SqlDataType.Char, 10)]
    public string ACODPAIS { get; set; } = string.Empty;

    [SqlColumnDefinition("ACODMONED", SqlDataType.Char, 10)]
    public string ACODMONED { get; set; } = string.Empty;

    [SqlColumnDefinition("AMTOENVIA", SqlDataType.Char, 20)]
    public string AMTOENVIA { get; set; } = string.Empty;

    [SqlColumnDefinition("AMTOCALCU", SqlDataType.Char, 20)]
    public string AMTOCALCU { get; set; } = string.Empty;

    [SqlColumnDefinition("AFACTCAMB", SqlDataType.Char, 20)]
    public string AFACTCAMB { get; set; } = string.Empty;

    [SqlColumnDefinition("BPRIMNAME", SqlDataType.Char, 50)]
    public string BPRIMNAME { get; set; } = string.Empty;

    [SqlColumnDefinition("BSECUNAME", SqlDataType.Char, 50)]
    public string BSECUNAME { get; set; } = string.Empty;

    [SqlColumnDefinition("BAPELLIDO", SqlDataType.Char, 50)]
    public string BAPELLIDO { get; set; } = string.Empty;

    [SqlColumnDefinition("BSEGUAPE", SqlDataType.Char, 50)]
    public string BSEGUAPE { get; set; } = string.Empty;

    [SqlColumnDefinition("BDIRECCIO", SqlDataType.Char, 100)]
    public string BDIRECCIO { get; set; } = string.Empty;

    [SqlColumnDefinition("BCIUDAD", SqlDataType.Char, 50)]
    public string BCIUDAD { get; set; } = string.Empty;

    [SqlColumnDefinition("BESTADO", SqlDataType.Char, 50)]
    public string BESTADO { get; set; } = string.Empty;

    [SqlColumnDefinition("BPAIS", SqlDataType.Char, 50)]
    public string BPAIS { get; set; } = string.Empty;

    [SqlColumnDefinition("BCODPOST", SqlDataType.Char, 10)]
    public string BCODPOST { get; set; } = string.Empty;

    [SqlColumnDefinition("BTELEFONO", SqlDataType.Char, 15)]
    public string BTELEFONO { get; set; } = string.Empty;

    [SqlColumnDefinition("CPRIMNAME", SqlDataType.Char, 50)]
    public string CPRIMNAME { get; set; } = string.Empty;

    [SqlColumnDefinition("CSECUNAME", SqlDataType.Char, 50)]
    public string CSECUNAME { get; set; } = string.Empty;

    [SqlColumnDefinition("CAPELLIDO", SqlDataType.Char, 50)]
    public string CAPELLIDO { get; set; } = string.Empty;

    [SqlColumnDefinition("CSEGUAPE", SqlDataType.Char, 50)]
    public string CSEGUAPE { get; set; } = string.Empty;

    [SqlColumnDefinition("CDIRECCIO", SqlDataType.Char, 100)]
    public string CDIRECCIO { get; set; } = string.Empty;

    [SqlColumnDefinition("CCIUDAD", SqlDataType.Char, 50)]
    public string CCIUDAD { get; set; } = string.Empty;

    [SqlColumnDefinition("CESTADO", SqlDataType.Char, 50)]
    public string CESTADO { get; set; } = string.Empty;

    [SqlColumnDefinition("CPAIS", SqlDataType.Char, 50)]
    public string CPAIS { get; set; } = string.Empty;

    [SqlColumnDefinition("CCODPOST", SqlDataType.Char, 10)]
    public string CCODPOST { get; set; } = string.Empty;

    [SqlColumnDefinition("CTELEFONO", SqlDataType.Char, 15)]
    public string CTELEFONO { get; set; } = string.Empty;

    [SqlColumnDefinition("DTIDENT", SqlDataType.Char, 20)]
    public string DTIDENT { get; set; } = string.Empty;

    [SqlColumnDefinition("ESALEDT", SqlDataType.Char, 8)]
    public string ESALEDT { get; set; } = string.Empty;

    [SqlColumnDefinition("EMONREFER", SqlDataType.Char, 10)]
    public string EMONREFER { get; set; } = string.Empty;

    [SqlColumnDefinition("ETASAREFE", SqlDataType.Char, 20)]
    public string ETASAREFE { get; set; } = string.Empty;

    [SqlColumnDefinition("EMTOREF", SqlDataType.Char, 20)]
    public string EMTOREF { get; set; } = string.Empty;
}
