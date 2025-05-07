@using X.PagedList.Mvc.Core
@using X.PagedList
@model CAUAdministracion.Models.AgenciaModel

@{
    ViewData["Title"] = "Mantenimiento de Agencias";
    var lista = (IPagedList<CAUAdministracion.Models.AgenciaModel>)ViewBag.Lista;
    var editId = ViewBag.EditId as int?;
}

<h2 class="text-danger mb-4">@ViewData["Title"]</h2>

@if (TempData["Mensaje"] != null)
{
    <div class="alert alert-info alert-dismissible fade show" role="alert">
        @TempData["Mensaje"]
        <button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Close"></button>
    </div>
}

<!-- Filtro por Agencia -->
<div class="mb-3">
    <form method="get" asp-action="Index">
        <label for="codcco">Agencia:</label>
        <select name="codcco" class="form-select" style="width: 300px; display:inline-block;" onchange="this.form.submit()">
            <option value="">-- Todas las Agencias --</option>
            @foreach (var agencia in (List<SelectListItem>)ViewBag.AgenciasFiltro)
            {
                var selected = (agencia.Value == ViewBag.CodccoSeleccionado?.ToString()) ? "selected" : "";
                @:<option value="@agencia.Value" @selected>@agencia.Text</option>
            }
        </select>
    </form>
</div>

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
            if (editId != null && item.Codcco == editId)
            {
                <!-- Fila en edición -->
                <tr>
                    <form asp-action="GuardarEdicion" method="post">
                        <td>
                            <input type="hidden" name="Codcco" value="@item.Codcco" />
                            @item.Codcco
                        </td>
                        <td><input name="NomAge" value="@item.NomAge" class="form-control" maxlength="40" required /></td>
                        <td>
                            <select name="Zona" class="form-select" required>
                                <option value="1" selected="@(item.Zona == 1)">CENTRO SUR</option>
                                <option value="2" selected="@(item.Zona == 2)">NOR OCCIDENTE</option>
                                <option value="3" selected="@(item.Zona == 3)">NOR ORIENTE</option>
                            </select>
                        </td>
                        <td>
                            <input type="hidden" name="Marquesina" value="NO" />
                            <input type="checkbox" name="Marquesina" value="SI" class="form-check-input" @(item.Marquesina == "SI" ? "checked" : "") />
                        </td>
                        <td>
                            <input type="hidden" name="RstBranch" value="NO" />
                            <input type="checkbox" name="RstBranch" value="SI" class="form-check-input" @(item.RstBranch == "SI" ? "checked" : "") />
                        </td>
                        <td><input name="IpSer" value="@item.IpSer" class="form-control" maxlength="20" required /></td>
                        <td><input name="NomSer" value="@item.NomSer" class="form-control" maxlength="18" required /></td>
                        <td><input name="NomBD" value="@item.NomBD" class="form-control" maxlength="20" required /></td>
                        <td class="text-center">
                            <button type="submit" class="btn btn-success btn-sm me-1">Guardar</button>
                            <a asp-action="Index" class="btn btn-secondary btn-sm">Cancelar</a>
                        </td>
                    </form>
                </tr>
            }
            else
            {
                <!-- Fila normal -->
                <tr>
                    <td>@item.Codcco</td>
                    <td>@item.NomAge</td>
                    <td>
                        @(item.Zona switch
                        {
                            1 => "CENTRO SUR",
                            2 => "NOR OCCIDENTE",
                            3 => "NOR ORIENTE",
                            _ => "DESCONOCIDA"
                        })
                    </td>
                    <td>@item.Marquesina</td>
                    <td>@item.RstBranch</td>
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
@if (lista != null && lista.PageCount > 1)
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
