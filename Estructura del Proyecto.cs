// (B) Naturaleza y monto bruto a postear (sin desglose)
var nat = guardarTransaccionesDto.NaturalezaContable;     // "C" o "D"
var montoBruto = nat == "C" ? cre : deb;

// (C) Validación de fondos (solo para débito)
if (nat.Equals("D", StringComparison.OrdinalIgnoreCase))
{
    // Nota funcional:
    // - Este método consulta el disponible de la cuenta contra el core (vista/tabla real a definir).
    // - Si no existe la cuenta o el disponible es menor al monto, se bloquea el débito.
    var disponible = ObtenerSaldoDisponible(guardarTransaccionesDto.NumeroCuenta);
    if (disponible < montoBruto)
        return BuildError("402", "Fondos insuficientes para realizar el débito.");
}

// (D) Posteo simple (un único renglón POP802) según naturaleza contable
var seq = 1; // Primera y única línea del lote para esta transacción

if (nat.Equals("C", StringComparison.OrdinalIgnoreCase))
{
    // Crédito (convención core: 0783)
    InsertPop802(
        perfil: perfilTranserver,
        lote: numeroLote,
        seq: seq,
        fechaYyyyMmDd: Convert.ToInt32(yyyyMMdd),
        cuenta: guardarTransaccionesDto.NumeroCuenta,
        centroCosto: 0,
        codTrn: "0783",
        monto: montoBruto,
        al1: Trunc(guardarTransaccionesDto.NombreComercio, 30),
        al2: Trunc($"{guardarTransaccionesDto.CodigoComercio}-{guardarTransaccionesDto.Terminal}", 30),
        al3: Trunc($"&CR&{guardarTransaccionesDto.IdTransaccionUnico}&Cr Tot.", 30)
    );
}
else if (nat.Equals("D", StringComparison.OrdinalIgnoreCase))
{
    // Débito (convención core: 0784)
    InsertPop802(
        perfil: perfilTranserver,
        lote: numeroLote,
        seq: seq,
        fechaYyyyMmDd: Convert.ToInt32(yyyyMMdd),
        cuenta: guardarTransaccionesDto.NumeroCuenta,
        centroCosto: 0,
        codTrn: "0784",
        monto: montoBruto,
        al1: Trunc(guardarTransaccionesDto.NombreComercio, 30),
        al2: Trunc($"{guardarTransaccionesDto.CodigoComercio}-{guardarTransaccionesDto.Terminal}", 30),
        al3: Trunc($"&DB&{guardarTransaccionesDto.IdTransaccionUnico}&Db Tot.", 30)
    );
}
else
{
    return BuildError("00001", "Naturaleza contable inválida. Use 'C' o 'D'.");
}

return BuildError(code: "200", message: "Transacción procesada correctamente.");



/// <summary>
/// Obtiene el saldo disponible de una cuenta para validar débitos.
/// </summary>
/// <remarks>
/// Funcionalidad:
/// - Consulta el disponible en el core bancario para bloquear débitos sin fondos.
/// - La fuente exacta (tabla/vista/SP) depende de tu instalación:
///   * Ej.: una vista de cuentas corrientes/ahorro (DISPONIBLE),
///   * o un procedimiento que calcule saldo en línea.
/// - Retorna 0 si no encuentra la cuenta o en caso de error controlado.
/// </remarks>
/// <param name="cuenta">Número de cuenta en formato del core.</param>
/// <returns>Monto disponible como <see cref="decimal"/>.</returns>
private decimal ObtenerSaldoDisponible(string cuenta)
{
    try
    {
        // TODO: Ajusta origen real de datos:
        // - Esquema/Librería: BNKPRD01 (ejemplo); cambia si aplique.
        // - Objeto: SALDOS_CUENTA (ejemplo de vista/tablar); reemplaza por la tuya.
        // - Columnas: CUENTA (PK) y DISPONIBLE (monto).
        var q = QueryBuilder.Core.QueryBuilder
            .From("SALDOS_CUENTA", "BNKPRD01")           // <--- AJUSTAR
            .Select("DISPONIBLE")                         // <--- AJUSTAR
            .WhereRaw($"CUENTA = '{cuenta.Trim()}'")      // <--- AJUSTAR (idealmente parametriza si tu IDbCommand soporta)
            .FetchNext(1)
            .Build();

        using var cmd = _connection.GetDbCommand(q, _contextAccessor.HttpContext!);
        var obj = cmd.ExecuteScalar();

        // Conversión robusta a decimal
        return obj is null || obj is DBNull
            ? 0m
            : Convert.ToDecimal(obj, System.Globalization.CultureInfo.InvariantCulture);
    }
    catch
    {
        // Nota funcional: por seguridad ante error, retornamos 0 para forzar bloqueo del débito.
        return 0m;
    }
}
