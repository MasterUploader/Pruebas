Si vemos el index de mensajes

@model List<CAUAdministracion.Models.MensajeModel>
@using Microsoft.AspNetCore.Mvc.Rendering

@{
    ViewData["Title"] = "Mantenimiento de Mensajes";
    var agencias = ViewBag.Agencias as List<SelectListItem>;
    var codccoSel = ViewBag.CodccoSeleccionado as string;
}

<h2 class="text-danger">@ViewData["Title"]</h2>

<!-- Filtro por Agencia -->
<div class="mb-3">
    <form method="get" asp-controller="Messages" asp-action="Index" class="d-inline">
        <label for="codcco">Agencia:</label>
        <select id="codcco" name="codcco" class="form-select d-inline-block" style="width: 320px" onchange="this.form.submit()">
            <option value="">-- Seleccione Agencia --</option>
            @if (agencias != null)
            {
                foreach (var a in agencias)
                {
                    var selected = (a.Value == codccoSel) ? "selected" : "";
                    @:<option value="@a.Value" @selected>@a.Text</option>
                }
            }
        </select>
    </form>
</div>

@if (Model != null && Model.Any())
{
    <table class="table table-bordered table-striped align-middle">
        <thead class="table-dark">
            <tr>
                <th style="width:110px">Código</th>
                <th style="width:120px">Secuencia</th>
                <th>Mensaje</th>
                <th style="width:160px">Estado</th>
                <th style="width:120px">Actualizar</th>
                <th style="width:120px">Eliminar</th>
            </tr>
        </thead>
        <tbody>
            @foreach (var item in Model)
            {
                var formUpdateId = $"f-upd-{item.Codcco}-{item.CodMsg}";
                var formDeleteId = $"f-del-{item.Codcco}-{item.CodMsg}";
                <tr>
                    <!-- Formulario de ACTUALIZAR (oculto) -->
                    <td colspan="6" class="p-0">
                        <form id="@formUpdateId" asp-controller="Messages" asp-action="Actualizar" method="post">
                            @Html.AntiForgeryToken()
                            <input type="hidden" name="Codcco" value="@item.Codcco" />
                            <input type="hidden" name="CodMsg" value="@item.CodMsg" />
                        </form>

                        <!-- Formulario de ELIMINAR (oculto) -->
                        <form id="@formDeleteId" asp-controller="Messages" asp-action="Eliminar" method="post">
                            @Html.AntiForgeryToken()
                            <input type="hidden" name="Codcco" value="@item.Codcco" />
                            <input type="hidden" name="CodMsg" value="@item.CodMsg" />
                        </form>
                    </td>
                </tr>
                <tr>
                    <td>@item.CodMsg</td>
                    <td>@item.Seq</td>
                    <td>
                        <!-- Este input pertenece al form de actualizar de ESTA fila -->
                        <input type="text"
                               name="Mensaje"
                               form="@formUpdateId"
                               value="@item.Mensaje"
                               class="form-control" />
                    </td>
                    <td>
                        <select name="Estado" form="@formUpdateId" class="form-select form-select-sm">
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
                    </td>
                    <td>
                        <button type="submit"
                                form="@formUpdateId"
                                class="btn btn-sm btn-success w-100">
                            Actualizar
                        </button>
                    </td>
                    <td>
                        <button type="submit"
                                form="@formDeleteId"
                                class="btn btn-sm btn-danger w-100"
                                onclick="return confirm('¿Está seguro de eliminar este mensaje?');">
                            Eliminar
                        </button>
                    </td>
                </tr>
            }
        </tbody>
    </table>
}
else
{
    <div class="alert alert-info">No se encontraron mensajes para esta agencia.</div>
}

<a href="@Url.Action("Agregar", "Messages")" class="btn btn-primary">Agregar Nuevo Mensaje</a>




Con el index de Agencias


@model CAUAdministracion.Models.AgenciaIndexViewModel
@using X.PagedList.Mvc.Core
@using Microsoft.AspNetCore.Mvc.Rendering

@{
    ViewData["Title"] = "Mantenimiento de Agencias";
    var lista = Model.Lista;
    var agenciaEditar = Model.AgenciaEnEdicion;
    var codccoSel = Model.CodccoSeleccionado;
}

<h2 class="text-danger mb-4">@ViewData["Title"]</h2>

@if (TempData["Mensaje"] != null)
{
    <div class="alert alert-info alert-dismissible fade show" role="alert">
        @TempData["Mensaje"]
        <button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Close"></button>
    </div>
}

<!-- Filtro -->
<div class="mb-3">
    <form method="get" asp-controller="Agencias" asp-action="Index">
        <label for="codcco">Agencia:</label>
