PGM        PARM(&PERFIL)
/* ------------------------------------------------------------------ */
/*  GETADQPER: Retorna el perfil desde la DTAARA BCAH96DTA/ADQDTA      */
/*  - Sin entradas; 1 salida: &PERFIL (13)                             */
/*  - Evita dependencias de funciones SQL (DATA_AREA_*)                */
/* ------------------------------------------------------------------ */
             DCL        VAR(&PERFIL) TYPE(*CHAR) LEN(13)
             DCL        VAR(&VAL)    TYPE(*CHAR) LEN(13)

/* Lee la data area (ajusta lib/nombre si cambia) */
             RTVDTAARA  DTAARA(BCAH96DTA/ADQDTA) RTNVAR(&VAL)

/* Mueve a salida (right-trim básico) */
             CHGVAR     VAR(&PERFIL) VALUE(&VAL)

             ENDPGM




             using Connections.Abstractions;
using Microsoft.AspNetCore.Http;
using RestUtilities.QueryBuilder;

namespace Adquirencia.Services.Config;

/// <summary>
/// Obtiene el perfil (FTTSKY) desde la DTAARA BCAH96DTA/ADQDTA
/// invocando un CL wrapper con ProgramCallBuilder (sin dependencias de funciones SQL).
/// </summary>
/// <remarks>
/// - El CL devuelve SOLO salidas (sin entradas) para ajustarse al patrón OutChar de ProgramCallBuilder.
/// - Si más adelante deseas parametrizar librería/nombre o longitud, se añade una versión con InChar.
/// </remarks>
public class PerfilDesdeDataAreaService(IDatabaseConnection _cn, IHttpContextAccessor _ctx)
{
    /// <summary>
    /// Lee la DTAARA y retorna el perfil (recorte derecho y trim).
    /// </summary>
    public string ObtenerPerfil()
    {
        // ================== CL llamado: ICBSUSER/GETADQPER ==================
        // OUT: PERFIL  CHAR(13)  ← valor de la data area ADQDTA
        var call = ProgramCallBuilder
            .ForConnection(_cn, "ICBSUSER", "GETADQPER")  // biblioteca y programa CL
            .OutChar("PERFIL", 13)                        // único parámetro de salida
            .WithTimeout(30)
            .Call(_ctx.HttpContext!);                     // síncrono; usa CallAsync si prefieres

        // Lectura directa del resultado
        if (call.Result.TryGetString("PERFIL", out var perfil))
            return (perfil ?? string.Empty).Trim();

        return string.Empty;
    }
}
