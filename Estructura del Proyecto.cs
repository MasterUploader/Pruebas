[HttpPost]
public async Task<IActionResult> GuardarEdicion(AgenciaModel model)
{
    if (!ModelState.IsValid)
    {
        var agencias = await _agenciaService.ObtenerAgenciasAsync();
        var modelo = new AgenciaIndexViewModel
        {
            Lista = agencias.ToPagedList(1, 10),
            AgenciaEnEdicion = model,
            CodccoSeleccionado = model.Codcco.ToString()
        };

        ViewBag.AgenciasFiltro = await _agenciaService.ObtenerAgenciasFiltroAsync(); // << ASEGURA ESTO

        return View("Index", modelo);
    }

    await _agenciaService.ActualizarAgenciaAsync(model);
    TempData["Mensaje"] = "Agencia actualizada correctamente.";
    return RedirectToAction("Index");
}
