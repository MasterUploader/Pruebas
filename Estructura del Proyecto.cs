// Usando RestUtilities.Connection y Microsoft.EntityFrameworkCore

using Microsoft.EntityFrameworkCore;
using SitiosIntranet.Web.Helpers;
using SitiosIntranet.Web.Models;
using RestUtilities.Connections.Interfaces;

namespace SitiosIntranet.Web.Services
{
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
                var context = _as400.GetDbContext();

                var query = $"SELECT TIPUSU, ESTADO, PASS FROM BCAH96DTA.USUADMIN WHERE USUARIO = '{username}'";

                var datos = context.Usuarios
                    .FromSqlRaw(query)
                    .AsEnumerable()
                    .FirstOrDefault();

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

                // Migrar si es formato antiguo
                if (!OperacionesVarias.EsFormatoNuevo(datos.PASS))
                {
                    var nuevoFormato = OperacionesVarias.EncriptarCadenaAES(password);
                    context.Database.ExecuteSqlRaw($@"
                        UPDATE BCAH96DTA.USUADMIN SET PASS = '{nuevoFormato}' WHERE USUARIO = '{username}'
                    ");
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
