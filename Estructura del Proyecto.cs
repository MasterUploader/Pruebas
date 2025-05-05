public async Task<IActionResult> Index()
{
    var agencias = await _videoService.ObtenerAgenciasSelectAsync(); // o el método correcto
    var videos = await _videoService.ObtenerListaVideosAsync();

    ViewBag.Agencias = agencias; // importante
    return View(videos); // Model = lista de videos
}



@{
    var agencias = ViewBag.Agencias as List<SelectListItem> ?? new List<SelectListItem>();
}

<select id="codcco" name="codcco" class="form-select" required>
    <option value="">Seleccione...</option>
    @foreach (var agencia in agencias)
    {
        <option value="@agencia.Value" selected="@(agencia.Value == codccoActual)"> @agencia.Text </option>
    }
</select>
