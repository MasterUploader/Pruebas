using System.ComponentModel.DataAnnotations;

namespace CAUAdministracion.Models
{
    /// <summary>
    /// DTO para crear un nuevo usuario de administración.
    /// </summary>
    public class UsuarioCreateModel
    {
        [Required(ErrorMessage = "El usuario es obligatorio")]
        [StringLength(32, ErrorMessage = "Máximo 32 caracteres")]
        public string Usuario { get; set; } = string.Empty;

        [Required(ErrorMessage = "La clave es obligatoria")]
        [StringLength(10, ErrorMessage = "Máximo 10 caracteres")]
        [DataType(DataType.Password)]
        public string Clave { get; set; } = string.Empty;

        [Required(ErrorMessage = "Debe confirmar la clave")]
        [StringLength(10)]
        [DataType(DataType.Password)]
        [Compare(nameof(Clave), ErrorMessage = "Las claves no coinciden")]
        public string ConfirmarClave { get; set; } = string.Empty;

        /// <summary>
        /// 1=Administrador, 2=Admin. Videos, 3=Admin. Mensajes
        /// </summary>
        [Range(1, 3, ErrorMessage = "Tipo de usuario inválido")]
        public int TipoUsuario { get; set; } = 2;

        /// <summary>
        /// A = Activo, I = Inactivo
        /// </summary>
        [Required]
        [RegularExpression("A|I", ErrorMessage = "Estado inválido")]
        public string Estado { get; set; } = "A";
    }
}



using CAUAdministracion.Models;

namespace CAUAdministracion.Services.Usuarios
{
    public interface IUsuarioService
    {
        bool ExisteUsuario(string usuario);
        bool InsertarUsuario(UsuarioCreateModel model);
    }
}





using System.Data;
using System.Data.Common;
using CAUAdministracion.Models;
using CAUAdministracion.Services.Usuarios;
using Connections.Interfaces;

namespace CAUAdministracion.Services.Usuarios
{
    /// <summary>
    /// Servicio para crear usuarios en AS400 (tabla BCAH96DTA.USUADMIN).
    /// </summary>
    public class UsuarioService : IUsuarioService
    {
        private readonly IDatabaseConnection _as400;

        public UsuarioService(IDatabaseConnection as400)
        {
            _as400 = as400;
        }

        public bool ExisteUsuario(string usuario)
        {
            try
            {
                _as400.Open();
                using var cmd = _as400.GetDbCommand();
                cmd.CommandText = "SELECT 1 FROM BCAH96DTA.USUADMIN WHERE USUARIO = @u FETCH FIRST 1 ROWS ONLY";
                var p = cmd.CreateParameter();
                p.ParameterName = "@u";
                p.Value = usuario;
                cmd.Parameters.Add(p);

                var result = cmd.ExecuteScalar();
                return result != null && result != DBNull.Value;
            }
            catch
            {
                // Ante error, asumimos que no existe para permitir captura manual del fallo en Insertar
                return false;
            }
            finally
            {
                _as400.Close();
            }
        }

        public bool InsertarUsuario(UsuarioCreateModel model)
        {
            try
            {
                // Cifrado placeholder (reemplazar por el algoritmo legacy real si lo tienes)
                string claveCifrada = CifrarClaveLegacy(model.Clave);

                _as400.Open();
                using var cmd = _as400.GetDbCommand();

                // Evita concatenaciones; usa parámetros
                cmd.CommandText = @"
                    INSERT INTO BCAH96DTA.USUADMIN (USUARIO, CLAVE, TIPO, ESTADO)
                    VALUES (@usuario, @clave, @tipo, @estado)";

                var p1 = cmd.CreateParameter(); p1.ParameterName = "@usuario"; p1.Value = model.Usuario;          cmd.Parameters.Add(p1);
                var p2 = cmd.CreateParameter(); p2.ParameterName = "@clave";   p2.Value = claveCifrada;           cmd.Parameters.Add(p2);
                var p3 = cmd.CreateParameter(); p3.ParameterName = "@tipo";    p3.Value = model.TipoUsuario;      cmd.Parameters.Add(p3);
                var p4 = cmd.CreateParameter(); p4.ParameterName = "@estado";  p4.Value = model.Estado;           cmd.Parameters.Add(p4);

                int rows = cmd.ExecuteNonQuery();
                return rows > 0;
            }
            catch
            {
                return false;
            }
            finally
            {
                _as400.Close();
            }
        }

        /// <summary>
        /// Placeholder: reemplaza por OperacionesVarias.encriptarCadena (o el cifrado real del legacy).
        /// </summary>
        private static string CifrarClaveLegacy(string clave)
        {
            // *** IMPORTANTE ***
            // Esto es solo para tener el flujo funcionando.
            // Reemplaza por el mismo algoritmo usado antes (OperacionesVarias.encriptarCadena).
            return Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(clave));
        }
    }
}



