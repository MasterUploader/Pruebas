using QueryBuilder.Attributes;

public class BtsaCtaModel
{
    [SqlColumnDefinition("CHAR", Length = 20)] public string INOCONFIR { get; set; }
    [SqlColumnDefinition("CHAR", Length = 8)]  public string IDATRECI { get; set; }
    [SqlColumnDefinition("CHAR", Length = 9)]  public string IHORRECI { get; set; }
    [SqlColumnDefinition("CHAR", Length = 8)]  public string IDATCONF { get; set; }
    [SqlColumnDefinition("CHAR", Length = 9)]  public string IHORCONF { get; set; }
    [SqlColumnDefinition("CHAR", Length = 8)]  public string IDATVAL { get; set; }
    [SqlColumnDefinition("CHAR", Length = 9)]  public string IHORVAL { get; set; }
    [SqlColumnDefinition("CHAR", Length = 8)]  public string IDATPAGO { get; set; }
    [SqlColumnDefinition("CHAR", Length = 9)]  public string IHORPAGO { get; set; }
    [SqlColumnDefinition("CHAR", Length = 8)]  public string IDATACRE { get; set; }
    [SqlColumnDefinition("CHAR", Length = 9)]  public string IHORACRE { get; set; }
    [SqlColumnDefinition("CHAR", Length = 8)]  public string IDATRECH { get; set; }
    [SqlColumnDefinition("CHAR", Length = 9)]  public string IHORRECH { get; set; }

    [SqlColumnDefinition("CHAR", Length = 10)] public string ITIPPAGO { get; set; }
    [SqlColumnDefinition("CHAR", Length = 10)] public string ISERVICD { get; set; }
    [SqlColumnDefinition("CHAR", Length = 10)] public string IDESPAIS { get; set; }
    [SqlColumnDefinition("CHAR", Length = 10)] public string IDESMONE { get; set; }

    [SqlColumnDefinition("CHAR", Length = 10)] public string ISAGENCD { get; set; }
    [SqlColumnDefinition("CHAR", Length = 10)] public string ISPAISCD { get; set; }
    [SqlColumnDefinition("CHAR", Length = 10)] public string ISTATECD { get; set; }
    [SqlColumnDefinition("CHAR", Length = 10)] public string IRAGENCD { get; set; }
    [SqlColumnDefinition("CHAR", Length = 10)] public string ITICUENTA { get; set; }
    [SqlColumnDefinition("CHAR", Length = 20)] public string INOCUENTA { get; set; }

    [SqlColumnDefinition("CHAR", Length = 20)] public string INUMREFER { get; set; }
    [SqlColumnDefinition("CHAR", Length = 10)] public string ISTSREM { get; set; }
    [SqlColumnDefinition("CHAR", Length = 10)] public string ISTSPRO { get; set; }
    [SqlColumnDefinition("CHAR", Length = 10)] public string IERR { get; set; }
    [SqlColumnDefinition("CHAR", Length = 100)] public string IERRDSC { get; set; }
    [SqlColumnDefinition("CHAR", Length = 100)] public string IDSCRECH { get; set; }

    [SqlColumnDefinition("CHAR", Length = 10)] public string ACODPAIS { get; set; }
    [SqlColumnDefinition("CHAR", Length = 10)] public string ACODMONED { get; set; }
    [SqlColumnDefinition("CHAR", Length = 20)] public string AMTOENVIA { get; set; }
    [SqlColumnDefinition("CHAR", Length = 20)] public string AMTOCALCU { get; set; }
    [SqlColumnDefinition("CHAR", Length = 20)] public string AFACTCAMB { get; set; }

    [SqlColumnDefinition("CHAR", Length = 50)] public string BPRIMNAME { get; set; }
    [SqlColumnDefinition("CHAR", Length = 50)] public string BSECUNAME { get; set; }
    [SqlColumnDefinition("CHAR", Length = 50)] public string BAPELLIDO { get; set; }
    [SqlColumnDefinition("CHAR", Length = 50)] public string BSEGUAPE { get; set; }
    [SqlColumnDefinition("CHAR", Length = 100)] public string BDIRECCIO { get; set; }
    [SqlColumnDefinition("CHAR", Length = 50)] public string BCIUDAD { get; set; }
    [SqlColumnDefinition("CHAR", Length = 20)] public string BESTADO { get; set; }
    [SqlColumnDefinition("CHAR", Length = 10)] public string BPAIS { get; set; }
    [SqlColumnDefinition("CHAR", Length = 10)] public string BCODPOST { get; set; }
    [SqlColumnDefinition("CHAR", Length = 20)] public string BTELEFONO { get; set; }

    [SqlColumnDefinition("CHAR", Length = 50)] public string CPRIMNAME { get; set; }
    [SqlColumnDefinition("CHAR", Length = 50)] public string CSECUNAME { get; set; }
    [SqlColumnDefinition("CHAR", Length = 50)] public string CAPELLIDO { get; set; }
    [SqlColumnDefinition("CHAR", Length = 50)] public string CSEGUAPE { get; set; }
    [SqlColumnDefinition("CHAR", Length = 100)] public string CDIRECCIO { get; set; }
    [SqlColumnDefinition("CHAR", Length = 50)] public string CCIUDAD { get; set; }
    [SqlColumnDefinition("CHAR", Length = 20)] public string CESTADO { get; set; }
    [SqlColumnDefinition("CHAR", Length = 10)] public string CPAIS { get; set; }
    [SqlColumnDefinition("CHAR", Length = 10)] public string CCODPOST { get; set; }
    [SqlColumnDefinition("CHAR", Length = 20)] public string CTELEFONO { get; set; }

    [SqlColumnDefinition("CHAR", Length = 20)] public string DTIDENT { get; set; }
    [SqlColumnDefinition("CHAR", Length = 8)]  public string ESALEDT { get; set; }

    [SqlColumnDefinition("CHAR", Length = 10)] public string EMONREFER { get; set; }
    [SqlColumnDefinition("CHAR", Length = 20)] public string ETASAREFE { get; set; }
    [SqlColumnDefinition("CHAR", Length = 20)] public string EMTOREF { get; set; }
}
