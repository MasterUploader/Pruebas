[HttpGet]
[AutorizarPorTipoUsuario("1")]
public async Task<IActionResult> Index(int? page, int? codcco, int? editId)
{
    var agencias = await _agenciaService.ObtenerAgenciasAsync();

    // Filtro por agencia si aplica
    if (codcco.HasValue && codcco.Value > 0)
        agencias = agencias.Where(a => a.Codcco == codcco.Value).ToList();

    // Dropdown de filtro
    ViewBag.AgenciasFiltro = agencias
        .Select(a => new SelectListItem
        {
            Value = a.Codcco.ToString(),
            Text = $"{a.Codcco} - {a.NomAge}"
        })
        .OrderBy(a => a.Text)
        .ToList();

    ViewBag.CodccoSeleccionado = codcco;

    // Si se quiere editar una agencia, cargarla en el modelo
    AgenciaModel agenciaSeleccionada = new AgenciaModel();
    if (editId.HasValue)
        agenciaSeleccionada = agencias.FirstOrDefault(a => a.Codcco == editId.Value) ?? new AgenciaModel();

    ViewBag.Model = agenciaSeleccionada; // Este es el modelo editable

    int pageSize = 10;
    int pageNumber = page ?? 1;
    return View("Index", agencias.ToPagedList(pageNumber, pageSize));
}
