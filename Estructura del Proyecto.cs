using System.Data.Common;
using Connections.Abstractions;
using Microsoft.AspNetCore.Http;

namespace Adquirencia.Services.Config;

/// <summary>
/// Lee el perfil (FTTSKY) desde la DTAARA BCAH96DTA/ADQDTA usando funciones disponibles
/// en tu versión de IBM i. Si la función no existe, retorna string.Empty.
/// </summary>
public class PerfilDesdeDataAreaService(IDatabaseConnection _cn, IHttpContextAccessor _ctx)
{
    /// <summary>
    /// Intenta leer la DTAARA con DATA_AREA_INFO, luego con DATA_AREA. Si ninguna existe, retorna "".
    /// </summary>
    public string ObtenerPerfil()
    {
        _cn.Open();
        try
        {
            using var cmd = _cn.GetDbCommand(_ctx.HttpContext!);

            // ====== Intento 1: QSYS2.DATA_AREA_INFO (recomendado en IBM i recientes) ======
            // Nota funcional: VALUE puede ser CHAR/VARCHAR; lo casteamos a VARCHAR(128) y TRIM.
            try
            {
                cmd.CommandText =
                    "SELECT RTRIM(CAST(VALUE AS VARCHAR(128))) " +
                    "FROM TABLE(QSYS2.DATA_AREA_INFO(DATA_AREA_LIBRARY => 'BCAH96DTA', DATA_AREA_NAME => 'ADQDTA')) X";
                using (var rd = cmd.ExecuteReader())
                {
                    if (rd.Read() && !rd.IsDBNull(0))
                        return rd.GetString(0).Trim();
                }
            }
            catch
            {
                // ignoramos y probamos el siguiente método
            }

            // ====== Intento 2: QSYS2.DATA_AREA (algunas releases lo exponen así) ======
            try
            {
                cmd.CommandText =
                    "SELECT RTRIM(CAST(VALUE AS VARCHAR(128))) " +
                    "FROM TABLE(QSYS2.DATA_AREA(LIBRARY => 'BCAH96DTA', NAME => 'ADQDTA')) T";
                using (var rd = cmd.ExecuteReader())
                {
                    if (rd.Read() && !rd.IsDBNull(0))
                        return rd.GetString(0).Trim();
                }
            }
            catch
            {
                // si tampoco existe, devolvemos vacío y que el caller use la ruta por cuenta/comercio
            }

            return string.Empty;
        }
        finally
        {
            _cn.Close();
        }
    }
}
