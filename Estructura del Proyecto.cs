@Html.PagedListPager(
    Model,
    page => Url.Action("Index", new { page }),
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


<td>@(item.Marquesina?.ToUpper() == "SI" ? "APLICA" : "NO APLICA")</td>
<td>@(item.RstBranch?.ToUpper() == "SI" ? "APLICA" : "NO APLICA")</td>


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
