[HttpGet]
[AutorizarPorTipoUsuario("1")]
public async Task<IActionResult> Index(int? page, int? codcco, int? editId)
{
    var agencias = await _agenciaService.ObtenerAgenciasAsync();

    // Generar lista de agencias para el filtro
    var listaSelect = agencias.Select(a => new SelectListItem
    {
        Value = a.Codcco.ToString(),
        Text = $"{a.Codcco} - {a.NomAge}"
    }).ToList();

    // Filtrar si se seleccionó una agencia
    if (codcco.HasValue && codcco.Value > 0)
        agencias = agencias.Where(a => a.Codcco == codcco.Value).ToList();

    // Obtener agencia en edición si corresponde
    AgenciaModel? agenciaEnEdicion = null;
    if (editId.HasValue)
        agenciaEnEdicion = agencias.FirstOrDefault(a => a.Codcco == editId.Value);

    // Paginación
    int pageNumber = page ?? 1;
    int pageSize = 10;

    // Construir el ViewModel completo
    var modelo = new AgenciaIndexViewModel
    {
        Lista = agencias.ToPagedList(pageNumber, pageSize),
        AgenciaEnEdicion = agenciaEnEdicion,
        CodccoSeleccionado = codcco?.ToString(),
        AgenciasFiltro = listaSelect // Asegurado aquí
    };

    return View("Index", modelo);
}

<select name="codcco" class="form-select" style="width: 300px; display:inline-block;" onchange="this.form.submit()">
    <option value="">-- Seleccione Agencia --</option>
    @foreach (var agencia in Model.AgenciasFiltro)
    {
        var selected = (agencia.Value == Model.CodccoSeleccionado) ? "selected" : "";
        @:<option value="@agencia.Value" @selected>@agencia.Text</option>
    }
</select>
