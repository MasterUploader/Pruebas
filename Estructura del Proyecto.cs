namespace MS_BAN_56_ProcesamientoTransaccionesPOS.Models.AS400.BCAH96DTA;

/// <summary>
/// DTO para la tabla <c>BCAH96DTA/ADQECTL</c> (control de parámetros e-commerce).
/// </summary>
/// <remarks>
/// - Mapea 1:1 los campos físicos del PF/índice para usarlos en QueryBuilder (lecturas/filters tipados).  
/// - Los NUMERIC/DECIMAL se modelan como <see cref="decimal"/> (sin escala cuando no se especifica).  
/// - Los CHARACTER se modelan como <see cref="string"/>.  
/// - Indicadores CR/DB se conservan como <see cref="string"/> de 1 char para permitir valores 'C'/'D'.
/// </remarks>
public class AdqectlDto()
{
    // ===================== Claves / Control =====================

    /// <summary>CONTROL.</summary>
    public string ADQECONT { get; set; } = string.Empty;

    /// <summary>SECUENCIA.</summary>
    public decimal ADQENUM { get; set; }

    // ===================== Cuentas contables (1..15) =====================

    /// <summary>CONTABLE 1.</summary>
    public decimal ADQECNT1 { get; set; }
    /// <summary>CONTABLE 2.</summary>
    public decimal ADQECNT2 { get; set; }
    /// <summary>CONTABLE 3.</summary>
    public decimal ADQECNT3 { get; set; }
    /// <summary>CONTABLE 4.</summary>
    public decimal ADQECNT4 { get; set; }
    /// <summary>CONTABLE 5.</summary>
    public decimal ADQECNT5 { get; set; }
    /// <summary>CONTABLE 6.</summary>
    public decimal ADQECNT6 { get; set; }
    /// <summary>CONTABLE 7.</summary>
    public decimal ADQECNT7 { get; set; }
    /// <summary>CONTABLE 8.</summary>
    public decimal ADQECNT8 { get; set; }
    /// <summary>CONTABLE 9.</summary>
    public decimal ADQECNT9 { get; set; }
    /// <summary>CONTABLE 10.</summary>
    public decimal ADQECNT10 { get; set; }
    /// <summary>CONTABLE 11.</summary>
    public decimal ADQECNT11 { get; set; }
    /// <summary>CONTABLE 12.</summary>
    public decimal ADQECNT12 { get; set; }
    /// <summary>CONTABLE 13.</summary>
    public decimal ADQECNT13 { get; set; }
    /// <summary>CONTABLE 14.</summary>
    public decimal ADQECNT14 { get; set; }
    /// <summary>CONTABLE 15.</summary>
    public decimal ADQECNT15 { get; set; }

    // ===================== Centros de costo (1..15) =====================

    /// <summary>COSTO 1.</summary>
    public int ADQECCO1 { get; set; }
    /// <summary>COSTO 2.</summary>
    public int ADQECCO2 { get; set; }
    /// <summary>COSTO 3.</summary>
    public int ADQECCO3 { get; set; }
    /// <summary>COSTO 4.</summary>
    public int ADQECCO4 { get; set; }
    /// <summary>COSTO 5.</summary>
    public int ADQECCO5 { get; set; }
    /// <summary>COSTO 6.</summary>
    public int ADQECCO6 { get; set; }
    /// <summary>COSTO 7.</summary>
    public int ADQECCO7 { get; set; }
    /// <summary>COSTO 8.</summary>
    public int ADQECCO8 { get; set; }
    /// <summary>COSTO 9.</summary>
    public int ADQECCO9 { get; set; }
    /// <summary>COSTO 10.</summary>
    public int ADQECC10 { get; set; }
    /// <summary>COSTO 11.</summary>
    public int ADQECC11 { get; set; }
    /// <summary>COSTO 12.</summary>
    public int ADQECC12 { get; set; }
    /// <summary>COSTO 13.</summary>
    public int ADQECC13 { get; set; }
    /// <summary>COSTO 14.</summary>
    public int ADQECC14 { get; set; }
    /// <summary>COSTO 15.</summary>
    public int ADQECC15 { get; set; }

    // ===================== Códigos de transacción (1..15) =====================

