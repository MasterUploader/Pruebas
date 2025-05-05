[HttpGet]
public async Task<IActionResult> Index(string codcco, int page = 1, int pageSize = 10)
{
    var agencias = _videoService.ObtenerAgenciasSelectList();

    if (agencias == null || agencias.Count == 0)
    {
        ViewBag.Mensaje = "No se pudieron cargar las agencias.";
        return View(new List<VideoModel>());
    }

    var todosLosVideos = await _videoService.ObtenerListaVideosAsync();

    // Filtrar por agencia si se especificó
    if (!string.IsNullOrWhiteSpace(codcco))
        todosLosVideos = todosLosVideos.Where(v => v.Codcco == codcco).ToList();

    // Calcular paginación
    var totalRegistros = todosLosVideos.Count;
    var totalPaginas = (int)Math.Ceiling(totalRegistros / (double)pageSize);

    var videosPaginados = todosLosVideos
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .ToList();

    // Enviar info adicional a la vista
    ViewBag.Agencias = agencias;
    ViewBag.PaginaActual = page;
    ViewBag.TotalPaginas = totalPaginas;
    ViewBag.CodccoSeleccionado = codcco;

    return View(videosPaginados);
}
