@{
    var lista = (IPagedList<CAUAdministracion.Models.AgenciaModel>)ViewBag.Lista;
}

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
