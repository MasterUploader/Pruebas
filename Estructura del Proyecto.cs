/// <summary>
/// Regla de cargo (porcentaje o monto fijo) dirigida a una cuenta GL.
/// </summary>
public sealed class ReglaCargo()
{
    public string Codigo { get; set; } = string.Empty;   // Ej. "INT", "COM", "IVA"
    public string CuentaGl { get; set; } = string.Empty; // Cuenta contable destino
    public decimal Porcentaje { get; set; }              // 0..1 (3% => 0.03)
    public decimal MontoFijo { get; set; }               // Si aplica monto fijo
}


/// <summary>
/// Lee reglas “base” (no-ecommerce) para un perfil/comercio desde IADQCTL.
/// </summary>
/// <remarks>
/// - Devuelve lista de <see cref="ReglaCargo"/> con porcentaje y/o monto fijo.
/// - Ajusta nombres de columnas según tu PF (ej.: ADQCTR*, ADQCNT*, tasas y flags).
/// - Regla: usa porcentajes si &gt; 0; si MontoFijo &gt; 0 se suma al porcentaje.
/// </remarks>
private List<ReglaCargo> ObtenerReglasDesdeIadqctl(string perfil, int comercio)
{
    var reglas = new List<ReglaCargo>();

    // === Ejemplo de lectura de IADQCTL (parametría general) ===
    var q = QueryBuilder.Core.QueryBuilder
        .From("IADQCTL", "BCAH96DTA")
        .Select(
            // Cuentas GL de cargos
            "ADQCNT1 AS CTA_INT",     // cuenta de interés
            "ADQCNT2 AS CTA_COM",     // cuenta de comisión
            "ADQCNT3 AS CTA_IVA",     // cuenta de IVA
            // Porcentajes (0..100 o 0..1 según layout)
            "ADQMDVC AS PCT_INT",     // % interés (ej.)
            "ADQMDVT AS PCT_COM",     // % comisión (ej.)
            "ADQMFAI AS PCT_IVA",     // % IVA (ej.)
            // Montos fijos
            "ADQMTO1 AS MF_INT",
            "ADQMTO2 AS MF_COM",
            "ADQMTO3 AS MF_OTR"
        )
        // Claves de ejemplo: por perfil o por comercio (ajústalo a tu modelo real)
        .WhereRaw("ADQPERF = :p OR ADQCOME = :c") // si tu PF tiene columna de perfil
        .WithParameters(new { p = perfil, c = comercio })
        .FetchNext(1)
        .Build();

    using var cmd = _connection.GetDbCommand(q, _contextAccessor.HttpContext!);
    using var rd = cmd.ExecuteReader();
    if (!rd.Read()) return reglas;

    // Normalizador % (si tu tabla guarda 3.00 ≈ 3%, convierte a 0.03)
    static decimal pct(object? o)
        => o is DBNull or null ? 0m : Convert.ToDecimal(o) / 100m;

    // Monto fijo directo
    static decimal mf(object? o)
        => o is DBNull or null ? 0m : Convert.ToDecimal(o);

    // Cuenta GL (string)
    static string gl(object? o)
        => o is DBNull or null ? "" : Convert.ToString(o)!.Trim();

    var ctaInt = gl(rd["CTA_INT"]);
    var ctaCom = gl(rd["CTA_COM"]);
    var ctaIva = gl(rd["CTA_IVA"]);

    var rInt = new ReglaCargo { Codigo = "INT", CuentaGl = ctaInt, Porcentaje = pct(rd["PCT_INT"]), MontoFijo = mf(rd["MF_INT"]) };
    var rCom = new ReglaCargo { Codigo = "COM", CuentaGl = ctaCom, Porcentaje = pct(rd["PCT_COM"]), MontoFijo = mf(rd["MF_COM"]) };
    var rIva = new ReglaCargo { Codigo = "IVA", CuentaGl = ctaIva, Porcentaje = pct(rd["PCT_IVA"]), MontoFijo = 0m };

    if (!rInt.CuentaGl.IsNullOrEmpty() && (rInt.Porcentaje > 0m || rInt.MontoFijo > 0m)) reglas.Add(rInt);
    if (!rCom.CuentaGl.IsNullOrEmpty() && (rCom.Porcentaje > 0m || rCom.MontoFijo > 0m)) reglas.Add(rCom);
    if (!rIva.CuentaGl.IsNullOrEmpty() &&  rIva.Porcentaje > 0m) reglas.Add(rIva);

    return reglas;
}


