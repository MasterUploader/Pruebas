// ============================ Modelos de cargos ============================

/// <summary>
/// Regla de cargo a aplicar sobre el total (porcentaje o monto fijo) y su cuenta GL.
/// </summary>
/// <remarks>
/// - Si <see cref="Porcentaje"/> &gt; 0, se calcula (Total * Porcentaje).  
/// - Si <see cref="MontoFijo"/> &gt; 0, se usa ese monto.  
/// - La naturaleza del cargo es la **opuesta** a la línea principal (para compensar).  
/// </remarks>
public sealed class ReglaCargo()
{
    /// <summary>Código legible del cargo (ej. "INT", "COM", "IVA").</summary>
    public string Codigo { get; set; } = string.Empty;

    /// <summary>Cuenta contable (GL) destino del cargo.</summary>
    public string CuentaGl { get; set; } = string.Empty;

    /// <summary>Porcentaje (0..1). Ej. 0.03 = 3%.</summary>
    public decimal Porcentaje { get; set; }

    /// <summary>Monto fijo del cargo (si aplica).</summary>
    public decimal MontoFijo { get; set; }
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



// ============================ Integración en GuardarTransaccionesAsync ============================
// Reemplaza el switch actual por esta llamada (después de crear el lote 'numeroLote'):

// Naturaleza 'C' o 'D' (una sola por request):
var nat = guardarTransaccionesDto.NaturalezaContable;

// Reglas de cargos: aquí las obtienes (perfil/comercio/parametría). Dejo stub para que las levantes de DB.
var reglas = ObtenerReglasCargos(perfilTranserver, int.Parse(guardarTransaccionesDto.CodigoComercio));

// Monto bruto según naturaleza:
var montoBruto = nat == "C" ? cre : deb;

// Posteamos desglose (línea principal + cargos)
secuencia = PostearDesglose(
    perfil: perfilTranserver,
    numeroLote: numeroLote,
    fechaYyyyMmDd: Convert.ToInt32(yyyyMMdd),
    naturalezaPrincipal: nat,                   // "C" o "D"
    cuentaComercio: guardarTransaccionesDto.NumeroCuenta,
    totalBruto: montoBruto,
    reglas: reglas,
    codComercio: guardarTransaccionesDto.CodigoComercio,
    terminal: guardarTransaccionesDto.Terminal,
    nombreComercio: guardarTransaccionesDto.NombreComercio,
    idUnico: guardarTransaccionesDto.IdTransaccionUnico,
    secuenciaInicial: secuencia
);

// (Opcional) Si quieres responder OK aquí, deja el return:
return BuildError(code: "200", message: "Transacción procesada correctamente.");


// ============================ Núcleo del desglose ============================

/// <summary>
/// Postea una línea principal (neto) y N líneas de cargos en POP802.
/// Actualiza totales de POP801 por cada línea insertada.
/// </summary>
/// <returns>Secuencia final utilizada.</returns>
private int PostearDesglose(
    string perfil,
    int numeroLote,
    int fechaYyyyMmDd,
    string naturalezaPrincipal,      // "C" o "D"
    string cuentaComercio,
    decimal totalBruto,
    List<ReglaCargo> reglas,
    string codComercio,
    string terminal,
    string nombreComercio,
    string idUnico,
    int secuenciaInicial = 0)
{
    // 1) Calcular cargos
    var cargos = CalcularCargos(totalBruto, reglas);
    var totalCargos = cargos.Sum(x => x.Monto);

    // 2) Determinar neto (lo que finalmente se acredita/debita al comercio)
    var neto = Decimal.Round(totalBruto - totalCargos, 2, MidpointRounding.AwayFromZero);

    // 3) Insertar línea principal (misma naturaleza que el request)
    var seq = secuenciaInicial + 1;
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
        al3: Trunc($"&{EtiquetaConcepto(naturalezaPrincipal)}&{idUnico}&{(naturalezaPrincipal=="C"?"Cr Neto":"Db Neto")}", 30)
    );
    // Actualizar totales POP801 para la principal
    ActualizarTotalesPop801(perfil, numeroLote, naturalezaPrincipal, neto);

    // 4) Insertar cargos (naturaleza opuesta a la principal, a sus cuentas GL)
    var natCargo = naturalezaPrincipal == "C" ? "D" : "C";
    foreach (var c in cargos.Where(c => c.Monto > 0m))
    {
        seq += 1;

        InsertPop802(
            perfil: perfil,
            lote: numeroLote,
            seq: seq,
            fechaYyyyMmDd: fechaYyyyMmDd,
            cuenta: c.CuentaGl,                // << cuenta GL del cargo
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

// ============================ Cálculo de cargos ============================

/// <summary>
/// Aplica cada regla (porcentaje y/o monto fijo) sobre el total bruto.
/// </summary>
private static List<CargoCalculado> CalcularCargos(decimal totalBruto, List<ReglaCargo> reglas)
{
    var res = new List<CargoCalculado>();
    foreach (var r in reglas)
    {
        // Monto por porcentaje
        var mp = r.Porcentaje > 0m ? Decimal.Round(totalBruto * r.Porcentaje, 2, MidpointRounding.AwayFromZero) : 0m;
        // Monto fijo
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


// ============================ Totales POP801 ============================

/// <summary>
/// Incrementa conteos e importes del encabezado del lote (POP801) según naturaleza.
/// </summary>
private void ActualizarTotalesPop801(string perfil, int lote, string naturaleza, decimal monto)
{
    // Si C → FTTSCI++ y FTTSIC += monto; Si D → FTTSDI++ y FTTSID += monto.
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



// ============================ Totales POP801 ============================

/// <summary>
/// Incrementa conteos e importes del encabezado del lote (POP801) según naturaleza.
/// </summary>
private void ActualizarTotalesPop801(string perfil, int lote, string naturaleza, decimal monto)
{
    // Si C → FTTSCI++ y FTTSIC += monto; Si D → FTTSDI++ y FTTSID += monto.
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


// ============================ Parametría de cargos (stub) ============================

/// <summary>
/// Obtiene reglas de cargos a aplicar para un perfil/comercio.
/// </summary>
/// <remarks>
/// - Aquí debes mapear con tus tablas de parametría (ej. IADQCTL/ADQECTL/CFP801 custom).  
/// - Devuelvo ejemplo: 2 cargos (3% de interés y comisión fija 1.50) a cuentas GL ilustrativas.
/// </remarks>
private static List<ReglaCargo> ObtenerReglasCargos(string perfil, int comercio)
{
    // TODO: reemplazar por SELECTs reales con QueryBuilder (según tu modelo de parametría).
    return
    [
        new() { Codigo = "INT", CuentaGl = "1102003001", Porcentaje = 0.03m, MontoFijo = 0m },  // 3% interés
        new() { Codigo = "COM", CuentaGl = "4101001001", Porcentaje = 0m,     MontoFijo = 1.50m } // 1.50 comisión fija
    ];
}








