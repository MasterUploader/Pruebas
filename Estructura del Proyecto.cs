using CAUAdministracion.Models;
using Connections.Helpers;
using Connections.Interfaces;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Data;
using System.Data.Common;

namespace CAUAdministracion.Services.Mensajes
{
    /// <summary>
    /// Servicio para la gestión de mensajes (tabla MANTMSG) en AS400.
    /// </summary>
    public class MensajeService : IMensajeService
    {
        private readonly IDatabaseConnection _as400;

        public MensajeService(IDatabaseConnection as400)
        {
            _as400 = as400;
        }

        /// <summary>
        /// Obtiene todas las agencias que tienen marquesina activada, en formato SelectListItem.
        /// </summary>
        public List<SelectListItem> ObtenerAgenciasSelectList()
        {
            var agencias = new List<SelectListItem>();
            try
            {
                _as400.Open();
                using var command = _as400.GetDbCommand();

                command.CommandText = @"
                    SELECT CODCCO, NOMAGE 
                    FROM BCAH96DTA.RSAGE01 
                    WHERE MARQUESINA = 'SI' 
                    ORDER BY NOMAGE";

                using var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    agencias.Add(new SelectListItem
                    {
                        Value = reader["CODCCO"].ToString(),
                        Text = reader["NOMAGE"].ToString()
                    });
                }
            }
            catch (Exception ex)
            {
                agencias.Clear();
                agencias.Add(new SelectListItem
                {
                    Value = "",
                    Text = "Error: " + ex.Message
                });
            }
            finally
            {
                _as400.Close();
            }

            return agencias;
        }

        /// <summary>
        /// Lista los mensajes filtrados por código de agencia.
        /// </summary>
        public List<MensajeModel> ListarMensajes(string codcco)
        {
            var lista = new List<MensajeModel>();
            try
            {
                _as400.Open();
                using var command = _as400.GetDbCommand();

                command.CommandText = $@"
                    SELECT CODMSG, SEQ, MENSAJE, ESTADO 
                    FROM BCAH96DTA.MANTMSG 
                    WHERE CODCCO = '{codcco}'
                    ORDER BY SEQ";

                using var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    lista.Add(new MensajeModel
                    {
                        Codcco = codcco,
                        CodMsg = Convert.ToInt32(reader["CODMSG"]),
                        Seq = Convert.ToInt32(reader["SEQ"]),
                        Mensaje = reader["MENSAJE"].ToString(),
                        Estado = reader["ESTADO"].ToString()
                    });
                }
            }
            catch
            {
                // Error controlado, se puede loguear si se desea
            }
            finally
            {
                _as400.Close();
            }

            return lista;
        }

        /// <summary>
        /// Elimina un mensaje de la tabla MANTMSG por su ID.
        /// </summary>
        public bool EliminarMensaje(int codMsg)
        {
            try
            {
                _as400.Open();
                using var command = _as400.GetDbCommand();

                command.CommandText = $@"
                    DELETE FROM BCAH96DTA.MANTMSG 
                    WHERE CODMSG = {codMsg}";

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
        /// Actualiza un mensaje existente.
        /// </summary>
        public bool ActualizarMensaje(MensajeModel mensaje)
        {
            try
            {
                _as400.Open();
                using var command = _as400.GetDbCommand();

                command.CommandText = $@"
                    UPDATE BCAH96DTA.MANTMSG
                    SET SEQ = {mensaje.Seq}, 
                        MENSAJE = '{mensaje.Mensaje}', 
                        ESTADO = '{mensaje.Estado}'
                    WHERE CODMSG = {mensaje.CodMsg} 
                      AND CODCCO = '{mensaje.Codcco}'";

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
        /// Obtiene el siguiente valor de CODMSG (MAX + 1).
        /// </summary>
        public int ObtenerSiguienteId()
        {
            try
            {
                _as400.Open();
                using var command = _as400.GetDbCommand();

                command.CommandText = "SELECT MAX(CODMSG) FROM BCAH96DTA.MANTMSG";
                var result = command.ExecuteScalar();

                return result != DBNull.Value ? Convert.ToInt32(result) + 1 : 1;
            }
            catch
            {
                return 1;
            }
            finally
            {
                _as400.Close();
            }
        }

        /// <summary>
        /// Inserta un nuevo mensaje en la base de datos.
        /// </summary>
        public bool InsertarMensaje(MensajeModel mensaje)
        {
            try
            {
                _as400.Open();
                using var command = _as400.GetDbCommand();

                int nuevoId = ObtenerSiguienteId();

                command.CommandText = $@"
                    INSERT INTO BCAH96DTA.MANTMSG (CODCCO, CODMSG, SEQ, MENSAJE, ESTADO) 
                    VALUES ('{mensaje.Codcco}', {nuevoId}, {mensaje.Seq}, '{mensaje.Mensaje}', '{mensaje.Estado}')";

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
