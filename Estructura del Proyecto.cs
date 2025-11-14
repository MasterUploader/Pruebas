// ===== Inserta la reserva inicial en POSRE01G =====
private async Task<(bool inserted, bool duplicate, string? errorMsg)> GuardarReservaPosre01gAsync(
    GuardarTransaccionesDto dto,
    CancellationToken ct)
{
    // Claves normalizadas usadas por el LF único (NUMCORTE + IDTRANUNI).
    var (corte6, stan6) = ClavesCorteStan(dto.NumeroDeCorte, dto.IdTransaccionUnico);

    var now = DateTime.Now;
    var guid = Guid.NewGuid().ToString().ToUpperInvariant();

    // Valores ya ajustados a longitudes DDS
    var FECHAPOST = now.ToString("yyyyMMdd");
    var HORAPOST = now.ToString("HHmmss");

    var NUMCUENTA = dto.NumeroCuenta;
    var MTODEBITO = dto.MontoDebitado;
    var MTOACREDI = dto.MontoAcreditado;

    var CODCOMERC = dto.CodigoComercio;
    var NOMCOMERC = dto.NombreComercio;
    var TERMINAL = dto.Terminal;
    var DESCRIPC = dto.Descripcion;

    var NATCONTA = dto.NaturalezaContable.ToUpperInvariant();
    var NUMCORTE = corte6;
    var IDTRANUNI = stan6;

    var ESTADO = "P";                   // Pendiente
    var DESCESTADO = "En proceso";
    var CODERROR = "99999";             // ≠ éxito
    var DESCERROR = "Reserva inicial";  // Marca que aún no se ha ejecutado INT_LOTES

    // Builder de INSERT usando el LF IPOSRE01G1 (índice único).
    var ins = new InsertQueryBuilder("IPOSRE01G1", "BCAH96DTA") // Índice IPOSRE01G1 sobre tabla POSRE01G
        .IntoColumns(
            "GUID", "FECHAPOST", "HORAPOST", "NUMCUENTA", "MTODEBITO", "MTOACREDI",
            "CODCOMERC", "NOMCOMERC", "TERMINAL", "DESCRIPC", "NATCONTA",
            "NUMCORTE", "IDTRANUNI", "ESTADO", "DESCESTADO", "CODERROR", "DESCERROR"
        )
        .Values(
            ("GUID", guid),
            ("FECHAPOST", FECHAPOST),
            ("HORAPOST", HORAPOST),
            ("NUMCUENTA", NUMCUENTA),
            ("MTODEBITO", MTODEBITO),
            ("MTOACREDI", MTOACREDI),
            ("CODCOMERC", CODCOMERC),
            ("NOMCOMERC", NOMCOMERC),
            ("TERMINAL", TERMINAL),
            ("DESCRIPC", DESCRIPC),
            ("NATCONTA", NATCONTA),
            ("NUMCORTE", NUMCORTE),
            ("IDTRANUNI", IDTRANUNI),
            ("ESTADO", ESTADO),
            ("DESCESTADO", DESCESTADO),
            ("CODERROR", CODERROR),
            ("DESCERROR", DESCERROR)
        )
        // Comentario funcional que aparecerá en el log SQL y facilita trazabilidad.
        .WithComment($"Reserva inicial anti-duplicados via LF IPOSRE01G1 ({NUMCORTE} + {IDTRANUNI})")
        .Build();

    try
    {
        // IMPORTANTE:
        // Se usa ExecuteNonQuery (síncrono) para que el wrapper de logging SQL
        // (LoggingDbCommandWrapper / As400ConnectionProvider) intercepte la ejecución
        // y agregue este comando al bloque de log estructurado, incluso si luego
        // el flujo termina en el if (!inserted).
        using var cmd = _connection.GetDbCommand(ins, _contextAccessor.HttpContext!);
        cmd.ExecuteNonQuery();

        return (true, false, null);
    }
    catch (Exception ex) when (IsUniqueViolation(ex))
    {
        // Clave duplicada detectada por el índice único IPOSRE01G1:
        // se marca como duplicado pero NO se trata como error SQL fatal.
        // El log SQL ya fue generado por el wrapper al lanzar la excepción.
        return (false, true, ex.Message);
    }
    catch (Exception ex)
    {
        // Error SQL o general distinto a duplicado; se retorna indicador de fallo.
        // El wrapper SQL también registra este fallo con el texto del comando y el mensaje.
        return (false, false, ex.Message);
    }
}


// ===== Actualiza el resultado del posteo en POSRE01G =====
private async Task<bool> ActualizarResultadoPosre01gAsync(
    string numeroCorte,
    string idTransaccionUnico,
    string codigo,
    string descripcion,
    CancellationToken ct)
{
    var (corte6, stan6) = ClavesCorteStan(numeroCorte, idTransaccionUnico);
    var code5 = EstadoOkCode(codigo);

    var ESTADO = IsOk(code5) ? "A" : "R";           // A=aprobada, R=rechazada
    var DESCESTADO = IsOk(code5) ? "Aprobada" : "Rechazada";

    var upd = new UpdateQueryBuilder("POSRE01G", "BCAH96DTA", SqlDialect.Db2i)
        .Set("CODERROR", code5)
        .Set("DESCERROR", descripcion)
        .Set("ESTADO", ESTADO)
        .Set("DESCESTADO", DESCESTADO)
        .WhereRaw($"NUMCORTE = '{corte6}'")
        .WhereRaw($"IDTRANUNI = '{stan6}'")
        .Build();

    using var cmd = _connection.GetDbCommand(upd, _contextAccessor.HttpContext!);

    // Igual que con el INSERT, usamos la versión síncrona para garantizar
    // que el wrapper de logging SQL capture la sentencia y su resultado.
    var rows = cmd.ExecuteNonQuery();

    // Como el método es async por contrato, mantenemos el Task pero sin trabajo real.
    await Task.CompletedTask;

    return rows > 0;
}
