using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using RestUtilities.Connections.Interfaces;
using System.Data.Common;

namespace SitiosIntranet.Web.Services
{
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
        /// Guarda el archivo en el disco local dentro de wwwroot/videos/{agencia}/
        /// </summary>
        public async Task<bool> GuardarArchivoEnDisco(IFormFile archivo, string codcco, string rutaServer, string nombreArchivo)
        {
            try
            {
                string subcarpeta = codcco == "0" ? "comun" : codcco;
                string rutaRelativa = Path.Combine("videos", subcarpeta);
                string rutaFisica = Path.Combine(_env.WebRootPath, rutaRelativa);

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
        /// Inserta el registro del video en la tabla MANTVIDEO del AS400
        /// </summary>
        public bool GuardarRegistroEnAs400(string codcco, string estado, string nombreArchivo, string rutaServer)
        {
            _as400.Open();
            var context = _as400.GetDbContext();
            var conn = context.Database.GetDbConnection();

            try
            {
                if (conn.State != System.Data.ConnectionState.Open)
                    conn.Open();

                var agencias = codcco == "0"
                    ? ObtenerAgencias(conn)
                    : new List<string> { codcco };

                foreach (var agencia in agencias)
                {
                    int codVideo = GetUltimoId(conn) + 1;
                    int sec = GetSecuencia(conn, agencia);

                    string ruta = Path.Combine(rutaServer, agencia, "Marquesin");

                    string query = $@"
                        INSERT INTO BCAH96DTA.MANTVIDEO(CODCCO, CODVIDEO, RUTA, NOMBRE, ESTADO, SEQ)
                        VALUES('{agencia}', {codVideo}, '{ruta}', '{nombreArchivo}', '{estado}', {sec})";

                    using var cmd = conn.CreateCommand();
                    cmd.CommandText = query;
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
        /// Obtiene todas las agencias registradas en RSAGE01
        /// </summary>
        public List<string> ObtenerAgencias(DbConnection conn)
        {
            var lista = new List<string>();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT CODCCO FROM BCAH96DTA.RSAGE01";
            using var reader = cmd.ExecuteReader();

            while (reader.Read())
                lista.Add(reader["CODCCO"].ToString());

            return lista;
        }

        /// <summary>
        /// Obtiene el siguiente valor de CODVIDEO (MAX + 1)
        /// </summary>
        public int GetUltimoId(DbConnection conn)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT MAX(CODVIDEO) FROM BCAH96DTA.MANTVIDEO";
            var result = cmd.ExecuteScalar();
            return result != DBNull.Value ? Convert.ToInt32(result) + 1 : 1;
        }

        /// <summary>
        /// Obtiene el siguiente valor de SEQ para una agencia
        /// </summary>
        public int GetSecuencia(DbConnection conn, string codcco)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = $"SELECT MAX(SEQ) FROM BCAH96DTA.MANTVIDEO WHERE CODCCO = '{codcco}'";
            var result = cmd.ExecuteScalar();
            return result != DBNull.Value ? Convert.ToInt32(result) + 1 : 1;
        }
    }
}
