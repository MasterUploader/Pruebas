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

// Models/UserLogin.cs using System.ComponentModel.DataAnnotations;

namespace SitiosIntranet.Web.Models { public class UserLogin { [Required] public string Username { get; set; }

[Required]
    [DataType(DataType.Password)]
    public string Password { get; set; }
}

public class LoginResult
{
    public bool IsSuccessful { get; set; }
    public string ErrorMessage { get; set; }
    public string Username { get; set; }
    public string TipoUsuario { get; set; }
}

}

// Services/ILoginService.cs namespace SitiosIntranet.Web.Services { public interface ILoginService { LoginResult ValidateUser(string username, string password); } }

// Services/LoginService.cs using SitiosIntranet.Web.Models; using System.Data; using IBM.Data.DB2.iSeries;

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

// Helpers/OperacionesVarias.cs namespace SitiosIntranet.Web.Helpers { public static class OperacionesVarias { public static string DesencriptarCadena(string cadenaEncriptada) { // Simulación de desencriptación (reemplaza por la lógica real) return cadenaEncriptada; // ejemplo: return AESDecrypt(cadenaEncriptada); } } }

// Controllers/AccountController.cs using Microsoft.AspNetCore.Mvc; using Microsoft.AspNetCore.Authentication; using Microsoft.AspNetCore.Authentication.Cookies; using System.Security.Claims; using System.Threading.Tasks; using SitiosIntranet.Web.Models; using SitiosIntranet.Web.Services;

namespace SitiosIntranet.Web.Controllers { public class AccountController : Controller { private readonly ILoginService _loginService;

public AccountController(ILoginService loginService)
    {
        _loginService = loginService;
    }

    [HttpGet]
    public IActionResult Login(string returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Login(UserLogin model, string returnUrl = null)
    {
        if (ModelState.IsValid)
        {
            var result = _loginService.ValidateUser(model.Username, model.Password);

            if (result.IsSuccessful)
            {
                var claims = new[]
                {
                    new Claim(ClaimTypes.Name, result.Username),
                    new Claim("TipoUsuario", result.TipoUsuario)
                };

                var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var principal = new ClaimsPrincipal(identity);

                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
                return RedirectToLocal(returnUrl);
            }

            ModelState.AddModelError("", result.ErrorMessage);
        }
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Login");
    }

    private IActionResult RedirectToLocal(string returnUrl)
    {
        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);
        else
            return RedirectToAction("Index", "Home");
    }
}

}

