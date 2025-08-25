// GET: /Usuarios/Actualizar/123
[HttpGet]
public IActionResult Actualizar(int id)
{
    var model = _usuarioService.ObtenerPorId(id);
    if (model == null) return NotFound();
    return View(model); // Views/Usuarios/Actualizar.cshtml
}

// POST: /Usuarios/Actualizar
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> Actualizar(UsuarioEditModel model)
{
    if (!ModelState.IsValid) return View(model);

    var ok = await _usuarioService.Actualizar(model);
    TempData["Mensaje"] = ok ? "Usuario actualizado." : "No se pudo actualizar.";
    TempData["MensajeTipo"] = ok ? "success" : "danger";
    return RedirectToAction("Index");
}