using CAUAdministracion.Helpers;
using CAUAdministracion.Models;
using CAUAdministracion.Services.Usuarios;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CAUAdministracion.Controllers
{
    /// <summary>
    /// Alta de usuarios administrados (tabla USUADMIN).
    /// Solo tipo 1 (Administrador) debe acceder.
    /// </summary>
    [Authorize]
    public class UsuariosController : Controller
    {
        private readonly IUsuarioService _usuarioService;

        public UsuariosController(IUsuarioService usuarioService)
        {
            _usuarioService = usuarioService;
        }

        // GET: /Usuarios/Agregar
        [HttpGet]
        [AutorizarPorTipoUsuario("1")]
        public IActionResult Agregar()
        {
            return View(new UsuarioCreateModel());
        }

        // POST: /Usuarios/Agregar
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AutorizarPorTipoUsuario("1")]
        public IActionResult Agregar(UsuarioCreateModel model)
        {
            // Validaciones adicionales de negocio
            if (string.IsNullOrWhiteSpace(model.Usuario))
                ModelState.AddModelError(nameof(model.Usuario), "Debe ingresar el usuario.");

            if (model.TipoUsuario < 1 || model.TipoUsuario > 3)
                ModelState.AddModelError(nameof(model.TipoUsuario), "Tipo de usuario inválido.");

            if (model.Estado != "A" && model.Estado != "I")
                ModelState.AddModelError(nameof(model.Estado), "Estado inválido.");

            if (!ModelState.IsValid)
                return View(model);

            // Usuario duplicado
            if (_usuarioService.ExisteUsuario(model.Usuario))
            {
                ModelState.AddModelError(nameof(model.Usuario), "El usuario ya existe.");
                return View(model);
            }

            var ok = _usuarioService.InsertarUsuario(model);
            if (ok)
            {
                TempData["Mensaje"] = $"Usuario '{model.Usuario}' agregado correctamente.";
                return RedirectToAction("Index", "Home");
            }

            ModelState.AddModelError(string.Empty, "Ocurrió un error al guardar el usuario.");
            return View(model);
        }
    }
}




@model CAUAdministracion.Models.UsuarioCreateModel
@{
    ViewData["Title"] = "Agregar Nuevo Usuario";
}

<h2 class="text-danger mb-3">@ViewData["Title"]</h2>

@if (TempData["Mensaje"] != null)
{
    <div class="alert alert-success alert-dismissible fade show" role="alert">
        @TempData["Mensaje"]
        <button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Close"></button>
    </div>
}

<form asp-action="Agregar" method="post" class="row g-3" autocomplete="off">
    @Html.AntiForgeryToken()

    <div asp-validation-summary="ModelOnly" class="text-danger"></div>

    <div class="col-md-6">
        <label asp-for="Usuario" class="form-label">Usuario</label>
        <input asp-for="Usuario" class="form-control" maxlength="32" />
        <span asp-validation-for="Usuario" class="text-danger"></span>
    </div>

    <div class="col-md-3">
        <label asp-for="TipoUsuario" class="form-label">Tipo de Usuario</label>
        <select asp-for="TipoUsuario" class="form-select">
            <option value="1">Administrador</option>
            <option value="2">Admin. Videos</option>
            <option value="3">Admin. Mensajes</option>
        </select>
        <span asp-validation-for="TipoUsuario" class="text-danger"></span>
    </div>

    <div class="col-md-3">
        <label asp-for="Estado" class="form-label">Estado</label>
        <select asp-for="Estado" class="form-select">
            <option value="A">Activo</option>
            <option value="I">Inactivo</option>
        </select>
        <span asp-validation-for="Estado" class="text-danger"></span>
    </div>

    <div class="col-md-6">
        <label asp-for="Clave" class="form-label">Clave</label>
        <input asp-for="Clave" class="form-control" maxlength="10" />
        <span asp-validation-for="Clave" class="text-danger"></span>
    </div>

    <div class="col-md-6">
        <label asp-for="ConfirmarClave" class="form-label">Confirmar Clave</label>
        <input asp-for="ConfirmarClave" class="form-control" maxlength="10" />
        <span asp-validation-for="ConfirmarClave" class="text-danger"></span>
    </div>

    <div class="col-12 mt-2">
        <button type="submit" class="btn btn-primary">Aceptar</button>
        <a asp-controller="Home" asp-action="Index" class="btn btn-secondary ms-2">Cancelar</a>
    </div>
</form>

@section Scripts{
    @{await Html.RenderPartialAsync("_ValidationScriptsPartial");}
}



// ...
using CAUAdministracion.Services.Usuarios;
// ...
builder.Services.AddScoped<IUsuarioService, UsuarioService>();
// ...



