using System.Data.Common;
using System.Globalization;
using Connections.Abstractions;
using Microsoft.AspNetCore.Http;
using RestUtilities.QueryBuilder;

namespace MS_BAN_56_ProcesamientoTransaccionesPOS.Features.Posteo;

/// <summary>
/// Postea en POP802 dos líneas totalizadas: Débito (si &gt; 0) y Crédito (si &gt; 0).
/// - Lee fecha operativa (TAP001) para TSTTDT (YYYYMMDD).
/// - Resuelve el perfil (FTTSKY) por cuenta; si no hay, usa comercio (A02COME) padded a 13.
/// - Inserta POP802 con códigos: 0784=Débito, 0783=Crédito (convención del core).
/// </summary>
/// <remarks>
/// Reglas de estilo pedidas:
/// 1) Constructor primario. 2) new() simplificado y listas []. 3) Comentarios XML + en línea sobre **funcionalidad**.
/// </remarks>
public class PosteadorTotalesService(
    IDatabaseConnection _db,
    IHttpContextAccessor _http)
{
    /// <summary>
    /// Postea totales (débito/crédito) del comercio/cliente en POP802 usando el lote recibido.
    /// </summary>
    /// <param name="dto">DTO con totales y metadatos del comercio.</param>
    /// <returns>Resumen con claves operativas posteadas.</returns>
    public PosteoTotalesResult Postear(GuardarTransaccionesDto dto)
    {
        // Normalización de importes: tolera "." o "," y espacios
        var deb = ParseMonto(dto.MontoDebitado);
        var cre = ParseMonto(dto.MontoAcreditado);
        if (deb <= 0m && cre <= 0m)
            throw new ArgumentException("No hay importes a postear (ambos montos son 0).");

        // 1) Resolver perfil (FTTSKY) por cuenta; fallback: comercio padded a 13
        var perfil = ResolverPerfil(dto.NumeroCuenta, dto.CodigoComercio);
        if (string.IsNullOrWhiteSpace(perfil))
            throw new InvalidOperationException("No fue posible resolver el perfil (FTTSKY) por cuenta ni por comercio.");

        // 2) Lote y fecha efectiva
        var lote = ParseEntero(dto.NumeroDeCorte); // TSBTCH (001-999)
        if (lote is < 1 or > 999) throw new ArgumentException("NumeroDeCorte inválido. Debe estar entre 001 y 999.");
        var fechaCyyMmDd = LeerFechaOperativa();   // TAP001.DSCDT (CYYMMDD)
        var fechaYyyyMmDd = CyyMmDdToYyyyMmDd(fechaCyyMmDd);

        // 3) Inserción POP802 (una o dos filas) en transacción única
        _db.Open();
        using var cmd = _db.GetDbCommand(_http.HttpContext!);
        using var tx = cmd.Connection!.BeginTransaction();
        cmd.Transaction = tx;

        try
        {
            var secuencia = 0;

            // Crédito totalizado (si aplica)
            if (cre > 0m)
            {
                secuencia += 1;
                InsertPop802(
                    cmd: cmd,
                    bank: 1,
                    perfil: perfil,
                    lote: lote,
                    seq: secuencia,
                    fechaYyyyMmDd: fechaYyyyMmDd,
                    cuenta: dto.NumeroCuenta,      // TSTACT: cuenta objetivo (cliente/comercio)
                    centroCosto: 0,                // TSWSCC: si requieres C.C., cámbialo aquí
                    codTrn: "0783",                // 0783 = Crédito (convención del core)
                    monto: cre,
                    al1: dto.NombreComercio,       // leyenda 1
                    al2: $"{dto.CodigoComercio}-{dto.Terminal}", // leyenda 2
                    al3: $"&{EtiquetaConcepto(dto.NaturalezaContable)}&{dto.IdTransaccionUnico}&Cr Tot." // leyenda 3
                );
            }

            // Débito totalizado (si aplica)
            if (deb > 0m)
            {
                secuencia += 1;
                InsertPop802(
                    cmd: cmd,
                    bank: 1,
                    perfil: perfil,
                    lote: lote,
                    seq: secuencia,
                    fechaYyyyMmDd: fechaYyyyMmDd,
                    cuenta: dto.NumeroCuenta,
                    centroCosto: 0,
                    codTrn: "0784",                // 0784 = Débito (convención del core)
                    monto: deb,
                    al1: dto.NombreComercio,
                    al2: $"{dto.CodigoComercio}-{dto.Terminal}",
                    al3: $"&{EtiquetaConcepto(dto.NaturalezaContable)}&{dto.IdTransaccionUnico}&Db Tot."
                );
            }

            tx.Commit();

            return new()
            {
                Lote = lote,
                Perfil = perfil,
                FechaEfectivaYyyyMmDd = fechaYyyyMmDd,
                SecuenciasGeneradas = secuencia
            };
        }
        catch
        {
            tx.Rollback();
            throw;
        }
        finally
        {
            _db.Close();
        }
    }

    // ============================ Datos base ============================

    /// <summary>
    /// Lee DSCDT (CYYMMDD) de TAP001 (DSBK=1).
    /// </summary>
    private int LeerFechaOperativa()
    {
        var q = QueryBuilder.Core.QueryBuilder
            .From("TAP001", "BNKPRD01")
            .Select<TAP001>(x => x.DSCDT)
            .Where<TAP001>(x => x.DSBK == 1)
            .FetchNext(1)
            .Build();

        using var cmd = _db.GetDbCommand(_http.HttpContext!);
        cmd.CommandText = q.Sql;

        using var rd = cmd.ExecuteReader();
        if (!rd.Read()) throw new InvalidOperationException("No se pudo obtener fecha operativa (TAP001).");
        return rd.GetInt32(0); // CYYMMDD
    }

    /// <summary>
    /// Inserta una fila en POP802 (detalle de posteo) con campos esenciales.
    /// </summary>
    private void InsertPop802(
        DbCommand cmd,
        int bank,
        string perfil,
        int lote,
        int seq,
        int fechaYyyyMmDd,
        string cuenta,
        int centroCosto,
        string codTrn,
        decimal monto,
        string al1,
        string al2,
        string al3)
    {
        // Nota funcional: POP802 requiere varias columnas obligatorias del core.
        // Aquí posteamos lo esencial (override, fecha, cuenta, tcode, monto y leyendas).
        var ins = new InsertQueryBuilder("POP802", "BNKPRD01")
            .IntoColumns([
                "TSBK",    // Bank
                "TSTSKY",  // Perfil
                "TSBTCH",  // Lote
                "TSWSEQ",  // Secuencia
                "TSTOVR",  // Override
                "TSTTDT",  // Fecha efectiva (YYYYMMDD)
                "TSTACT",  // Cuenta
                "TSWSCC",  // Centro de costo
                "TSWTCD",  // Código de transacción
                "TSTCC",   // Monto
                "TSTAL1",  // Leyenda 1
                "TSTAL2",  // Leyenda 2
                "TSTAL3"   // Leyenda 3
            ])
            .Row([
                bank,
                perfil,
                lote,
                seq,
                "S",
                fechaYyyyMmDd,
                cuenta,
                centroCosto,
                codTrn,
                monto,
                Trunc(al1, 30),
                Trunc(al2, 30),
                Trunc(al3, 30)
            ])
            .Build();

        cmd.Parameters.Clear();
        cmd.CommandText = ins.Sql;
        var aff = cmd.ExecuteNonQuery();
        if (aff <= 0) throw new InvalidOperationException("No se pudo insertar el detalle POP802.");
    }

    // ============================ Resolución de perfil ============================

    /// <summary>
    /// Intenta resolver FTTSKY por cuenta; si no hay, usa comercio padded (13).
    /// </summary>
    private string ResolverPerfil(string numeroCuenta, string codigoComercio)
    {
        // 1) CFP102: cuenta ↔ perfil (si existe)
        var per = BuscarPerfilEnCfp102(numeroCuenta);
        if (!string.IsNullOrWhiteSpace(per)) return per;

        // 2) Convención: perfil = comercio (NUM) padded a 13
        var cand = PadLeftDigits(codigoComercio, 13);
        return ExistePerfil(cand) ? cand : string.Empty;
    }

    private string BuscarPerfilEnCfp102(string cuenta)
    {
        var cuenta12 = NormalizarCuenta12(cuenta);

        var q = QueryBuilder.Core.QueryBuilder
            .From("CFP102", "BNKPRD01")
            .Select<CFP102_Min>(x => x.CFTSKY)
            .Where<CFP102_Min>(x => x.CFTSBK == 1)
            .Where<CFP102_Min>(x => x.CUX1AC == cuenta12)
            .FetchNext(1)
            .Build();

        using var cmd = _db.GetDbCommand(_http.HttpContext!);
        cmd.CommandText = q.Sql;

        using var rd = cmd.ExecuteReader();
        return rd.Read() && !rd.IsDBNull(0) ? rd.GetString(0).Trim() : string.Empty;
    }

    private bool ExistePerfil(string perfil)
    {
        var q = QueryBuilder.Core.QueryBuilder
            .From("CFP801", "BNKPRD01")
            .Select<CFP801_Min>(x => x.CFTSKY)
            .Where<CFP801_Min>(x => x.CFTSBK == 1)
            .Where<CFP801_Min>(x => x.CFTSKY == perfil)
            .FetchNext(1)
            .Build();

        using var cmd = _db.GetDbCommand(_http.HttpContext!);
        cmd.CommandText = q.Sql;

        using var rd = cmd.ExecuteReader();
        return rd.Read();
    }

    // ============================ Utilidades ============================

    /// <summary>Parsea monto desde string con formatos comunes.</summary>
    private static decimal ParseMonto(string s)
    {
        s = (s ?? "").Trim().Replace(" ", "");
        if (decimal.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var v)) return v;
        if (decimal.TryParse(s, NumberStyles.Any, new CultureInfo("es-HN"), out v)) return v;
        if (decimal.TryParse(s, NumberStyles.Any, new CultureInfo("es-ES"), out v)) return v;
        return 0m;
    }

    /// <summary>Convierte CYYMMDD (p.ej., 1250916) a YYYYMMDD (20250916).</summary>
    private static int CyyMmDdToYyyyMmDd(int cyyMmDd)
    {
        var s = cyyMmDd.ToString().PadLeft(7, '0');
        var c = int.Parse(s[..1], CultureInfo.InvariantCulture);
        var yy = int.Parse(s.Substring(1, 2), CultureInfo.InvariantCulture);
        var mm = int.Parse(s.Substring(3, 2), CultureInfo.InvariantCulture);
        var dd = int.Parse(s.Substring(5, 2), CultureInfo.InvariantCulture);
        var year = (c == 1 ? 1900 : 2000) + yy;
        return int.Parse($"{year:D4}{mm:D2}{dd:D2}", CultureInfo.InvariantCulture);
    }

    /// <summary>Padded a 12 dígitos para CUX1AC (CFP102).</summary>
    private static string NormalizarCuenta12(string? cuenta)
    {
        var digits = new string((cuenta ?? "").Where(char.IsDigit).ToArray());
        if (digits.Length > 12) digits = digits[^12..];
        return digits.PadLeft(12, '0');
    }

    /// <summary>Pad numérico a longitud fija (p.ej., perfil=13).</summary>
    private static string PadLeftDigits(string? s, int len)
    {
        var digits = new string((s ?? "").Where(char.IsDigit).ToArray());
        if (digits.Length > len) digits = digits[^len..];
        return digits.PadLeft(len, '0');
    }

    /// <summary>Recorta leyendas de POP802 a longitud permitida.</summary>
    private static string Trunc(string? s, int max) => string.IsNullOrEmpty(s) ? string.Empty : (s.Length <= max ? s : s[..max]);

    /// <summary>Convierte "C"/"D" a etiqueta corta funcional.</summary>
    private static string EtiquetaConcepto(string nat) => (nat ?? "C").ToUpperInvariant() == "D" ? "DB" : "CR";
}

