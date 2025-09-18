¡perfecto! Tomando tu clase tal cual como base, aquí te dejo lo que falta para cubrir lo que hacía RPG:

1. Completar ReservarNumeroLote(...) (te quedó truncado).


2. Agregar tipo de cambio USD (ObtenerValorDolar).


3. Agregar lectura de parámetros EC (LeerAdqectlEc).


4. Agregar membresía pendiente + marcar pagado.


5. Agregar posteo de e-commerce (PostearEcommerce) y llamarlo solo si la terminal es virtual y si hay monto (>0).



Colócalos dentro de tu misma clase TransaccionesServices (mismo archivo), debajo de tus otros métodos. Al final te dejo un snippet muy pequeño de invocación para no tocar nada más de tu flujo.


---

1) ReservarNumeroLote(...) completo

/// <summary>
/// Reserva un número de lote (FTSBT) insertando POP801 con encabezado base.
/// </summary>
/// <remarks>
/// Usa reintento optimista ante colisiones de clave. Si FTSBT llega a 999, vuelve a 1.
/// La fecha almacenada en POP801.FTTSDT se graba en formato juliano (7,0).
/// </remarks>
private (int ftsbt, bool ok) ReservarNumeroLote(string perfil, int fechaYyyyMmDd, string usuario)
{
    // Conversión a juliano (7,0) según tu helper Utilities
    int fttsdt = Convert.ToInt32(Utilities.ToJulian(fechaYyyyMmDd.ToString(CultureInfo.InvariantCulture)));

    for (var intento = 0; intento < 5; intento++)
    {
        // 1) MAX(FTSBT) para (bank=001, perfil)
        var sel = QueryBuilder.Core.QueryBuilder
            .From("POP801", "BNKPRD01")
            .Select("COALESCE(MAX(FTSBT), 0) AS MAXFTSBT")
            .Where<Pop801>(x => x.FTTSBK == 1)
            .Where<Pop801>(x => x.FTTSKY == perfil)
            .Build();

        int max;
        using (var selCmd = _connection.GetDbCommand(sel, _contextAccessor.HttpContext!))
        {
            var obj = selCmd.ExecuteScalar();
            max = obj is null || obj is DBNull ? 0 : Convert.ToInt32(obj, CultureInfo.InvariantCulture);
        }

        // 2) Siguiente con wrap 999→1
        var next = max >= 999 ? 1 : max + 1;

        // 3) Intento de reserva: insertar encabezado base
        var ins = new InsertQueryBuilder("POP801", "BNKPRD01")
            .IntoColumns("FTTSBK", "FTTSKY", "FTTSBT", "FTTSST", "FTTSOR", "FTTSDT",
                         "FTTSDI", "FTTSCI", "FTTSID", "FTTSIC", "FTTSDP", "FTTSCP",
                         "FTTSPD", "FTTSPC", "FTTSBD", "FTTSLD", "FTTSBC", "FTTSLC")
            .Row([
                1, perfil, next, 2, usuario, fttsdt,
                0, 0, 0m, 0m, 0, 0,
                0m, 0m, 0m, 0m, 0m, 0m
            ])
            .Build();

        try
        {
            using var insCmd = _connection.GetDbCommand(ins, _contextAccessor.HttpContext!);
            var aff = insCmd.ExecuteNonQuery();
            if (aff > 0) return (next, true);
        }
        catch
        {
            // Colisión (alguien tomó el next); reintenta
        }
    }

    return (0, false);
}


---

2) Tipo de cambio USD

/// <summary>
/// Obtiene el tipo de cambio de compra para USD desde <c>BNKPRD01/GLC002</c>.
/// </summary>
/// <returns>Tasa decimal; si no existe registro retorna 1.00.</returns>
private decimal ObtenerValorDolar()
{
    var sql = QueryBuilder.Core.QueryBuilder
        .From("GLC002", "BNKPRD01")
        .Select("GBBKXR")
        .WhereRaw("GBBKCD = 001")
        .WhereRaw("GBCRCD = 'USD'")
        .FetchNext(1)
        .Build();

    using var cmd = _connection.GetDbCommand(sql, _contextAccessor.HttpContext!);
    var obj = cmd.ExecuteScalar();
    return obj is null || obj is DBNull ? 1m : Convert.ToDecimal(obj, CultureInfo.InvariantCulture);
}


---

