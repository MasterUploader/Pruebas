@using X.PagedList
@using X.PagedList.Mvc.Core
@using X.PagedList.Mvc.Core.Common
@model IPagedList<CAUAdministracion.Models.VideoModel>

@{
    ViewData["Title"] = "Mantenimiento de Videos";
    var agencias = ViewBag.Agencias as List<SelectListItem>;
    var codccoSeleccionado = ViewBag.CodccoSeleccionado as string;
}

<h2 class="text-danger">@ViewData["Title"]</h2>

<!-- Filtro por Agencia -->
<form method="get" asp-controller="Videos" asp-action="Index">
    <div class="row mb-3">
        <div class="col-md-4">
            <label for="codcco" class="form-label">Seleccione Agencia:</label>
            <select id="codcco" name="codcco" class="form-select" required>
                <option value="">-- Seleccione --</option>
                @foreach (var agencia in agencias)
                {
                    <option value="@agencia.Value" selected="@(agencia.Value == codccoSeleccionado ? "selected" : null)">
                        @agencia.Text
                    </option>
                }
            </select>
        </div>
        <div class="col-auto align-self-end">
            <button type="submit" class="btn btn-primary">Filtrar</button>
        </div>
    </div>
</form>

<!-- Tabla de Videos -->
<table class="table table-bordered table-striped">
    <thead class="table-dark">
        <tr>
            <th>Código</th>
            <th>Agencia</th>
            <th>Estado</th>
            <th>Archivo</th>
            <th class="text-center">Acciones</th>
        </tr>
    </thead>
    <tbody>
        @if (Model != null && Model.Any())
        {
            foreach (var video in Model)
            {
                <tr>
                    <td>@video.Codigo</td>
                    <td>@video.NombreAgencia</td>
                    <td>@(video.Estado == "A" ? "Activo" : "Inactivo")</td>
                    <td>@video.NombreArchivo</td>
                    <td class="text-center">
                        <a asp-action="Editar" asp-route-id="@video.Codigo" class="btn btn-sm btn-warning">Editar</a>
                        <form asp-action="Eliminar" asp-route-id="@video.Codigo" method="post" style="display:inline;" onsubmit="return confirm('¿Desea eliminar este video?');">
                            <button type="submit" class="btn btn-sm btn-danger">Eliminar</button>
                        </form>
                    </td>
                </tr>
            }
        }
        else
        {
            <tr><td colspan="5" class="text-center">No hay videos registrados para la agencia seleccionada.</td></tr>
        }
    </tbody>
</table>

<!-- Paginación -->
<div class="text-center mt-3">
    @Html.PagedListPager(
        Model,
        page => Url.Action("Index", new { page, codcco = codccoSeleccionado }),
        new PagedListRenderOptions
        {
            LiElementClasses = new[] { "page-item" },
            PageClasses = new[] { "page-link" },
            UlElementClasses = new[] { "pagination", "justify-content-center" },
            DisplayLinkToFirstPage = PagedListDisplayMode.Always,
            DisplayLinkToLastPage = PagedListDisplayMode.Always,
            DisplayLinkToPreviousPage = PagedListDisplayMode.Always,
            DisplayLinkToNextPage = PagedListDisplayMode.Always,
            MaximumPageNumbersToDisplay = 5
        }
    )
</div>
