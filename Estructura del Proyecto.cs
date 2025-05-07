// GET: Agencias/Agregar
[HttpGet]
public IActionResult Agregar()
{
    return View(new AgenciaModel());
}

// POST: Agencias/Agregar
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> Agregar(AgenciaModel model)
{
    if (!ModelState.IsValid)
        return View(model);

    var existe = await _agenciaService.ExisteCentroCostoAsync(model.Codcco);
    if (existe)
    {
        ModelState.AddModelError("Codcco", "Ya existe una agencia con ese centro de costo.");
        return View(model);
    }

    try
    {
        await _agenciaService.AgregarAgenciaAsync(model);
        TempData["Mensaje"] = "Agencia agregada correctamente.";
        return RedirectToAction("Index");
    }
    catch (Exception ex)
    {
        ModelState.AddModelError(string.Empty, $"Error al guardar: {ex.Message}");
        return View(model);
    }
}


// GET: Agencias/Editar/5
[HttpGet]
public async Task<IActionResult> Editar(int id)
{
    var agencia = await _agenciaService.ObtenerPorIdAsync(id);
    if (agencia == null)
        return NotFound();

    return View(agencia);
}

// POST: Agencias/Editar
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> Editar(AgenciaModel model)
{
    if (!ModelState.IsValid)
        return View(model);

    try
    {
        await _agenciaService.EditarAgenciaAsync(model);
        TempData["Mensaje"] = "Agencia actualizada correctamente.";
        return RedirectToAction("Index");
    }
    catch (Exception ex)
    {
        ModelState.AddModelError(string.Empty, $"Error al actualizar: {ex.Message}");
        return View(model);
    }
}
