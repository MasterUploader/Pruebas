using System.Data.Common;
using System.Globalization;
using Connections.Abstractions;
using Microsoft.AspNetCore.Http;
using RestUtilities.QueryBuilder;

namespace Adquirencia.Api.Empresas.Services;

/// <summary>
/// Orquesta en una sola transacción: lectura de fecha operativa (TAP001),
/// generación de lote (POP801) y posteo contable D/C (GLC002).
/// </summary>
/// <remarks>
/// - Usa un único <see cref="DbCommand"/> y una única transacción para garantizar atomicidad.
/// - Emplea QueryBuilder tipado (Select/Where genéricos) e INSERT parametrizado (placeholders '?').
/// - Devuelve el número de lote generado y el resumen del posteo.
/// </remarks>
public class LoteEmpresasOrchestratorTx(IDatabaseConnection _as400, IHttpContextAccessor _ctx)
{
    /// <summary>
    /// Ejecuta el flujo completo de empresas en una sola transacción.
    /// </summary>
    /// <param name="req">Payload de entrada (empresas).</param>
    /// <param name="usuarioOrigen">Usuario trazador que quedará en los asientos.</param>
    /// <returns>número de lote generado y respuesta resumida del posteo.</returns>
    public (int numeroLote, PosteoEmpresaResponse posteo) Procesar(PosteoEmpresaRequest req, string usuarioOrigen)
    {
        // Normalización y validaciones funcionales previas a la transacción.
        var nat = (req.NaturalezaContable ?? "C").Trim().ToUpperInvariant(); // 'C' o 'D'
        if (nat != "C" && nat != "D")
            throw new ArgumentException("NaturalezaContable inválida. Use 'C' para crédito o 'D' para débito.");

        var deb = ParsearMonto(req.MontoDebitado);
        var cre = ParsearMonto(req.MontoAcreditado);
        var montoPrimario = nat == "D" ? deb : cre;
        if (montoPrimario <= 0m)
            throw new ArgumentException(nat == "D" ? "MontoDebitado debe ser > 0." : "MontoAcreditado debe ser > 0.");

        // Perfil de lote (FTTSKY). Ajusta si tu perfil difiere del código de comercio.
        var perfilKey = string.IsNullOrWhiteSpace(req.CodigoComercio) ? "EMPRESA" : req.CodigoComercio;

        _as400.Open();
        using var cmd = _as400.GetDbCommand(_ctx.HttpContext!);
        using var tx  = cmd.Connection!.BeginTransaction(); // Transacción única para todo el flujo
        cmd.Transaction = tx;

        try
        {
            // 1) Fecha operativa (TAP001 → DSCDT CYYMMDD). Se consulta en esta misma conexión/transacción.
            var fechaCyyMmDd = LeerFechaOperativa(cmd);

            // 2) Leer último número de lote para el perfil y crear el siguiente en POP801, en la misma transacción.
            var ultimoLote = LeerUltimoLote(cmd, perfilKey);
            var nuevoLote  = InsertarNuevoLote(cmd, perfilKey, usuarioOrigen, fechaCyyMmDd, ultimoLote);

            // 3) Generar asientos contables D/C en GLC002 amarrados al nuevo lote (NumeroDeCorte = nuevoLote).
            InsertarAsientosDc(cmd,
                cuentaPrimaria: req.NumeroCuenta,
                cuentaContra:   req.NumeroCuenta, // ← si tienes contracuenta por comercio, cámbiala aquí
                monto:          montoPrimario,
                naturaleza:     nat,              // 'C' / 'D'
                descripcion:    req.Descripción,
                referencia:     req.IdTransaccionUnico,
                comercio:       req.CodigoComercio,
                terminal:       req.Terminal,
                numeroCorte:    nuevoLote.ToString(),
                usuario:        usuarioOrigen,
                fechaCyyMmDd:   fechaCyyMmDd);

            // Todo OK → commit
            tx.Commit();

            // Respuesta consolidada
            return (nuevoLote, new()
            {
                IdTransaccionUnico = req.IdTransaccionUnico,
                Debitado   = nat == "D" ? montoPrimario : 0m,
                Acreditado = nat == "C" ? montoPrimario : 0m,
                NumeroDeCorte = nuevoLote.ToString(),
                Mensaje = "Lote generado y asientos D/C confirmados"
            });
        }
        catch
        {
            tx.Rollback(); // Reversa total si algo falla en cualquiera de los pasos
            throw;
        }
        finally
        {
            _as400.Close();
        }
    }

