// ============================ Ver_cta ============================

private enum TipoCuenta { Desconocido = 0, Cheques = 1, Ahorros = 2 }

private sealed class VerCtaResult
{
    public bool Found { get; init; }
    public int CodigoTipo { get; init; }            // DMTYPE real del core
    public TipoCuenta Tipo { get; init; }           // Mapeo normalizado
    public string DescCorta { get; init; } = "";    // "CHQ" / "AHO" / ""
}

/// <summary>
/// Emula el RPG VER_CTA: lee BNKPRD01.TAP002 y clasifica la cuenta (Cheques/Ahorros).
/// </summary>
/// <remarks>
/// - Usa DMBK=1 (ajústalo si tu banco es otro) y DMACCT = número de cuenta.
/// - DMTYPE (3,0) es el “Account Type” del core; aquí lo mapeamos a Cheques/Ahorros.
/// - Si necesitas un mapeo exacto de códigos, edita el diccionario _mapTiposCore.
/// </remarks>
private VerCtaResult VerCta(string numeroCuenta)
{
    // 1) Query a TAP002
    var q = new SelectQueryBuilder("TAP002", "BNKPRD01")
        .Select("DMTYPE")                 // Tipo de cuenta (3,0)
        .WhereRaw("DMBK = 1")             // Banco 001 (ajustable)
        .AndRaw("DMACCT = @pCuenta")      // Número de cuenta
        .Build();

    using var cmd = _connection.GetDbCommand(q, _contextAccessor.HttpContext!);
    var p = cmd.CreateParameter();
    p.ParameterName = "@pCuenta";
    p.Value = numeroCuenta;
    cmd.Parameters.Add(p);

    int tipoCore = 0;
    using (var rd = cmd.ExecuteReader())
    {
        if (!rd.Read())
            return new VerCtaResult { Found = false, CodigoTipo = 0, Tipo = TipoCuenta.Desconocido, DescCorta = "" };

        // DMTYPE puede venir como decimal/numeric → convertir con seguridad
        var ordinal = rd.GetOrdinal("DMTYPE");
        if (!rd.IsDBNull(ordinal))
            tipoCore = Convert.ToInt32(rd.GetValue(ordinal), System.Globalization.CultureInfo.InvariantCulture);
    }

    // 2) Mapeo core → semántica negocio
    // TODO: sustituye por los códigos reales de tu core si difieren.
    // Convención común: 10 = Cheques (DDA), 20 = Ahorros (SAV).
    var _mapTiposCore = new Dictionary<int, TipoCuenta>
    {
        { 10, TipoCuenta.Cheques },
        { 20, TipoCuenta.Ahorros },
    };

    var tipo = _mapTiposCore.TryGetValue(tipoCore, out var t) ? t : TipoCuenta.Desconocido;
    var desc = tipo switch
    {
        TipoCuenta.Cheques    => "CHQ",
        TipoCuenta.Ahorros    => "AHO",
        _                     => ""
    };

    return new VerCtaResult
    {
        Found = true,
        CodigoTipo = tipoCore,
        Tipo = tipo,
        DescCorta = desc
    };
}



// Antes de PostearDesglose/InsertPop802, resuelve el tipo de cuenta:
var verCta = VerCta(guardarTransaccionesDto.NumeroCuenta);

// Ejemplo de uso directo cuando armes tus leyendas:
var al3 = $"&{EtiquetaConcepto(nat)}&{guardarTransaccionesDto.IdTransaccionUnico}&{(verCta.DescCorta)}";

// y pasas `al3` al InsertPop802 como ya lo haces.
