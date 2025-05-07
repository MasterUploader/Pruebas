[HttpPost]
public async Task<IActionResult> GuardarEdicion(AgenciaModel model)
{
    if (!ModelState.IsValid)
    {
        // Recargar la vista con los datos anteriores si hay error de validación
        var lista = await _agenciaService.ObtenerAgenciasAsync();
        var viewModel = new AgenciaIndexViewModel
        {
            Lista = lista.ToPagedList(1, 10),
            AgenciaEnEdicion = model,
            CodccoSeleccionado = model.Codcco.ToString()
        };
        return View("Index", viewModel);
    }

    await _agenciaService.ActualizarAgenciaAsync(model);
    TempData["Mensaje"] = "Agencia actualizada correctamente.";
    return RedirectToAction("Index");
}
