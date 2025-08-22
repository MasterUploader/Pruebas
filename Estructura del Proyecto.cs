using CAUAdministracion.Helpers;
using CAUAdministracion.Models;
using CAUAdministracion.Services.Usuarios; // <- tu interfaz/servicio de usuarios
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

public class UsuariosController : Controller
{
    private readonly IUsuarioService _usuarioService;

    public UsuariosController(IUsuarioService usuarioService)
    {
        _usuarioService = usuarioService;
    }

    // ========= AGREGAR =========

    // Formulario
    [AutorizarPorTipoUsuario("1")]
    [HttpGet]
    public IActionResult Agregar()
    {
        return View();
    }

    // Guardar
    [AutorizarPorTipoUsuario("1")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Agregar(
        [FromForm] string Usuario,
        [FromForm] int    TipoUsu,
        [FromForm] string Estado,
        [FromForm] string Clave,
        [FromForm] string ConfirmarClave)
    {
        // --- Validaciones básicas del lado servidor ---
        if (string.IsNullOrWhiteSpace(Usuario))
            ModelState.AddModelError(nameof(Usuario), "El usuario es obligatorio.");

        if (TipoUsu < 1 || TipoUsu > 3)
            ModelState.AddModelError(nameof(TipoUsu), "Tipo de usuario inválido.");

        if (Estado != "A" && Estado != "I")
            ModelState.AddModelError(nameof(Estado), "Estado inválido.");

        if (string.IsNullOrWhiteSpace(Clave) || string.IsNullOrWhiteSpace(ConfirmarClave))
            ModelState.AddModelError(nameof(Clave), "Debe indicar la clave y su confirmación.");

        if (!string.Equals(Clave, ConfirmarClave))
            ModelState.AddModelError(nameof(ConfirmarClave), "Las claves no coinciden.");

        // Usuario duplicado
        if (await _usuarioService.ExisteUsuarioAsync(Usuario))
            ModelState.AddModelError(nameof(Usuario), "El usuario ya existe.");

        if (!ModelState.IsValid)
            return View(); // la vista rehidrata desde Request.Form

        // --- Construcción del modelo de dominio ---
        var nuevo = new UsuarioModel
        {
            Usuario = Usuario,
            TipoUsu = TipoUsu,
            Estado  = Estado
        };

        // --- Creación en base de datos ---
        // Usa el método que ya tienes en tu servicio.
        // Si tu servicio expone otro nombre/firma (p.ej. InsertarUsuarioAsync),
        // sustituye la línea siguiente.
        var ok = await _usuarioService.CrearUsuarioAsync(nuevo, Clave);

        if (!ok)
        {
            ModelState.AddModelError(string.Empty, "No se pudo crear el usuario.");
            return View();
        }

        TempData["Mensaje"] = $"Usuario {Usuario} creado correctamente.";
        return RedirectToAction(nameof(Index));
    }

    // ... (resto de endpoints del controlador)
}
