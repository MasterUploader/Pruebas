using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SitiosIntranet.Web.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        public IActionResult MenuPrincipal()
        {
            // Se obtiene el tipo de usuario desde el ClaimsPrincipal o Session si prefieres
            var tipoUsuario = HttpContext.Session.GetString("tipo_usuario");
            ViewData["TipoUsuario"] = tipoUsuario;

            return View();
        }
    }
}



@{
    ViewData["Title"] = "Menú Principal";
    var tipoUsuario = ViewData["TipoUsuario"]?.ToString();
}

<div class="container mt-4">
    <div class="d-flex justify-content-between align-items-center bg-primary text-white p-3 rounded">
        <h2 class="mb-0">Menú Principal</h2>
        <a asp-controller="Account" asp-action="Logout" class="btn btn-light">Cerrar Sesión</a>
    </div>

    <div class="row text-center mt-4">
        @switch (tipoUsuario)
        {
            case "1": // Super usuario
                <partial name="_MenuIcono" model='new MenuItem("/Mensajes", "Mensajes", "boton_mensajes.png")' />
                <partial name="_MenuIcono" model='new MenuItem("/Videos", "Videos", "boton_videos.png")' />
                <partial name="_MenuIcono" model='new MenuItem("/Configuracion", "Configuración", "configuracion.png")' />
                break;

            case "2": // Solo videos
                <partial name="_MenuIcono" model='new MenuItem("/Videos", "Videos", "boton_videos.png")' />
                break;

            case "3": // Solo mensajes
                <partial name="_MenuIcono" model='new MenuItem("/Mensajes", "Mensajes", "boton_mensajes.png")' />
                break;
        }
    </div>
</div>



@model SitiosIntranet.Web.Models.MenuItem

<div class="col-md-4 mb-4">
    <a href="@Model.Url" class="text-decoration-none">
        <img src="~/images/@Model.Imagen" class="img-fluid" style="max-height: 100px;" />
        <p class="mt-2 h5 text-dark">@Model.Titulo</p>
    </a>
</div>




namespace SitiosIntranet.Web.Models
{
    public class MenuItem
    {
        public string Url { get; set; }
        public string Titulo { get; set; }
        public string Imagen { get; set; }

        public MenuItem(string url, string titulo, string imagen)
        {
            Url = url;
            Titulo = titulo;
            Imagen = imagen;
        }
    }
}




using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SitiosIntranet.Web.Controllers
{
    [Authorize]
    public class MensajesController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}



@{
    ViewData["Title"] = "Mensajes";
}

<h2>Gestión de Mensajes</h2>
<p>Aquí irá la funcionalidad para agregar y listar mensajes.</p>



using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SitiosIntranet.Web.Controllers
{
    [Authorize]
    public class ConfiguracionController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}


@{
    ViewData["Title"] = "Configuración";
}

<h2>Configuración General</h2>
<p>Desde aquí se podrá administrar usuarios y agencias.</p>
