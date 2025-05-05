@{
    ViewData["Title"] = "Agregar Video";
}

<h2 class="text-danger">@ViewData["Title"]</h2>

<form asp-action="Agregar" method="post" enctype="multipart/form-data">
    <div asp-validation-summary="ModelOnly" class="text-danger mb-3"></div>

    <!-- Campo: Código de Agencia -->
    <div class="mb-3">
        <label for="codcco" class="form-label">Código de Agencia</label>
        @if (ViewBag.Agencias is List<SelectListItem> listaAgencias)
        {
            var esError = listaAgencias.Count == 1 && string.IsNullOrEmpty(listaAgencias[0].Value);

            <select class="form-select" id="codcco" name="codcco" required>
                @foreach (var agencia in listaAgencias)
                {
                    <option value="@agencia.Value">@agencia.Text</option>
                }
            </select>

            @if (esError)
            {
                <div class="text-danger mt-1">@listaAgencias[0].Text</div>
            }
        }
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

