¡pinta muy bien! Te faltó cerrar lo que quedó en ObtenerRegla… y agregar las piezas del desglose + reglas. Te dejo todo lo que falta para que compile y funcione con tu SelectQueryBuilder/InsertQueryBuilder/UpdateQueryBuilder (RestUtilities.QueryBuilder). Mantengo tu estilo: constructor primario, new() y listas [], más XML docs y comentarios funcionales.


---

1) Modelos de reglas (colócalos fuera de la clase, en el mismo archivo o en Models)

/// <summary>
/// Regla de cargo (porcentaje o monto fijo) dirigida a una cuenta GL.
/// </summary>
/// <remarks>
/// Se aplica sobre el total enviado en la petición; si hay % y monto fijo, se suman.
/// </remarks>
public sealed class ReglaCargo()
{
    public string Codigo { get; set; } = string.Empty;   // Ej. "INT", "COM", "IVA"
    public string CuentaGl { get; set; } = string.Empty; // Cuenta contable destino (GL)
    public decimal Porcentaje { get; set; }              // 0..1 (3% => 0.03)
    public decimal MontoFijo { get; set; }               // importe fijo
}

/// <summary>
/// Resultado de un cargo calculado (monto final y metadatos).
/// </summary>
public sealed class CargoCalculado()
{
    public string Codigo { get; set; } = string.Empty;
    public string CuentaGl { get; set; } = string.Empty;
    public decimal Monto { get; set; }
}


---

2) Métodos que faltan dentro de TransaccionesServices

2.1 Completar ObtenerReglasDesdeIadqctl(...)

/// <summary>
/// Lee reglas base para un perfil/comercio desde IADQCTL (cuentas y porcentajes).
/// </summary>
/// <remarks>
/// Convierte % base 100 → factor (0..1). Ajusta nombres de columnas si tu PF difiere.
/// </remarks>
private List<ReglaCargo> ObtenerReglasDesdeIadqctl(string perfil, int comercio)
{
    var reglas = new List<ReglaCargo>();

    var q = QueryBuilder.Core.QueryBuilder
        .From("IADQCTL", "BCAH96DTA")
        .Select(
            "ADQCNT1 AS CTA_INT",
            "ADQCNT2 AS CTA_COM",
            "ADQCNT3 AS CTA_IVA",
            "ADQMDVC AS PCT_INT",
            "ADQMDVT AS PCT_COM",
            "ADQMFAI AS PCT_IVA",
            "ADQMTO1 AS MF_INT",
            "ADQMTO2 AS MF_COM"
        )
        // Puedes filtrar por PERFIL o por COMERCIO; aquí se intenta PERFIL y si no hay fila, se podría hacer fallback a comercio.
        .WhereRaw("(ADQPERF = :p OR ADQCOME = :c)")
        .WithParameters(new { p = perfil, c = comercio })
        .FetchNext(1)
        .Build();

    using var cmd = _connection.GetDbCommand(q, _contextAccessor.HttpContext!);
    using var rd = cmd.ExecuteReader();
    if (!rd.Read()) return reglas;

    static decimal Pct(object? o) => o is DBNull or null ? 0m : Convert.ToDecimal(o, CultureInfo.InvariantCulture) / 100m;
    static decimal Mf(object? o)  => o is DBNull or null ? 0m : Convert.ToDecimal(o, CultureInfo.InvariantCulture);
    static string Gl(object? o)   => o is DBNull or null ? ""  : Convert.ToString(o)!.Trim();

    var rInt = new ReglaCargo { Codigo = "INT", CuentaGl = Gl(rd["CTA_INT"]), Porcentaje = Pct(rd["PCT_INT"]), MontoFijo = Mf(rd["MF_INT"]) };
    var rCom = new ReglaCargo { Codigo = "COM", CuentaGl = Gl(rd["CTA_COM"]), Porcentaje = Pct(rd["PCT_COM"]), MontoFijo = Mf(rd["MF_COM"]) };
    var rIva = new ReglaCargo { Codigo = "IVA", CuentaGl = Gl(rd["CTA_IVA"]), Porcentaje = Pct(rd["PCT_IVA"]), MontoFijo = 0m };

    if (!rInt.CuentaGl.IsNullOrEmpty() && (rInt.Porcentaje > 0m || rInt.MontoFijo > 0m)) reglas.Add(rInt);
    if (!rCom.CuentaGl.IsNullOrEmpty() && (rCom.Porcentaje > 0m || rCom.MontoFijo > 0m)) reglas.Add(rCom);
    if (!rIva.CuentaGl.IsNullOrEmpty() &&  rIva.Porcentaje > 0m) reglas.Add(rIva);

    return reglas;
}

