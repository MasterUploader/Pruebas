/// <summary>
/// Obtiene en UNA SOLA CONSULTA el ADQNUM/ADQENUM y, si corresponde,
/// la cuenta GL (CNTk) y el centro de costo (CCOk) cuyo T-CODE (CTRk) coincide con <paramref name="tcodeGL"/>.
/// - Si <paramref name="esEcommerce"/> = true, consulta ADQECTL (CONTROL='EC').
/// - Si es false, consulta ADQCTL (CONTROL='GL').
/// Devuelve: (AdqNum, GlCuenta, GlCC).
/// </summary>
private (string AdqNum, string? GlCuenta, int GlCC) ObtenerAdqNumYGL(bool esEcommerce, string tcodeGL)
{
    // --- 1) Armar SELECT único con alias estándar (CTRn, CNTn, CCOn, ADQNUM) ---
    var q = esEcommerce
        ? QueryBuilder.Core.QueryBuilder
            .From("ADQECTL", "BCAH96DTA")
            .Select(
                "ADQENUM AS ADQNUM",
                "ADQECTR1 AS CTR1","ADQECNT1 AS CNT1","ADQECCO1 AS CCO1",
                "ADQECTR2 AS CTR2","ADQECNT2 AS CNT2","ADQECCO2 AS CCO2",
                "ADQECTR3 AS CTR3","ADQECNT3 AS CNT3","ADQECCO3 AS CCO3",
                "ADQECTR4 AS CTR4","ADQECNT4 AS CNT4","ADQECCO4 AS CCO4",
                "ADQECTR5 AS CTR5","ADQECNT5 AS CNT5","ADQECCO5 AS CCO5",
                "ADQECTR6 AS CTR6","ADQECNT6 AS CNT6","ADQECCO6 AS CCO6",
                "ADQECTR7 AS CTR7","ADQECNT7 AS CNT7","ADQECCO7 AS CCO7",
                "ADQECTR8 AS CTR8","ADQECNT8 AS CNT8","ADQECCO8 AS CCO8",
                "ADQECTR9 AS CTR9","ADQECNT9 AS CNT9","ADQECCO9 AS CCO9",
                "ADQECTR10 AS CTR10","ADQECNT10 AS CNT10","ADQECC10 AS CCO10",
                "ADQECTR11 AS CTR11","ADQECNT11 AS CNT11","ADQECC11 AS CCO11",
                "ADQECTR12 AS CTR12","ADQECNT12 AS CNT12","ADQECC12 AS CCO12",
                "ADQECTR13 AS CTR13","ADQECNT13 AS CNT13","ADQECC13 AS CCO13",
                "ADQECTR14 AS CTR14","ADQECNT14 AS CNT14","ADQECC14 AS CCO14",
                "ADQECTR15 AS CTR15","ADQECNT15 AS CNT15","ADQECC15 AS CCO15"
            )
            .WhereRaw(
                "ADQECONT = 'EC' AND (" +
                "ADQECTR1 = @T OR ADQECTR2 = @T OR ADQECTR3 = @T OR ADQECTR4 = @T OR ADQECTR5 = @T OR " +
                "ADQECTR6 = @T OR ADQECTR7 = @T OR ADQECTR8 = @T OR ADQECTR9 = @T OR ADQECTR10 = @T OR " +
                "ADQECTR11 = @T OR ADQECTR12 = @T OR ADQECTR13 = @T OR ADQECTR14 = @T OR ADQECTR15 = @T)"
            )
            .FetchNext(1)
            .Build()
        : QueryBuilder.Core.QueryBuilder
            .From("ADQCTL", "BCAH96DTA")
            .Select(
                "ADQNUM AS ADQNUM",
                "ADQCTR1 AS CTR1","ADQCNT1 AS CNT1","ADQCCO1 AS CCO1",
                "ADQCTR2 AS CTR2","ADQCNT2 AS CNT2","ADQCCO2 AS CCO2",
                "ADQCTR3 AS CTR3","ADQCNT3 AS CNT3","ADQCCO3 AS CCO3",
                "ADQCTR4 AS CTR4","ADQCNT4 AS CNT4","ADQCCO4 AS CCO4",
                "ADQCTR5 AS CTR5","ADQCNT5 AS CNT5","ADQCCO5 AS CCO5",
                "ADQCTR6 AS CTR6","ADQCNT6 AS CNT6","ADQCCO6 AS CCO6",
                "ADQCTR7 AS CTR7","ADQCNT7 AS CNT7","ADQCCO7 AS CCO7",
                "ADQCTR8 AS CTR8","ADQCNT8 AS CNT8","ADQCCO8 AS CCO8",
                "ADQCTR9 AS CTR9","ADQCNT9 AS CNT9","ADQCCO9 AS CCO9",
                "ADQCTR10 AS CTR10","ADQCNT10 AS CNT10","ADQCC10 AS CCO10",
                "ADQCTR11 AS CTR11","ADQCNT11 AS CNT11","ADQCC11 AS CCO11",
                "ADQCTR12 AS CTR12","ADQCNT12 AS CNT12","ADQCC12 AS CCO12",
                "ADQCTR13 AS CTR13","ADQCNT13 AS CNT13","ADQCC13 AS CCO13",
                "ADQCTR14 AS CTR14","ADQCNT14 AS CNT14","ADQCC14 AS CCO14",
                "ADQCTR15 AS CTR15","ADQCNT15 AS CNT15","ADQCC15 AS CCO15"
            )
            .WhereRaw(
                "ADQCONT = 'GL' AND (" +
                "ADQCTR1 = @T OR ADQCTR2 = @T OR ADQCTR3 = @T OR ADQCTR4 = @T OR ADQCTR5 = @T OR " +
                "ADQCTR6 = @T OR ADQCTR7 = @T OR ADQCTR8 = @T OR ADQCTR9 = @T OR ADQCTR10 = @T OR " +
                "ADQCTR11 = @T OR ADQCTR12 = @T OR ADQCTR13 = @T OR ADQCTR14 = @T OR ADQCTR15 = @T)"
            )
            .FetchNext(1)
            .Build();

    using var cmd = _connection.GetDbCommand(q, _contextAccessor.HttpContext!);
    // Si tu QueryBuilder no soporta parámetros, inyecta el valor literal una sola vez:
    cmd.CommandText = cmd.CommandText.Replace("@T", $"'{tcodeGL}'");

    using var rd = cmd.ExecuteReader();
    if (!rd.Read())
        return ("0", null, 0);

    // --- 2) ADQNUM común ---
    var adqNum = rd.IsDBNull(rd.GetOrdinal("ADQNUM"))
        ? "0"
        : Convert.ToDecimal(rd.GetValue(rd.GetOrdinal("ADQNUM"))).ToString("0");

    // --- 3) Localizar el índice k (1..15) cuyo CTRk == tcodeGL y devolver CNTk/CCOk ---
    string? gl = null; int cc = 0;
    for (int k = 1; k <= 15; k++)
    {
        var ctr = rd.GetString(rd.GetOrdinal($"CTR{k}")).Trim();
        if (string.Equals(ctr, tcodeGL, StringComparison.OrdinalIgnoreCase))
        {
            // CNTk puede ser DECIMAL en DB2; lo leemos como string "plano"
            var cntOrdinal = rd.GetOrdinal($"CNT{k}");
            gl = rd.IsDBNull(cntOrdinal) ? null : Convert.ToString(rd.GetValue(cntOrdinal))?.Trim();

            var ccoOrdinal = rd.GetOrdinal($"CCO{k}");
            cc = rd.IsDBNull(ccoOrdinal) ? 0 : Convert.ToInt32(rd.GetValue(ccoOrdinal));
            break;
        }
    }

    return (adqNum, gl, cc);
}



