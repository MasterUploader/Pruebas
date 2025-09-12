using Connections.Abstractions;
using RestUtilities.QueryBuilder; // InsertQueryBuilder / QueryResult
using System.Data.Common;

namespace Adquirencia.Services;

/// <summary>
/// Operaciones de Lotes sobre POP801. Equivalente a la subrutina RPGLE <c>NuevoLote</c>.
/// </summary>
/// <remarks>
/// Inserta un nuevo registro en POP801 calculando el siguiente número de lote (FTSBT = último + 1),
/// fija estado 02 y persiste usuario/fecha operativa. Usa INSERT parametrizado del QueryBuilder,
/// lo que permite a la conexión agregar los parámetros al <see cref="DbCommand"/> automáticamente
/// (placeholders con '?') y evita inyección SQL.
/// </remarks>
public class LoteService(IDatabaseConnection _connection, IHttpContextAccessor _httpContextAccessor)
{
    /// <summary>
    /// Inserta un nuevo lote en <c>BCAH96DTA.POP801</c>.
    /// </summary>
    /// <param name="perfil">Valor para FTTSKY.</param>
    /// <param name="usuario">Valor para FTTSOR.</param>
    /// <param name="dsdt">Fecha operativa (CYYMMDD) para FTTSDT.</param>
    /// <param name="ultimoFtsbt">Último FTSBT existente (para calcular el siguiente).</param>
    /// <returns>El número de lote generado (FTSBT) y si se persistió correctamente.</returns>
    public (int lote, bool ok) NuevoLote(string perfil, string usuario, int dsdt, int ultimoFtsbt)
    {
        // ► En RPG: wFTSBT = wFTSBT + 1; FTTSBK = 001; FTTSKY = PERFIL; FTSBT = wFTSBT; FTSST = 02; FTTSOR = Usuario; FTTSDT = DSDT; write Pop8011;
        var siguienteFtsbt = ultimoFtsbt + 1; // número de lote que se insertará

        try
        {
            _connection.Open();

            // INSERT parametrizado (placeholders '?') generado por el QueryBuilder.
            // IntoColumns define el orden de columnas; Row especifica los valores respetando ese orden.
            var insert = new InsertQueryBuilder("POP801", "BCAH96DTA")
                .IntoColumns(["FTTSBK", "FTTSKY", "FTTSBT", "FTTSST", "FTTSOR", "FTTSDT"])
                .Row([1,           perfil,   siguienteFtsbt, 2,        usuario,   dsdt])
                .Build(); // → QueryResult con Sql y Parameters en el mismo orden

            using var cmd = _connection.GetDbCommand(_httpContextAccessor.HttpContext!);
            cmd.CommandText = insert.Sql;

            // Nota funcional:
            // RestUtilities.Connections agrega automáticamente los parámetros cuando detecta '?' en el SQL.
            // Si tu versión requiere agregar manualmente, descomenta el bloque:
            //
            // foreach (var p in insert.Parameters)
            // {
            //     var dbp = cmd.CreateParameter();
            //     dbp.Value = p.Value ?? DBNull.Value;
            //     cmd.Parameters.Add(dbp);
            // }

            var affected = cmd.ExecuteNonQuery(); // write Pop8011;
            return (siguienteFtsbt, affected > 0);
        }
        finally
        {
            _connection.Close();
        }
    }
}
