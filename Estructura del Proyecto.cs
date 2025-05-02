using SitiosIntranet.Web.Models;
using System.Collections.Generic;

namespace SitiosIntranet.Web.Services
{
    /// <summary>
    /// Define las operaciones de consulta para videos registrados en AS400.
    /// </summary>
    public interface IVideoQueryService
    {
        /// <summary>
        /// Retorna la lista de todos los videos registrados.
        /// </summary>
        List<VideoModel> ObtenerTodos();

        /// <summary>
        /// Obtiene un video por su código de agencia y código de video.
        /// </summary>
        VideoModel ObtenerPorId(string codcco, int codvideo);

        /// <summary>
        /// Elimina un registro de video por su código y agencia.
        /// </summary>
        bool Eliminar(string codcco, int codvideo);
    }
}











using RestUtilities.Connections.Interfaces;
using SitiosIntranet.Web.Models;
using System;
using System.Collections.Generic;
using System.Data.Common;

namespace SitiosIntranet.Web.Services
{
    /// <summary>
    /// Implementación del servicio de consultas para videos usando DbCommand.
    /// </summary>
    public class VideoQueryService : IVideoQueryService
    {
        private readonly IDatabaseConnection _as400;

        public VideoQueryService(IDatabaseConnection as400)
        {
            _as400 = as400;
        }

        public List<VideoModel> ObtenerTodos()
        {
            var lista = new List<VideoModel>();

            try
            {
                _as400.Open();
                using var command = _as400.GetDbCommand();

                command.CommandText = "SELECT CODCCO, CODVIDEO, RUTA, NOMBRE, ESTADO, SEQ FROM BCAH96DTA.MANTVIDEO";

                using var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    var video = new VideoModel
                    {
                        CODCCO = reader["CODCCO"].ToString(),
                        CODVIDEO = Convert.ToInt32(reader["CODVIDEO"]),
                        RUTA = reader["RUTA"].ToString(),
                        NOMBRE = reader["NOMBRE"].ToString(),
                        ESTADO = reader["ESTADO"].ToString(),
                        SEQ = Convert.ToInt32(reader["SEQ"])
                    };

                    lista.Add(video);
                }

                return lista;
            }
            finally
            {
                _as400.Close();
            }
        }

        public VideoModel ObtenerPorId(string codcco, int codvideo)
        {
            VideoModel? video = null;

            try
            {
                _as400.Open();
                using var command = _as400.GetDbCommand();

                command.CommandText = $@"
                    SELECT CODCCO, CODVIDEO, RUTA, NOMBRE, ESTADO, SEQ 
                    FROM BCAH96DTA.MANTVIDEO 
                    WHERE CODCCO = '{codcco}' AND CODVIDEO = {codvideo}";

                using var reader = command.ExecuteReader();
                if (reader.Read())
                {
                    video = new VideoModel
                    {
                        CODCCO = reader["CODCCO"].ToString(),
                        CODVIDEO = Convert.ToInt32(reader["CODVIDEO"]),
                        RUTA = reader["RUTA"].ToString(),
                        NOMBRE = reader["NOMBRE"].ToString(),
                        ESTADO = reader["ESTADO"].ToString(),
                        SEQ = Convert.ToInt32(reader["SEQ"])
                    };
                }

                return video!;
            }
            finally
            {
                _as400.Close();
            }
        }

        public bool Eliminar(string codcco, int codvideo)
        {
            try
            {
                _as400.Open();
                using var command = _as400.GetDbCommand();

                command.CommandText = $@"
                    DELETE FROM BCAH96DTA.MANTVIDEO 
                    WHERE CODCCO = '{codcco}' AND CODVIDEO = {codvideo}";

                return command.ExecuteNonQuery() > 0;
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
    }
}






namespace SitiosIntranet.Web.Models
{
    /// <summary>
    /// Representa un registro de la tabla MANTVIDEO en AS400.
    /// </summary>
    public class VideoModel
    {
        public string CODCCO { get; set; }
        public int CODVIDEO { get; set; }
        public string RUTA { get; set; }
        public string NOMBRE { get; set; }
        public string ESTADO { get; set; }
        public int SEQ { get; set; }
    }
}




builder.Services.AddScoped<IVideoQueryService, VideoQueryService>();