    // ===================== PASO 1: FECHA OPERATIVA (TAP001) =====================

    /// <summary>
    /// Lee DSCDT (CYYMMDD) desde BNKPRD01.TAP001 con DSBK = 1 usando QueryBuilder tipado.
    /// </summary>
    private static int LeerFechaOperativa(DbCommand cmd)
    {
        var q = QueryBuilder.Core.QueryBuilder
            .From("TAP001", "BNKPRD01")
            .Select<TAP001>(x => x.DSCDT)
            .Where<TAP001>(x => x.DSBK == 1)
            .FetchNext(1)
            .Build();

        cmd.Parameters.Clear();
        cmd.CommandText = q.Sql;

        using var rd = cmd.ExecuteReader();
        if (!rd.Read())
            throw new InvalidOperationException("No se pudo obtener fecha operativa (TAP001).");

        return rd.GetInt32(0); // DSCDT CYYMMDD
    }

    // ===================== PASO 2: LOTE (POP801) =====================

    /// <summary>
    /// Obtiene el último FTSBT para FTTSKY = perfil, ordenando desc y tomando la primera fila.
    /// </summary>
    private static int LeerUltimoLote(DbCommand cmd, string perfilKey)
    {
        var q = QueryBuilder.Core.QueryBuilder
            .From("POP801", "BNKPRD01")
            .Select<POP801>(x => x.FTSBT)
            .Where<POP801>(x => x.FTTSBK == 1)              // 001 numérico
            .Where<POP801>(x => x.FTTSKY == perfilKey)
            .OrderBy("FTSBT DESC")
            .FetchNext(1)
            .Build();

        cmd.Parameters.Clear();
        cmd.CommandText = q.Sql;

        using var rd = cmd.ExecuteReader();
        return rd.Read() ? rd.GetInt32(0) : 0;
    }

    /// <summary>
    /// Inserta nuevo registro en POP801 con FTSBT = ultimo + 1 y estado inicial 02.
    /// </summary>
    private static int InsertarNuevoLote(DbCommand cmd, string perfilKey, string usuario, int fechaCyyMmDd, int ultimo)
    {
        var siguiente = ultimo + 1;

        var ins = new InsertQueryBuilder("POP801", "BNKPRD01")
            .IntoColumns(["FTTSBK","FTTSKY","FTTSDT","FTSBT","FTTSOR","FTSST"])
            .Row([1, perfilKey, fechaCyyMmDd, siguiente, usuario, 2]) // 2 = estado inicial del lote
            .Build();

        cmd.Parameters.Clear();
        cmd.CommandText = ins.Sql;
        var aff = cmd.ExecuteNonQuery();
        if (aff <= 0) throw new InvalidOperationException("No se pudo crear POP801 (NuevoLote).");

        return siguiente;
    }

    // ===================== PASO 3: ASIENTOS (GLC002) =====================

    /// <summary>
    /// Inserta el asiento primario (naturaleza solicitada) y su contrapartida (naturaleza opuesta).
    /// </summary>
    private static void InsertarAsientosDc(
        DbCommand cmd,
        string cuentaPrimaria,
        string cuentaContra,
        decimal monto,
        string naturaleza,      // 'C' o 'D'
        string descripcion,
        string referencia,
        string comercio,
        string terminal,
        string numeroCorte,
        string usuario,
        int    fechaCyyMmDd)
    {
        // Asiento primario
        var ins1 = new InsertQueryBuilder("GLC002", "BNKPRD01")
            .IntoColumns(["GLCCTA","GLCMON","GLCNAT","GLCDESC","GLCREF","GLCCOM","GLCTER","GLCCORT","GLCUSER","GLCFECC"])
            .Row([ cuentaPrimaria, monto, naturaleza,
                   Trunc(descripcion, 60), referencia,
                   comercio, terminal, numeroCorte,
                   usuario, fechaCyyMmDd ])
            .Build();

        cmd.Parameters.Clear();
        cmd.CommandText = ins1.Sql;
        var a1 = cmd.ExecuteNonQuery();
        if (a1 <= 0) throw new InvalidOperationException("No se confirmó el asiento primario (GLC002).");

        // Contrapartida (naturaleza opuesta)
        var natOpp = naturaleza == "D" ? "C" : "D";

        var ins2 = new InsertQueryBuilder("GLC002", "BNKPRD01")
            .IntoColumns(["GLCCTA","GLCMON","GLCNAT","GLCDESC","GLCREF","GLCCOM","GLCTER","GLCCORT","GLCUSER","GLCFECC"])
            .Row([ cuentaContra, monto, natOpp,
                   Trunc("Contra " + descripcion, 60), referencia,
                   comercio, terminal, numeroCorte,
                   usuario, fechaCyyMmDd ])
            .Build();

        cmd.Parameters.Clear();
        cmd.CommandText = ins2.Sql;
        var a2 = cmd.ExecuteNonQuery();
        if (a2 <= 0) throw new InvalidOperationException("No se confirmó el asiento contrapartida (GLC002).");
    }

