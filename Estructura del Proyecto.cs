@using X.PagedList
@model CAUAdministracion.Models.AgenciaModel
@{
    ViewData["Title"] = "Mantenimiento de Agencias";
    var lista = (IPagedList<CAUAdministracion.Models.AgenciaModel>)ViewBag.Lista;
}

<h2 class="text-danger mb-4">@ViewData["Title"]</h2>

@if (TempData["Mensaje"] != null)
{
    <div class="alert alert-info alert-dismissible fade show" role="alert">
        @TempData["Mensaje"]
        <button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Close"></button>
    </div>
}

<!-- Filtro -->
<form method="get" asp-action="Index" class="row mb-3">
    <div class="col-md-4">
        <label for="codcco" class="form-label">Filtrar por Agencia</label>
        <select id="codcco" name="codcco" class="form-select" onchange="this.form.submit()">
            <option value="">-- Todas las Agencias --</option>
            @foreach (var agencia in (List<SelectListItem>)ViewBag.AgenciasFiltro)
            {
                var selected = ViewBag.CodccoSeleccionado?.ToString() == agencia.Value ? "selected" : "";
                <option value="@agencia.Value" @selected>@agencia.Text</option>
            }
        </select>
    </div>
</form>

<!-- Tabla -->
<table class="table table-bordered table-hover table-striped">
    <thead class="table-dark text-center align-middle">
        <tr>
            <th>Código</th>
            <th>Nombre</th>
            <th>Zona</th>
            <th>Marquesina</th>
            <th>RST Branch</th>
            <th>IP Server</th>
            <th>Nom. Server</th>
            <th>Base Datos</th>
            <th style="width: 120px">Acciones</th>
        </tr>
    </thead>
    <tbody>
        @foreach (var item in lista)
        {
            if (Model != null && Model.Codcco == item.Codcco)
            {
                <!-- FILA EN EDICIÓN -->
                <tr>
                    <form asp-action="GuardarEdicion" method="post">
                        <td>
                            <input asp-for="Codcco" type="hidden" />
                            @Model.Codcco
                        </td>
                        <td><input asp-for="NomAge" class="form-control" maxlength="40" required /></td>
                        <td>
                            <select asp-for="Zona" class="form-select" required>
                                <option value="1">CENTRO SUR</option>
                                <option value="2">NOR OCCIDENTE</option>
                                <option value="3">NOR ORIENTE</option>
                            </select>
                        </td>
                        <td>
                            <input type="hidden" name="MarqCheck" value="false" />
                            <input asp-for="MarqCheck" class="form-check-input" />
                        </td>
                        <td>
                            <input type="hidden" name="RstCheck" value="false" />
                            <input asp-for="RstCheck" class="form-check-input" />
                        </td>
                        <td><input asp-for="IpSer" class="form-control" maxlength="20" required /></td>
                        <td><input asp-for="NomSer" class="form-control" maxlength="18" required /></td>
                        <td><input asp-for="NomBD" class="form-control" maxlength="20" required /></td>
                        <td class="text-center">
                            <button type="submit" class="btn btn-success btn-sm me-1">Guardar</button>
                            <a asp-action="Index" class="btn btn-secondary btn-sm">Cancelar</a>
                        </td>
                    </form>
                </tr>
            }
            else
            {
                <!-- FILA NORMAL -->
                <tr>
                    <td>@item.Codcco</td>
                    <td>@item.NomAge</td>
                    <td>@(item.Zona switch
                    {
                        1 => "CENTRO SUR",
                        2 => "NOR OCCIDENTE",
                        3 => "NOR ORIENTE",
                        _ => "DESCONOCIDA"
                    })</td>
                    <td>@(item.Marquesina)</td>
                    <td>@(item.RstBranch)</td>
                    <td>@item.IpSer</td>
                    <td>@item.NomSer</td>
                    <td>@item.NomBD</td>
                    <td class="text-center">
                        <a asp-action="Index" asp-route-editId="@item.Codcco" class="btn btn-warning btn-sm me-1">Editar</a>
                        <form asp-action="Eliminar" asp-route-id="@item.Codcco" method="post" class="d-inline" onsubmit="return confirm('¿Está seguro de eliminar esta agencia?');">
                            <button type="submit" class="btn btn-danger btn-sm">Eliminar</button>
                        </form>
                    </td>
                </tr>
            }
        }
    </tbody>
</table>

<!-- Paginación -->
@if (lista.PageCount > 1)
{
    <div class="d-flex justify-content-center">
        @Html.PagedListPager(
            lista,
            page => Url.Action("Index", new { page, codcco = ViewBag.CodccoSeleccionado }),
            new PagedListRenderOptions
            {
                UlElementClasses = new[] { "pagination", "justify-content-center" },
                LiElementClasses = new[] { "page-item" },
                PageClasses = new[] { "page-link" }
            })
    </div>
}

<!-- Botón agregar -->
<div class="mt-4">
    <a asp-action="Agregar" class="btn btn-primary">Agregar Nueva Agencia</a>
</div>


[HttpGet]
[AutorizarPorTipoUsuario("1")]
public async Task<IActionResult> Index(int? page, int? codcco, int? editId)
{
    var agencias = await _agenciaService.ObtenerAgenciasAsync();
    ViewBag.Lista = agencias.ToPagedList(page ?? 1, 10);

    ViewBag.AgenciasFiltro = agencias
        .Select(a => new SelectListItem
        {
            Value = a.Codcco.ToString(),
            Text = $"{a.Codcco} - {a.NomAge}"
        })
        .OrderBy(a => a.Text)
        .ToList();

    ViewBag.CodccoSeleccionado = codcco;

    if (editId.HasValue)
    {
        var agencia = agencias.FirstOrDefault(a => a.Codcco == editId.Value);
        return View(agencia);
    }

    return View(new AgenciaModel()); // Si no hay edición activa
}

[HttpPost]
[AutorizarPorTipoUsuario("1")]
[ValidateAntiForgeryToken]
public async Task<IActionResult> GuardarEdicion(AgenciaModel model)
{
    if (!ModelState.IsValid)
    {
        TempData["Mensaje"] = "Datos inválidos.";
    }
    else
    {
        var actualizado = _agenciaService.ActualizarAgencia(model);
        TempData["Mensaje"] = actualizado
            ? "Agencia actualizada correctamente."
            : "Ocurrió un error al actualizar.";
    }

    // Regenerar la vista con la lista
    var agencias = await _agenciaService.ObtenerAgenciasAsync();
    ViewBag.Lista = agencias.ToPagedList(1, 10);
    ViewBag.AgenciasFiltro = agencias
        .Select(a => new SelectListItem
        {
            Value = a.Codcco.ToString(),
            Text = $"{a.Codcco} - {a.NomAge}"
        }).ToList();
    return View("Index", new AgenciaModel());
}

