using Connections.Abstractions;
using RestUtilities.QueryBuilder;
using System.Data.Common;

namespace TuNamespace;

/// <summary>
/// Servicio utilitario para resolver fecha/hora de sistema desde DB2 for i
/// usando una sola sentencia SQL (equivalente funcional al CALL FECTIM).
/// </summary>
/// <remarks>
/// - Consulta a SYSIBM.SYSDUMMY1 para obtener CURRENT_DATE y CURRENT_TIME.
/// - Formatea como 'yyyyMMdd' y 'HHmmss' para emular FECSYS/HORASYS de RPGLE.
/// - No requiere tablas de negocio; usa la tabla dummy de DB2.
/// </remarks>
public class FechaHoraService(IDatabaseConnection _connection, IHttpContextAccessor _httpContextAccessor)
{
    /// <summary>
    /// Obtiene fecha (FECSYS) y hora (HORASYS) del sistema con un SELECT.
    /// </summary>
    /// <returns>Tupla (fecsys, horasys) en formato 'yyyyMMdd' y 'HHmmss'.</returns>
    public (string fecsys, string horasys) FecReal()
    {
        // Valores por defecto en caso de no leer fila (DB2 siempre devuelve 1 fila aquí).
        var fecsys = string.Empty;
        var horasys = string.Empty;

        try
        {
            _connection.Open();

            // ================== SQL generado ==================
            // SELECT
            //   VARCHAR_FORMAT(CURRENT_DATE, 'YYYYMMDD') AS FECSYS,
            //   VARCHAR_FORMAT(CURRENT_TIME, 'HH24MISS') AS HORASYS
            // FROM SYSIBM.SYSDUMMY1
            //
            // Uso de Select(...) con expresiones y alias para no depender de un DTO.
            // ==================================================
            var query = QueryBuilder.Core.QueryBuilder
                .From("SYSDUMMY1", "SYSIBM") // Tabla dummy de DB2; siempre 1 fila
                .Select(
                    "VARCHAR_FORMAT(CURRENT_DATE, 'YYYYMMDD') AS FECSYS",
                    "VARCHAR_FORMAT(CURRENT_TIME, 'HH24MISS') AS HORASYS"
                )
                .FetchNext(1) // Seguridad: aseguramos una sola fila
                .Build();

            using var command = _connection.GetDbCommand(_httpContextAccessor.HttpContext!);
            command.CommandText = query.Sql;

            using var reader = command.ExecuteReader();
            if (reader.Read())
            {
                // Lectura por índice para máximo rendimiento.
                fecsys = reader.GetString(0);   // 'yyyyMMdd'
                horasys = reader.GetString(1);  // 'HHmmss'
            }
        }
        finally
        {
            _connection.Close();
        }

        return (fecsys, horasys);
    }
}
