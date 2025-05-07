---Mantenimiento Agencias---
@using X.PagedList.Mvc.Core
@using X.PagedList.Mvc
@using X.PagedList
@model CAUAdministracion.Models.AgenciaModel
@{
    ViewData["Title"] = "Mantenimiento de Agencias";
    var lista = (IPagedList<CAUAdministracion.Models.AgenciaModel>)ViewBag.Lista;
}

<h2 class="text-danger mb-4">@ViewData["Title"]</h2>

@if (TempData["Mensaje"] != null)
{
    <div class="alert alert-info alert-dismissible fade show" role="alert">
        @TempData["Mensaje"]
        <button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Close"></button>
    </div>
}

<div class="mb-3">
    <form method="get" asp-controller="Messages" asp-action="Index">
        <label for="codcco">Agencia:</label>
        <select name="codcco" class="form-select" style="width: 300px; display:inline-block;" onchange="this.form.submit()">
            <option value="">-- Seleccione Agencia --</option>
               @foreach (var agencia in (List<SelectListItem>)ViewBag.AgenciasFiltro)
                {
                    var selected = (agencia.Value == ViewBag.CodccoSeleccionado) ? "selected" : "";
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
            if (Model != null && Model.Codcco == item.Codcco)
            {
                <!-- FILA EN EDICIÓN -->
                <tr>
                    <form asp-action="GuardarEdicion" method="post">
                        <td>
                            <input asp-for="Codcco" type="hidden" />
                            @Model.Codcco
                        </td>
                        <td><input asp-for="NomAge" class="form-control" maxlength="40" required /></td>
                        <td>
                            <select asp-for="Zona" class="form-select" required>
                                <option value="1">CENTRO SUR</option>
                                <option value="2">NOR OCCIDENTE</option>
                                <option value="3">NOR ORIENTE</option>
                            </select>
                        </td>
                    <input type="hidden" name="Marquesina" value="NO" />
                    <input type="checkbox" name="Marquesina" value="SI" class="form-check-input" @(item.Marquesina == "SI" ? "checked" : "") />

                    <!-- Para RstBranch -->
                    <input type="hidden" name="RstBranch" value="NO" />
                    <input type="checkbox" name="RstBranch" value="SI" class="form-check-input" @(item.RstBranch == "SI" ? "checked" : "") />
                        <td><input asp-for="IpSer" class="form-control" maxlength="20" required /></td>
                        <td><input asp-for="NomSer" class="form-control" maxlength="18" required /></td>
                        <td><input asp-for="NomBD" class="form-control" maxlength="20" required /></td>
                        <td class="text-center">
                            <button type="submit" class="btn btn-success btn-sm me-1">Guardar</button>
                            <a asp-action="Index" class="btn btn-secondary btn-sm">Cancelar</a>
                        </td>
                    </form>
                </tr>
            }
            else
            {
                <!-- FILA NORMAL -->
                <tr>
                    <td>@item.Codcco</td>
                    <td>@item.NomAge</td>
                    <td>@(item.Zona switch
                    {
                        1 => "CENTRO SUR",
                        2 => "NOR OCCIDENTE",
                        3 => "NOR ORIENTE",
                        _ => "DESCONOCIDA"
                    })</td>
                    <td>@(item.Marquesina)</td>
                    <td>@(item.RstBranch)</td>
                    <td>@item.IpSer</td>
                    <td>@item.NomSer</td>
                    <td>@item.NomBD</td>
                    <td class="text-center">
                        <a asp-action="Index" asp-route-editId="@item.Codcco" class="btn btn-warning btn-sm me-1">Editar</a>
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
                page => Url.Action("Index", new { page, codcco = ViewBag.CodccoSeleccionado }),
                new PagedListRenderOptions
    {
        UlElementClasses = new[] { "pagination", "justify-content-center" },
        LiElementClasses = new[] { "page-item" },
        PageClasses = new[] { "page-link" }
    })
    </div>
}

<!-- Botón agregar -->
<div class="mt-4">
    <a asp-action="Agregar" class="btn btn-primary">Agregar Nueva Agencia</a>
</div>

---Mantenimiento Agencias---



        ---Mantenimiento Mensajes---

        @model List<MensajeModel>
@{
    ViewData["Title"] = "Mantenimiento de Mensajes";
    var codccoActual = Context.Request.Query["codcco"].ToString();
}

<h2 class="text-danger">@ViewData["Title"]</h2>

<div class="mb-3">
    <form method="get" asp-controller="Messages" asp-action="Index">
        <label for="codcco">Agencia:</label>
        <select name="codcco" class="form-select" style="width: 300px; display:inline-block;" onchange="this.form.submit()">
            <option value="">-- Seleccione Agencia --</option>
               @foreach (var agencia in ViewBag.Agencias as List<SelectListItem>)
                {
                    var selected = (agencia.Value == ViewBag.CodccoSeleccionado) ? "selected" : "";
                    @:<option value="@agencia.Value" @selected>@agencia.Text</option>
                }
        </select>
    </form>
</div>

@if (Model != null && Model.Any())
{
    <form asp-action="Actualizar" method="post">
        <table class="table table-bordered table-striped">
            <thead class="table-dark">
                <tr>
                    <th>Código</th>
                    <th>Secuencia</th>
                    <th>Mensaje</th>
                    <th>Estado</th>
                    <th>Actualizar</th>
                    <th>Eliminar</th>
                </tr>
            </thead>
            <tbody>
                @foreach (var item in Model)
                {
                    <tr>
                        <td>
                            <input type="hidden" name="Codcco" value="@item.Codcco" />
                            <input type="hidden" name="CodMsg" value="@item.CodMsg" />
                            @item.CodMsg
                        </td>
                        <td>
                            @item.Seq
                        </td>
                        <td>
                            <input type="text" name="Mensaje" value="@item.Mensaje" class="form-control" />
                        </td>
                        <td>
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
                        </td>
                        <td>
                            <button type="submit" formaction="@Url.Action("Actualizar", "Messages")" class="btn btn-sm btn-success">Actualizar</button>
                        </td>
                        <td>
                            <form method="post" asp-action="Eliminar">
                                <input type="hidden" name="CodMsg" value="@item.CodMsg" />
                                <input type="hidden" name="Codcco" value="@item.Codcco" />
                                <button type="submit" formaction="@Url.Action("Eliminar", "Messages")" class="btn btn-sm btn-danger" onclick="return confirm('¿Está seguro de eliminar este mensaje?');">Eliminar</button>
                            </form>
                        </td>
                    </tr>
                }
            </tbody>
        </table>
    </form>
}
else
{
    <div class="alert alert-info">No se encontraron mensajes para esta agencia.</div>
}

<a href="@Url.Action("Agregar", "Messages")" class="btn btn-primary">Agregar Nuevo Mensaje</a>

        ---Mantenimiento Mensajes---

