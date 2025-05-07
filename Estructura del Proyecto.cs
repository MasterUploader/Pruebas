@using X.PagedList.Mvc.Core
@using X.PagedList.Mvc
@using X.PagedList
@model IPagedList<CAUAdministracion.Models.AgenciaModel>
@{
    ViewData["Title"] = "Mantenimiento de Agencias";
}

<h2 class="text-danger mb-4">@ViewData["Title"]</h2>

@if (TempData["Mensaje"] != null)
{
    <div class="alert alert-info alert-dismissible fade show" role="alert">
        @TempData["Mensaje"]
        <button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Close"></button>
    </div>
}

<form method="get" asp-action="Index" class="row mb-3">
    <div class="col-md-4">
        <label for="codcco" class="form-label">Filtrar por Agencia</label>
        <select id="codcco" name="codcco" class="form-select" onchange="this.form.submit()">
            <option value="">-- Todas las Agencias --</option>
            @foreach (var agencia in (List<SelectListItem>)ViewBag.AgenciasFiltro)
               {
                string selected = ViewBag.CodccoSeleccionado?.ToString() == agencia.Value ? "selected" : "";
                @:<option value="@agencia.Value" @selected>@agencia.Text</option>
               }

        </select>
    </div>
</form>


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
        @if (Model != null && Model.Any())
        {

            @foreach (var item in Model)
            {
                if (ViewBag.EditId != null && ViewBag.EditId == item.Codcco)
                {
                    <!-- FILA EN MODO EDICIÓN -->
                    <tr>
                        <form asp-action="GuardarEdicion" method="post">
                            <input type="hidden" name="Codcco" value="@item.Codcco" />
                        <td>@item.Codcco</td>
                        <td><input name="NomAge" value="@item.NomAge" maxlength="40" class="form-control" required /></td>
                        <td>
                            <select name="Zona" class="form-select" required>
                                <option value="1" selected="@(item.Zona == 1)">CENTRO SUR</option>
                                <option value="2" selected="@(item.Zona == 2)">NOR OCCIDENTE</option>
                                <option value="3" selected="@(item.Zona == 3)">NOR ORIENTE</option>
                            </select>
                        </td>
                        <td>
                            <!-- Valor por defecto si no se marca -->
                            <input type="hidden" name="MarqCheck" value="false" />
                            <!-- Checkbox que se enlaza a MarqCheck (lo que actualiza Marquesina internamente) -->
                            <input type="checkbox" name="MarqCheck" value="true" class="form-check-input" @(item.MarqCheck ? "checked" : "") />
                        </td>
                        <td>
                            <!-- Valor por defecto si no se marca -->
                            <input type="hidden" name="RstCheck" value="false" />
                            <!-- Checkbox que se enlaza a RstCheck (lo que actualiza RstBranch internamente) -->
                            <input type="checkbox" name="RstCheck" value="true" class="form-check-input" @(item.RstCheck ? "checked" : "") />
                        </td>
                        <td><input name="IpSer" value="@item.IpSer" maxlength="20" class="form-control" required /></td>
                        <td><input name="NomSer" value="@item.NomSer" maxlength="18" class="form-control" required /></td>
                        <td><input name="NomBD" value="@item.NomBD" maxlength="20" class="form-control" required /></td>
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
                        <td>
                            @{
                                string zonaTexto = item.Zona switch
                                {
                                    1 => "CENTRO SUR",
                                    2 => "NOR OCCIDENTE",
                                    3 => "NOR ORIENTE",
                                    _ => "DESCONOCIDA"
                                };
                            }
                            @zonaTexto
                        </td>
                        <td>
                        <!-- Valor por defecto si no se marca -->
                        <input type="hidden" name="MarqCheck" value="false" />
                        <!-- Checkbox que se enlaza a MarqCheck (lo que actualiza Marquesina internamente) -->
                        <input type="checkbox" name="MarqCheck" value="true" class="form-check-input" @(item.MarqCheck ? "checked" : "") />
                    </td>
                    <td>
                        <!-- Valor por defecto si no se marca -->
                        <input type="hidden" name="RstCheck" value="false" />
                        <!-- Checkbox que se enlaza a RstCheck (lo que actualiza RstBranch internamente) -->
                        <input type="checkbox" name="RstCheck" value="true" class="form-check-input" @(item.RstCheck ? "checked" : "") />
                    </td>
                        <td>@item.IpSer</td>
                        <td>@item.NomSer</td>
                        <td>@item.NomBD</td>
                        <td class="text-center">
                            <a asp-action="Index" asp-route-editId="@item.Codcco" class="btn btn-sm btn-warning me-1">Editar</a>
                            <form asp-action="Eliminar" asp-route-id="@item.Codcco" method="post" class="d-inline" onsubmit="return confirm('¿Está seguro de eliminar esta agencia?');">
                                <button type="submit" class="btn btn-sm btn-danger">Eliminar</button>
                            </form>
                        </td>
                    </tr>
                }
            }
        }

    </tbody>
</table>

<!-- Paginación -->
@if (Model != null && Model.PageCount > 1)
{
    <div class="d-flex justify-content-center">
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
    </div>
}

<!-- Botón agregar -->
<div class="mt-4">
    <a asp-action="Agregar" class="btn btn-primary">Agregar Nueva Agencia</a>
</div>
