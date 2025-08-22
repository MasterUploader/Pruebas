Tengo un problema, en esta vista de mensajes siempre esta activado la opción de actualizar cuando deberia dar clic en cual fila deseo actualizar, porque así como esta ahorita siempre toma el primer elemento, a pesar que no es el que quiero actualiza.
    Te dejo el codigo de la vista para que lo revises:

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




@model CAUAdministracion.Models.MensajeModel
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

<form asp-action="Agregar" method="post">
    <div class="mb-3">
        <label for="codcco" class="form-label">Agencia</label>
        <select id="codcco" name="Codcco" class="form-select" asp-for="Codcco" required>
            <option value="">Seleccione una agencia</option>
            @foreach (var agencia in agencias)
            {
                <option value="@agencia.Value">@agencia.Text</option>
            }
        </select>
    </div>

    <div class="mb-3">
        <label for="mensaje" class="form-label">Mensaje</label>
        <textarea class="form-control" id="mensaje" name="Mensaje" rows="4" required>@Model?.Mensaje</textarea>
    </div>

    <div class="mb-3">
        <label for="estado" class="form-label">Estado</label>
        <select id="estado" name="Estado" class="form-select" asp-for="Estado" required>
            <option value="A" selected>Activo</option>
            <option value="I">Inactivo</option>
        </select>
    </div>

    <button type="submit" class="btn btn-primary">Guardar</button>
    <a asp-controller="Messages" asp-action="Index" class="btn btn-secondary ms-2">Cancelar</a>
</form>



Entregame el codigo corregio completo listo para copiar y pegar
