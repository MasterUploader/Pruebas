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
                            <option value="A" @(item.Estado == "A" ? "selected" : "")>Activo</option>
                            <option value="I" @(item.Estado == "I" ? "selected" : "")>Inactivo</option>
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





@model CAUAdministracion.Models.MensajeModel
@using Microsoft.AspNetCore.Mvc.Rendering

@{
    ViewData["Title"] = "Agregar Mensaje";
    var agencias = ViewBag.Agencias as List<SelectListItem>;
}

<h2 class="text-danger">@ViewData["Title"]</h2>

@if (!string.IsNullOrEmpty(ViewBag.Mensaje))
{
    <div class="alert alert-info alert-dismissible fade show" role="alert">
        @ViewBag.Mensaje
        <button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Close"></button>
    </div>
}

<form asp-action="Agregar" asp-controller="Messages" method="post">
    @Html.AntiForgeryToken()

    <div class="mb-3">
        <label for="codcco" class="form-label">Agencia</label>
        <select id="codcco" name="Codcco" class="form-select" required>
            <option value="">Seleccione una agencia</option>
            @if (agencias != null)
            {
                foreach (var a in agencias)
                {
                    <option value="@a.Value">@a.Text</option>
                }
            }
        </select>
    </div>

    <div class="mb-3">
        <label for="mensaje" class="form-label">Mensaje</label>
        <textarea id="mensaje" name="Mensaje" class="form-control" rows="4" required>@Model?.Mensaje</textarea>
    </div>

    <div class="mb-3">
        <label for="estado" class="form-label">Estado</label>
        <select id="estado" name="Estado" class="form-select" required>
            <option value="A" selected>Activo</option>
            <option value="I">Inactivo</option>
        </select>
    </div>

    <button type="submit" class="btn btn-primary">Guardar</button>
    <a asp-controller="Messages" asp-action="Index" class="btn btn-secondary ms-2">Cancelar</a>
</form>
