/// <summary>
/// Obtiene el siguiente FTSBT para un perfil de forma segura y lo reserva insertando POP801 base.
/// </summary>
/// <remarks>
/// - Previene colisiones bajo concurrencia usando el propio INSERT como lock optimista.
/// - Si llega a 999, reinicia a 1 (ajusta si tu negocio requiere otra política).
/// </remarks>
private (int ftsbt, bool ok) ReservarNumeroLote(string perfil, int dsdt, string usuario)
{
    for (var intento = 0; intento < 5; intento++)
    {
        var sel = QueryBuilder.Core.QueryBuilder
            .From("POP801", "BNKPRD01")
            .Select("COALESCE(MAX(FTSBT), 0) AS MAXFTSBT")
            .Where<Pop801>(x => x.FTTSBK == 1)
            .Where<Pop801>(x => x.FTTSKY == perfil)
            .Build();

        var max = _connection.ExecuteScalar<int?>(sel) ?? 0;
        var next = max >= 999 ? 1 : max + 1;

        var ins = new QueryBuilder.Builders.InsertQueryBuilder("POP801", "BNKPRD01")
            .IntoColumns("FTTSBK","FTTSKY","FTTSBT","FTTSST","FTTSOR","FTTSDT",
                         "FTTSDI","FTTSCI","FTTSID","FTTSIC","FTTSDP","FTTSCP",
                         "FTTSPD","FTTSPC","FTTSBD","FTTSLD","FTTSBC","FTTSLC")
            .Row([
                1, perfil, next, 2, usuario, dsdt,
                0, 0, 0m, 0m, 0, 0,
                0m, 0m, 0m, 0m, 0m, 0m
            ])
            .Build();

        try
        {
            using var cmd = _connection.GetDbCommand(ins, _contextAccessor.HttpContext!);
            var aff = cmd.ExecuteNonQuery();
            if (aff > 0) return (next, true);
        }
        catch
        {
            // Colisión por clave duplicada: reintentar
        }
    }
    return (0, false);
}



/// <summary>
/// Orquestador: trae reglas base (IADQCTL) y fusiona con e-commerce (ADQECTL) si aplica.
/// </summary>
private List<ReglaCargo> ObtenerReglasCargos(string perfil, int comercio, bool esTerminalVirtual = false)
{
    var baseRules = ObtenerReglasDesdeIadqctl(perfil, comercio);
    return MergeConEcommerce(baseRules, comercio, esTerminalVirtual);
}


