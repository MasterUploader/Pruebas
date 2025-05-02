using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using RestUtilities.Connections.Interfaces;
using System.Data.OleDb;

namespace SitiosIntranet.Web.Services
{
    /// <summary>
    /// Servicio que administra la lógica de almacenamiento de videos
    /// tanto en disco como en la base de datos AS400 usando OleDb.
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
        /// Guarda el archivo de video en disco local, en la ruta wwwroot/videos/{agencia}/
        /// </summary>
        /// <param name="archivo">Archivo enviado desde el formulario</param>
        /// <param name="codcco">Código de agencia. Si es 0, se guarda en carpeta 'comun'</param>
        /// <param name="rutaServer">Ruta base donde se construye la ruta completa</param>
        /// <param name="nombreArchivo">Nombre final del archivo</param>
        public async Task<bool> GuardarArchivoEnDisco(IFormFile archivo, string codcco, string rutaServer, string nombreArchivo)
        {
            try
            {
                string subcarpeta = codcco == "0" ? "comun" : codcco;
                string rutaRelativa = Path.Combine("videos", subcarpeta);
                string rutaFisica = Path.Combine(_env.WebRootPath, rutaRelativa);

                // Crear carpeta si no existe
                Directory.CreateDirectory(rutaFisica);

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
        /// Inserta registros en la tabla MANTVIDEO de AS400, una por agencia si es 0.
        /// </summary>
        /// <param name="codcco">Código de agencia o 0 para insertar en todas</param>
        /// <param name="estado">Estado del video ('A' o 'I')</param>
        /// <param name="nombreArchivo">Nombre del archivo guardado</param>
        /// <param name="rutaServer">Ruta base donde se guardó el archivo</param>
        public bool GuardarRegistroEnAs400(string codcco, string estado, string nombreArchivo, string rutaServer)
        {
            _as400.Open();
            using var conn = _as400.GetOleDbConnection();

            try
            {
                if (conn.State != System.Data.ConnectionState.Open)
                    conn.Open();

                // Si es 0, obtener todas las agencias
                var agencias = codcco == "0" ? ObtenerAgencias(conn) : new List<string> { codcco };

                foreach (var agencia in agencias)
                {
                    int codVideo = GetUltimoId(conn);        // Nuevo ID autogenerado
                    int sec = GetSecuencia(conn, agencia);   // Siguiente SEQ

                    string ruta = Path.Combine(rutaServer, agencia, "Marquesin");

                    // Consulta de inserción
                    string insert = $@"
                        INSERT INTO BCAH96DTA.MANTVIDEO(CODCCO, CODVIDEO, RUTA, NOMBRE, ESTADO, SEQ)
                        VALUES('{agencia}', {codVideo}, '{ruta}', '{nombreArchivo}', '{estado}', {sec})";

                    using var cmd = new OleDbCommand(insert, conn);
                    cmd.ExecuteNonQuery();
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
        /// Obtiene la lista de agencias desde la tabla RSAGE01
        /// </summary>
        public List<string> ObtenerAgencias(OleDbConnection conn)
        {
            var lista = new List<string>();
            string query = "SELECT CODCCO FROM BCAH96DTA.RSAGE01";

            using var cmd = new OleDbCommand(query, conn);
            using var reader = cmd.ExecuteReader();

            while (reader.Read())
                lista.Add(reader["CODCCO"].ToString());

            return lista;
        }

        /// <summary>
        /// Obtiene el próximo valor de CODVIDEO a insertar (MAX + 1)
        /// </summary>
        public int GetUltimoId(OleDbConnection conn)
        {
            string query = "SELECT MAX(CODVIDEO) FROM BCAH96DTA.MANTVIDEO";

            using var cmd = new OleDbCommand(query, conn);
            var result = cmd.ExecuteScalar();

            return result != DBNull.Value ? Convert.ToInt32(result) + 1 : 1;
        }

        /// <summary>
        /// Obtiene el próximo valor de SEQ para la agencia específica (MAX + 1)
        /// </summary>
        public int GetSecuencia(OleDbConnection conn, string codcco)
        {
            string query = $"SELECT MAX(SEQ) FROM BCAH96DTA.MANTVIDEO WHERE CODCCO = '{codcco}'";

            using var cmd = new OleDbCommand(query, conn);
            var result = cmd.ExecuteScalar();

            return result != DBNull.Value ? Convert.ToInt32(result) + 1 : 1;
        }
    }
}