// ======================= DTOs mínimos de soporte QueryBuilder =======================

/// <summary>TAP001 (solo campos usados).</summary>
public class TAP001
{
    public int DSBK { get; set; }
    public int DSCDT { get; set; } // CYYMMDD
}

/// <summary>CFP102 mínimo para cuenta↔perfil.</summary>
public class CFP102_Min
{
    public int CFTSBK { get; set; }
    public string CFTSKY { get; set; } = string.Empty;
    public string CUX1AC { get; set; } = string.Empty;
}

/// <summary>CFP801 mínimo para validar existencia de perfil.</summary>
public class CFP801_Min
{
    public int CFTSBK { get; set; }
    public string CFTSKY { get; set; } = string.Empty;
}

/// <summary>
/// Resultado resumido del posteo totalizado.
/// </summary>
public class PosteoTotalesResult
{
    /// <summary>Número de lote usado (TSBTCH).</summary>
    public int Lote { get; set; }

    /// <summary>Perfil (FTTSKY) resuelto.</summary>
    public string Perfil { get; set; } = string.Empty;

    /// <summary>Fecha efectiva en YYYYMMDD.</summary>
    public int FechaEfectivaYyyyMmDd { get; set; }

    /// <summary>Cantidad de filas POP802 insertadas (1..2).</summary>
    public int SecuenciasGeneradas { get; set; }
}
