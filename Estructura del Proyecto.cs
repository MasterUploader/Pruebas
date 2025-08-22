Este es el controlador de mensajes, ya existe un endpoint actualizar, usa ese:

using CAUAdministracion.Helpers;
using CAUAdministracion.Models;
using CAUAdministracion.Services.Menssages;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace CAUAdministracion.Controllers;


[Authorize]
public class MessagesController : Controller
{
    private readonly IMensajeService _mensajeService;

    public MessagesController(IMensajeService mensajeService)
    {
        _mensajeService = mensajeService;
    }

    // =======================================
    //     1. AGREGAR NUEVO MENSAJE
    // =======================================
    [AutorizarPorTipoUsuario("1", "3")]
    [HttpGet]
    public IActionResult Agregar()
    {
        // Cargar la lista de agencias para el selector
        var agencias = _mensajeService.ObtenerAgenciasSelectList();
        ViewBag.Agencias = agencias;

        return View();
    }

    [AutorizarPorTipoUsuario("1", "3")]
    [HttpPost]
    public IActionResult Agregar(MensajeModel model)
    {
        // Validar datos obligatorios básicos
        if (string.IsNullOrWhiteSpace(model.Codcco) || string.IsNullOrWhiteSpace(model.Mensaje))
        {
            ModelState.AddModelError("", "Debe completar todos los campos.");
        }

        // Obtener secuencia antes de validar
        model.Seq = _mensajeService.GetSecuencia(model.Codcco); // <- Aquí estableces la secuencia

        // Si el modelo aún no es válido, regresar la vista
        if (!ModelState.IsValid)
        {
            ViewBag.Agencias = _mensajeService.ObtenerAgenciasSelectList();
            return View(model);
        }

        // Insertar mensaje
        bool ok = _mensajeService.InsertarMensaje(model);

        if (ok)
            return RedirectToAction("Index");

        ViewBag.Mensaje = "Error al guardar el mensaje.";
        ViewBag.Agencias = _mensajeService.ObtenerAgenciasSelectList();
        return View(model);
    }

    // =======================================
    //     2. MANTENIMIENTO DE MENSAJES
    // =======================================

    [AutorizarPorTipoUsuario("1", "3")]
    [HttpGet]
    public async Task<IActionResult> Index(string codcco = null)
    {
        // Obtener todas las agencias para el filtro
        var agencias = _mensajeService.ObtenerAgenciasSelectList();
        ViewBag.Agencias = agencias;
        ViewBag.CodigoAgenciaSeleccionado = codcco;

        // Obtener mensajes filtrados si hay código de agencia
        var mensajes = await _mensajeService.ObtenerMensajesAsync();
        if (!string.IsNullOrEmpty(codcco))
            mensajes = mensajes.Where(m => m.Codcco == codcco).ToList();

        return View(mensajes);
    }

    [AutorizarPorTipoUsuario("1", "3")]
    [HttpPost]
    public IActionResult Actualizar(int codMsg, string codcco, string mensaje, string estado, int seq)
    {
        var model = new MensajeModel
        {
            CodMsg = codMsg,
            Codcco = codcco,
            Mensaje = mensaje,
            Estado = estado,
            Seq = seq
        };

        var actualizado = _mensajeService.ActualizarMensaje(model);

        TempData["Mensaje"] = actualizado
            ? "Mensaje actualizado correctamente."
            : "Error al actualizar el mensaje.";

        return RedirectToAction("Index", new { codcco = codcco });
    }

    [AutorizarPorTipoUsuario("1", "3")]
    [HttpPost]
    public IActionResult Eliminar(int codMsg, string codcco)
    {
        // Validar si el mensaje tiene dependencias
        if (_mensajeService.TieneDependencia(codcco, codMsg))
        {
            TempData["Mensaje"] = "No se puede eliminar el mensaje porque tiene dependencias.";
            return RedirectToAction("Index", new { codcco = codcco });
        }

        var eliminado = _mensajeService.EliminarMensaje(codMsg);

        TempData["Mensaje"] = eliminado
            ? "Mensaje eliminado correctamente."
            : "Error al eliminar el mensaje.";

        return RedirectToAction("Index", new { codcco = 0 });
    }
}
