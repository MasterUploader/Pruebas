@model CAUAdministracion.Models.MensajeModel
@{
    ViewData["Title"] = "Agregar Mensaje";
    var agencias = ViewBag.Agencias as List<SelectListItem>;
}

<h2>Agregar Nuevo Mensaje</h2>

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
