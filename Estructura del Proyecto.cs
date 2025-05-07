using X.PagedList;

public async Task<IActionResult> Index(int? page)
{
    var agencias = await _agenciaService.ObtenerAgenciasAsync();

    int pageSize = 10;
    int pageNumber = page ?? 1;

    var pagedAgencias = agencias.ToPagedList(pageNumber, pageSize);

    return View(pagedAgencias);
}
