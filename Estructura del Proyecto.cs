using System.Globalization;

/// <summary>
/// Incrementa conteos e importes del encabezado del lote (POP801) según naturaleza.
/// </summary>
private void ActualizarTotalesPop801(string perfil, int lote, string naturaleza, decimal monto)
{
    // Forzamos el punto decimal (evita coma por cultura local)
    var m = monto.ToString(CultureInfo.InvariantCulture);

    UpdateQueryBuilder updBuilder;

    if (naturaleza == "C")
    {
        // Crédito: FTTSCI++ y FTTSIC += monto
        updBuilder = new UpdateQueryBuilder("POP801", "BNKPRD01")
            .SetRaw("FTTSCI", "FTTSCI + 1")
            .SetRaw("FTTSIC", $"FTTSIC + {m}")
            .Where<Pop801>(x => x.FTTSBK == 1)
            .Where<Pop801>(x => x.FTTSKY == perfil)
            .Where<Pop801>(x => x.FTSBT == lote);
    }
    else
    {
        // Débito: FTTSDI++ y FTTSID += monto
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
