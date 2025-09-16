using System.Data.Common;
using System.Globalization;
using Connections.Abstractions;
using Microsoft.AspNetCore.Http;
using RestUtilities.QueryBuilder;

namespace Adquirencia.Consultas;

/// <summary>
/// Resuelve el perfil (FTTSKY) a partir de una cuenta de depósito, consultando ADQ02COM
/// y validando contra CFP801/CFP102. Implementa estrategias en cascada para ambientes
/// donde el perfil puede estar ligado al comercio o a la cuenta.
/// </summary>
/// <remarks>
/// Estrategia:
/// 1) ADQ02COM.A02CTDE = cuenta → obtener A02COME (comercio)
/// 2) Candidato A: perfil = comercio (A02COME) padded a 13; validar existencia en CFP801
/// 3) Candidato B: si existe CFP102, buscar por CUX1AC = cuenta para traer CFTSKY
/// 4) (Opcional) Confirmar candidato en POP801 (existe algún lote con FTTSKY=candidato)
/// </remarks>
public class PerfilPorCuentaService(IDatabaseConnection _cn, IHttpContextAccessor _ctx)
{
    /// <summary>
    /// Devuelve el primer perfil encontrado para una cuenta de depósito.
    /// </summary>
    /// <param name="numeroCuenta">Cuenta en texto tal como se guarda en ADQ02COM.A02CTDE (CHAR 20).</param>
    /// <returns>Perfil (FTTSKY) o string.Empty si no se pudo resolver.</returns>
    public string ObtenerPerfilPorCuenta(string numeroCuenta)
    {
        // 1) Buscar comercio(s) por cuenta en ADQ02COM
        var comercios = BuscarComerciosPorCuenta(numeroCuenta);
        foreach (var com in comercios)
        {
            // 2) Candidato por convención: perfil = comercio padded a 13
            var perfilCand = PadLeftDigits(com.ToString(CultureInfo.InvariantCulture), 13);
            if (ExistePerfil(perfilCand))
                return perfilCand;

            // 3) Candidato por CFP102 (si tu mapeo cuenta↔perfil existe)
            var perfilPorCta = BuscarPerfilEnCfp102(numeroCuenta);
            if (!string.IsNullOrWhiteSpace(perfilPorCta))
                return perfilPorCta;

            // 4) Confirmación opcional en POP801: si hay lotes con ese perfil candidato, lo aceptamos
            if (ExisteLoteConPerfil(perfilCand))
                return perfilCand;
        }

        // Si no hubo match, intentamos directo por CFP102 (por si no hubo match en ADQ02COM)
        var perfilSoloCuenta = BuscarPerfilEnCfp102(numeroCuenta);
        return perfilSoloCuenta;
    }

    /// <summary>
    /// Devuelve todos los perfiles posibles para una cuenta (útil para auditoría).
    /// </summary>
    public List<string> ObtenerPerfilesPosibles(string numeroCuenta)
    {
        var perfiles = new List<string>();

        // Comercios asociados a la cuenta
        var comercios = BuscarComerciosPorCuenta(numeroCuenta);
        foreach (var com in comercios)
        {
            var cand = PadLeftDigits(com.ToString(CultureInfo.InvariantCulture), 13);
            if (ExistePerfil(cand) && !perfiles.Contains(cand))
                perfiles.Add(cand);

            var cfp102 = BuscarPerfilEnCfp102(numeroCuenta);
            if (!string.IsNullOrWhiteSpace(cfp102) && !perfiles.Contains(cfp102))
                perfiles.Add(cfp102);

            if (ExisteLoteConPerfil(cand) && !perfiles.Contains(cand))
                perfiles.Add(cand);
        }

        // Si no hubo comercios (o no devolvieron match), probar CFP102 directo
        if (perfiles.Count == 0)
        {
            var cfp102 = BuscarPerfilEnCfp102(numeroCuenta);
            if (!string.IsNullOrWhiteSpace(cfp102))
                perfiles.Add(cfp102);
        }

        return perfiles;
    }

    // ========================= CONSULTAS A DATOS (QueryBuilder) =========================

    /// <summary>
    /// En ADQ02COM, busca todos los comercios (A02COME) que tienen la cuenta A02CTDE indicada.
    /// </summary>
    private List<long> BuscarComerciosPorCuenta(string cuenta)
    {
        var res = new List<long>();

        var q = QueryBuilder.Core.QueryBuilder
            .From("ADQ02COM", "BCAH96DTA")
            .Select<ADQ02COM>(x => x.A02COME)
            .Where<ADQ02COM>(x => x.A02CTDE == cuenta) // match exacto sobre CHAR(20)
            .Build();

        _cn.Open();
        try
        {
            using var cmd = _cn.GetDbCommand(_ctx.HttpContext!);
            cmd.CommandText = q.Sql;

            using var rd = cmd.ExecuteReader();
            while (rd.Read())
            {
                if (!rd.IsDBNull(0))
                    res.Add(rd.GetInt64(0));
            }
        }
        finally
        {
            _cn.Close();
        }

        return res;
    }