/// <summary>
/// Lee reglas de e-commerce (terminal virtual) en ADQECTL y las fusiona (override/suma) con las “base”.
/// </summary>
/// <remarks>
/// - Si hay cuenta/porcentaje específico en e-commerce, prioriza esa cuenta (override) y suma porcentajes/montos cuando aplique.
/// - Si no es e-commerce, retorna la lista base sin cambios.
/// </remarks>
private List<ReglaCargo> MergeConEcommerce(List<ReglaCargo> baseRules, int comercio, bool esTerminalVirtual)
{
    if (!esTerminalVirtual) return baseRules;

    var reglas = baseRules.ToDictionary(r => r.Codigo, r => r);

    // === Ejemplo de lectura de ADQECTL ===
    var q = QueryBuilder.Core.QueryBuilder
        .From("ADQECTL", "BCAH96DTA")
        .Select(
            "ADQECNT1 AS CTA_INT_EC",
            "ADQECNT5 AS CTA_COM_EC",
            "ADQECTR1 AS PCT_INT_EC",
            "ADQECTR5 AS PCT_COM_EC"
        )
        .WhereRaw("A02COME = :c") // clave comercio (ajusta si tu PF difiere)
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

    // INT
    if (!ctaInt.IsNullOrEmpty() || pctInt > 0m)
    {
        if (!reglas.TryGetValue("INT", out var r))
            r = reglas["INT"] = new() { Codigo = "INT" };

        if (!ctaInt.IsNullOrEmpty()) r.CuentaGl = ctaInt; // override cuenta GL
        r.Porcentaje += pctInt;                           // sumatoria de % para INT EC
    }

    // COM
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
/// Orquestador: trae reglas base (IADQCTL) y las fusiona con reglas e-commerce (ADQECTL) si aplica.
/// </summary>
private List<ReglaCargo> ObtenerReglasCargos(string perfil, int comercio, bool esTerminalVirtual = false)
{
    var baseRules = ObtenerReglasDesdeIadqctl(perfil, comercio);
    return MergeConEcommerce(baseRules, comercio, esTerminalVirtual);
}


var reglas = ObtenerReglasCargos(perfilTranserver, int.Parse(guardarTransaccionesDto.CodigoComercio), esTerminalVirtual: true /* o valida */);



/// <summary>
/// Obtiene el siguiente FTSBT para un perfil (1..999) de forma segura y lo reserva con INSERT.
/// </summary>
/// <remarks>
/// - Previene colisiones bajo concurrencia usando el propio INSERT como “cerrojo”.  
/// - Si llega a 999, vuelve a 1 (ajusta si tu negocio requiere otra política).
/// </remarks>
private (int ftsbt, bool ok) ReservarNumeroLote(string perfil, int dsdt, string usuario)
{
    for (var intento = 0; intento < 5; intento++)
    {
        // 1) Leer MAX(FTSBT) existente
        var sel = QueryBuilder.Core.QueryBuilder
            .From("POP801", "BNKPRD01")
            .Select("COALESCE(MAX(FTSBT), 0) AS MAXFTSBT")
            .Where<Pop801>(x => x.FTTSBK == 1)
            .Where<Pop801>(x => x.FTTSKY == perfil)
            .Build();

        var max = _connection.ExecuteScalar<int?>(sel) ?? 0;

        // 2) Proponer siguiente (wrap 999→1)
        var next = max >= 999 ? 1 : max + 1;

        // 3) Intentar reservar insertando encabezado mínimo
        var ins = new InsertQueryBuilder("POP801", "BNKPRD01")
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
            if (aff > 0) return (next, true); // reservado y creado
        }
        catch
        {
            // Colisión (FTSBT ya lo tomó otro thread). Reintentamos.
        }
    }
    return (0, false);
}


var (numeroLote, reservado) = ReservarNumeroLote(perfilTranserver, Convert.ToInt32(yyyyMMdd), "usuario");
if (!reservado) return BuildError("400", "No fue posible reservar un número de lote.");
// a partir de aquí, usa 'numeroLote' en tus inserts POP802 + ActualizarTotalesPop801(...)





