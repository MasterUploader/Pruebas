@model CAUAdministracion.Models.AgenciaModel
@{
    ViewData["Title"] = "Agregar Agencia";
}

<h2 class="text-danger mb-4">@ViewData["Title"]</h2>

@if (ViewBag.Mensaje != null)
{
    <div class="alert alert-info alert-dismissible fade show" role="alert">
        @ViewBag.Mensaje
        <button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Close"></button>
    </div>
}

<form asp-action="Agregar" method="post">
    <div asp-validation-summary="ModelOnly" class="text-danger mb-3"></div>

    <!-- Centro de Costo -->
    <div class="mb-3">
        <label asp-for="Codcco" class="form-label">Centro de Costo</label>
        <input asp-for="Codcco" class="form-control" maxlength="3" required />
        <span asp-validation-for="Codcco" class="text-danger"></span>
    </div>

    <!-- Nombre de Agencia -->
    <div class="mb-3">
        <label asp-for="Nomage" class="form-label">Nombre de Agencia</label>
        <input asp-for="Nomage" class="form-control" maxlength="40" required />
        <span asp-validation-for="Nomage" class="text-danger"></span>
    </div>

    <!-- Zona -->
    <div class="mb-3">
        <label asp-for="Zona" class="form-label">Zona</label>
        <select asp-for="Zona" class="form-select" required>
            <option value="">Seleccione una zona</option>
            <option value="1">CENTRO SUR</option>
            <option value="2">NOR OCCIDENTE</option>
            <option value="3">NOR ORIENTE</option>
        </select>
        <span asp-validation-for="Zona" class="text-danger"></span>
    </div>

    <!-- IP Servidor -->
    <div class="mb-3">
        <label asp-for="Ipser" class="form-label">IP del Servidor</label>
        <input asp-for="Ipser" class="form-control" maxlength="20" />
        <span asp-validation-for="Ipser" class="text-danger"></span>
    </div>

    <!-- Nombre del Servidor -->
    <div class="mb-3">
        <label asp-for="Nomser" class="form-label">Nombre del Servidor</label>
        <input asp-for="Nomser" class="form-control" maxlength="18" />
        <span asp-validation-for="Nomser" class="text-danger"></span>
    </div>

    <!-- Nombre de la Base de Datos -->
    <div class="mb-3">
        <label asp-for="Nombd" class="form-label">Nombre de Base de Datos</label>
        <input asp-for="Nombd" class="form-control" maxlength="18" />
        <span asp-validation-for="Nombd" class="text-danger"></span>
    </div>

    <!-- Checkboxes -->
    <div class="mb-3 form-check form-switch">
        <input asp-for="Marquesina" class="form-check-input" type="checkbox" value="true" />
        <label class="form-check-label" asp-for="Marquesina">¿Aplica Marquesina?</label>
    </div>

    <div class="mb-3 form-check form-switch">
        <input asp-for="Rstbranch" class="form-check-input" type="checkbox" value="true" />
        <label class="form-check-label" asp-for="Rstbranch">¿Aplica Reinicio Branch?</label>
    </div>

    <!-- Botones -->
    <div class="d-flex justify-content-start mt-4">
        <button type="submit" class="btn btn-success me-2">Guardar</button>
        <a asp-action="Index" class="btn btn-secondary">Cancelar</a>
    </div>
</form>

@section Scripts {
    <partial name="_ValidationScriptsPartial" />
}