    // ===================== UTILIDADES =====================

    /// <summary>Normaliza montos desde string tolerando formatos comunes.</summary>
    private static decimal ParsearMonto(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return 0m;
        s = s.Trim().Replace(" ", "");
        if (decimal.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var v)) return v;
        if (decimal.TryParse(s, NumberStyles.Any, new CultureInfo("es-HN"), out v)) return v;
        if (decimal.TryParse(s, NumberStyles.Any, new CultureInfo("es-ES"), out v)) return v;
        return 0m;
    }

    /// <summary>Acota texto a la longitud máxima esperada por el PF.</summary>
    private static string Trunc(string? s, int max) => string.IsNullOrEmpty(s) ? string.Empty : (s.Length <= max ? s : s[..max]);
}

// ===================== DTOs TIPADOS PARA QUERYBUILDER =====================

/// <summary>PF BNKPRD01.TAP001 (fecha operativa).</summary>
public class TAP001
{
    /// <summary>Clave de banco.</summary>
    public int DSBK { get; set; }
    /// <summary>Fecha operativa en formato CYYMMDD.</summary>
    public int DSCDT { get; set; }
}

/// <summary>PF BNKPRD01.POP801 (lotes).</summary>
public class POP801
{
    public int    FTTSBK { get; set; }   // 001 (numérico)
    public string FTTSKY { get; set; } = string.Empty; // perfil
    public int    FTTSDT { get; set; }   // CYYMMDD
    public int    FTSBT  { get; set; }   // número de lote
    public string FTTSOR { get; set; } = string.Empty; // usuario
    public int    FTSST  { get; set; }   // estado
}

/// <summary>PF BNKPRD01.GLC002 (libro contable) - columnas usadas en inserción.</summary>
public class GLC002
{
    public string GLCCTA { get; set; } = string.Empty;
    public decimal GLCMON { get; set; }
    public string GLCNAT { get; set; } = string.Empty; // 'C'/'D'
    public string GLCDESC { get; set; } = string.Empty;
    public string GLCREF { get; set; } = string.Empty;
    public string GLCCOM { get; set; } = string.Empty;
    public string GLCTER { get; set; } = string.Empty;
    public string GLCCORT { get; set; } = string.Empty;
    public string GLCUSER { get; set; } = string.Empty;
    public int    GLCFECC { get; set; }              // CYYMMDD
}

// ===================== DTOs de request/response del endpoint =====================

/// <summary>Payload del endpoint de empresas.</summary>
public class PosteoEmpresaRequest
{
    public string NumeroCuenta { get; set; } = string.Empty;
    public string MontoDebitado { get; set; } = string.Empty;
    public string MontoAcreditado { get; set; } = string.Empty;
    public string CodigoComercio { get; set; } = string.Empty;
    public string NombreComercio { get; set; } = string.Empty;
    public string Terminal { get; set; } = string.Empty;
    public string Descripción { get; set; } = string.Empty;
    public string NaturalezaContable { get; set; } = "C"; // 'C' crédito, 'D' débito
    public string NumeroDeCorte { get; set; } = string.Empty; // ignorado: se usa lote generado
    public string IdTransaccionUnico { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public string DescripcionEstado { get; set; } = string.Empty;
}

/// <summary>Respuesta estandar del posteo con lote.</summary>
public class PosteoEmpresaResponse
{
    public string IdTransaccionUnico { get; set; } = string.Empty;
    public decimal Debitado { get; set; }
    public decimal Acreditado { get; set; }
    public string NumeroDeCorte { get; set; } = string.Empty; // lote generado
    public string Mensaje { get; set; } = "OK";
}
