using Microsoft.AspNetCore.Http;
using System.Data.Common;
using System.Threading.Tasks;

namespace SitiosIntranet.Web.Services
{
    /// <summary>
    /// Interfaz para operaciones de manejo de archivos y registros de video en AS400.
    /// </summary>
    public interface IVideoService
    {
        /// <summary>
        /// Guarda el archivo físicamente en el disco local bajo wwwroot/videos/{agencia}/
        /// </summary>
        Task<bool> GuardarArchivoEnDisco(IFormFile archivo, string codcco, string rutaServer, string nombreArchivo);

        /// <summary>
        /// Inserta uno o varios registros en la tabla MANTVIDEO del AS400, según la agencia.
        /// Si la agencia es 0, inserta en todas.
        /// </summary>
        bool GuardarRegistroEnAs400(string codcco, string estado, string nombreArchivo, string rutaServer);

        /// <summary>
        /// Obtiene el próximo valor para CODVIDEO (MAX + 1).
        /// </summary>
        int GetUltimoId(DbCommand command);

        /// <summary>
        /// Obtiene el próximo valor de SEQ para una agencia (MAX + 1).
        /// </summary>
        int GetSecuencia(DbCommand command, string codcco);

        /// <summary>
        /// Devuelve la lista de agencias activas (desde RSAGE01).
        /// </summary>
        List<string> ObtenerAgencias(DbCommand command);
    }
}











using Microsoft.AspNetCore.Http;
using RestUtilities.Connections.Interfaces;
using System.Data.Common;

namespace SitiosIntranet.Web.Services
{
    /// <summary>
    /// Servicio para manejo de archivos y registros en la tabla MANTVIDEO del AS400.
    /// Toda interacción con la base de datos se hace vía DbCommand.
    /// </summary>
    public class VideoService : IVideoService
    {
        private readonly IDatabaseConnection _as400;
        private readonly IWebHostEnvironment _env;

        public VideoService(IDatabaseConnection as400, IWebHostEnvironment env)
        {
            _as400 = as400;
            _env = env;
        }

        /// <summary>
        /// Guarda el archivo en disco local (en carpeta /wwwroot/videos/{agencia}/)
        /// </summary>
        public async Task<bool> GuardarArchivoEnDisco(IFormFile archivo, string codcco, string rutaServer, string nombreArchivo)
        {
            try
            {
                string subcarpeta = codcco == "0" ? "comun" : codcco;
                string rutaRelativa = Path.Combine("videos", subcarpeta);
                string rutaFisica = Path.Combine(_env.WebRootPath, rutaRelativa);

                Directory.CreateDirectory(rutaFisica); // Crear si no existe

                string rutaCompleta = Path.Combine(rutaFisica, nombreArchivo);
                using var stream = new FileStream(rutaCompleta, FileMode.Create);
                await archivo.CopyToAsync(stream);

                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Inserta el registro del video en AS400.
        /// Si codcco = 0, inserta para todas las agencias.
        /// Usa GetDbCommand() de RestUtilities para ejecutar consultas SQL.
        /// </summary>
        public bool GuardarRegistroEnAs400(string codcco, string estado, string nombreArchivo, string rutaServer)
        {
            _as400.Open();

            try
            {
                using var command = _as400.GetDbCommand();

                if (command.Connection.State != System.Data.ConnectionState.Open)
                    command.Connection.Open();

                var agencias = codcco == "0" ? ObtenerAgencias(command) : new List<string> { codcco };

                foreach (var agencia in agencias)
                {
                    int codVideo = GetUltimoId(command); // Obtener nuevo ID
                    int sec = GetSecuencia(command, agencia); // Obtener secuencia

                    string ruta = Path.Combine(rutaServer, agencia, "Marquesin");

                    command.CommandText = $@"
                        INSERT INTO BCAH96DTA.MANTVIDEO(CODCCO, CODVIDEO, RUTA, NOMBRE, ESTADO, SEQ)
                        VALUES('{agencia}', {codVideo}, '{ruta}', '{nombreArchivo}', '{estado}', {sec})";

                    command.ExecuteNonQuery();
                }

                return true;
            }
            catch
            {
                return false;
            }
            finally
            {
                _as400.Close();
            }
        }

        /// <summary>
        /// Obtiene la lista de agencias activas desde RSAGE01.
        /// </summary>
        public List<string> ObtenerAgencias(DbCommand command)
        {
            var agencias = new List<string>();
            command.CommandText = "SELECT CODCCO FROM BCAH96DTA.RSAGE01";

            using var reader = command.ExecuteReader();
            while (reader.Read())
                agencias.Add(reader["CODCCO"].ToString());

            reader.Close();
            return agencias;
        }

        /// <summary>
        /// Retorna el siguiente valor de CODVIDEO (MAX + 1)
        /// </summary>
        public int GetUltimoId(DbCommand command)
        {
            command.CommandText = "SELECT MAX(CODVIDEO) FROM BCAH96DTA.MANTVIDEO";
            var result = command.ExecuteScalar();
            return result != DBNull.Value ? Convert.ToInt32(result) + 1 : 1;
        }

        /// <summary>
        /// Retorna el siguiente valor de SEQ por agencia (MAX + 1)
        /// </summary>
        public int GetSecuencia(DbCommand command, string codcco)
        {
            command.CommandText = $"SELECT MAX(SEQ) FROM BCAH96DTA.MANTVIDEO WHERE CODCCO = '{codcco}'";
            var result = command.ExecuteScalar();
            return result != DBNull.Value ? Convert.ToInt32(result) + 1 : 1;
        }
    }
}
