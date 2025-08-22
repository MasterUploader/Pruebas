@using X.PagedList
@using X.PagedList.Mvc.Core
@using X.PagedList.Web.Common
@model IPagedList<CAUAdministracion.Models.UsuarioModel>

@{
    ViewData["Title"] = "Administración de Usuarios";

    // Valores actuales de filtro (los puedes pasar por ViewBag desde el controlador)
    var q         = ViewBag.Q as string;
    var tipoSel   = ViewBag.TipoSel as string;   // "1","2","3" o null
    var estadoSel = ViewBag.EstadoSel as string; // "A","I" o null

    // Usuario que está en modo edición (string usuario)
    var editUser  = ViewBag.EditUser as string;
}

<h2 class="text-danger mb-3">@ViewData["Title"]</h2>

@if (TempData["Mensaje"] is string msg && !string.IsNullOrWhiteSpace(msg))
{
    <div class="alert alert-info alert-dismissible fade show" role="alert" id="autoclose-alert">
        @msg
        <button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Close"></button>
    </div>
}

<form method="get" asp-controller="Usuarios" asp-action="Index" class="row g-3 mb-3">
    <div class="col-md-4">
        <label class="form-label">Usuario</label>
        <input type="text" name="q" value="@q" class="form-control" placeholder="Buscar por usuario..." />
    </div>

    <div class="col-md-3">
        <label class="form-label">Tipo</label>
        <select name="tipo" class="form-select">
            <option value="">-- Todos --</option>
            @if (tipoSel == "1") { @:<option value="1" selected="selected">Administrador</option> } else { @:<option value="1">Administrador</option> }
            @if (tipoSel == "2") { @:<option value="2" selected="selected">Admin. Videos</option> } else { @:<option value="2">Admin. Videos</option> }
            @if (tipoSel == "3") { @:<option value="3" selected="selected">Admin. Mensajes</option> } else { @:<option value="3">Admin. Mensajes</option> }
        </select>
    </div>

    <div class="col-md-3">
        <label class="form-label">Estado</label>
        <select name="estado" class="form-select">
            <option value="">-- Todos --</option>
            @if (string.Equals(estadoSel, "A", StringComparison.OrdinalIgnoreCase)) {
                @:<option value="A" selected="selected">Activo</option>
                @:<option value="I">Inactivo</option>
            } else if (string.Equals(estadoSel, "I", StringComparison.OrdinalIgnoreCase)) {
                @:<option value="A">Activo</option>
                @:<option value="I" selected="selected">Inactivo</option>
            } else {
                @:<option value="A">Activo</option>
                @:<option value="I">Inactivo</option>
            }
        </select>
    </div>

    <div class="col-md-2 d-grid">
        <label class="form-label d-none d-md-block">&nbsp;</label>
        <button type="submit" class="btn btn-primary">Filtrar</button>
    </div>
</form>

<div class="mb-3">
    <a asp-controller="Usuarios" asp-action="Crear" class="btn btn-success">Agregar Nuevo Usuario</a>
</div>

