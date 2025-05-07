ViewBag.AgenciasFiltro = agencias
    .Select(a => new SelectListItem
    {
        Value = a.Codcco.ToString(),
        Text = $"{a.Codcco} - {a.NomAge}"
    })
    .OrderBy(a => a.Text)
    .ToList();

ViewBag.CodccoSeleccionado = null;



[HttpPost]
[AutorizarPorTipoUsuario("1")]
public async Task<IActionResult> GuardarEdicion(AgenciaModel model)
{
    if (ModelState.IsValid)
    {
        var actualizado = _agenciaService.ActualizarAgencia(model);
        TempData["Mensaje"] = actualizado
            ? "Agencia actualizada correctamente."
            : "Ocurrió un error al actualizar.";
    }

    var agencias = await _agenciaService.ObtenerAgenciasAsync();

    ViewBag.AgenciasFiltro = agencias
        .Select(a => new SelectListItem
        {
            Value = a.Codcco.ToString(),
            Text = $"{a.Codcco} - {a.NomAge}"
        })
        .OrderBy(a => a.Text)
        .ToList();

    ViewBag.CodccoSeleccionado = null;

    return View("Index", agencias.ToPagedList(1, 50));
}
