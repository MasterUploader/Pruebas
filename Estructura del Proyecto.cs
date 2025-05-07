[HttpPost]
[AutorizarPorTipoUsuario("1")]
[ValidateAntiForgeryToken]
public async Task<IActionResult> GuardarEdicion(AgenciaIndexViewModel model)
{
    var agencia = model.AgenciaEnEdicion;

    if (!ModelState.IsValid)
    {
        var agencias = await _agenciaService.ObtenerAgenciasAsync();

        model.Lista = agencias.ToPagedList(1, 10);
        model.AgenciasFiltro = agencias.Select(a => new SelectListItem
        {
            Value = a.Codcco.ToString(),
            Text = $"{a.Codcco} - {a.NomAge}"
        }).ToList();

        return View("Index", model);
    }

    var actualizado = _agenciaService.ActualizarAgencia(agencia);

    TempData["Mensaje"] = actualizado
        ? "Agencia actualizada correctamente."
        : "Ocurrió un error al actualizar.";

    return RedirectToAction("Index");
}
