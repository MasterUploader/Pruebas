[HttpGet]
public async Task<IActionResult> Editar(int id)
{
    var agencia = await _agenciaService.ObtenerAgenciaPorIdAsync(id);
    if (agencia == null)
    {
        TempData["Mensaje"] = "Agencia no encontrada.";
        return RedirectToAction("Index");
    }

    return View(agencia);
}


[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> Editar(AgenciaModel model)
{
    if (!ModelState.IsValid)
    {
        return View(model);
    }

    var resultado = await _agenciaService.ActualizarAgenciaAsync(model);

    if (resultado)
    {
        TempData["Mensaje"] = "Agencia actualizada exitosamente.";
        return RedirectToAction("Index");
    }
    else
    {
        ModelState.AddModelError("", "Error al actualizar la agencia.");
        return View(model);
    }
}
