@model CAUAdministracion.Models.VideoModel
@{
    ViewData["Title"] = "Agregar Video";
    var agencias = ViewBag.Agencias as List<SelectListItem>;
}

<h2 class="text-danger">@ViewData["Title"]</h2>

<form asp-action="Agregar" method="post" enctype="multipart/form-data">
    <div asp-validation-summary="ModelOnly" class="text-danger mb-3"></div>

    <!-- Campo: Código de Agencia -->
    <div class="mb-3">
        <label asp-for="Codcco" class="form-label">Código de Agencia</label>
        <select asp-for="Codcco" class="form-select" required>
            <option value="">Seleccione una agencia</option>
            @if (agencias != null)
            {
                foreach (var agencia in agencias)
                {
                    <option value="@agencia.Value">@agencia.Text</option>
                }
            }
        </select>
        <span asp-validation-for="Codcco" class="text-danger"></span>
        <small class="form-text text-muted">Seleccione una o ingrese 0 si aplica a todas las agencias.</small>
    </div>

    <!-- Campo: Estado del Video -->
    <div class="mb-3">
        <label for="estado" class="form-label">Estado</label>
        <select class="form-select" id="estado" name="estado" required>
            <option value="">Seleccione...</option>
            <option value="A">Activo</option>
            <option value="I">Inactivo</option>
        </select>
    </div>

    <!-- Campo: Archivo de Video -->
    <div class="mb-3">
        <label for="archivo" class="form-label">Archivo de Video</label>
        <input type="file" class="form-control" id="archivo" name="archivo" accept="video/*" required />
    </div>

    <!-- Botón Enviar -->
    <button type="submit" class="btn btn-primary">Subir Video</button>
</form>
