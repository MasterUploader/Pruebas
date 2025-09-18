// (A) Reservar número de lote (reemplaza tu llamada a NuevoLote(...))
var (numeroLote, reservado) = ReservarNumeroLote(perfilTranserver, Convert.ToInt32(yyyyMMdd), "usuario");
if (!reservado) return BuildError("400", "No fue posible reservar un número de lote (POP801).");

// (B) Preparar reglas y postear desglose
var nat = guardarTransaccionesDto.NaturalezaContable;             // "C" o "D"
var montoBruto = nat == "C" ? cre : deb;

// Si tienes una validación real de terminal virtual, úsala; aquí dejo false por defecto
var reglas = ObtenerReglasCargos(perfilTranserver, int.Parse(guardarTransaccionesDto.CodigoComercio), esTerminalVirtual: false);

// Si quieres continuar numeración de secuencia que ya traías:
var secuencia = 0;

secuencia = PostearDesglose(
    perfil: perfilTranserver,
    numeroLote: numeroLote,
    fechaYyyyMmDd: Convert.ToInt32(yyyyMMdd),
    naturalezaPrincipal: nat,
    cuentaComercio: guardarTransaccionesDto.NumeroCuenta,
    totalBruto: montoBruto,
    reglas: reglas,
    codComercio: guardarTransaccionesDto.CodigoComercio,
    terminal: guardarTransaccionesDto.Terminal,
    nombreComercio: guardarTransaccionesDto.NombreComercio,
    idUnico: guardarTransaccionesDto.IdTransaccionUnico,
    secuenciaInicial: secuencia
);

return BuildError(code: "200", message: "Transacción procesada correctamente.");