<table class="table table-bordered table-striped align-middle">
    <thead class="table-dark">
        <tr>
            <th>Usuario</th>
            <th>Tipo</th>
            <th>Estado</th>
            <th style="width:200px">Acciones</th>
        </tr>
    </thead>
    <tbody>
    @foreach (var u in Model)
    {
        var formUpdateId = $"f-upd-{u.Usuario}";
        var formDeleteId = $"f-del-{u.Usuario}";

        <!-- Formularios invisibles por cada fila -->
        <tr class="d-none">
            <td colspan="4" class="p-0">
                <form id="@formUpdateId" asp-controller="Usuarios" asp-action="Actualizar" method="post">
                    @Html.AntiForgeryToken()
                    <input type="hidden" name="usuario" value="@u.Usuario" />
                </form>

                <form id="@formDeleteId" asp-controller="Usuarios" asp-action="Eliminar" method="post">
                    @Html.AntiForgeryToken()
                    <input type="hidden" name="usuario" value="@u.Usuario" />
                </form>
            </td>
        </tr>

        if (string.Equals(editUser, u.Usuario, StringComparison.OrdinalIgnoreCase))
        {
            <!-- Fila en edición -->
            <tr>
                <td><strong>@u.Usuario</strong></td>
                <td>
                    <select name="tipoUsuario" form="@formUpdateId" class="form-select">
                        @if (u.TipoUsuario == 1) { <option value="1" selected="selected">Administrador</option> } else { <option value="1">Administrador</option> }
                        @if (u.TipoUsuario == 2) { <option value="2" selected="selected">Admin. Videos</option> } else { <option value="2">Admin. Videos</option> }
                        @if (u.TipoUsuario == 3) { <option value="3" selected="selected">Admin. Mensajes</option> } else { <option value="3">Admin. Mensajes</option> }
                    </select>
                </td>
                <td>
                    @if (string.Equals(u.Estado, "A", StringComparison.OrdinalIgnoreCase)) {
                        <select name="estado" form="@formUpdateId" class="form-select">
                            <option value="A" selected="selected">Activo</option>
                            <option value="I">Inactivo</option>
                        </select>
                    } else {
                        <select name="estado" form="@formUpdateId" class="form-select">
                            <option value="A">Activo</option>
                            <option value="I" selected="selected">Inactivo</option>
                        </select>
                    }
                </td>
                <td class="text-nowrap">
                    <button type="submit" form="@formUpdateId" class="btn btn-success btn-sm me-2">Guardar</button>
                    <a class="btn btn-secondary btn-sm"
                       asp-controller="Usuarios" asp-action="Index"
                       asp-route-q="@q" asp-route-tipo="@tipoSel" asp-route-estado="@estadoSel">Cancelar</a>
                </td>
            </tr>
        }
        else
        {
            <!-- Fila normal -->
            <tr>
                <td>@u.Usuario</td>
                <td>@(u.TipoUsuario == 1 ? "Administrador" : u.TipoUsuario == 2 ? "Admin. Videos" : "Admin. Mensajes")</td>
                <td>@(string.Equals(u.Estado, "A", StringComparison.OrdinalIgnoreCase) ? "Activo" : "Inactivo")</td>
                <td class="text-nowrap">
                    <a class="btn btn-warning btn-sm me-2"
                       asp-controller="Usuarios" asp-action="Index"
                       asp-route-editUser="@u.Usuario"
                       asp-route-q="@q" asp-route-tipo="@tipoSel" asp-route-estado="@estadoSel">
                        Editar
                    </a>
                    <button type="submit" form="@formDeleteId"
                            class="btn btn-danger btn-sm"
                            onclick="return confirm('¿Eliminar el usuario @u.Usuario?');">
                        Eliminar
                    </button>
                </td>
            </tr>
        }
    }
    </tbody>
</table>

@if (Model != null && Model.PageCount > 1)
{
    <div class="d-flex justify-content-center">
        @Html.PagedListPager(
            Model,
            page => Url.Action("Index", new { page, q, tipo = tipoSel, estado = estadoSel }),
            new PagedListRenderOptions {
                UlElementClasses = new[] { "pagination", "justify-content-center" },
                LiElementClasses = new[] { "page-item" },
                PageClasses = new[] { "page-link" },
                DisplayLinkToFirstPage = PagedListDisplayMode.Always,
                DisplayLinkToLastPage = PagedListDisplayMode.Always,
                DisplayLinkToPreviousPage = PagedListDisplayMode.Always,
                DisplayLinkToNextPage = PagedListDisplayMode.Always,
                MaximumPageNumbersToDisplay = 7
            })
    </div>
}

@section Scripts{
<script>
  // Cierra alert en 5s
  setTimeout(() => {
    const a = document.getElementById('autoclose-alert');
    if (a) a.remove();
  }, 5000);
</script>
    }
