/// <summary>
/// Lee reglas base (sólo cuentas GL y metadatos) desde IADQCTL/ADQCTL.
/// </summary>
/// <param name="control">Valor de ADQCONT (ej. 'TS').</param>
/// <param name="numero">Valor de ADQNUM (identificador).</param>
/// <remarks>
/// ADQCTL no posee porcentajes ni montos fijos; esos deben venir de otras PF.
/// </remarks>
private List<ReglaCargo> ObtenerReglasDesdeIadqctl(string control, decimal numero)
{
    var reglas = new List<ReglaCargo>();

    var q = QueryBuilder.Core.QueryBuilder
        .From("IADQCTL", "BCAH96DTA")
        .Select(
            // cuentas GL típicas para intereses/comisión/IVA; ajusta si usas otras posiciones
            "ADQCNT1 AS CTA_INT",
            "ADQCNT2 AS CTA_COM",
            "ADQCNT3 AS CTA_IVA",
            // opcional: códigos de trn y naturalezas por si los quieres mapear
            "ADQCTR1 AS TCD_INT",
            "ADQCTR2 AS TCD_COM",
            "ADQCTR3 AS TCD_IVA",
            "ADQDB1  AS NAT_INT",
            "ADQDB2  AS NAT_COM",
            "ADQDB3  AS NAT_IVA"
        )
        .WhereRaw("ADQCONT = :ctrl AND ADQNUM = :num")
        .WithParameters(new { ctrl = control, num = numero })
        .FetchNext(1)
        .Build();

    using var cmd = _connection.GetDbCommand(q, _contextAccessor.HttpContext!);
    using var rd = cmd.ExecuteReader();
    if (!rd.Read()) return reglas;

    static string Gl(object o) => (o is DBNull) ? "" : Convert.ToString(o)!.Trim();

    // Como ADQCTL no guarda % ni montos fijos, ambos quedan en 0 (se complementan con otras reglas)
    var rInt = new ReglaCargo { Codigo = "INT", CuentaGl = Gl(rd["CTA_INT"]), Porcentaje = 0m, MontoFijo = 0m };
    var rCom = new ReglaCargo { Codigo = "COM", CuentaGl = Gl(rd["CTA_COM"]), Porcentaje = 0m, MontoFijo = 0m };
    var rIva = new ReglaCargo { Codigo = "IVA", CuentaGl = Gl(rd["CTA_IVA"]), Porcentaje = 0m, MontoFijo = 0m };

    if (!rInt.CuentaGl.IsNullOrEmpty()) reglas.Add(rInt);
    if (!rCom.CuentaGl.IsNullOrEmpty()) reglas.Add(rCom);
    if (!rIva.CuentaGl.IsNullOrEmpty()) reglas.Add(rIva);

    return reglas;
}


namespace MS_BAN_56_ProcesamientoTransaccionesPOS.Models.AS400.BCAH96DTA;

/// <summary>
/// DTO que representa la estructura de la tabla <c>BCAH96DTA/ADQCTL</c>.
/// Contiene configuración contable, centros de costo, códigos de transacción
/// y banderas de naturaleza (CR/DB) asociadas a perfiles y comercios.
/// </summary>
public class AdqctlDto
{
    /// <summary>CONTROL.</summary>
    public string ADQCONT { get; set; } = string.Empty;

    /// <summary>SECUENCIA.</summary>
    public decimal ADQNUM { get; set; }

    /// <summary>CONTABLE 1.</summary>
    public decimal ADQCNT1 { get; set; }

    /// <summary>CONTABLE 2.</summary>
    public decimal ADQCNT2 { get; set; }

    /// <summary>CONTABLE 3.</summary>
    public decimal ADQCNT3 { get; set; }

    /// <summary>CONTABLE 4.</summary>
    public decimal ADQCNT4 { get; set; }

    /// <summary>CONTABLE 5.</summary>
    public decimal ADQCNT5 { get; set; }

    /// <summary>CONTABLE 6.</summary>
    public decimal ADQCNT6 { get; set; }

    /// <summary>CONTABLE 7.</summary>
    public decimal ADQCNT7 { get; set; }

    /// <summary>CONTABLE 8.</summary>
    public decimal ADQCNT8 { get; set; }

    /// <summary>CONTABLE 9.</summary>
    public decimal ADQCNT9 { get; set; }

    /// <summary>COSTO 1.</summary>
    public decimal ADQCCO1 { get; set; }

    /// <summary>COSTO 2.</summary>
    public decimal ADQCCO2 { get; set; }

    /// <summary>COSTO 3.</summary>
    public decimal ADQCCO3 { get; set; }

    /// <summary>COSTO 4.</summary>
    public decimal ADQCCO4 { get; set; }

    /// <summary>COSTO 5.</summary>
    public decimal ADQCCO5 { get; set; }

    /// <summary>COSTO 6.</summary>
    public decimal ADQCCO6 { get; set; }

    /// <summary>COSTO 7.</summary>
    public decimal ADQCCO7 { get; set; }