// 1) Calcular t-codes
string tcodeCliente = naturalezaCliente == "C" ? "0783" : "0784";
string tcodeGL      = naturalezaCliente == "C" ? "0784" : "0783";

// 2) Traer en UNA consulta ADQNUM y (si aplica) GL/CC de control
var (adqNumCtl, glCtl, ccCtl) = ObtenerAdqNumYGL(esEcommerce, tcodeGL);

// 3) Auto-balance (si existe) o usar lo de control
var auto = TryGetAutoBalance(perfil);

string? glCuenta;
int glCC;
if (auto.enabled)
{
    if (naturalezaCliente.Equals("C", StringComparison.OrdinalIgnoreCase))
        (glCuenta, glCC) = (auto.glDebito,  auto.ccDebito);
    else
        (glCuenta, glCC) = (auto.glCredito, auto.ccCredito);
}
else
{
    glCuenta = glCtl;
    glCC     = ccCtl;
}

// 4) Descripciones EXACTAS (40 chars) con ADQNUM (si no hay, queda "0")
string concepto = "VTA";
string fechaFormateada = DateTime.Now.ToString("yyyyMMdd");
var infoCta = VerCta(numeroCuenta);
string tipoCliente = infoCta.EsAhorro ? "AHO" : infoCta.EsCheques ? "CHE" : "CTE";
string adqNumPadded = (adqNumCtl ?? "0").PadRight(10);
string ochoEspacios = "        ";

string desDb1 = Trunc("Total Neto Db liquidacion come", 40);
string desCr1 = Trunc("Total Neto Cr liquidacion come", 40);
string descDb2 = Trunc($"{codigoComercio}{ochoEspacios}-{fechaFormateada}-{tipoCliente}-{numeroCuenta}", 40);
string descCr2 = Trunc($"{codigoComercio}{ochoEspacios}-{fechaFormateada}", 40);
string descDb3 = Trunc($"&{concepto}&{adqNumPadded}Db Net.Liq1  ||", 40);
string descCr3 = Trunc($"&{concepto}&{adqNumPadded}Cr Net.Liq2  ||", 40);

// 5) Construir y devolver tu IntLotesParamsDto con esta info (sin más consultas).
