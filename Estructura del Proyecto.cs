public async Task<IActionResult> Index(int? codcco, int? page, int? editId)
{
    int pageSize = 10;
    int pageNumber = page ?? 1;

    var agencias = await _agenciaService.ObtenerAgenciasAsync(); // tu lógica

    if (codcco.HasValue)
    {
        agencias = agencias.Where(a => a.Codcco == codcco).ToList();
    }

    ViewBag.CodccoSeleccionado = codcco;
    ViewBag.AgenciasFiltro = agencias
        .Select(a => new SelectListItem { Value = a.Codcco.ToString(), Text = a.NomAge })
        .DistinctBy(x => x.Value)
        .ToList();

    ViewBag.Lista = agencias.ToPagedList(pageNumber, pageSize);
    ViewBag.EditId = editId;

    return View(new AgenciaModel()); // necesario para el Model en modo edición
}
