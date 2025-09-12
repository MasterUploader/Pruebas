using RestUtilities.QueryBuilder;
using Connections.Abstractions;
using System.Data.Common;

/// <summary>
/// Servicio de perfiles que contiene la lógica equivalente a la subrutina VerPerfil de RPGLE.
/// </summary>
public class PerfilService(IDatabaseConnection _connection, IHttpContextAccessor _httpContextAccessor, LoteService _loteService)
{
    /// <summary>
    /// Verifica si existe un perfil en la tabla CFP801 y ejecuta la lógica correspondiente.
    /// </summary>
    /// <param name="perfil">Clave de perfil (CFTSKY en RPGLE).</param>
    public void VerPerfil(string perfil)
    {
        // Equivalente a MsgNoPerfil = '1'
        bool noExistePerfil = true;

        try
        {
            _connection.Open();

            // ✅ Construimos la consulta (equivalente al CHAIN en RPGLE)
            var query = QueryBuilder.Core.QueryBuilder
                .From("CFP801", "BCAH96DTA")
                .Select("CFTSBK", "CFTSKY")  // Solo necesitamos validar existencia
                .Where("CFTSBK = 001")       // Condición fija
                .Where<CFP801>(x => x.CFTSKY == perfil) // Filtro dinámico por perfil
                .FetchNext(1)                // Solo necesitamos un registro
                .Build();

            using var command = _connection.GetDbCommand(_httpContextAccessor.HttpContext!);
            command.CommandText = query.Sql;

            using var reader = command.ExecuteReader();
            if (reader.Read()) // %FOUND en RPGLE
            {
                noExistePerfil = false;

                // En RPGLE: Exsr VerUltlote
                _loteService.VerUltLote(perfil);
            }
        }
        finally
        {
            _connection.Close();
        }

        // En RPGLE: If MsgNoPerfil = '1' Dsply 'No existe perfil'
        if (noExistePerfil)
        {
            Console.WriteLine("No existe perfil");
        }
    }
}

/// <summary>
/// DTO que representa la tabla CFP801 para el QueryBuilder.
/// </summary>
public class CFP801
{
    public string CFTSBK { get; set; } = string.Empty;
    public string CFTSKY { get; set; } = string.Empty;
}
