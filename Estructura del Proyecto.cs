using Connections.Abstractions;
using RestUtilities.QueryBuilder;
using System.Data.Common;

namespace TuNamespace;

/// <summary>
/// Servicio que sustituye el CALL FECTIM de CLLE usando un SELECT a DB2 for i.
/// </summary>
/// <remarks>
/// - Produce FAAAAMMDD (yyyyMMdd) y HORA (HHmmss) como en el PGM original.
/// - Usa SYSIBM.SYSDUMMY1 (tabla dummy: siempre 1 fila).
/// - Devuelve tupla con bandera de éxito, fecha y hora.
/// </remarks>
public class FechaHoraService(IDatabaseConnection _connection, IHttpContextAccessor _contextAccessor)
{
    /// <summary>
    /// Equivalente a: 
    /// <c>CALL FECTIM PARM(&FAAAAMMDD &HORA)</c>.
    /// </summary>
    /// <returns>
    /// (respuesta: true/false, fecsys: "yyyyMMdd" (8), horasys: "HHmmss" (7))
    /// </returns>
    public (bool respuesta, string fecsys, string horasys) FecReal()
    {
        // Variables de salida: simulan los PARM de CLLE.
        string fecsys = string.Empty;   // &FAAAAMMDD (8)
        string horasys = string.Empty;  // &HORA      (7)

        try
        {
            _connection.Open();

            // ================== SQL generado ==================
            // SELECT
            //   VARCHAR_FORMAT(CURRENT_DATE, 'YYYYMMDD') AS FAAAAMMDD,
            //   VARCHAR_FORMAT(CURRENT_TIME, 'HH24MISS') AS HORA
            // FROM SYSIBM.SYSDUMMY1
            //
            // Notas:
            // - 'YYYYMMDD' -> 8 caracteres (yyyyMMdd)
            // - 'HH24MISS' -> 6 caracteres (HHmmss). En CLLE definiste LEN(7),
            //   así que abajo lo ajustamos a longitud 7 con PadRight(7).
            // ==================================================
            var query = QueryBuilder.Core.QueryBuilder
                .From("SYSDUMMY1", "SYSIBM")
                .Select(
                    "VARCHAR_FORMAT(CURRENT_DATE, 'YYYYMMDD') AS FAAAAMMDD",
                    "VARCHAR_FORMAT(CURRENT_TIME, 'HH24MISS') AS HORA"
                )
                .FetchNext(1)
                .Build();

            using var cmd = _connection.GetDbCommand(_contextAccessor.HttpContext!);
            cmd.CommandText = query.Sql;

            using var rd = cmd.ExecuteReader();
            if (!rd.Read())
                return (false, fecsys, horasys);

            // Lectura directa por índice para máximo rendimiento
            fecsys  = rd.GetString(0);                 // "yyyyMMdd" (8)
            horasys = rd.GetString(1).PadRight(7);     // "HHmmss" -> ajustado a LEN(7)

            return (true, fecsys, horasys);
        }
        catch
        {
            // Si hay error, mantenemos contrato similar al PGM (bandera false)
            return (false, fecsys, horasys);
        }
        finally
        {
            _connection.Close();
        }
    }
}
