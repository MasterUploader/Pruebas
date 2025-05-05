[Authorize]
public class VideosController : Controller
{
    public IActionResult Index()
    {
        var tipoUsuario = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;

        if (tipoUsuario != "3") // solo tipo 3 tiene acceso
        {
            return Forbid(); // o RedirectToAction("NoAutorizado")
        }

        return View();
    }
}
