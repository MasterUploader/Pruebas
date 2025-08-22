@model X.PagedList.IPagedList<CAUAdministracion.Models.UsuarioListadoModel>
@using X.PagedList.Mvc.Core

@{
    ViewData["Title"] = "Usuarios";
    // valores actuales de filtros (GET)
    var q = Context.Request.Query["q"].ToString();
    var tipoSel = Context.Request.Query["tipo"].ToString();    // "1","2","3" o ""
    var estadoSel = Context.Request.Query["estado"].ToString(); // "A","I" o ""
}

<h2 class="text-danger mb-3">@ViewData["Title"]</h2>

@if (TempData["Mensaje"] != null)
{
    <div class="alert alert-success alert-dismissible fade show" role="alert" id="autoclose-alert">
        @TempData["Mensaje"]
        <button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Cerrar"></button>
    </div>
}

@if (TempData["Error"] != null)
{
    <div class="alert alert-danger alert-dismissible fade show" role="alert" id="autoclose-alert-err">
        @TempData["Error"]
        <button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Cerrar"></button>
    </div>
}

<!-- Filtros -->
<form method="get" asp-controller="Usuarios" asp-action="Index" class="row g-2 align-items-end mb-3">
    <div class="col-md-4">
        <label class="form-label">Usuario</label>
        <input type="text" name="q" value="@q" class="form-control" placeholder="Buscar por usuario..." />
    </div>

    <div class="col-md-3">
        <label class="form-label">Tipo</label>
        <select name="tipo" class="form-select">
            <option value="">-- Todos --</option>
            <option value="1" @(tipoSel=="1" ? "selected" : "")>Administrador</option>
            <option value="2" @(tipoSel=="2" ? "selected" : "")>Admin. Videos</option>
            <option value="3" @(tipoSel=="3" ? "selected" : "")>Admin. Mensajes</option>
        </select>
    </div>

    <div class="col-md-3">
        <label class="form-label">Estado</label>
        <select name="estado" class="form-select">
            <option value="">-- Todos --</option>
            <option value="A" @(estadoSel=="A" ? "selected" : "")>Activo</option>
            <option value="I" @(estadoSel=="I" ? "selected" : "")>Inactivo</option>
        </select>
    </div>

    <div class="col-md-2 d-grid">
        <button type="submit" class="btn btn-primary">Filtrar</button>
    </div>
</form>

<div class="mb-3 text-end">
    <a asp-controller="Usuarios" asp-action="Agregar" class="btn btn-success">
        + Agregar usuario
    </a>
</div>

<!-- Tabla -->
<table class="table table-bordered table-hover table-striped align-middle">
    <thead class="table-dark text-center">
        <tr>
            <th style="width:30%">Usuario</th>
            <th style="width:30%">Tipo</th>
            <th style="width:15%">Estado</th>
            <th style="width:25%">Acciones</th>
        </tr>
    </thead>
    <tbody>
    @if (Model != null && Model.Any())
    {
        foreach (var item in Model)
        {
            var tipoTexto = item.TipoUsuario switch
            {
                1 => "Administrador",
                2 => "Admin. Videos",
                3 => "Admin. Mensajes",
                _ => "Desconocido"
            };
            var esActivo = item.Estado == "A";
            var accionEstado = esActivo ? "Desactivar" : "Activar";
            var nuevoEstado = esActivo ? "I" : "A";
            <tr>
                <td>@item.Usuario</td>
                <td>@tipoTexto</td>
                <td>@(esActivo ? "Activo" : "Inactivo")</td>
                <td class="text-center">
                    <a class="btn btn-sm btn-warning me-2"
                       asp-controller="Usuarios"
                       asp-action="Editar"
                       asp-route-id="@item.Usuario">
                        Editar
                    </a>

                    <form asp-controller="Usuarios"
                          asp-action="CambiarEstado"
                          method="post"
                          class="d-inline">
                        @Html.AntiForgeryToken()
                        <input type="hidden" name="usuario" value="@item.Usuario" />
                        <input type="hidden" name="estado" value="@nuevoEstado" />
                        <button type="submit" class="btn btn-sm btn-secondary me-2">
                            @accionEstado
                        </button>
                    </form>

                    <form asp-controller="Usuarios"
                          asp-action="Eliminar"
                          method="post"
                          class="d-inline"
                          onsubmit="return confirm('¿Eliminar el usuario @item.Usuario?');">
                        @Html.AntiForgeryToken()
                        <input type="hidden" name="usuario" value="@item.Usuario" />
                        <button type="submit" class="btn btn-sm btn-danger">
                            Eliminar
                        </button>
                    </form>
                </td>
            </tr>
        }
    }
    else
    {
        <tr>
            <td colspan="4" class="text-center text-muted">Sin resultados.</td>
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
            page => Url.Action("Index", new {
                page,
                q = q,
                tipo = tipoSel,
                estado = estadoSel
            }),
            new X.PagedList.Mvc.Core.Common.PagedListRenderOptions {
                UlElementClasses = new[] { "pagination", "justify-content-center" },
                LiElementClasses = new[] { "page-item" },
                PageClasses = new[] { "page-link" },
                DisplayLinkToFirstPage = X.PagedList.Mvc.Core.Common.PagedListDisplayMode.Always,
                DisplayLinkToLastPage = X.PagedList.Mvc.Core.Common.PagedListDisplayMode.Always,
                DisplayLinkToPreviousPage = X.PagedList.Mvc.Core.Common.PagedListDisplayMode.Always,
                DisplayLinkToNextPage = X.PagedList.Mvc.Core.Common.PagedListDisplayMode.Always,
                MaximumPageNumbersToDisplay = 7
            }
        )
    </div>
}

@section Scripts{
    <script>
        // Cierra alerts en 5s
        setTimeout(() => {
            const ok = document.getElementById('autoclose-alert');
            const er = document.getElementById('autoclose-alert-err');
            if (ok) ok.classList.remove('show');
            if (er) er.classList.remove('show');
        }, 5000);
    </script>
}



namespace CAUAdministracion.Models
{
    public class UsuarioListadoModel
    {
        public string Usuario { get; set; } = string.Empty;
        public int TipoUsuario { get; set; } // 1,2,3
        public string Estado { get; set; } = "A"; // "A" o "I"
    }
}
