using System.Data.Common;
using SitiosIntranet.Web.Models;
using SitiosIntranet.Web.Helpers;
using RestUtilities.Connections.Interfaces;

namespace SitiosIntranet.Web.Services
{
    /// <summary>
    /// Servicio de autenticación utilizando comandos SQL directos en AS400.
    /// </summary>
    public class LoginService : ILoginService
    {
        private readonly IDatabaseConnection _as400;

        public LoginService(IDatabaseConnection as400)
        {
            _as400 = as400;
        }

        public LoginResult ValidateUser(string username, string password)
        {
            var result = new LoginResult();
            _as400.Open();

            try
            {
                // Usa método GetDbCommand para conexión directa desde la librería
                using var command = _as400.GetDbCommand();

                // Consulta SQL directa para verificar credenciales
                command.CommandText = $"SELECT TIPUSU, ESTADO, PASS FROM BCAH96DTA.USUADMIN WHERE USUARIO = '{username}'";

                if (command.Connection.State == System.Data.ConnectionState.Closed)
                    command.Connection.Open();

                Usuario datos = null;

                using var reader = command.ExecuteReader();
                if (reader.Read())
                {
                    datos = new Usuario
                    {
                        TIPUSU = reader["TIPUSU"].ToString(),
                        ESTADO = reader["ESTADO"].ToString(),
                        PASS = reader["PASS"].ToString()
                    };
                }

                if (datos == null)
                {
                    result.ErrorMessage = "Usuario Incorrecto";
                    return result;
                }

                var passDesencriptada = OperacionesVarias.DesencriptarAuto(datos.PASS);

                if (!password.Equals(passDesencriptada))
                {
                    result.ErrorMessage = "Contraseña Incorrecta";
                    return result;
                }

                if (datos.ESTADO != "A")
                {
                    result.ErrorMessage = "Usuario Inhabilitado";
                    return result;
                }

                // Migración de contraseña si el formato es antiguo
                if (!OperacionesVarias.EsFormatoNuevo(datos.PASS))
                {
                    var nuevoFormato = OperacionesVarias.EncriptarCadenaAES(password);
                    using var updateCommand = _as400.GetDbCommand();
                    updateCommand.CommandText = $"UPDATE BCAH96DTA.USUADMIN SET PASS = '{nuevoFormato}' WHERE USUARIO = '{username}'";
                    updateCommand.Connection.Open();
                    updateCommand.ExecuteNonQuery();
                }

                result.IsSuccessful = true;
                result.Username = username;
                result.TipoUsuario = datos.TIPUSU;
                return result;
            }
            catch (Exception ex)
            {
                result.ErrorMessage = ex.Message;
                return result;
            }
            finally
            {
                _as400.Close();
            }
        }
    }
}
