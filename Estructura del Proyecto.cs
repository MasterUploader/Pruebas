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
