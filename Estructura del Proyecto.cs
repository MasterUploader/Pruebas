@model List<CAUAdministracion.Models.Video.VideoModel>
@{
    ViewBag.Title = "Mantenimiento de Videos";
}

<h2>Mantenimiento de Videos</h2>

@if (ViewBag.Mensaje != null)
{
    <div class="alert alert-info">@ViewBag.Mensaje</div>
}

<form asp-action="Actualizar" method="post">
    <table class="table table-bordered table-hover">
        <thead class="table-dark">
            <tr>
                <th>Agencia</th>
                <th>ID Video</th>
                <th>Nombre Archivo</th>
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
                        <button type="submit" formaction="@Url.Action("Actualizar", new { codVideo = Model[i].CodVideo, codcco = Model[i].Codcco })" class="btn btn-primary btn-sm">Guardar</button>
                        <button type="submit" formaction="@Url.Action("Eliminar", new { codVideo = Model[i].CodVideo, codcco = Model[i].Codcco })" class="btn btn-danger btn-sm" onclick="return confirm('¿Está seguro de eliminar este video?');">Eliminar</button>
                    </td>
                </tr>
            }
        </tbody>
    </table>
</form>
