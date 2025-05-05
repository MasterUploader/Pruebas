@model List<CAUAdministracion.Models.Video.VideoModel>
@using Microsoft.AspNetCore.Mvc.Rendering
@{
    ViewBag.Title = "Mantenimiento de Videos";
    var agencias = ViewBag.Agencias as List<SelectListItem>;
    string codccoActual = ViewBag.Codcco as string;
}

<h2>Mantenimiento de Videos</h2>

<!-- Mensaje general -->
@if (ViewBag.Mensaje != null)
{
    <div class="alert alert-info">@ViewBag.Mensaje</div>
}

<!-- Selector de agencia -->
<form asp-action="Index" method="get" class="mb-4">
    <div class="row g-2 align-items-end">
        <div class="col-auto">
            <label for="codcco" class="form-label">Seleccione Agencia:</label>
            <select id="codcco" name="codcco" class="form-select" required>
                <option value="">-- Seleccione --</option>
                @foreach (var agencia in agencias)
                {
                    <option value="@agencia.Value" @(agencia.Value == codccoActual ? "selected" : "")>
                        @agencia.Text
                    </option>
                }
            </select>
        </div>
        <div class="col-auto">
            <button type="submit" class="btn btn-primary">Buscar</button>
        </div>
    </div>
</form>

<!-- Tabla solo si hay datos -->
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
                @for (int i = 0; i < Model.Count; i++)
                {
                    <tr>
                        <td>
                            @Html.DisplayFor(m => m[i].Codcco)
                            <input type="hidden" name="videos[@i].Codcco" value="@Model[i].Codcco" />
                        </td>
                        <td>
                            @Html.DisplayFor(m => m[i].CodVideo)
                            <input type="hidden" name="videos[@i].CodVideo" value="@Model[i].CodVideo" />
                        </td>
                        <td>@Model[i].Nombre</td>
                        <td>
                            <input type="number" name="videos[@i].Seq" value="@Model[i].Seq" class="form-control" />
                        </td>
                        <td>
                            <select name="videos[@i].Estado" class="form-control">
                                <option value="A" selected="@("A" == Model[i].Estado)">Activo</option>
                                <option value="I" selected="@("I" == Model[i].Estado)">Inactivo</option>
                            </select>
                        </td>
                        <td>
                            <button type="submit"
                                    formaction="@Url.Action("Actualizar", new { codVideo = Model[i].CodVideo, codcco = Model[i].Codcco })"
                                    class="btn btn-primary btn-sm">
                                Guardar
                            </button>

                            <button type="submit"
                                    formaction="@Url.Action("Eliminar", new { codVideo = Model[i].CodVideo, codcco = Model[i].Codcco })"
                                    class="btn btn-danger btn-sm"
                                    onclick="return confirm('¿Está seguro de eliminar este video?');">
                                Eliminar
                            </button>
                        </td>
                    </tr>
                }
            </tbody>
        </table>
    </form>
}
else if (!string.IsNullOrEmpty(codccoActual))
{
    <div class="alert alert-warning">No se encontraron videos para la agencia seleccionada.</div>
}
