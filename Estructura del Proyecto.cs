// Services/LoginService.cs using SitiosIntranet.Web.Models; using SitiosIntranet.Web.Helpers; using System.Data; using IBM.Data.DB2.iSeries;

namespace SitiosIntranet.Web.Services { public class LoginService : ILoginService { public LoginResult ValidateUser(string username, string password) { var result = new LoginResult(); string query = $"SELECT TIPUSU, ESTADO, PASS FROM BCAH96DTA.USUADMIN WHERE USUARIO = '{username}'";

using var conn = new iDB2Connection("<cadena-conexion-AS400>");
        try
        {
            conn.Open();
            var adapter = new iDB2DataAdapter(query, conn);
            var table = new DataTable();
            adapter.Fill(table);

            if (table.Rows.Count == 0)
            {
                result.ErrorMessage = "Usuario Incorrecto";
                return result;
            }

            var tipoUsuario = table.Rows[0]["TIPUSU"].ToString();
            var estado = table.Rows[0]["ESTADO"].ToString();
            var passEncriptada = table.Rows[0]["PASS"].ToString();

            // Detecta y desencripta automáticamente
            string passDesencriptada = OperacionesVarias.DesencriptarAuto(passEncriptada);

            if (!password.Equals(passDesencriptada))
            {
                result.ErrorMessage = "Contraseña Incorrecta";
                return result;
            }

            if (estado != "A")
            {
                result.ErrorMessage = "Usuario Inhabilitado";
                return result;
            }

            // Migrar solo si es formato antiguo
            if (!OperacionesVarias.EsFormatoNuevo(passEncriptada))
            {
                string nuevoFormato = OperacionesVarias.EncriptarCadenaAES(password);
                string updateQuery = $"UPDATE BCAH96DTA.USUADMIN SET PASS = '{nuevoFormato}' WHERE USUARIO = '{username}'";

                using var cmd = new iDB2Command(updateQuery, conn);
                cmd.ExecuteNonQuery();
            }

            result.IsSuccessful = true;
            result.Username = username;
            result.TipoUsuario = tipoUsuario;
            return result;
        }
        catch (Exception ex)
        {
            result.ErrorMessage = ex.Message;
            return result;
        }
    }
}

}

