@using X.PagedList
@using X.PagedList.Mvc.Core
@using X.PagedList.Web.Common
@model IPagedList<CAUAdministracion.Models.UsuarioModel>

@{
    ViewData["Title"] = "Administración de Usuarios";

    // Filtros actuales que el controlador coloca en ViewBag
    var q         = ViewBag.Q as string;
    var tipoSel   = ViewBag.TipoSel?.ToString();
    var estadoSel = ViewBag.EstadoSel?.ToString();
}

<h2 class="text-danger">@ViewData["Title"]</h2>

@if (TempData["Mensaje"] != null)
{
    <div id="autoclose-alert" class="alert alert-info alert-dismissible fade show" role="alert">
        @TempData["Mensaje"]
        <button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Close"></button>
    </div>
}

<!-- Filtros -->
<form method="get" asp-controller="Usuarios" asp-action="Index" class="row g-3 mb-3">
    <div class="col-md-4">
        <label class="form-label">Usuario</label>
        <input type="text" name="q" value="@(q ?? "")" class="form-control" placeholder="Buscar por usuario..." />
    </div>

    <div class="col-md-3">
        <label class="form-label">Tipo</label>
        <select name="tipo" class="form-select">
            @: <option value="">-- Todos --</option>
            @{
                // Usamos líneas literales (@:) para evitar RZ1031 en <option>.
                var sel1 = (tipoSel == "1") ? "selected" : "";
                var sel2 = (tipoSel == "2") ? "selected" : "";
                var sel3 = (tipoSel == "3") ? "selected" : "";
            }
            @: <option value="1" @sel1>Administrador</option>
            @: <option value="2" @sel2>Admin. Videos</option>
            @: <option value="3" @sel3>Admin. Mensajes</option>
        </select>
    </div>

    <div class="col-md-3">
        <label class="form-label">Estado</label>
        <select name="estado" class="form-select">
            @: <option value="">-- Todos --</option>
            @{
                var sA = (estadoSel == "A") ? "selected" : "";
                var sI = (estadoSel == "I") ? "selected" : "";
            }
            @: <option value="A" @sA>Activo</option>
            @: <option value="I" @sI>Inactivo</option>
        </select>
    </div>

    <div class="col-md-2 d-grid">
        <label class="form-label d-none d-md-block">&nbsp;</label>
        <button type="submit" class="btn btn-primary">Filtrar</button>
    </div>
</form>

<div class="mb-3">
    <a asp-controller="Usuarios" asp-action="Agregar" class="btn btn-success">Agregar nuevo usuario</a>
</div>

@if (Model != null && Model.Any())
{
    <table class="table table-bordered table-striped align-middle">
        <thead class="table-dark">
            <tr>
                <th>Usuario</th>
                <th>Tipo</th>
                <th>Estado</th>
                <th style="width:160px">Acciones</th>
            </tr>
        </thead>
        <tbody>
        @foreach (var u in Model)
        {
            var tipoTxt = u.TipoUsu switch
            {
                1 => "Administrador",
                2 => "Admin. Videos",
                3 => "Admin. Mensajes",
                _ => $"Tipo {u.TipoUsu}"
            };
            var estadoTxt = (u.Estado == "A") ? "Activo" : "Inactivo";

            <tr>
                <td>@u.Usuario</td>
                <td>@tipoTxt</td>
                <td>@estadoTxt</td>
                <td class="text-nowrap">
                    <a asp-controller="Usuarios"
                       asp-action="Editar"
                       asp-route-usuario="@u.Usuario"
                       class="btn btn-sm btn-warning me-2">
                        Editar
                    </a>

                    <form asp-controller="Usuarios"
                          asp-action="Eliminar"
                          asp-route-usuario="@u.Usuario"
                          method="post"
                          class="d-inline"
                          onsubmit="return confirm('¿Eliminar el usuario @u.Usuario?');">
                        @Html.AntiForgeryToken()
                        <button type="submit" class="btn btn-sm btn-danger">Eliminar</button>
                    </form>
                </td>
            </tr>
        }
        </tbody>
    </table>

    <div class="d-flex justify-content-center">
        @Html.PagedListPager(
            Model,
            page => Url.Action("Index", new { page, q, tipo = tipoSel, estado = estadoSel }),
            new PagedListRenderOptions {
                UlElementClasses = new[] { "pagination", "justify-content-center" },
                LiElementClasses = new[] { "page-item" },
                PageClasses = new[] { "page-link" },
                DisplayLinkToFirstPage = PagedListDisplayMode.Always,
                DisplayLinkToLastPage  = PagedListDisplayMode.Always,
                DisplayLinkToPreviousPage = PagedListDisplayMode.Always,
                DisplayLinkToNextPage  = PagedListDisplayMode.Always,
                MaximumPageNumbersToDisplay = 7
            })
    </div>
}
else
{
    <div class="alert alert-info">No se encontraron usuarios con los criterios seleccionados.</div>
}