2.2 MergeConEcommerce(...) (si no usas e-commerce, deja esTerminalVirtual:false y no altera nada)

/// <summary>
/// Funde reglas e-commerce (ADQECTL) con las base si la terminal es virtual.
/// </summary>
private List<ReglaCargo> MergeConEcommerce(List<ReglaCargo> baseRules, int comercio, bool esTerminalVirtual)
{
    if (!esTerminalVirtual) return baseRules;

    var reglas = baseRules.ToDictionary(r => r.Codigo, r => r);

    var q = QueryBuilder.Core.QueryBuilder
        .From("ADQECTL", "BCAH96DTA")
        .Select(
            "ADQECNT1 AS CTA_INT_EC",
            "ADQECNT5 AS CTA_COM_EC",
            "ADQECTR1 AS PCT_INT_EC",
            "ADQECTR5 AS PCT_COM_EC"
        )
        .WhereRaw("A02COME = :c")
        .WithParameters(new { c = comercio })
        .FetchNext(1)
        .Build();

    using var cmd = _connection.GetDbCommand(q, _contextAccessor.HttpContext!);
    using var rd = cmd.ExecuteReader();
    if (!rd.Read()) return baseRules;

    static string Gl(object? o)   => o is DBNull or null ? ""  : Convert.ToString(o)!.Trim();
    static decimal Pct(object? o) => o is DBNull or null ? 0m : Convert.ToDecimal(o, CultureInfo.InvariantCulture) / 100m;

    var ctaInt = Gl(rd["CTA_INT_EC"]);
    var ctaCom = Gl(rd["CTA_COM_EC"]);
    var pctInt = Pct(rd["PCT_INT_EC"]);
    var pctCom = Pct(rd["PCT_COM_EC"]);

    if (!ctaInt.IsNullOrEmpty() || pctInt > 0m)
    {
        if (!reglas.TryGetValue("INT", out var r)) r = reglas["INT"] = new() { Codigo = "INT" };
        if (!ctaInt.IsNullOrEmpty()) r.CuentaGl = ctaInt;
        r.Porcentaje += pctInt;
    }
    if (!ctaCom.IsNullOrEmpty() || pctCom > 0m)
    {
        if (!reglas.TryGetValue("COM", out var r)) r = reglas["COM"] = new() { Codigo = "COM" };
        if (!ctaCom.IsNullOrEmpty()) r.CuentaGl = ctaCom;
        r.Porcentaje += pctCom;
    }

    return reglas.Values
        .Where(r => !r.CuentaGl.IsNullOrEmpty() && (r.Porcentaje > 0m || r.MontoFijo > 0m))
        .ToList();
}

2.3 PostearDesglose(...) (retorna bool como lo estás usando)

