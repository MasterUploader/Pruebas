using CAUAdministracion.Models;
using CAUAdministracion.Services.Mensajes;
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

    [HttpGet]
    public IActionResult Agregar()
    {
        // Cargar la lista de agencias para el selector
        var agencias = _mensajeService.ObtenerAgenciasSelectList();
        ViewBag.Agencias = agencias;

        return View();
    }

    [HttpPost]
    public IActionResult Agregar(MensajeModel model)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Agencias = _mensajeService.ObtenerAgenciasSelectList();
            return View(model);
        }

        var insertado = _mensajeService.InsertarMensaje(model);

        if (insertado)
            return RedirectToAction("Index");
        
        ModelState.AddModelError("", "No se pudo insertar el mensaje.");
        ViewBag.Agencias = _mensajeService.ObtenerAgenciasSelectList();
        return View(model);
    }

    // =======================================
    //     2. MANTENIMIENTO DE MENSAJES
    // =======================================

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

        return RedirectToAction("Index", new { codcco = codcco });
    }
}
