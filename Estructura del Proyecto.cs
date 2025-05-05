using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

public class AuthorizeTipoUsuarioAttribute : Attribute, IAuthorizationFilter
{
    private readonly string[] _tiposPermitidos;

    public AuthorizeTipoUsuarioAttribute(params string[] tiposPermitidos)
    {
        _tiposPermitidos = tiposPermitidos;
    }

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var tipoUsuario = context.HttpContext.User.Claims
            .FirstOrDefault(c => c.Type == "TipoUsuario")?.Value;

        if (string.IsNullOrEmpty(tipoUsuario) || !_tiposPermitidos.Contains(tipoUsuario))
        {
            context.Result = new RedirectToActionResult("NoAutorizado", "Home", null);
        }
    }
}

@{
    var tipo = User.FindFirst("TipoUsuario")?.Value;
    string tipoTexto = tipo switch
    {
        "1" => "Administrador",
        "2" => "Editor",
        "3" => "Consulta",
        _ => "Desconocido"
    };
}
<h4>Bienvenido, tipo de usuario: @tipoTexto</h4>
