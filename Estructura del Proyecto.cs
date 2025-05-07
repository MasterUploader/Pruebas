[HttpGet]
[AutorizarPorTipoUsuario("1")]
public async Task<IActionResult> Index(int? page, int? codcco, int? editId)
{
    var agencias = await _agenciaService.ObtenerAgenciasAsync();

    if (codcco.HasValue && codcco.Value > 0)
        agencias = agencias.Where(a => a.Codcco == codcco.Value).ToList();

    ViewBag.AgenciasFiltro = agencias
        .Select(a => new SelectListItem
        {
            Value = a.Codcco.ToString(),
            Text = $"{a.Codcco} - {a.NomAge}"
        })
        .OrderBy(a => a.Text)
        .ToList();

    ViewBag.CodccoSeleccionado = codcco;
    ViewBag.EditId = editId; // <<=== Este es el que activa el modo edición

    int pageSize = 10;
    int pageNumber = page ?? 1;
    return View(agencias.ToPagedList(pageNumber, pageSize));
}


<a asp-action="Index" asp-route-editId="@item.Codcco" class="btn btn-sm btn-warning me-1">Editar</a>
