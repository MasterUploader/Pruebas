[HttpPost("ValidarTransacciones")]
[Consumes(MediaTypeNames.Application.Json)]
[ProducesResponseType(typeof(RespuestaValidarTransaccionesDto), StatusCodes.Status200OK)]
[ProducesResponseType(typeof(RespuestaValidarTransaccionesDto), StatusCodes.Status400BadRequest)]
[ProducesResponseType(typeof(RespuestaValidarTransaccionesDto), StatusCodes.Status409Conflict)]
[ProducesResponseType(typeof(RespuestaValidarTransaccionesDto), StatusCodes.Status500InternalServerError)]
public async Task<IActionResult> ValidarTransacciones([FromBody] ValidarTransaccionesDto validarTransaccionesDto, CancellationToken ct = default)
{
    if (!ModelState.IsValid)
    {
        var bad = new RespuestaValidarTransaccionesDto
        {
            CodigoError = BizCodes.SolicitudInvalida,
            DescripcionError = "Solicitud inválida, modelo DTO inválido."
        };
        return StatusCode(BizHttpMapper.ToHttpStatusInt(bad.CodigoError), bad);
    }

    try
    {
        var respuesta = await _validarTransaccionService.ValidarTransaccionesAsync(validarTransaccionesDto, ct);
        var http = BizHttpMapper.ToHttpStatusInt(respuesta.CodigoError ?? BizCodes.ErrorDesconocido);
        return StatusCode(http, respuesta);
    }
    catch (Exception ex)
    {
        var dto = new RespuestaValidarTransaccionesDto
        {
            CodigoError = BizCodes.ErrorDesconocido,
            DescripcionError = ex.Message
        };
        return StatusCode(BizHttpMapper.ToHttpStatusInt(dto.CodigoError), dto);
    }
}





public async Task<RespuestaValidarTransaccionesDto> ValidarTransaccionesAsync(
    ValidarTransaccionesDto validarTransaccionDto, CancellationToken ct = default)
{
    var corte = validarTransaccionDto.NumeroDeCorte;
    var stan  = validarTransaccionDto.IdTransaccionUnico;

    var qb = new SelectQueryBuilder("POSRE01G01", "BCAH96DTA", SqlDialect.Db2i)
        .Select(
            ("GUID", "GUID"),
            ("FECHAPOST", "FECHA_POSTEO"),
            ("HORAPOST", "HORA_POSTEO"),
            ("NUMCUENTA", "NUMERO_CUENTA"),
            ("MTODEBITO", "MONTO_DEBITADO"),
            ("MTOACREDI", "MONTO_ACREDITADO"),
            ("CODCOMERC", "CODIGO_COMERCIO"),
            ("NOMCOMERC", "NOMBRE_COMERCIO"),
            ("TERMINAL", "TERMINAL_COMERCIO"),
            ("DESCRIPC", "DESCRIPCION"),
            ("NATCONTA", "NATURALEZA_CONTABLE"),
            ("NUMCORTE", "NUMERO_CORTE"),
            ("IDTRANUNI", "ID_TRANSACCION_UNICO"),
            ("ESTADO", "ESTADO_TRANSACCION"),
            ("DESCESTADO", "DESCRIPCION_ESTADO"),
            ("CODERROR", "CODIGO_ERROR"),
            ("DESCERROR", "DESCRIPCION_ERROR")
        )
        .WhereRaw($"NUMCORTE = '{corte}'")
        .WhereRaw($"IDTRANUNI = '{stan}'")
        .FetchNext(1);

    var qr = qb.Build();

    _connection.Open();
    if (!_connection.IsConnected)
        return Build("ConexionDbFallida", "No se pudo conectar a la base de datos.");

    try
    {
        DbCommand cmd;
        try { cmd = _connection.GetDbCommand(qr, _contextAccessor?.HttpContext); }
        catch { cmd = _connection.GetDbCommand(); cmd.CommandText = qr.Sql; }

        using var rd = await cmd.ExecuteReaderAsync(ct);
        if (await rd.ReadAsync(ct))
        {
            // ← SI EXISTE: devolvemos **todos los datos** en el DTO
            return new RespuestaValidarTransaccionesDto
            {
                NumeroCuenta        = rd["NUMERO_CUENTA"]?.ToString() ?? "",
                MontoDebitado       = rd["MONTO_DEBITADO"]?.ToString() ?? "",
                MontoAcreditado     = rd["MONTO_ACREDITADO"]?.ToString() ?? "",
                CodigoComercio      = rd["CODIGO_COMERCIO"]?.ToString() ?? "",
                NombreComercio      = rd["NOMBRE_COMERCIO"]?.ToString() ?? "",
                TerminalComercio    = rd["TERMINAL_COMERCIO"]?.ToString() ?? "",
                Descripcion         = rd["DESCRIPCION"]?.ToString() ?? "",
                NaturalezaContable  = rd["NATURALEZA_CONTABLE"]?.ToString() ?? "",
                NumeroCorte         = rd["NUMERO_CORTE"]?.ToString() ?? "",
                IdTransaccionUnico  = rd["ID_TRANSACCION_UNICO"]?.ToString() ?? "",
                EstadoTransaccion   = rd["ESTADO_TRANSACCION"]?.ToString() ?? "",
                DescripcionEstado   = rd["DESCRIPCION_ESTADO"]?.ToString() ?? "",
                CodigoError         = BizCodes.YaProcesado, // -> BizHttpMapper lo mapeará a 409
                DescripcionError    = "La transacción ya existe (NUMCORTE, IDTRANUNI)."
            };
        }

        // ← NO EXISTE: 200 OK con claves eco y mensaje
        return new RespuestaValidarTransaccionesDto
        {
            NumeroCorte        = corte,
            IdTransaccionUnico = stan,
            CodigoError        = BizCodes.Ok,
            DescripcionError   = "No existe el registro. Puede continuar."
        };
    }
    finally
    {
        _connection.Close();
    }
}

private static RespuestaValidarTransaccionesDto Build(string code, string message) =>
    new() { CodigoError = code, DescripcionError = message };
