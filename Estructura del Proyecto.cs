using System;
using System.Threading.Tasks;
using System.Data.Common;

namespace RestUtilities.Connections.Interfaces
{
    /// <summary>
    /// Define los métodos estándar para una conexión a bases de datos.
    /// </summary>
    public interface IDatabaseConnection : IDisposable
    {
        /// <summary>
        /// Abre la conexión a la base de datos.
        /// </summary>
        Task OpenAsync();

        /// <summary>
        /// Cierra la conexión a la base de datos.
        /// </summary>
        void Close();

        /// <summary>
        /// Ejecuta una consulta SQL y devuelve los resultados.
        /// </summary>
        /// <param name="query">Consulta SQL a ejecutar.</param>
        /// <returns>Resultado en un `DbDataReader`.</returns>
        Task<DbDataReader> ExecuteQueryAsync(string query);

        /// <summary>
        /// Ejecuta una consulta SQL sin devolver datos.
        /// </summary>
        /// <param name="query">Consulta SQL a ejecutar.</param>
        /// <returns>Número de filas afectadas.</returns>
        Task<int> ExecuteNonQueryAsync(string query);
    }
}
