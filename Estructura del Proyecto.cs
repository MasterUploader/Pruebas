using RestUtilities.QueryBuilder;
using Connections.Abstractions;
using System.Data.Common;

/// <summary>
/// Clase que contiene la lógica equivalente a la subrutina VerUltlote de RPGLE.
/// </summary>
public class LoteService(IDatabaseConnection _connection, IHttpContextAccessor _httpContextAccessor)
{
    /// <summary>
    /// Obtiene el último valor de FTSBT para un perfil dado (equivalente al VerUltlote en RPGLE).
    /// </summary>
    /// <param name="perfil">Clave de perfil que corresponde a FTTSKY.</param>
    /// <returns>El último valor de FTSBT encontrado o 0 si no existe.</returns>
    public int VerUltLote(string perfil)
    {
        // Variable resultado (equivalente a wFTSBT en RPGLE)
        int ultimoFTSBT = 0;

        try
        {
            _connection.Open();

            // ✅ Construimos el query con QueryBuilder
            var query = QueryBuilder.Core.QueryBuilder
                .From("POP801", "BCAH96DTA")   // Tabla POP801 en librería AS400
                .Select("FTSBT")               // Campo que queremos traer
                .Where("FTTSBK = 001")         // Condición fija de RPGLE
                .Where<POP801>(x => x.FTTSKY == perfil) // Filtro dinámico por PERFIL
                .OrderBy("FTSBT DESC")         // Simula leer hasta el último FTSBT
                .FetchNext(1)                  // Solo el último
                .Build();

            using var command = _connection.GetDbCommand(_httpContextAccessor.HttpContext!);
            command.CommandText = query.Sql;

            using var reader = command.ExecuteReader();
            if (reader.Read())
            {
                ultimoFTSBT = reader.GetInt32(0);
            }
        }
        finally
        {
            _connection.Close();
        }

        // En RPGLE se llama Exsr NuevoLote aquí
        // En C# lo encapsulamos como método aparte:
        NuevoLote(ultimoFTSBT);

        return ultimoFTSBT;
    }

    /// <summary>
    /// Simula la llamada a la subrutina NuevoLote de RPGLE.
    /// </summary>
    private void NuevoLote(int valor)
    {
        // Aquí iría la lógica de la subrutina NuevoLote.
        // Por ejemplo, inicializar un nuevo lote con el último FTSBT.
    }
}

/// <summary>
/// DTO que representa la tabla POP801, necesario para QueryBuilder con expresiones lambda.
/// </summary>
public class POP801
{
    public string FTTSBK { get; set; } = string.Empty;
    public string FTTSKY { get; set; } = string.Empty;
    public int FTSBT { get; set; }
}
