using X.PagedList;

[HttpGet]
public async Task<IActionResult> Index(int? page, string codcco)
{
    var agencias = _videoService.ObtenerAgenciasSelectList();
    var videos = string.IsNullOrEmpty(codcco)
        ? await _videoService.ObtenerListaVideosAsync()
        : _videoService.ListarVideos(codcco);

    int pageSize = 10;
    int pageNumber = page ?? 1;

    ViewBag.Agencias = agencias;
    ViewBag.CodccoSeleccionado = codcco;

    return View(videos.ToPagedList(pageNumber, pageSize));
}

@using X.PagedList.Mvc.Core
@using X.PagedList

<!-- Tu tabla aquí -->

<div class="text-center mt-3">
    @Html.PagedListPager((IPagedList)Model, page => Url.Action("Index", new { page, codcco = ViewBag.CodccoSeleccionado }))
</div>
