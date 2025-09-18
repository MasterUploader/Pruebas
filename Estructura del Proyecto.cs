using System.Globalization; // <-- arriba del archivo

/// <summary>
/// Incrementa conteos e importes del encabezado del lote (POP801) según naturaleza.
/// </summary>
private void ActualizarTotalesPop801(string perfil, int lote, string naturaleza, decimal monto)
{
    // Valor como literal SQL con punto decimal (evita coma por cultura local)
    var m = monto.ToString(CultureInfo.InvariantCulture);

    var upd = naturaleza == "C"
        ? new UpdateQueryBuilder("POP801", "BNKPRD01")
            .Set(("FTTSCI", "FTTSCI + 1"), ("FTTSIC", $"FTTSIC + {m}"))
            .Where<Pop801>(x => x.FTTSBK == 1)
            .Where<Pop801>(x => x.FTTSKY == perfil)
            .Where<Pop801>(x => x.FTSBT == lote)
            .Build()
        : new UpdateQueryBuilder("POP801", "BNKPRD01")
            .Set(("FTTSDI", "FTTSDI + 1"), ("FTTSID", $"FTTSID + {m}"))
            .Where<Pop801>(x => x.FTTSBK == 1)
            .Where<Pop801>(x => x.FTTSKY == perfil)
            .Where<Pop801>(x => x.FTSBT == lote)
            .Build();

    using var cmd = _connection.GetDbCommand(upd, _contextAccessor.HttpContext!);
    _ = cmd.ExecuteNonQuery();
}


/// <summary>
/// Obtiene el siguiente FTSBT para un perfil de forma segura y lo reserva insertando POP801 base.
/// </summary>
/// <remarks>
/// - Usa el propio INSERT como lock optimista; si colisiona, reintenta.
/// - Si llega a 999, vuelve a 1 (ajústalo si tu negocio requiere otra política).
/// </remarks>
private (int ftsbt, bool ok) ReservarNumeroLote(string perfil, int dsdt, string usuario)
{
    for (var intento = 0; intento < 5; intento++)
    {
        // 1) MAX(FTSBT) para ese perfil/banco
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

        // 2) Proponer siguiente (wrap 999→1)
        var next = max >= 999 ? 1 : max + 1;

        // 3) Intentar reservar insertando el encabezado base
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
            using var insCmd = _connection.GetDbCommand(ins, _contextAccessor.HttpContext!);
            var aff = insCmd.ExecuteNonQuery();
            if (aff > 0) return (next, true); // reservado con éxito
        }
        catch
        {
            // Colisión de clave (otro hilo tomó el número): reintentar
        }
    }

    return (0, false);
}



using var cmd = _connection.GetDbCommand(query, _contextAccessor.HttpContext!);
var obj = cmd.ExecuteScalar();
