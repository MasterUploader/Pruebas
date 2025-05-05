@using X.PagedList.Mvc.Core
@using X.PagedList.Mvc
@using X.PagedList
@model IPagedList<CAUAdministracion.Models.VideoModel>
@{
    ViewData["Title"] = "Mantenimiento de Videos";
    var codccoSeleccionado = ViewBag.CodccoSeleccionado as string;
}

<h2 class="text-danger">@ViewData["Title"]</h2>

<!-- Mostrar mensaje si existe -->
@if (!string.IsNullOrEmpty(ViewBag.Mensaje))
{
    <div class="alert alert-info alert-dismissible fade show" role="alert" id="alertMensaje">
        @ViewBag.Mensaje
        <button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Cerrar"></button>
    </div>

    <script>
        setTimeout(function () {
            var alerta = document.getElementById("alertMensaje");
            if (alerta) {
                alerta.classList.remove("show");
                alerta.classList.add("hide");
            }
        }, 5000); // 5 segundos
    </script>
}

<!-- Filtro por agencia -->
<form method="get">
    <div class="row g-2 align-items-end">
        <div class="col-md-4">
            <label for="codcco" class="form-label">Seleccione Agencia:</label>
            <select id="codcco" name="codcco" class="form-select" required>
                <option value="">--Seleccione--</option>
                @foreach (var agencia in ViewBag.Agencias as List<SelectListItem>)
                {
                    var selected = (agencia.Value == ViewBag.CodccoSeleccionado) ? "selected" : "";
                    @:<option value="@agencia.Value" @selected>@agencia.Text</option>
                }
            </select>
        </div>
        <div class="col-auto">
            <button type="submit" class="btn btn-primary">Filtrar</button>
        </div>
    </div>
</form>

<hr />

<!-- Tabla si hay datos -->
@if (Model != null && Model.Any())
{
    <form method="post">
        <table class="table table-bordered table-hover">
    <thead class="table-dark">
        <tr>
            <th>Agencia</th>
            <th>ID Video</th>
            <th>Nombre</th>
            <th>Ruta</th>
            <th>Estado</th>
            <th>Secuencia</th>
            <th>Acciones</th>
        </tr>
    </thead>
    <tbody>
    @foreach (var item in Model)
    {
        <tr>
            <td>@item.Codcco</td>
            <td>@item.CodVideo</td>
            <td>@item.Nombre</td>
            <td>@item.Ruta</td>

            <!-- Columna editable para actualizar solo el Estado -->
            <td>
                <form asp-action="Actualizar" method="post" class="d-flex">
                    <input type="hidden" name="codVideo" value="@item.CodVideo" />
                    <input type="hidden" name="codcco" value="@item.Codcco" />
                    <input type="hidden" name="Seq" value="@item.Seq" />

                    <select name="Estado" class="form-select form-select-sm me-2">
                        @if (item.Estado == "A")
                        {
                            <option value="A" selected>Activo</option>
                            <option value="I">Inactivo</option>
                        }
                        else
                        {
                            <option value="A">Activo</option>
                            <option value="I" selected>Inactivo</option>
                        }
                    </select>

                     <button type="submit" class="btn btn-sm btn-success">Guardar</button>
                </form>
            </td>

            <!-- Solo lectura para Secuencia -->
            <td>@item.Seq</td>

            <!-- Botón para eliminar -->
            <td>
                <form asp-action="Eliminar" method="post" onsubmit="return confirm('¿Desea eliminar este video?');">
                    <input type="hidden" name="codVideo" value="@item.CodVideo" />
                    <input type="hidden" name="codcco" value="@item.Codcco" />
                    <button type="submit" class="btn btn-sm btn-danger">Eliminar</button>
                </form>
            </td>
        </tr>
    }
    </tbody>
</table>

<div class="text-center mt-3">
    @Html.PagedListPager(
    Model,
    page => Url.Action("Index", new { page, codcco = ViewBag.CodccoSeleccionado }),
    new PagedList.Mvc.Core.Common.PagedListRenderOptions
    {
        LiElementClasses = new[] { "page-item" },
        PageClasses = new[] { "page-link" },
        UlElementClasses = new[] { "pagination", "justify-content-center" },
        DisplayLinkToFirstPage = PagedList.Mvc.Core.Common.PagedListDisplayMode.Always,
        DisplayLinkToLastPage = PagedList.Mvc.Core.Common.PagedListDisplayMode.Always,
        DisplayLinkToPreviousPage = PagedList.Mvc.Core.Common.PagedListDisplayMode.Always,
        DisplayLinkToNextPage = PagedList.Mvc.Core.Common.PagedListDisplayMode.Always,
        MaximumPageNumbersToDisplay = 5
    }
)
</div>
    </form>
}
else
{
    <div class="alert alert-info mt-3">No se encontraron videos para esta agencia.</div>
}

