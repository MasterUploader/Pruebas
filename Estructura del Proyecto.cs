[HttpGet]
public async Task<IActionResult> Index(string codcco)
{
    ViewBag.CodccoSeleccionado = codcco;

    var agencias = _videoService.ObtenerAgenciasSelectList();
    var videos = await _videoService.ObtenerListaVideosAsync();

    if (!string.IsNullOrEmpty(codcco))
        videos = videos.Where(v => v.Codcco == codcco).ToList();

    ViewBag.Agencias = agencias;

    return View(videos);
}



<form method="get">
    <div class="row g-2 align-items-end">
        <div class="col-md-4">
            <label for="codcco" class="form-label">Seleccione Agencia:</label>
            <select id="codcco" name="codcco" class="form-select" required>
                <option value="">--Seleccione--</option>
                @foreach (var agencia in ViewBag.Agencias as List<SelectListItem>)
                {
                    var selected = (agencia.Value == ViewBag.CodccoSeleccionado) ? "selected" : "";
                    @:<option value="@agencia.Value" @selected>@agencia.Text</option>
                }
            </select>
        </div>
        <div class="col-auto">
            <button type="submit" class="btn btn-primary">Filtrar</button>
        </div>
    </div>
</form>