@*         <select name="codcco" class="form-select" style="width: 300px; display:inline-block;" onchange="this.form.submit()">
            <option value="">-- Seleccione Agencia --</option>
            @foreach (var agencia in Model.AgenciasFiltro)
            {
                var selected = (agencia.Value == codccoSel) ? "selected" : "";
                @:<option value="@agencia.Value" @selected>@agencia.Text</option>
            }
        </select> *@

        <select name="codcco" class="form-select" style="width: 300px; display:inline-block;" onchange="this.form.submit()">
            <option value="">-- Seleccione Agencia --</option>
            @foreach (var agencia in Model.AgenciasFiltro)
            {
                var selected = (agencia.Value == Model.CodccoSeleccionado) ? "selected" : "";
                @:<option value="@agencia.Value" @selected>@agencia.Text</option>
            }
        </select>
    </form>
</div>

<!-- Tabla -->
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
        @foreach (var item in lista)
        {
            if (agenciaEditar != null && item.Codcco == agenciaEditar.Codcco)
            {
                <tr>
                    <form asp-action="GuardarEdicion" method="post">
                        <td>
                            <input type="hidden" asp-for="AgenciaEnEdicion.Codcco" />
                            @agenciaEditar.Codcco
                        </td>
                        <td><input asp-for="AgenciaEnEdicion.NomAge" class="form-control" /></td>
                        <td>
                            <select asp-for="AgenciaEnEdicion.Zona" class="form-select" required>
                                <option value="1">CENTRO SUR</option>
                                <option value="2">NOR OCCIDENTE</option>
                                <option value="3">NOR ORIENTE</option>
                            </select>
                        </td>
                        <td>
                            <select asp-for="AgenciaEnEdicion.Marquesina" class="form-select" required>
                                <option value="SI" selected="@(agenciaEditar.Marquesina == "SI")">SI</option>
                                <option value="NO" selected="@(agenciaEditar.Marquesina == "NO")">NO</option>
                            </select>
                        </td>
                        <td>
                            <select asp-for="AgenciaEnEdicion.RstBranch" class="form-select" required>
                                <option value="SI" selected="@(agenciaEditar.RstBranch == "SI")">SI</option>
                                <option value="NO" selected="@(agenciaEditar.RstBranch == "NO")">NO</option>
                            </select>
                        </td>
                        <td><input asp-for="AgenciaEnEdicion.IpSer" class="form-control" /></td>
                        <td><input asp-for="AgenciaEnEdicion.NomSer" class="form-control" /></td>
                        <td><input asp-for="AgenciaEnEdicion.NomBD" class="form-control" /></td>
                        <td class="text-center">
                            <button type="submit" class="btn btn-success btn-sm me-1">Guardar</button>
                            <a asp-action="Index" class="btn btn-secondary btn-sm">Cancelar</a>
                        </td>
                    </form>
                </tr>
            }
            else
            {
                <tr>
                    <td>@item.Codcco</td>
                    <td>@item.NomAge</td>
                    <td>@(item.Zona switch {
                        1 => "CENTRO SUR",
                        2 => "NOR OCCIDENTE",
                        3 => "NOR ORIENTE",
                        _ => "DESCONOCIDA"
                    })</td>
                    <td>@item.Marquesina</td>
                    <td>@item.RstBranch</td>
                    <td>@item.IpSer</td>
                    <td>@item.NomSer</td>
                    <td>@item.NomBD</td>
                    <td class="text-center">
                        <a asp-action="Index" asp-route-editId="@item.Codcco" asp-route-codcco="@codccoSel" class="btn btn-warning btn-sm me-1">Editar</a>
                        <form asp-action="Eliminar" asp-route-id="@item.Codcco" method="post" class="d-inline" onsubmit="return confirm('¿Está seguro de eliminar esta agencia?');">
                            <button type="submit" class="btn btn-danger btn-sm">Eliminar</button>
                        </form>
                    </td>
                </tr>
            }
        }
    </tbody>
</table>

<!-- Paginación -->
@if (lista != null && lista.PageCount > 1)
{
    <div class="d-flex justify-content-center">
        @Html.PagedListPager(
            lista,
            page => Url.Action("Index", new { page, codcco = codccoSel }),
            new PagedListRenderOptions
            {
                UlElementClasses = new[] { "pagination", "justify-content-center" },
                LiElementClasses = new[] { "page-item" },
                PageClasses = new[] { "page-link" }
            }
        )
    </div>
}

<!-- Botón agregar -->
<div class="mt-4">
    <a asp-action="Agregar" class="btn btn-primary">Agregar Nueva Agencia</a>
</div>


Se puede ver que hay una columna eliminar y editar, esa columna se debe de agregar a la de mensajes para que sea consistente.
