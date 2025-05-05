using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Claims;

public class AutorizarPorTipoUsuarioAttribute : Attribute, IAuthorizationFilter
{
    private readonly string[] _tiposPermitidos;

    public AutorizarPorTipoUsuarioAttribute(params string[] tiposPermitidos)
    {
        _tiposPermitidos = tiposPermitidos;
    }

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var tipoUsuario = context.HttpContext.User.Claims
            .FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;

        if (tipoUsuario == null || !_tiposPermitidos.Contains(tipoUsuario))
        {
            // Redirige a una vista de acceso denegado si el tipo no es válido
            context.Result = new RedirectToRouteResult(
                new RouteValueDictionary(new { controller = "Home", action = "NoAutorizado" }));
        }
    }
}


public IActionResult NoAutorizado()
{
    return View();
}


<h2 class="text-danger">Acceso denegado</h2>
<p>No tienes permisos para acceder a esta sección.</p>


[AutorizarPorTipoUsuario("1", "3")]
public class VideosController : Controller
{
    // tus métodos aquí...
}