    /// <summary>COSTO 8.</summary>
    public decimal ADQCCO8 { get; set; }

    /// <summary>COSTO 9.</summary>
    public decimal ADQCCO9 { get; set; }

    /// <summary>CONTABLE 10.</summary>
    public decimal ADQCNT10 { get; set; }

    /// <summary>CONTABLE 11.</summary>
    public decimal ADQCNT11 { get; set; }

    /// <summary>CONTABLE 12.</summary>
    public decimal ADQCNT12 { get; set; }

    /// <summary>CONTABLE 13.</summary>
    public decimal ADQCNT13 { get; set; }

    /// <summary>CONTABLE 14.</summary>
    public decimal ADQCNT14 { get; set; }

    /// <summary>CONTABLE 15.</summary>
    public decimal ADQCNT15 { get; set; }

    /// <summary>COSTO 10.</summary>
    public decimal ADQCC10 { get; set; }

    /// <summary>COSTO 11.</summary>
    public decimal ADQCC11 { get; set; }

    /// <summary>COSTO 12.</summary>
    public decimal ADQCC12 { get; set; }

    /// <summary>COSTO 13.</summary>
    public decimal ADQCC13 { get; set; }

    /// <summary>COSTO 14.</summary>
    public decimal ADQCC14 { get; set; }

    /// <summary>COSTO 15.</summary>
    public decimal ADQCC15 { get; set; }

    /// <summary>COD TRN 1.</summary>
    public string ADQCTR1 { get; set; } = string.Empty;

    /// <summary>COD TRN 2.</summary>
    public string ADQCTR2 { get; set; } = string.Empty;

    /// <summary>COD TRN 3.</summary>
    public string ADQCTR3 { get; set; } = string.Empty;

    /// <summary>COD TRN 4.</summary>
    public string ADQCTR4 { get; set; } = string.Empty;

    /// <summary>COD TRN 5.</summary>
    public string ADQCTR5 { get; set; } = string.Empty;

    /// <summary>COD TRN 6.</summary>
    public string ADQCTR6 { get; set; } = string.Empty;

    /// <summary>COD TRN 7.</summary>
    public string ADQCTR7 { get; set; } = string.Empty;

    /// <summary>COD TRN 8.</summary>
    public string ADQCTR8 { get; set; } = string.Empty;

    /// <summary>COD TRN 9.</summary>
    public string ADQCTR9 { get; set; } = string.Empty;

    /// <summary>COD TRN 10.</summary>
    public string ADQCTR10 { get; set; } = string.Empty;

    /// <summary>COD TRN 11.</summary>
    public string ADQCTR11 { get; set; } = string.Empty;

    /// <summary>COD TRN 12.</summary>
    public string ADQCTR12 { get; set; } = string.Empty;

    /// <summary>COD TRN 13.</summary>
    public string ADQCTR13 { get; set; } = string.Empty;

    /// <summary>COD TRN 14.</summary>
    public string ADQCTR14 { get; set; } = string.Empty;

    /// <summary>COD TRN 15.</summary>
    public string ADQCTR15 { get; set; } = string.Empty;

    /// <summary>CR-DB 1.</summary>
    public string ADQDB1 { get; set; } = string.Empty;

    /// <summary>CR-DB 2.</summary>
    public string ADQDB2 { get; set; } = string.Empty;

    /// <summary>CR-DB 3.</summary>
    public string ADQDB3 { get; set; } = string.Empty;

    /// <summary>CR-DB 4.</summary>
    public string ADQDB4 { get; set; } = string.Empty;

    /// <summary>CR-DB 5.</summary>
    public string ADQDB5 { get; set; } = string.Empty;

    /// <summary>CR-DB 6.</summary>
    public string ADQDB6 { get; set; } = string.Empty;

    /// <summary>CR-DB 7.</summary>
    public string ADQDB7 { get; set; } = string.Empty;

    /// <summary>CR-DB 8.</summary>
    public string ADQDB8 { get; set; } = string.Empty;

    /// <summary>CR-DB 9.</summary>
    public string ADQDB9 { get; set; } = string.Empty;

    /// <summary>CR-DB 10.</summary>
    public string ADQDB10 { get; set; } = string.Empty;

    /// <summary>CR-DB 11.</summary>
    public string ADQDB11 { get; set; } = string.Empty;

    /// <summary>CR-DB 12.</summary>
    public string ADQDB12 { get; set; } = string.Empty;

    /// <summary>CR-DB 13.</summary>
    public string ADQDB13 { get; set; } = string.Empty;

    /// <summary>CR-DB 14.</summary>
    public string ADQDB14 { get; set; } = string.Empty;

    /// <summary>CR-DB 15.</summary>
    public string ADQDB15 { get; set; } = string.Empty;
}

/// <summary>
/// DTO alias para el índice lógico <c>IADQCTL</c>, mismo layout que ADQCTL.
/// </summary>
public class IadqctlDto : AdqctlDto { }
