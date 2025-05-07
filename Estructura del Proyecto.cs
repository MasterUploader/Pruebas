[HttpPost]
[ValidateAntiForgeryToken]
[AutorizarPorTipoUsuario("1")]
public async Task<IActionResult> GuardarEdicion(AgenciaModel model)
{
    if (!ModelState.IsValid)
    {
        // Si el modelo no es válido, se reconstruye el view model
        var agencias = await _agenciaService.ObtenerAgenciasAsync();

        var viewModel = new AgenciaIndexViewModel
        {
            AgenciaEnEdicion = model,
            Lista = agencias.ToPagedList(1, 10),
            CodccoSeleccionado = null,
            AgenciasFiltro = agencias.Select(a => new SelectListItem
            {
                Value = a.Codcco.ToString(),
                Text = $"{a.Codcco} - {a.NomAge}"
            }).ToList()
        };

        return View("Index", viewModel);
    }

    // Actualiza la agencia
    var actualizado = _agenciaService.ActualizarAgencia(model);

    TempData["Mensaje"] = actualizado
        ? "Agencia actualizada correctamente."
        : "Ocurrió un error al actualizar.";

    // Redirige a Index limpio tras guardar
    return RedirectToAction("Index");
}