3) Parámetros EC (control ‘EC’) – ADQECTL

> Si tus nombres de columnas difieren, dime los reales y lo ajusto.



/// <summary>
/// Lee parámetros de e-commerce (control 'EC') para un comercio desde <c>BCAH96DTA/ADQECTL</c>.
/// </summary>
/// <returns>
/// (ctaIntEc, ctaComEc, pctIntEc, pctComEc); porcentajes vienen base 1 (ej. 0.02 = 2%).
/// </returns>
private (string ctaIntEc, string ctaComEc, decimal pctIntEc, decimal pctComEc) LeerAdqectlEc(decimal comercio)
{
    var sql = QueryBuilder.Core.QueryBuilder
        .From("ADQECTL", "BCAH96DTA")
        .Select(
            "ADQECNT1 AS CTA_INT_EC",   // GL Intereses EC
            "ADQECNT5 AS CTA_COM_EC",   // GL Comisión EC
            "ADQECTR1 AS PCT_INT_EC",   // % Intereses EC (0..100)
            "ADQECTR5 AS PCT_COM_EC"    // % Comisión EC (0..100)
        )
        .WhereRaw("ADQECONT = 'EC'")    // campo de control lógico (ajusta si tu PF usa otro nombre)
        .WhereRaw("A02COME = :COME")
        .WithParameters(new { COME = comercio })
        .FetchNext(1)
        .Build();

    using var cmd = _connection.GetDbCommand(sql, _contextAccessor.HttpContext!);
    using var rd = cmd.ExecuteReader();
    if (!rd.Read()) return ("", "", 0m, 0m);

    static string Gl(object o)   => o is DBNull ? "" : Convert.ToString(o)!.Trim();
    static decimal Pct(object o) => o is DBNull ? 0m : Convert.ToDecimal(o, CultureInfo.InvariantCulture) / 100m;

    var ctaInt = Gl(rd["CTA_INT_EC"]);
    var ctaCom = Gl(rd["CTA_COM_EC"]);
    var pctInt = Pct(rd["PCT_INT_EC"]);
    var pctCom = Pct(rd["PCT_COM_EC"]);

    return (ctaInt, ctaCom, pctInt, pctCom);
}


---

4) Membresía pendiente + cambiar estado

/// <summary>
/// Obtiene el monto de membresía pendiente (si es USD lo convierte a LCYE) para el comercio.
/// </summary>
/// <remarks>Si no hay pendiente o es 0, retorna 0 y no se postea.</remarks>
private decimal ObtenerMembresiaPendiente(decimal comercio)
{
    var sql = QueryBuilder.Core.QueryBuilder
        .From("ADQCOBRO", "BCAH96DTA")
        .Select("ADQCOBRO04 AS MONTO", "ADQCOBRO18 AS ESTADO", "ADQCOBROMO AS MONEDA")
        .WhereRaw("A02COME = :COME")
        .WhereRaw("ADQCOBRO18 = 'PENDIENTE'")
        .WithParameters(new { COME = comercio })
        .FetchNext(1)
        .Build();

    using var cmd = _connection.GetDbCommand(sql, _contextAccessor.HttpContext!);
    using var rd = cmd.ExecuteReader();
    if (!rd.Read()) return 0m;

    var monto = rd["MONTO"] is DBNull ? 0m : Convert.ToDecimal(rd["MONTO"], CultureInfo.InvariantCulture);
    if (monto <= 0m) return 0m;

    var moneda = rd["MONEDA"] is DBNull ? "" : Convert.ToString(rd["MONEDA"])?.Trim() ?? "";
    if (string.Equals(moneda, "USD", StringComparison.OrdinalIgnoreCase))
    {
        var tasa = ObtenerValorDolar();
        monto = Decimal.Round(monto * tasa, 2, MidpointRounding.AwayFromZero);
    }

    return monto;
}

/// <summary>
/// Cambia el estado de membresía pendiente a PAGADO para el comercio.
/// </summary>
private void MarcarMembresiaPagada(decimal comercio)
{
    var up = new UpdateQueryBuilder("ADQCOBRO", "BCAH96DTA")
        .Set(("ADQCOBRO18", "PAGADO"))
        .WhereRaw("A02COME = :COME")
        .WhereRaw("ADQCOBRO18 = 'PENDIENTE'")
        .WithParameters(new { COME = comercio })
        .Build();

    using var cmd = _connection.GetDbCommand(up, _contextAccessor.HttpContext!);
    _ = cmd.ExecuteNonQuery();
}