/// <summary>
/// Lee reglas base para un perfil/comercio desde IADQCTL (cuentas y porcentajes).
/// </summary>
/// <remarks>
/// Ajusta nombres de columnas a tu PF real. Convierto % base 100 → factor (0..1).
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
            "ADQMTO2 AS MF_COM",
            "ADQMTO3 AS MF_OTR"
        )
        .WhereRaw("ADQPERF = :p OR ADQCOME = :c")
        .WithParameters(new { p = perfil, c = comercio })
        .FetchNext(1)
        .Build();

    using var cmd = _connection.GetDbCommand(q, _contextAccessor.HttpContext!);
    using var rd = cmd.ExecuteReader();
    if (!rd.Read()) return reglas;

    static decimal pct(object? o) => o is DBNull or null ? 0m : Convert.ToDecimal(o) / 100m;
    static decimal mf(object? o)  => o is DBNull or null ? 0m : Convert.ToDecimal(o);
    static string gl(object? o)   => o is DBNull or null ? "" : Convert.ToString(o)!.Trim();

    var rInt = new ReglaCargo { Codigo = "INT", CuentaGl = gl(rd["CTA_INT"]), Porcentaje = pct(rd["PCT_INT"]), MontoFijo = mf(rd["MF_INT"]) };
    var rCom = new ReglaCargo { Codigo = "COM", CuentaGl = gl(rd["CTA_COM"]), Porcentaje = pct(rd["PCT_COM"]), MontoFijo = mf(rd["MF_COM"]) };
    var rIva = new ReglaCargo { Codigo = "IVA", CuentaGl = gl(rd["CTA_IVA"]), Porcentaje = pct(rd["PCT_IVA"]), MontoFijo = 0m };

    if (!rInt.CuentaGl.IsNullOrEmpty() && (rInt.Porcentaje > 0m || rInt.MontoFijo > 0m)) reglas.Add(rInt);
    if (!rCom.CuentaGl.IsNullOrEmpty() && (rCom.Porcentaje > 0m || rCom.MontoFijo > 0m)) reglas.Add(rCom);
    if (!rIva.CuentaGl.IsNullOrEmpty() &&  rIva.Porcentaje > 0m) reglas.Add(rIva);

    return reglas;
}

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

    static string gl(object? o) => o is DBNull or null ? "" : Convert.ToString(o)!.Trim();
    static decimal pct(object? o) => o is DBNull or null ? 0m : Convert.ToDecimal(o) / 100m;

    var ctaInt = gl(rd["CTA_INT_EC"]);
    var ctaCom = gl(rd["CTA_COM_EC"]);
    var pctInt = pct(rd["PCT_INT_EC"]);
    var pctCom = pct(rd["PCT_COM_EC"]);

    if (!ctaInt.IsNullOrEmpty() || pctInt > 0m)
    {
        if (!reglas.TryGetValue("INT", out var r))
            r = reglas["INT"] = new() { Codigo = "INT" };
        if (!ctaInt.IsNullOrEmpty()) r.CuentaGl = ctaInt;
        r.Porcentaje += pctInt;
    }
    if (!ctaCom.IsNullOrEmpty() || pctCom > 0m)
    {
        if (!reglas.TryGetValue("COM", out var r))
            r = reglas["COM"] = new() { Codigo = "COM" };
        if (!ctaCom.IsNullOrEmpty()) r.CuentaGl = ctaCom;
        r.Porcentaje += pctCom;
    }

    return reglas.Values
        .Where(r => !r.CuentaGl.IsNullOrEmpty() && (r.Porcentaje > 0m || r.MontoFijo > 0m))
        .ToList();
}


/// <summary>
/// Aplica reglas y postea: 1 línea principal (neto) + N líneas de cargos (opuestas).
/// También actualiza totales del POP801.
/// </summary>
private int PostearDesglose(
    string perfil,
    int numeroLote,
    int fechaYyyyMmDd,
    string naturalezaPrincipal,
    string cuentaComercio,
    decimal totalBruto,
    List<ReglaCargo> reglas,
    string codComercio,
    string terminal,
    string nombreComercio,
    string idUnico,
    int secuenciaInicial = 0)
{
    var cargos = CalcularCargos(totalBruto, reglas);
    var totalCargos = cargos.Sum(x => x.Monto);
    var neto = Decimal.Round(totalBruto - totalCargos, 2, MidpointRounding.AwayFromZero);

    var seq = secuenciaInicial + 1;

    // Línea principal (misma naturaleza del request)
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

    // Cargos (naturaleza opuesta)
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

    return seq;
}


/// <summary>
/// Calcula la lista de cargos aplicando porcentaje y/o monto fijo sobre el total.
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


/// <summary>
/// Incrementa conteos e importes del encabezado del lote (POP801) según naturaleza.
/// </summary>
private void ActualizarTotalesPop801(string perfil, int lote, string naturaleza, decimal monto)
{
    var sets = naturaleza == "C"
        ? "FTTSCI = FTTSCI + 1, FTTSIC = FTTSIC + :m"
        : "FTTSDI = FTTSDI + 1, FTTSID = FTTSID + :m";

    var upd = QueryBuilder.Core.QueryBuilder
        .Update("POP801", "BNKPRD01")
        .SetRaw(sets, new { m = monto })
        .Where<Pop801>(x => x.FTTSBK == 1)
        .Where<Pop801>(x => x.FTTSKY == perfil)
        .Where<Pop801>(x => x.FTSBT == lote)
        .Build();

    using var cmd = _connection.GetDbCommand(upd, _contextAccessor.HttpContext!);
    _ = cmd.ExecuteNonQuery();
}


