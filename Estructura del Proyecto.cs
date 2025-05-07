var agencias = await _agenciaService.ObtenerAgenciasAsync();
ViewBag.AgenciasFiltro = agencias
    .Select(a => new SelectListItem
    {
        Value = a.Codcco.ToString(),
        Text = a.NomAge
    }).ToList();
