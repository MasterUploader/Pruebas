@{
    ViewData["Title"] = "Agregar Video";
}

<h2 class="mb-4">Agregar nuevo video</h2>

<form asp-action="Agregar" method="post" enctype="multipart/form-data">
    <div asp-validation-summary="ModelOnly" class="text-danger mb-3"></div>

    <!-- Campo: Código de Agencia -->
    <div class="mb-3">
        <label for="codcco" class="form-label">Código de Agencia</label>
        <input type="text" class="form-control" id="codcco" name="codcco" required />
        <small class="form-text text-muted">Ingrese 0 para aplicar a todas las agencias.</small>
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
