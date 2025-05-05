@model List<MensajeModel>
@{
    ViewData["Title"] = "Mantenimiento de Mensajes";
    var codccoActual = Context.Request.Query["codcco"].ToString();
}

<h2 class="text-danger">Mantenimiento de Mensajes</h2>

<div class="mb-3">
    <form method="get" asp-controller="Messages" asp-action="Index">
        <label for="codcco">Agencia:</label>
        <select name="codcco" class="form-select" style="width: 300px; display:inline-block;" onchange="this.form.submit()">
            <option value="">-- Seleccione Agencia --</option>
            @foreach (var agencia in ViewBag.Agencias as List<SelectListItem>)
            {
                <option value="@agencia.Value" @(agencia.Value == codccoActual ? "selected" : "")>
                    @agencia.Text
                </option>
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
                            <select name="Estado" class="form-select">
                                <option value="A" @(item.Estado == "A" ? "selected" : "")>Activo</option>
                                <option value="I" @(item.Estado == "I" ? "selected" : "")>Inactivo</option>
                            </select>
                        </td>
                        <td>
                            <button type="submit" formaction="@Url.Action("Actualizar", "Messages")" class="btn btn-sm btn-success">Actualizar</button>
                        </td>
                        <td>
                            <form method="post" asp-action="Eliminar">
                                <input type="hidden" name="CodMsg" value="@item.CodMsg" />
                                <input type="hidden" name="Codcco" value="@item.Codcco" />
                                <button type="submit" class="btn btn-sm btn-danger" onclick="return confirm('¿Está seguro de eliminar este mensaje?');">Eliminar</button>
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