/// <summary>
/// Aplica reglas y postea: 1 línea principal (neto) + N líneas de cargos (naturaleza opuesta).
/// También actualiza totales de POP801.
/// </summary>
/// <returns>true si todas las inserciones se realizaron; false si alguna falla.</returns>
private bool PostearDesglose(
    string perfil,
    int numeroLote,
    int fechaYyyyMmDd,
    string naturalezaPrincipal,    // "C" (crédito) o "D" (débito)
    string cuentaComercio,         // cuenta del comercio
    decimal totalBruto,            // total recibido en el API
    List<ReglaCargo> reglas,       // reglas de cargos
    string codComercio,
    string terminal,
    string nombreComercio,
    string idUnico,
    int secuenciaInicial = 0)
{
    var cargos = CalcularCargos(totalBruto, reglas);
    var totalCargos = cargos.Sum(x => x.Monto);
    var neto = Decimal.Round(totalBruto - totalCargos, 2, MidpointRounding.AwayFromZero);

    var seq = secuenciaInicial;

    try
    {
        // 1) Línea principal (misma naturaleza que la petición)
        seq += 1;
        InsertPop802(
            perfil: perfil,
            lote: numeroLote,
            seq: seq,
            fechaYyyyMmDd: fechaYyyyMmDd,
            cuenta: cuentaComercio,
            centroCosto: 0,
            codTrn: naturalezaPrincipal == "C" ? "0783" : "0784",
            monto: neto,
            al1: Trunc(nombreComercio, 30),
            al2: Trunc($"{codComercio}-{terminal}", 30),
            al3: Trunc($"&{EtiquetaConcepto(naturalezaPrincipal)}&{idUnico}&Neto", 30)
        );
        ActualizarTotalesPop801(perfil, numeroLote, naturalezaPrincipal, neto);

        // 2) Cargos (naturaleza opuesta a la principal)
        var natCargo = naturalezaPrincipal == "C" ? "D" : "C";
        foreach (var c in cargos.Where(x => x.Monto > 0m))
        {
            seq += 1;

            InsertPop802(
                perfil: perfil,
                lote: numeroLote,
                seq: seq,
                fechaYyyyMmDd: fechaYyyyMmDd,
                cuenta: c.CuentaGl,
                centroCosto: 0,
                codTrn: natCargo == "C" ? "0783" : "0784",
                monto: c.Monto,
                al1: Trunc(nombreComercio, 30),
                al2: Trunc($"{codComercio}-{terminal}", 30),
                al3: Trunc($"&{c.Codigo}&{idUnico}&Cargo", 30)
            );

            ActualizarTotalesPop801(perfil, numeroLote, natCargo, c.Monto);
        }

        return true;
    }
    catch
    {
        return false;
    }
}

2.4 CalcularCargos(...)

/// <summary>
/// Calcula montos de cargos aplicando porcentaje y/o monto fijo sobre el total.
/// </summary>
private static List<CargoCalculado> CalcularCargos(decimal totalBruto, List<ReglaCargo> reglas)
{
    var res = new List<CargoCalculado>();
    foreach (var r in reglas)
    {
        var mp = r.Porcentaje > 0m ? Decimal.Round(totalBruto * r.Porcentaje, 2, MidpointRounding.AwayFromZero) : 0m;
        var mf = r.MontoFijo > 0m ? r.MontoFijo : 0m;
        var monto = mp + mf;
        if (monto <= 0m) continue;

        res.Add(new()
        {
            Codigo = r.Codigo,
            CuentaGl = r.CuentaGl,
            Monto = monto
        });
    }
    return res;
}

2.5 ActualizarTotalesPop801(...) (ya lo tienes, dejo la versión final por si acaso)

/// <summary>
/// Incrementa conteos e importes del encabezado del lote (POP801) según naturaleza.
/// </summary>
private void ActualizarTotalesPop801(string perfil, int lote, string naturaleza, decimal monto)
{
    var m = monto.ToString(CultureInfo.InvariantCulture);

    UpdateQueryBuilder updBuilder;

    if (naturaleza == "C")
    {
        updBuilder = new UpdateQueryBuilder("POP801", "BNKPRD01")
            .SetRaw("FTTSCI", "FTTSCI + 1")
            .SetRaw("FTTSIC", $"FTTSIC + {m}")
            .Where<Pop801>(x => x.FTTSBK == 1)
            .Where<Pop801>(x => x.FTTSKY == perfil)
            .Where<Pop801>(x => x.FTSBT == lote);
    }
    else
    {
        updBuilder = new UpdateQueryBuilder("POP801", "BNKPRD01")
            .SetRaw("FTTSDI", "FTTSDI + 1")
            .SetRaw("FTTSID", $"FTTSID + {m}")
            .Where<Pop801>(x => x.FTTSBK == 1)
            .Where<Pop801>(x => x.FTTSKY == perfil)
            .Where<Pop801>(x => x.FTSBT == lote);
    }

    var upd = updBuilder.Build();

    using var cmd = _connection.GetDbCommand(upd, _contextAccessor.HttpContext!);
    _ = cmd.ExecuteNonQuery();
}


---

Nota rápida sobre VerFecha()

Si TAP001.DSCDT ya viene como YYYYMMDD (9,0), no necesitas reordenar dd/mm/yy. Tu versión actual convierte a string y arma yyyyMMdd como yy + mm + dd. Si ves fechas raras, considera simplificar a:

// si DSCDT es 20250918 (YYYYMMDD)
yyyyMMdd = ((long)dscdt).ToString("D8");


---

Con esto queda completo: reserva de lote, desglose (línea principal + cargos), y actualización de totales POP801; todo usando RestUtilities.QueryBuilder y tu IDatabaseConnection.

    
