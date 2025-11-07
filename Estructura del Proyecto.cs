// ===== Normalizadores y helpers =====
private static string Fit(string? s, int len, bool padLeft = false, char pad = ' ')
{
    s ??= string.Empty;
    s = s.Trim();
    if (s.Length > len) return s[..len];
    return padLeft ? s.PadLeft(len, pad) : s.PadRight(len, pad);
}

private static string OnlyDigits(string? s)
    => new string((s ?? string.Empty).Where(char.IsDigit).ToArray());

private static string EstadoOkCode(string? code)
{
    // Acepta "0000" o "00000" y lo normaliza a 5 dígitos.
    var digits = OnlyDigits(code);
    if (string.IsNullOrEmpty(digits)) return "99999";
    return digits.PadLeft(5, '0');
}

private static bool IsOk(string? code)
{
    var c = EstadoOkCode(code);
    return c == "00000";
}

private static (string corte6, string stan6) ClavesCorteStan(string numeroCorte, string idTransaccionUnico)
{
    var corte6 = Fit(OnlyDigits(numeroCorte), 6, padLeft: true, pad: '0');
    var stan6  = Fit(OnlyDigits(idTransaccionUnico), 6, padLeft: true, pad: '0');
    return (corte6, stan6);
}

private static bool IsUniqueViolation(Exception ex)
{
    var msg = (ex?.ToString() ?? string.Empty).ToUpperInvariant();
    // DB2 for i: SQL0803N / SQLSTATE 23505 (clave duplicada)
    return msg.Contains("SQL0803") || msg.Contains("23505") || msg.Contains("DUPLIC");
}

// ===== Inserta la reserva inicial en POSRE01G =====
private async Task<(bool inserted, bool duplicate, string? errorMsg)> GuardarReservaPosre01gAsync(
    GuardarTransaccionesDto dto, CancellationToken ct)
{
    var (corte6, stan6) = ClavesCorteStan(dto.NumeroDeCorte, dto.IdTransaccionUnico);

    var now  = DateTime.Now;
    var guid = Guid.NewGuid().ToString().ToUpperInvariant();

    // Valores ya ajustados a longitudes DDS
    var FECHAPOST = now.ToString("yyyyMMdd");
    var HORAPOST  = now.ToString("HHmmss");

    var NUMCUENTA = Fit(dto.NumeroCuenta, 16);
    var MTODEBITO = Fit(dto.MontoDebitado, 18);
    var MTOACREDI = Fit(dto.MontoAcreditado, 18);

    var CODCOMERC = Fit(OnlyDigits(dto.CodigoComercio), 7, padLeft: true, pad: '0');
    var NOMCOMERC = Fit(dto.NombreComercio, 100);
    var TERMINAL  = Fit(dto.Terminal, 8);
    var DESCRIPC  = Fit(dto.Descripcion, 200);

    var NATCONTA  = Fit((dto.NaturalezaContable ?? "").ToUpperInvariant(), 1);
    var NUMCORTE  = corte6;
    var IDTRANUNI = stan6;

    var ESTADO       = "P";                             // Pendiente
    var DESCESTADO   = "En proceso";
    var CODERROR     = "99999";                         // ≠ éxito
    var DESCERROR    = "Reserva inicial";

    var ins = new InsertQueryBuilder("POSRE01G", "BCAH96DTA", SqlDialect.Db2i)
        .IntoColumns(
            "GUID","FECHAPOST","HORAPOST","NUMCUENTA","MTODEBITO","MTOACREDI",
            "CODCOMERC","NOMCOMERC","TERMINAL","DESCRIPC","NATCONTA",
            "NUMCORTE","IDTRANUNI","ESTADO","DESCESTADO","CODERROR","DESCERROR"
        )
        .Values(
            ("GUID", guid),
            ("FECHAPOST", FECHAPOST),
            ("HORAPOST",  HORAPOST),
            ("NUMCUENTA", NUMCUENTA),
            ("MTODEBITO", MTODEBITO),
            ("MTOACREDI", MTOACREDI),
            ("CODCOMERC", CODCOMERC),
            ("NOMCOMERC", NOMCOMERC),
            ("TERMINAL",  TERMINAL),
            ("DESCRIPC",  DESCRIPC),
            ("NATCONTA",  NATCONTA),
            ("NUMCORTE",  NUMCORTE),
            ("IDTRANUNI", IDTRANUNI),
            ("ESTADO",    ESTADO),
            ("DESCESTADO",DESCESTADO),
            ("CODERROR",  CODERROR),
            ("DESCERROR", DESCERROR)
        )
        .WithComment("Reserva inicial anti-duplicados via LF IPOSRE01G1 (NUMCORTE+IDTRANUNI)")
        .Build();

    try
    {
        using var cmd = _connection.GetDbCommand(ins, _contextAccessor.HttpContext!);
        await cmd.ExecuteNonQueryAsync(ct);
        return (true, false, null);
    }
    catch (Exception ex) when (IsUniqueViolation(ex))
    {
        // Ya existe la combinación (NUMCORTE, IDTRANUNI) por el índice UNIQUE IPOSRE01G1
        return (false, true, ex.Message);
    }
    catch (Exception ex)
    {
        return (false, false, ex.Message);
    }
}

// ===== Actualiza el resultado del posteo en POSRE01G =====
private async Task<bool> ActualizarResultadoPosre01gAsync(
    string numeroCorte, string idTransaccionUnico, string codigo, string descripcion, CancellationToken ct)
{
    var (corte6, stan6) = ClavesCorteStan(numeroCorte, idTransaccionUnico);
    var code5 = EstadoOkCode(codigo);

    var ESTADO     = IsOk(code5) ? "A" : "R";                    // A=aprobada, R=rechazada
    var DESCESTADO = IsOk(code5) ? "Aprobada" : "Rechazada";

    var upd = new UpdateQueryBuilder("POSRE01G", "BCAH96DTA", SqlDialect.Db2i)
        .Set(("CODERROR",    code5))
        .Set(("DESCERROR",   Fit(descripcion, 200)))
        .Set(("ESTADO",      ESTADO))
        .Set(("DESCESTADO",  DESCESTADO))
        .WhereRaw($"NUMCORTE = '{corte6}'")
        .WhereRaw($"IDTRANUNI = '{stan6}'")
        .Build();

    using var cmd = _connection.GetDbCommand(upd, _contextAccessor.HttpContext!);
    var rows = await cmd.ExecuteNonQueryAsync(ct);
    return rows > 0;
}




// =================== Reserva inicial anti-duplicados ===================
var (inserted, duplicate, insertErr) = await GuardarReservaPosre01gAsync(guardarTransaccionesDto, ct);
if (!inserted)
{
    if (duplicate)
        return BuildError(BizCodes.YaProcesado, "Transaccion ya registrada (NUMCORTE+IDTRANUNI)."); // 409 via BizHttpMapper
    return BuildError(BizCodes.ErrorSql, "No se pudo registrar reserva inicial: " + insertErr);
}
// ======================================================================



// 4) Ejecutamos INT_LOTES...
var respuesta = await PosteoLoteAsync(p);

// Actualizar el registro con el resultado del posteo
await ActualizarResultadoPosre01gAsync(
    guardarTransaccionesDto.NumeroDeCorte,
    guardarTransaccionesDto.IdTransaccionUnico,
    respuesta.CodigoErrorPosteo,
    respuesta.DescripcionErrorPosteo,
    ct);

// Cerrar y responder
_connection.Close();
return BuildError(respuesta.CodigoErrorPosteo, respuesta.DescripcionErrorPosteo);



