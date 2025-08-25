@model CAUAdministracion.Models.UsuarioEditModel
@{
    ViewData["Title"] = "Editar usuario";
}
<h3>@ViewData["Title"]</h3>

<form asp-action="Actualizar" method="post">
    @Html.AntiForgeryToken()

    <div class="mb-3">
        <label class="form-label">Usuario</label>
        <input asp-for="Usuario" class="form-control" readonly />
    </div>

    <div class="mb-3">
        <label class="form-label">Tipo de usuario</label>
        <select asp-for="TipoUsuario" class="form-select">
            <option value="1">Administrador</option>
            <option value="2">Admin. Videos</option>
            <option value="3">Admin. Mensajes</option>
        </select>
        <span asp-validation-for="TipoUsuario" class="text-danger"></span>
    </div>

    <div class="mb-3">
        <label class="form-label">Estado</label>
        <select asp-for="Estado" class="form-select">
            <option value="A">Activo</option>
            <option value="I">Inactivo</option>
        </select>
        <span asp-validation-for="Estado" class="text-danger"></span>
    </div>

    <div class="mb-3">
        <label class="form-label">Nueva contraseña (opcional)</label>
        <input asp-for="PASS" class="form-control" type="password" />
        <span class="form-text">Si la dejas vacía, no se cambiará.</span>
    </div>

    <button class="btn btn-primary" type="submit">Guardar</button>
    <a asp-action="Index" class="btn btn-secondary">Volver</a>
</form>
