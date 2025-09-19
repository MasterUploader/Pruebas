namespace MS_BAN_56_ProcesamientoTransaccionesPOS.Models.AS400.BNKPRD01
{
    /// <summary>Representa la cuenta de depósitos en TAP002 (encabezado de cuenta).</summary>
    public class Tap002Dto
    {
        /// <summary>Bank Nbr (DM BK). Identificador del banco.</summary>
        public int DMBK { get; set; }

        /// <summary>Account Nbr (DM ACCT). Número de cuenta en el core.</summary>
        public string DMACCT { get; set; } = string.Empty;

        /// <summary>Account Type (DM TYPE / DM TYP). Código de tipo de cuenta definido por el core.</summary>
        public int DMTYPE { get; set; }

        /// <summary>Additional Account Title (DM TITL). Título/alias de la cuenta.</summary>
        public string DMTITL { get; set; } = string.Empty;

        /// <summary>Current Balance (DM CBAL). Saldo contable actual.</summary>
        public decimal DMCBAL { get; set; }

        /// <summary>Balance Yesterday (DM YBAL). Saldo del día anterior.</summary>
        public decimal DMYBAL { get; set; }
    }
}



/// <summary>
/// Lee TAP002 por número de cuenta y determina el tipo (ahorro/cheques) según el código core.
/// </summary>
/// <param name="numeroCuenta">Cuenta core (DMACCT) tal como viene en la petición.</param>
/// <returns>(found, esAhorro, tipoCodigo, tituloCuenta)</returns>
private (bool found, bool esAhorro, int tipoCodigo, string titulo) VerCta(string numeroCuenta)
{
    // Traemos lo mínimo: tipo, título (opcional para leyendas)
    var q = new SelectQueryBuilder("TAP002", "BNKPRD01")
        .Select("DMTYPE", "DMACCT", "DMTITL")
        .WhereRaw("DMBK = 1")                 // si tu banco fijo es 001
        .And("DMACCT", Operador.Igual, numeroCuenta)
        .FetchNext(1)
        .Build();

    using var cmd = _connection.GetDbCommand(q, _contextAccessor.HttpContext!);
    using var rd  = cmd.ExecuteReader();
    if (!rd.Read()) return (false, false, 0, string.Empty);

    var tipo = Convert.ToInt32(rd["DMTYPE"]);
    var tit  = rd["DMTITL"] is DBNull ? "" : rd["DMTITL"]!.ToString()!.Trim();

    // ===== MAPEOS =====
    // En muchos cores, los rangos suelen separar Cheques vs Ahorros.
    // Déjalo configurable y con un default razonable.
    // Default: 1xx = Cheques, 2xx = Ahorros (ajústalo si tu RPG usa otros rangos).
    var esAhorro = GlobalConnection.Current?.MapeoTiposDeposito?.EsAhorro(tipo)
                   ?? (tipo >= 200 && tipo < 300);

    return (true, esAhorro, tipo, tit);
}



/// <summary>Etiqueta corta para la leyenda según naturaleza.</summary>
private static string EtiquetaConcepto(string nat)
    => (nat ?? "C").Equals("D", StringComparison.InvariantCultureIgnoreCase) ? "DB" : "CR";

/// <summary>Etiqueta de cuenta para AL3: Ahorros/Cheques (o genérico si no se conoce).</summary>
private static string EtiquetaCuenta(bool esAhorro)
    => esAhorro ? "Aho" : "Cta.Che";

/// <summary>
/// Construye AL1, AL2, AL3 tal cual haces hoy, pero cambiando el sufijo según tipo de cuenta.
/// </summary>
private static (string al1, string al2, string al3) ConstruirLeyendas(
    string naturaleza, string nombreComercio, string codComercio, string terminal, string idUnico, bool esAhorro)
{
    var al1 = nombreComercio;
    var al2 = $"{codComercio}-{terminal}";
    var al3 = $"&{EtiquetaConcepto(naturaleza)}&{idUnico}&{(naturaleza.Equals("D", StringComparison.OrdinalIgnoreCase) ? "Db" : "Cr")} {EtiquetaCuenta(esAhorro)}.";

    // Respeta tus límites de 30 chars en InsertPop802
    return (Trunc(al1, 30), Trunc(al2, 30), Trunc(al3, 30));
}


// Determinar tipo de cuenta (para afectar la descripción AL3)
var (ctaFound, esAho, tipoCta, tituloCta) = VerCta(guardarTransaccionesDto.NumeroCuenta);
// Si no existe la cuenta, puedes decidir si abortas o sigues con etiqueta genérica
if (!ctaFound) esAho = false; // seguirá “Cta.Che” como genérico; cambia si prefieres otro default.

// Cuando armes las leyendas para POP802:
var (al1, al2, al3) = ConstruirLeyendas(
    naturaleza: nat,
    nombreComercio: guardarTransaccionesDto.NombreComercio,
    codComercio: guardarTransaccionesDto.CodigoComercio,
    terminal: guardarTransaccionesDto.Terminal,
    idUnico: guardarTransaccionesDto.IdTransaccionUnico,
    esAhorro: esAho
);

// …y pasa al1/al2/al3 a tu InsertPop802 o dentro de tu PostearDesglose.




