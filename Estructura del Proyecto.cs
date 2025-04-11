// Controllers/HomeController.cs using Microsoft.AspNetCore.Authorization; using Microsoft.AspNetCore.Mvc;

namespace SitiosIntranet.Web.Controllers { [Authorize] public class HomeController : Controller { public IActionResult Index() { return View(); } } }

// Views/Home/Index.cshtml @{ ViewData["Title"] = "Inicio"; }

<div class="text-center">
    <h1 class="display-5">Bienvenido a Sitios Intranet</h1>
    <p class="lead">Desde aquí puedes acceder a los módulos disponibles según tu perfil.</p>
</div>
