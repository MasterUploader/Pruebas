/// <summary>
        /// Lista los videos activos e inactivos de una agencia desde AS400.
        /// </summary>
        public List<VideoModel> ListarVideos(string codcco)
        {
            var lista = new List<VideoModel>();

            try
            {
                _as400.Open();
                using var command = _as400.GetDbCommand();

                command.CommandText = $@"
                    SELECT CODCCO, CODVIDEO, RUTA, NOMBRE, ESTADO, SEQ
                    FROM BCAH96DTA.MANTVIDEO
                    WHERE CODCCO = '{codcco}'
                    ORDER BY SEQ";

                using var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    lista.Add(new VideoModel
                    {
                        Codcco = reader["CODCCO"].ToString(),
                        CodVideo = Convert.ToInt32(reader["CODVIDEO"]),
                        Ruta = reader["RUTA"].ToString(),
                        Nombre = reader["NOMBRE"].ToString(),
                        Estado = reader["ESTADO"].ToString(),
                        Seq = Convert.ToInt32(reader["SEQ"]),
                        RutaFisica = Path.Combine(GlobalConnection.ConnectionConfig.ContenedorVideos, reader["NOMBRE"].ToString())
                    });
                }
            }
            catch
            {
                // Manejo opcional con logger
            }
            finally
            {
                _as400.Close();
            }

            return lista;
        }

        /// <summary>
        /// Actualiza el estado y secuencia de un video.
        /// </summary>
        public bool ActualizarVideo(VideoModel video)
        {
            try
            {
                _as400.Open();
                using var command = _as400.GetDbCommand();

                command.CommandText = $@"
                    UPDATE BCAH96DTA.MANTVIDEO
                    SET ESTADO = '{video.Estado}',
                        SEQ = {video.Seq}
                    WHERE CODCCO = '{video.Codcco}'
                      AND CODVIDEO = {video.CodVideo}";

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

        /// <summary>
        /// Elimina el registro del video en AS400.
        /// </summary>
        public bool EliminarVideo(int codVideo, string codcco)
        {
            try
            {
                _as400.Open();
                using var command = _as400.GetDbCommand();

                command.CommandText = $@"
                    DELETE FROM BCAH96DTA.MANTVIDEO
                    WHERE CODCCO = '{codcco}'
                      AND CODVIDEO = {codVideo}";

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

        /// <summary>
        /// Verifica si existen dependencias del video antes de eliminarlo.
        /// </summary>
        public bool TieneDependencias(string codcco, int codVideo)
        {
            try
            {
                _as400.Open();
                using var command = _as400.GetDbCommand();

                command.CommandText = $@"
                    SELECT COUNT(*)
                    FROM BCAH96DTA.OTRATABLA
                    WHERE CODCCO = '{codcco}'
                      AND CODVIDEO = {codVideo}";

                var count = Convert.ToInt32(command.ExecuteScalar());
                return count > 0;
            }
            catch
            {
                return true; // Si hay error, asumimos que tiene dependencias para prevenir borrado
            }
            finally
            {
                _as400.Close();
            }
        }

        /// <summary>
        /// Elimina el archivo físico del video del disco.
        /// </summary>
        public bool EliminarArchivoFisico(string rutaArchivo)
        {
            try
            {
                if (File.Exists(rutaArchivo))
                {
                    File.Delete(rutaArchivo);
                    return true;
                }
                return false;
            }
            catch
            {
                return false;
            }
        }
    }