    /// <summary>
    /// Verifica existencia de un perfil en CFP801 (CFTSBK=1, CFTSKY = perfil).
    /// </summary>
    private bool ExistePerfil(string perfil)
    {
        var q = QueryBuilder.Core.QueryBuilder
            .From("CFP801", "BNKPRD01")
            .Select<CFP801>(x => x.CFTSKY)
            .Where<CFP801>(x => x.CFTSBK == 1)
            .Where<CFP801>(x => x.CFTSKY == perfil)
            .FetchNext(1)
            .Build();

        _cn.Open();
        try
        {
            using var cmd = _cn.GetDbCommand(_ctx.HttpContext!);
            cmd.CommandText = q.Sql;
            using var rd = cmd.ExecuteReader();
            return rd.Read();
        }
        finally
        {
            _cn.Close();
        }
    }

    /// <summary>
    /// Busca un perfil por cuenta en CFP102 si existe el mapeo cuenta↔perfil (CUX1AC).
    /// </summary>
    private string BuscarPerfilEnCfp102(string cuenta)
    {
        // Si en tu site CUX1AC se almacena con padding/solo dígitos, ajusta aquí:
        var cuentaNorm = NormalizarCuenta12(cuenta);

        var q = QueryBuilder.Core.QueryBuilder
            .From("CFP102", "BNKPRD01")
            .Select<CFP102>(x => x.CFTSKY)
            .Where<CFP102>(x => x.CFTSBK == 1)
            .Where<CFP102>(x => x.CUX1AC == cuentaNorm)
            .FetchNext(1)
            .Build();

        _cn.Open();
        try
        {
            using var cmd = _cn.GetDbCommand(_ctx.HttpContext!);
            cmd.CommandText = q.Sql;
            using var rd = cmd.ExecuteReader();
            return rd.Read() && !rd.IsDBNull(0) ? rd.GetString(0).Trim() : string.Empty;
        }
        finally
        {
            _cn.Close();
        }
    }

    /// <summary>
    /// Comprueba si existen lotes en POP801 para un perfil dado (sanidad de dato).
    /// </summary>
    private bool ExisteLoteConPerfil(string perfil)
    {
        var q = QueryBuilder.Core.QueryBuilder
            .From("POP801", "BNKPRD01")
            .Select<POP801>(x => x.FTSBT)
            .Where<POP801>(x => x.FTTSBK == 1)
            .Where<POP801>(x => x.FTTSKY == perfil)
            .FetchNext(1)
            .Build();

        _cn.Open();
        try
        {
            using var cmd = _cn.GetDbCommand(_ctx.HttpContext!);
            cmd.CommandText = q.Sql;
            using var rd = cmd.ExecuteReader();
            return rd.Read();
        }
        finally
        {
            _cn.Close();
        }
    }

    // ========================= UTILIDADES DE FORMATO =========================

    /// <summary>
    /// Normaliza una cadena numérica a 12 dígitos (CUX1AC, típico en CFP102).
    /// </summary>
    private static string NormalizarCuenta12(string? cuenta)
    {
        cuenta = (cuenta ?? string.Empty).Trim();
        var digits = new string(cuenta.Where(char.IsDigit).ToArray());
        if (digits.Length > 12) digits = digits[^12..];
        return digits.PadLeft(12, '0');
    }

    /// <summary>
    /// Rellena con ceros a la izquierda hasta alcanzar 'len' (para CFTSKY=13, por ejemplo).
    /// </summary>
    private static string PadLeftDigits(string s, int len)
    {
        var digits = new string((s ?? string.Empty).Where(char.IsDigit).ToArray());
        if (digits.Length > len) digits = digits[^len..];
        return digits.PadLeft(len, '0');
    }
}

// ========================= DTOs mínimos tipados =========================

/// <summary>ADQ02COM (BCAH96DTA) - Campos usados en las consultas.</summary>
public class ADQ02COM
{
    public long   A02COME { get; set; }          // NUM(15)
    public string A02CTDE { get; set; } = "";    // CHAR(20)
}

/// <summary>CFP801 (BNKPRD01) - Maestro de perfiles.</summary>
public class CFP801
{
    public int    CFTSBK { get; set; }           // NUM(3)
    public string CFTSKY { get; set; } = "";     // CHAR(13)
}

/// <summary>CFP102 (BNKPRD01) - Relación cuenta↔perfil (si aplica en tu site).</summary>
public class CFP102
{
    public int    CFTSBK { get; set; }           // NUM(3)
    public string CFTSKY { get; set; } = "";     // CHAR(13)
    public string CUX1AC { get; set; } = "";     // CHAR(12) cuenta
}

/// <summary>POP801 (BNKPRD01) - Cabecera de lote (para validar existencia de perfil).</summary>
public class POP801
{
    public int    FTTSBK { get; set; }           // NUM(3) → 1
    public string FTTSKY { get; set; } = "";     // CHAR(13)
    public int    FTSBT  { get; set; }           // NUM(3)
}


var svc = new Adquirencia.Consultas.PerfilPorCuentaService(_connection, _httpContext);

var perfil = svc.ObtenerPerfilPorCuenta("00123456789012345678");
// → "0000001234567" (ejemplo) o "" si no se pudo resolver

var perfiles = svc.ObtenerPerfilesPosibles("00123456789012345678");
// → ["0000001234567", "0000007654321"] (si hubiera más de uno)

