@model List<CAUAdministracion.Models.VideoModel>
@{
    ViewData["Title"] = "Mantenimiento de Videos";
    var codccoSeleccionado = ViewBag.CodccoSeleccionado as string;
}

<h2>@ViewData["Title"]</h2>

<!-- Mostrar mensaje si existe -->
@if (!string.IsNullOrEmpty(ViewBag.Mensaje))
{
    <div class="alert alert-info alert-dismissible fade show" role="alert" id="alertMensaje">
        @ViewBag.Mensaje
        <button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Cerrar"></button>
    </div>

    <script>
        setTimeout(function () {
            var alerta = document.getElementById("alertMensaje");
            if (alerta) {
                alerta.classList.remove("show");
                alerta.classList.add("hide");
            }
        }, 5000); // 5 segundos
    </script>
}

<!-- Filtro por agencia -->
<form method="get">
    <div class="row g-2 align-items-end">
        <div class="col-md-4">
            <label for="codcco" class="form-label">Seleccione Agencia:</label>
            <select id="codcco" name="codcco" class="form-select" required>
                <option value="">--Seleccione--</option>
                @foreach (var agencia in ViewBag.Agencias as List<SelectListItem>)
                {
                    var selected = agencia.Value == codccoSeleccionado ? "selected" : "";
                    <text>
                        <option value="@agencia.Value" @selected>@agencia.Text</option>
                    </text>
                }
            </select>
        </div>
        <div class="col-auto">
            <button type="submit" class="btn btn-primary">Filtrar</button>
        </div>
    </div>
</form>

<hr />

<!-- Tabla si hay datos -->
@if (Model != null && Model.Any())
{
    <form method="post">
        <table class="table table-bordered table-hover">
            <thead class="table-dark">
                <tr>
                    <th>Agencia</th>
                    <th>ID Video</th>
                    <th>Nombre</th>
                    <th>Secuencia</th>
                    <th>Estado</th>
                    <th>Acciones</th>
                </tr>
            </thead>
            <tbody>
                @foreach (var item in Model)
                {
                    <tr>
                        <td>@item.Codcco</td>
                        <td>@item.CodVideo</td>
                        <td>@item.Nombre</td>
                        <td>
                            <input type="number" name="Seq" value="@item.Seq" class="form-control" />
                        </td>
                        <td>
                            <select name="Estado" class="form-select">
                                <option value="A" selected="@(item.Estado == "A")">Activo</option>
                                <option value="I" selected="@(item.Estado == "I")">Inactivo</option>
                            </select>
                        </td>
                        <td>
                            <form asp-action="Actualizar" method="post" style="display:inline;">
                                <input type="hidden" name="CodVideo" value="@item.CodVideo" />
                                <input type="hidden" name="Codcco" value="@item.Codcco" />
                                <input type="hidden" name="Seq" value="@item.Seq" />
                                <input type="hidden" name="Estado" value="@item.Estado" />
                                <button type="submit" class="btn btn-sm btn-success">Actualizar</button>
                            </form>

                            <form asp-action="Eliminar" method="post" style="display:inline;" onsubmit="return confirm('¿Está seguro de eliminar este video?');">
                                <input type="hidden" name="CodVideo" value="@item.CodVideo" />
                                <input type="hidden" name="Codcco" value="@item.Codcco" />
                                <button type="submit" class="btn btn-sm btn-danger">Eliminar</button>
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
    <div class="alert alert-info mt-3">No se encontraron videos para esta agencia.</div>
}
