Si tengo un metodo así

 public IActionResult Eliminar(int codVideo, string codcco)
 {
     if (_videoService.TieneDependencias(codcco, codVideo))
     {
         TempData["Mensaje"] = "No se puede eliminar el video porque tiene dependencias.";
         TempData["MensajeTipo"] = "warning";
         return RedirectToAction("Index", new { codcco });
     }

     Y me da una advertencia S6967

         Y la vista esta así

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
    </form>
}
else
{
    <div class="alert alert-info mt-3">No se encontraron videos para esta agencia.</div>
}







    Es posible corregierlo sin hacer cambios en la vista?
