[HttpPost]
[AutorizarPorTipoUsuario("1")]
[ValidateAntiForgeryToken]
public async Task<IActionResult> GuardarEdicion(AgenciaModel model)
{
    if (!ModelState.IsValid)
    {
        TempData["Mensaje"] = "Datos inválidos, por favor revise el formulario.";
    }
    else
    {
        // Asegura que se interpreten correctamente los valores de los checkboxes
        model.Marquesina = model.MarqCheck ? "SI" : "NO";
        model.RstBranch = model.RstCheck ? "SI" : "NO";

        var actualizado = _agenciaService.ActualizarAgencia(model);
        TempData["Mensaje"] = actualizado
            ? "Agencia actualizada correctamente."
            : "Ocurrió un error al actualizar.";
    }

    // Recuperar todas las agencias y reconstruir el ViewBag
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