---

5) Posteo de e-commerce (Db/Cr) + cambio de estado

> Usa tu InsertPop802(...) para generar la partida. Ajusto el tcode según la naturaleza del cargo (si el total original era “C”, el cargo es “D”, y viceversa).



/// <summary>
/// Postea el cobro de membresía e-commerce (si procede) y cambia estado a PAGADO.
/// </summary>
/// <param name="comercio">DTO mínimo de comercio (A02COME/A02CTDE).</param>
/// <param name="fechaYyyyMmDd">Fecha efectiva YYYYMMDD.</param>
/// <param name="perfil">Perfil TS activo.</param>
/// <param name="lote">Número de lote reservado.</param>
/// <param name="secuenciaRef">Referencia de secuencia (se incrementa dentro del método).</param>
/// <param name="dto">Payload de la petición (para leyendas).</param>
/// <param name="naturalezaPrincipal">“C” o “D” de la transacción principal; el cargo se invierte.</param>
private void PostearEcommerce(
    (decimal A02COME, string? A02CTDE) comercio,
    int fechaYyyyMmDd,
    string perfil,
    int lote,
    ref int secuenciaRef,
    GuardarTransaccionesDto dto,
    string naturalezaPrincipal
)
{
    // 1) monto membresía
    var membresia = ObtenerMembresiaPendiente(comercio.A02COME);
    if (membresia <= 0m) return; // regla: si = 0, no postea ni cambia estado

    // 2) control EC (opcional: para redefinir cuenta destino)
    var (ctaIntEc, ctaComEc, _, _) = LeerAdqectlEc(comercio.A02COME);
    var cuentaDestino = !string.IsNullOrWhiteSpace(ctaComEc)
        ? ctaComEc
        : (!string.IsNullOrWhiteSpace(ctaIntEc) ? ctaIntEc : (comercio.A02CTDE?.Trim() ?? dto.NumeroCuenta));

    // 3) naturaleza del cargo (inversa a la principal) y tcode
    var natCargo = naturalezaPrincipal.Equals("C", StringComparison.OrdinalIgnoreCase) ? "D" : "C";
    var tcode = natCargo == "C" ? "0783" : "0784";

    // 4) Insert POP802
    secuenciaRef += 1;
    InsertPop802(
        perfil: perfil,
        lote: lote,
        seq: secuenciaRef,
        fechaYyyyMmDd: fechaYyyyMmDd,
        cuenta: cuentaDestino,
        centroCosto: 0,
        codTrn: tcode,
        monto: membresia,
        al1: Trunc(dto.NombreComercio, 30),
        al2: Trunc($"{dto.CodigoComercio}-{dto.Terminal}", 30),
        al3: Trunc($"&MEM&{dto.IdTransaccionUnico}&EC", 30)
    );

    // 5) marcar pagado
    MarcarMembresiaPagada(comercio.A02COME);
}


---

Dónde llamar PostearEcommerce(...)

Justo después de tu PostearDesglose(...). Tienes ya esTerminalVirtual de BuscarTerminal. Añade este bloque (no altera nada de lo tuyo):

// ya tienes: var secuencia = 0; y posteoDesglose...
if (!posteoDesglose) return BuildError("400", "No fue posible postear el detalle de la transacción (POP802).");

// >>> Añadir: cobro e-commerce solo si terminal virtual
if (esTerminalVirtual)
{
    // Si necesitas A02CTDE, puedes haberla traído en BuscarComercio; como ya validaste, mínimamente dispones de dto.NumeroCuenta.
    var comercioMin = (A02COME: Convert.ToDecimal(guardarTransaccionesDto.CodigoComercio, CultureInfo.InvariantCulture),
                       A02CTDE: guardarTransaccionesDto.NumeroCuenta);

    PostearEcommerce(
        comercio: comercioMin,
        fechaYyyyMmDd: Convert.ToInt32(yyyyMMdd),
        perfil: perfilTranserver,
        lote: numeroLote,
        secuenciaRef: ref secuencia,
        dto: guardarTransaccionesDto,
        naturalezaPrincipal: nat
    );
}


---

Si alguna PF tiene nombres de columnas distintos (por ejemplo el control en ADQECTL), dímelos y te lo dejo ajustado al 100%.

    
