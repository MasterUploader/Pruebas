[HttpGet]
[AutorizarPorTipoUsuario("1")]
public async Task<IActionResult> Index(int? page, int? codcco)
{
    var agencias = await _agenciaService.ObtenerAgenciasAsync();

    // Filtro por código de agencia si se selecciona
    if (codcco.HasValue && codcco.Value > 0)
        agencias = agencias.Where(a => a.Codcco == codcco.Value).ToList();

    // Generar lista desplegable para el filtro
    ViewBag.AgenciasFiltro = agencias
        .Select(a => new SelectListItem
        {
            Value = a.Codcco.ToString(),
            Text = $"{a.Codcco} - {a.NomAge}"
        })
        .OrderBy(a => a.Text)
        .ToList();

    ViewBag.CodccoSeleccionado = codcco;

    int pageSize = 10;
    int pageNumber = page ?? 1;
    return View(agencias.ToPagedList(pageNumber, pageSize));
}


<form method="get" asp-action="Index" class="row mb-3">
    <div class="col-md-4">
        <label for="codcco" class="form-label">Filtrar por Agencia</label>
        <select id="codcco" name="codcco" class="form-select" onchange="this.form.submit()">
            <option value="">-- Todas las Agencias --</option>
            @foreach (var agencia in (List<SelectListItem>)ViewBag.AgenciasFiltro)
            {
                <option value="@agencia.Value" @(ViewBag.CodccoSeleccionado?.ToString() == agencia.Value ? "selected" : "")>
                    @agencia.Text
                </option>
            }
        </select>
    </div>
</form>


@Html.PagedListPager(
    Model,
    page => Url.Action("Index", new { page, codcco = ViewBag.CodccoSeleccionado }),
    new PagedListRenderOptions
    {
        UlElementClasses = new[] { "pagination", "justify-content-center" },
        LiElementClasses = new[] { "page-item" },
        PageClasses = new[] { "page-link" },
        DisplayLinkToFirstPage = PagedListDisplayMode.Always,
        DisplayLinkToLastPage = PagedListDisplayMode.Always,
        DisplayLinkToPreviousPage = PagedListDisplayMode.Always,
        DisplayLinkToNextPage = PagedListDisplayMode.Always,
        MaximumPageNumbersToDisplay = 5
    }
)
