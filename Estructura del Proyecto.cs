[HttpPost]
public async Task<IActionResult> GuardarEdicion(AgenciaModel model)
{
    if (ModelState.IsValid)
    {
        var actualizado = await _agenciaService.ActualizarAgenciaAsync(model);
        if (actualizado)
            TempData["Mensaje"] = "Agencia actualizada correctamente.";
        else
            TempData["Mensaje"] = "Ocurrió un error al actualizar.";
    }

    var agencias = await _agenciaService.ObtenerAgenciasAsync();
    return View("Index", agencias.ToPagedList(1, 50)); // Asegúrate de mantener la paginación si aplica
}
