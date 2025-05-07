@model CAUAdministracion.Models.AgenciaModel

@{
    ViewData["Title"] = "Agregar Agencia";
}

<h2 class="text-primary mb-4">@ViewData["Title"]</h2>

<form asp-action="Agregar" method="post">
    <div class="mb-3">
        <label asp-for="Codcco" class="form-label"></label>
        <input asp-for="Codcco" class="form-control" />
        <span asp-validation-for="Codcco" class="text-danger"></span>
    </div>

    <div class="mb-3">
        <label asp-for="NomAge" class="form-label"></label>
        <input asp-for="NomAge" class="form-control" maxlength="40" />
        <span asp-validation-for="NomAge" class="text-danger"></span>
    </div>

    <div class="mb-3">
        <label asp-for="Zona" class="form-label"></label>
        <select asp-for="Zona" class="form-select">
            <option value="">-- Seleccione Zona --</option>
            <option value="1">CENTRO SUR</option>
            <option value="2">NOR OCCIDENTE</option>
            <option value="3">NOR ORIENTE</option>
        </select>
        <span asp-validation-for="Zona" class="text-danger"></span>
    </div>

    <div class="mb-3">
        <label asp-for="Marquesina" class="form-label">¿Aplica Marquesina?</label>
        <select asp-for="Marquesina" class="form-select">
            <option value="NO">NO</option>
            <option value="SI">SI</option>
        </select>
        <span asp-validation-for="Marquesina" class="text-danger"></span>
    </div>

    <div class="mb-3">
        <label asp-for="RstBranch" class="form-label">¿Aplica RST Branch?</label>
        <select asp-for="RstBranch" class="form-select">
            <option value="NO">NO</option>
            <option value="SI">SI</option>
        </select>
        <span asp-validation-for="RstBranch" class="text-danger"></span>
    </div>

    <div class="mb-3">
        <label asp-for="IpSer" class="form-label"></label>
        <input asp-for="IpSer" class="form-control" maxlength="20" />
        <span asp-validation-for="IpSer" class="text-danger"></span>
    </div>

    <div class="mb-3">
        <label asp-for="NomSer" class="form-label"></label>
        <input asp-for="NomSer" class="form-control" maxlength="18" />
        <span asp-validation-for="NomSer" class="text-danger"></span>
    </div>

    <div class="mb-3">
        <label asp-for="NomBD" class="form-label"></label>
        <input asp-for="NomBD" class="form-control" maxlength="20" />
        <span asp-validation-for="NomBD" class="text-danger"></span>
    </div>

    <button type="submit" class="btn btn-primary">Guardar</button>
    <a asp-action="Index" class="btn btn-secondary ms-2">Cancelar</a>
</form>

@section Scripts {
    @{await Html.RenderPartialAsync("_ValidationScriptsPartial");}
}