    /// <summary>COD TRN 1.</summary>
    public string ADQECTR1 { get; set; } = string.Empty;
    /// <summary>COD TRN 2.</summary>
    public string ADQECTR2 { get; set; } = string.Empty;
    /// <summary>COD TRN 3.</summary>
    public string ADQECTR3 { get; set; } = string.Empty;
    /// <summary>COD TRN 4.</summary>
    public string ADQECTR4 { get; set; } = string.Empty;
    /// <summary>COD TRN 5.</summary>
    public string ADQECTR5 { get; set; } = string.Empty;
    /// <summary>COD TRN 6.</summary>
    public string ADQECTR6 { get; set; } = string.Empty;
    /// <summary>COD TRN 7.</summary>
    public string ADQECTR7 { get; set; } = string.Empty;
    /// <summary>COD TRN 8.</summary>
    public string ADQECTR8 { get; set; } = string.Empty;
    /// <summary>COD TRN 9.</summary>
    public string ADQECTR9 { get; set; } = string.Empty;
    /// <summary>COD TRN 10.</summary>
    public string ADQECTR10 { get; set; } = string.Empty;
    /// <summary>COD TRN 11.</summary>
    public string ADQECTR11 { get; set; } = string.Empty;
    /// <summary>COD TRN 12.</summary>
    public string ADQECTR12 { get; set; } = string.Empty;
    /// <summary>COD TRN 13.</summary>
    public string ADQECTR13 { get; set; } = string.Empty;
    /// <summary>COD TRN 14.</summary>
    public string ADQECTR14 { get; set; } = string.Empty;
    /// <summary>COD TRN 15.</summary>
    public string ADQECTR15 { get; set; } = string.Empty;

    // ===================== Naturaleza CR/DB (1..15) =====================

    /// <summary>CR-DB 1.</summary>
    public string ADQEDB1 { get; set; } = string.Empty;
    /// <summary>CR-DB 2.</summary>
    public string ADQEDB2 { get; set; } = string.Empty;
    /// <summary>CR-DB 3.</summary>
    public string ADQEDB3 { get; set; } = string.Empty;
    /// <summary>CR-DB 4.</summary>
    public string ADQEDB4 { get; set; } = string.Empty;
    /// <summary>CR-DB 5.</summary>
    public string ADQEDB5 { get; set; } = string.Empty;
    /// <summary>CR-DB 6.</summary>
    public string ADQEDB6 { get; set; } = string.Empty;
    /// <summary>CR-DB 7.</summary>
    public string ADQEDB7 { get; set; } = string.Empty;
    /// <summary>CR-DB 8.</summary>
    public string ADQEDB8 { get; set; } = string.Empty;
    /// <summary>CR-DB 9.</summary>
    public string ADQEDB9 { get; set; } = string.Empty;
    /// <summary>CR-DB 10.</summary>
    public string ADQEDB10 { get; set; } = string.Empty;
    /// <summary>CR-DB 11.</summary>
    public string ADQEDB11 { get; set; } = string.Empty;
    /// <summary>CR-DB 12.</summary>
    public string ADQEDB12 { get; set; } = string.Empty;
    /// <summary>CR-DB 13.</summary>
    public string ADQEDB13 { get; set; } = string.Empty;
    /// <summary>CR-DB 14.</summary>
    public string ADQEDB14 { get; set; } = string.Empty;
    /// <summary>CR-DB 15.</summary>
    public string ADQEDB15 { get; set; } = string.Empty;

    // ===================== Metadatos auditoría =====================

    /// <summary>FECHA CREACIÓN (YYYYMMDD).</summary>
    public int FECHA_CREO { get; set; }

    /// <summary>HORA CREACIÓN (HHMMSS).</summary>
    public int HORA_CREO { get; set; }

    /// <summary>USUARIO CREACIÓN.</summary>
    public string USUARIO_CREO { get; set; } = string.Empty;

    /// <summary>PANTALLA CREACIÓN.</summary>
    public string PANTALLA_CREO { get; set; } = string.Empty;

    /// <summary>FECHA MODIFICÓ (YYYYMMDD).</summary>
    public int FECHA_MODIF { get; set; }

    /// <summary>HORA MODIFICÓ (HHMMSS).</summary>
    public int HORA_MODIF { get; set; }

    /// <summary>USUARIO MODIFICÓ.</summary>
    public string USUARIO_MODIF { get; set; } = string.Empty;

    /// <summary>PANTALLA MODIFICÓ.</summary>
    public string PANTALLA_MODIF { get; set; } = string.Empty;

    /// <summary>DESCRIPCIÓN.</summary>
    public string DESC { get; set; } = string.Empty;
}
