// Program.cs using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container builder.Services.AddControllersWithViews();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme) .AddCookie(options => { options.LoginPath = "/Account/Login"; options.LogoutPath = "/Account/Logout"; });

builder.Services.AddSession(); builder.Services.AddScoped<ILoginService, LoginService>();

var app = builder.Build();

// Configure the HTTP request pipeline if (!app.Environment.IsDevelopment()) { app.UseExceptionHandler("/Home/Error"); app.UseHsts(); }

app.UseHttpsRedirection(); app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication(); app.UseAuthorization(); app.UseSession();

app.MapControllerRoute( name: "default", pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

// Helpers/OperacionesVarias.cs using System; using System.Text;

namespace SitiosIntranet.Web.Helpers { /// <summary> /// Clase utilitaria para encriptar y desencriptar cadenas compatibles con el sistema anterior. /// </summary> public static class OperacionesVarias { public static string EncriptarCadena(string cadenaEncriptar) { byte[] encrypted = Encoding.Unicode.GetBytes(cadenaEncriptar); return Convert.ToBase64String(encrypted); }

public static string DesencriptarCadena(string cadenaDesencriptar)
    {
        try
        {
            byte[] decrypted = Convert.FromBase64String(cadenaDesencriptar);
            return Encoding.Unicode.GetString(decrypted);
        }
        catch
        {
            return string.Empty;
        }
    }
}

}

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

            string passDesencriptada = OperacionesVarias.DesencriptarCadena(passEncriptada);

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

            // Si validó correctamente con formato viejo, migrar al nuevo formato (encriptado fuerte si se desea)
            string nuevoFormato = OperacionesVarias.EncriptarCadena(password); // mismo método actual (puedes cambiarlo luego)
            string updateQuery = $"UPDATE BCAH96DTA.USUADMIN SET PASS = '{nuevoFormato}' WHERE USUARIO = '{username}'";

            using var cmd = new iDB2Command(updateQuery, conn);
            cmd.ExecuteNonQuery();

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

