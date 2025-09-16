using System.Data.Common;
using Connections.Abstractions;
using Microsoft.AspNetCore.Http;

namespace Adquirencia.Services.Config;

/// <summary>
/// Lee el perfil operativo (FTTSKY) desde una Data Area de IBM i.
/// Si la DTAARA cambia, el sistema usará el nuevo perfil sin redeploy.
/// </summary>
public class PerfilDesdeDataAreaService(IDatabaseConnection _cn, IHttpContextAccessor _ctx)
{
    /// <summary>
    /// Obtiene el perfil desde la DTAARA BCAH96DTA/ADQDTA.
    /// </summary>
    /// <remarks>
    /// Usa la función SQL nativa <c>QSYS2.DATA_AREA_VALUE</c>.
    /// </remarks>
    public string ObtenerPerfil()
    {
        // Nota: DATA_AREA_VALUE devuelve VARBINARY; lo casteamos a VARCHAR(13).
        const string sql =
            "SELECT CAST(QSYS2.DATA_AREA_VALUE('BCAH96DTA','ADQDTA') AS VARCHAR(13)) AS PERFIL " +
            "FROM SYSIBM.SYSDUMMY1";

        _cn.Open();
        try
        {
            using var cmd = _cn.GetDbCommand(_ctx.HttpContext!);
            cmd.CommandText = sql;

            using var rd = cmd.ExecuteReader();
            if (rd.Read() && !rd.IsDBNull(0))
                return rd.GetString(0).Trim();

            return string.Empty;
        }
        finally
        {
            _cn.Close();
        }
    }
}
