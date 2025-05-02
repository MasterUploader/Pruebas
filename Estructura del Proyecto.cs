using Microsoft.AspNetCore.Http;
using System.Data.OleDb;
using System.Threading.Tasks;

namespace SitiosIntranet.Web.Services
{
    /// <summary>
    /// Define operaciones para manejo de videos y registro en AS400 usando OleDb.
    /// </summary>
    public interface IVideoService
    {
        /// <summary>
        /// Guarda el archivo en disco local dentro de wwwroot/videos/{agencia}/
        /// </summary>
        Task<bool> GuardarArchivoEnDisco(IFormFile archivo, string codcco, string rutaServer, string nombreArchivo);

        /// <summary>
        /// Inserta el registro del video en la base de datos AS400 (una o varias agencias).
        /// </summary>
        bool GuardarRegistroEnAs400(string codcco, string estado, string nombreArchivo, string rutaServer);

        /// <summary>
        /// Obtiene el próximo CODVIDEO desde la tabla MANTVIDEO (MAX + 1)
        /// </summary>
        int GetUltimoId(OleDbConnection conn);

        /// <summary>
        /// Obtiene la próxima secuencia SEQ para una agencia (MAX + 1)
        /// </summary>
        int GetSecuencia(OleDbConnection conn, string codcco);

        /// <summary>
        /// Devuelve la lista de agencias activas desde RSAGE01
        /// </summary>
        List<string> ObtenerAgencias(OleDbConnection conn);
    }
}
