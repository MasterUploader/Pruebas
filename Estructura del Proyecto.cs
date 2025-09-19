private bool TieneFondosSuficientes(string numeroCuenta, decimal importeDebito)
{
    // Lee disponibles para retiro hoy (DMAVL2) desde BNKPRD01.TAP002
    var q = QueryBuilder.Core.QueryBuilder
        .From("TAP002", "BNKPRD01")
        .Select("DMAVL2", "DMCBAL", "DMHOLD", "DMUAV2")
        .WhereRaw("DMBK = 1")                  // banco 001 (ajusta si aplica)
        .WhereRaw("DMACCT = @cta")             // número de cuenta
        .FetchNext(1)
        .Build();

    using var cmd = _connection.GetDbCommand(q, _contextAccessor.HttpContext!);
    var p = cmd.CreateParameter(); p.ParameterName = "@cta"; p.Value = numeroCuenta.Trim();
    cmd.Parameters.Add(p);

    using var rd = cmd.ExecuteReader();
    if (!rd.Read()) return false; // no existe la cuenta → trata como sin fondos

    decimal dmavl2  = rd.IsDBNull(0) ? 0m : rd.GetDecimal(0); // Withdraw funds available today
    decimal dmcbal  = rd.IsDBNull(1) ? 0m : rd.GetDecimal(1); // Current Balance
    decimal dmhold  = rd.IsDBNull(2) ? 0m : rd.GetDecimal(2); // Hold Amount
    decimal dmuav2  = rd.IsDBNull(3) ? 0m : rd.GetDecimal(3); // Unavailable for Withdrawal

    // Validación principal: usa DMAVL2
    if (dmavl2 >= importeDebito) return true;

    // Fallback (por si DMAVL2 no está poblado en algún core): calcula disponibles
    var disponiblesCalculados = dmcbal - dmhold - dmuav2;
    return disponiblesCalculados >= importeDebito;
}


if (nat.Equals("D", StringComparison.OrdinalIgnoreCase))
{
    if (!TieneFondosSuficientes(guardarTransaccionesDto.NumeroCuenta, montoBruto))
        return BuildError("402", "Fondos insuficientes para realizar el débito.");
}
