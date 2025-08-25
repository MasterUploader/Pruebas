[Authorize]
public class UsuariosController : Controller
{
    // GET: /Usuarios/Editar/123
    [HttpGet]
    public IActionResult Editar(int id)
    {
        var usuario = _usuarioService.ObtenerPorId(id);
        if (usuario == null)
            return NotFound(); // 404 real si no existe en DB

        return View(usuario); // Busca Views/Usuarios/Editar.cshtml
    }

    // POST: /Usuarios/Editar
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Editar(UsuarioEditModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var ok = _usuarioService.Actualizar(model);
        TempData["Mensaje"] = ok ? "Usuario actualizado." : "No se pudo actualizar.";
        TempData["MensajeTipo"] = ok ? "success" : "danger";
        return RedirectToAction("Index");
    }
}


<a asp-controller="Usuarios"
   asp-action="Editar"
   asp-route-id="@usuario.Id">
   Editar
</a>




