@model IPagedList<CAUAdministracion.Models.AgenciaModel>
@using PagedList.Mvc.Core
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
        foreach (var item in Model)
        {
            <tr>
                <td>@item.Codcco</td>
                <td>@item.Nomage</td>
                <td>
                    @switch (item.Zona)
                    {
                        case 1: @:CENTRO SUR; break;
                        case 2: @:NOR OCCIDENTE; break;
                        case 3: @:NOR ORIENTE; break;
                        default: @:DESCONOCIDA; break;
                    }
                </td>
                <td>@(item.Marquesina ? "APLICA" : "NO APLICA")</td>
                <td>@(item.Rstbranch ? "APLICA" : "NO APLICA")</td>
                <td>@item.Ipser</td>
                <td>@item.Nomser</td>
                <td>@item.Nombd</td>
                <td class="text-center">
                    <a asp-action="Editar" asp-route-id="@item.Codcco" class="btn btn-sm btn-warning me-1">Editar</a>
                    <form asp-action="Eliminar" asp-route-id="@item.Codcco" method="post" class="d-inline" onsubmit="return confirm('¿Está seguro de eliminar esta agencia?');">
                        <button type="submit" class="btn btn-sm btn-danger">Eliminar</button>
                    </form>
                </td>
            </tr>
        }
    }
    else
    {
        <tr>
            <td colspan="9" class="text-center text-muted">No se encontraron agencias.</td>
        </tr>
    }
    </tbody>
</table>

<!-- Paginación -->
@if (Model != null && Model.PageCount > 1)
{
    <div class="d-flex justify-content-center">
        @Html.PagedListPager(
            Model,
            page => Url.Action("Index", new { page }),
            new PagedList.Mvc.Core.Common.PagedListRenderOptions
            {
                UlElementClasses = new[] { "pagination", "justify-content-center" },
                LiElementClasses = new[] { "page-item" },
                PageClasses = new[] { "page-link" },
                DisplayLinkToFirstPage = PagedList.Mvc.Core.Common.PagedListDisplayMode.Always,
                DisplayLinkToLastPage = PagedList.Mvc.Core.Common.PagedListDisplayMode.Always,
                DisplayLinkToPreviousPage = PagedList.Mvc.Core.Common.PagedListDisplayMode.Always,
                DisplayLinkToNextPage = PagedList.Mvc.Core.Common.PagedListDisplayMode.Always,
                MaximumPageNumbersToDisplay = 5
            })
    </div>
}

<!-- Botón agregar -->
<div class="mt-4">
    <a asp-action="Agregar" class="btn btn-primary">Agregar Nueva Agencia</a>
</div>
