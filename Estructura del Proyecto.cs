public async Task<IActionResult> GuardarEdicion(IFormCollection form)
{
    var model = new AgenciaModel
    {
        Codcco = int.Parse(form["Codcco"]),
        NomAge = form["NomAge"],
        Zona = int.Parse(form["Zona"]),
        IpSer = form["IpSer"],
        NomSer = form["NomSer"],
        NomBD = form["NomBD"],
        Marquesina = form["Marquesina"] == "SI" ? "SI" : "NO",
        RstBranch = form["RstBranch"] == "SI" ? "SI" : "NO"
    };

    var actualizado = _agenciaService.ActualizarAgencia(model);
    TempData["Mensaje"] = actualizado
        ? "Agencia actualizada correctamente."
        : "Ocurrió un error al actualizar.";

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
