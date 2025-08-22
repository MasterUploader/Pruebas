// Models/UsuarioCreateViewModel.cs
using System.ComponentModel.DataAnnotations;

namespace CAUAdministracion.Models
{
    public class UsuarioCreateViewModel
    {
        [Required, MaxLength(32)]
        public string Usuario { get; set; } = string.Empty;

        [Required, DataType(DataType.Password), StringLength(10, MinimumLength = 1)]
        public string Clave { get; set; } = string.Empty;

        [Required, DataType(DataType.Password), Compare(nameof(Clave), ErrorMessage = "Las claves no coinciden.")]
        public string ConfirmarClave { get; set; } = string.Empty;

        [Required, Range(1, 3, ErrorMessage = "Tipo de usuario inválido.")]
        public int TipoUsu { get; set; }

        [Required, RegularExpression("A|I", ErrorMessage = "Estado inválido.")]
        public string Estado { get; set; } = "A";
    }
}


// Controllers/UsuariosController.cs (extracto)
using CAUAdministracion.Models;
using CAUAdministracion.Services.Usuarios;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[Authorize]
public class UsuariosController : Controller
{
    private readonly IUsuarioService _usuarioService;

    public UsuariosController(IUsuarioService usuarioService)
    {
        _usuarioService = usuarioService;
    }

    [HttpGet]
    public IActionResult Agregar()
    {
        return View(new UsuarioCreateViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Agregar(UsuarioCreateViewModel model)
    {
        // Validaciones de negocio ANTES del IsValid, para que influyan
        if (!string.IsNullOrWhiteSpace(model.Usuario))
        {
            if (await _usuarioService.ExisteUsuarioAsync(model.Usuario.Trim()))
            {
                ModelState.AddModelError(nameof(model.Usuario), "El usuario ya existe.");
            }
        }

        // Si hay cualquier error de DataAnnotations o de negocio, será inválido
        if (!ModelState.IsValid)
            return View(model);

        var usuario = new UsuarioModel
        {
            Usuario = model.Usuario.Trim(),
            TipoUsu = model.TipoUsu,
            Estado  = model.Estado
        };

        var creado = await _usuarioService.CrearUsuarioAsync(usuario, model.Clave);
        if (creado)
        {
            TempData["Mensaje"] = "Usuario creado correctamente.";
            return RedirectToAction("Index");
        }

        ModelState.AddModelError(string.Empty, "No se pudo crear el usuario.");
        return View(model);
    }
}@model CAUAdministracion.Models.UsuarioCreateViewModel
@{
    ViewData["Title"] = "Agregar Usuario";
}
<h2 class="text-danger">@ViewData["Title"]</h2>

<form asp-action="Agregar" method="post" class="mt-3">
    @Html.AntiForgeryToken()

    <div asp-validation-summary="ModelOnly" class="text-danger mb-3"></div>

    <div class="mb-3">
        <label asp-for="Usuario" class="form-label"></label>
        <input asp-for="Usuario" class="form-control" />
        <span asp-validation-for="Usuario" class="text-danger"></span>
    </div>

    <div class="row">
        <div class="col-md-6 mb-3">
            <label asp-for="Clave" class="form-label"></label>
            <input asp-for="Clave" class="form-control" />
            <span asp-validation-for="Clave" class="text-danger"></span>
        </div>
        <div class="col-md-6 mb-3">
            <label asp-for="ConfirmarClave" class="form-label"></label>
            <input asp-for="ConfirmarClave" class="form-control" />
            <span asp-validation-for="ConfirmarClave" class="text-danger"></span>
        </div>
    </div>

    <div class="row">
        <div class="col-md-6 mb-3">
            <label asp-for="TipoUsu" class="form-label"></label>
            <select asp-for="TipoUsu" class="form-select">
                <option value="">-- Seleccione --</option>
                <option value="1">Administrador</option>
                <option value="2">Admin. Videos</option>
                <option value="3">Admin. Mensajes</option>
            </select>
            <span asp-validation-for="TipoUsu" class="text-danger"></span>
        </div>
        <div class="col-md-6 mb-3">
            <label asp-for="Estado" class="form-label"></label>
            <select asp-for="Estado" class="form-select">
                <option value="">-- Seleccione --</option>
                <option value="A">Activo</option>
                <option value="I">Inactivo</option>
            </select>
            <span asp-validation-for="Estado" class="text-danger"></span>
        </div>
    </div>

    <button type="submit" class="btn btn-primary">Guardar</button>
    <a asp-action="Index" class="btn btn-secondary ms-2">Cancelar</a>
</form>

@section Scripts{
    <partial name="_ValidationScriptsPartial" />
}