@section Scripts{
<script>
    // Cierra alerts en 5s
    setTimeout(function(){
        var el = document.getElementById('autoclose-alert');
        if(el){ var alert = bootstrap.Alert.getOrCreateInstance(el); alert.close(); }
    }, 5000);
</script>
    }


@model CAUAdministracion.Models.UsuarioModel
@{
    ViewData["Title"] = "Agregar nuevo usuario";
    // Si tu controlador devuelve ModelState con errores, mostramos un summary.
}

<h2 class="text-danger">@ViewData["Title"]</h2>

@if (TempData["Mensaje"] != null)
{
    <div class="alert alert-info alert-dismissible fade show" role="alert">
        @TempData["Mensaje"]
        <button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Close"></button>
    </div>
}

<form asp-controller="Usuarios" asp-action="Agregar" method="post" class="row g-3">
    @Html.AntiForgeryToken()

    <div class="col-md-6">
        <label class="form-label">Usuario</label>
        <input type="text" name="Usuario" value="@Model?.Usuario" maxlength="32" class="form-control" required />
    </div>

    <div class="col-md-3">
        <label class="form-label">Tipo de usuario</label>
        @{
            var tipoSel = (Model?.TipoUsu ?? 1).ToString();
            var sel1 = (tipoSel == "1") ? "selected" : "";
            var sel2 = (tipoSel == "2") ? "selected" : "";
            var sel3 = (tipoSel == "3") ? "selected" : "";
        }
        <select name="TipoUsu" class="form-select" required>
            @: <option value="1" @sel1>Administrador</option>
            @: <option value="2" @sel2>Admin. Videos</option>
            @: <option value="3" @sel3>Admin. Mensajes</option>
        </select>
    </div>

    <div class="col-md-3">
        <label class="form-label">Estado</label>
        @{
            var estadoSel = string.IsNullOrEmpty(Model?.Estado) ? "A" : Model.Estado;
            var sA = (estadoSel == "A") ? "selected" : "";
            var sI = (estadoSel == "I") ? "selected" : "";
        }
        <select name="Estado" class="form-select" required>
            @: <option value="A" @sA>Activo</option>
            @: <option value="I" @sI>Inactivo</option>
        </select>
    </div>

    <div class="col-md-6">
        <label class="form-label">Clave</label>
        <input type="password" name="Clave" maxlength="10" class="form-control" required />
    </div>

    <div class="col-md-6">
        <label class="form-label">Confirmar clave</label>
        <input type="password" name="ConfirmarClave" maxlength="10" class="form-control" required />
    </div>

    <div class="col-12">
        @* Si el controlador usa ModelState para errores, se mostrarán aquí *@
        <div class="text-danger">
            @Html.ValidationSummary(excludePropertyErrors: true)
        </div>
    </div>

    <div class="col-12">
        <button type="submit" class="btn btn-primary">Guardar</button>
        <a asp-controller="Usuarios" asp-action="Index" class="btn btn-secondary ms-2">Cancelar</a>
    </div>
</form>
