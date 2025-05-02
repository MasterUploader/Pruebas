using Microsoft.AspNetCore.Http;
using System.Data.Common;
using System.Threading.Tasks;

namespace SitiosIntranet.Web.Services
{
    /// <summary>
    /// Define las operaciones necesarias para manejar el guardado de videos en disco y AS400.
    /// </summary>
    public interface IVideoService
    {
        /// <summary>
        /// Guarda el archivo en disco local dentro de wwwroot/videos/{agencia}/
        /// </summary>
        /// <param name="archivo">Archivo de video cargado</param>
        /// <param name="codcco">Código de agencia</param>
        /// <param name="rutaServer">Ruta base del servidor</param>
        /// <param name="nombreArchivo">Nombre del archivo final</param>
        /// <returns>True si fue exitoso</returns>
        Task<bool> GuardarArchivoEnDisco(IFormFile archivo, string codcco, string rutaServer, string nombreArchivo);

        /// <summary>
        /// Inserta los registros del video en la base de datos AS400.
        /// </summary>
        /// <param name="codcco">Código de agencia (0 para todas)</param>
        /// <param name="estado">Estado A/I</param>
        /// <param name="nombreArchivo">Nombre del archivo</param>
        /// <param name="rutaServer">Ruta base del servidor</param>
        /// <returns>True si el insert fue exitoso</returns>
        bool GuardarRegistroEnAs400(string codcco, string estado, string nombreArchivo, string rutaServer);

        /// <summary>
        /// Obtiene el siguiente ID para CODVIDEO (MAX + 1)
        /// </summary>
        int GetUltimoId(DbConnection conn);

        /// <summary>
        /// Obtiene la siguiente secuencia SEQ para la agencia indicada.
        /// </summary>
        int GetSecuencia(DbConnection conn, string codcco);

        /// <summary>
        /// Devuelve la lista de códigos de agencias activas.
        /// </summary>
        List<string> ObtenerAgencias(DbConnection conn);
    }
}
