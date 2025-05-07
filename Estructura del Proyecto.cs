using CAUAdministracion.Models;
using CAUAdministracion.Services.Agencias;
using Microsoft.AspNetCore.Mvc;

namespace CAUAdministracion.Controllers;

/// <summary>
/// Controlador responsable de gestionar las agencias (agregar, listar, editar, eliminar).
/// </summary>
public class AgenciasController : Controller
{
    private readonly IAgenciaService _agenciaService;

    public AgenciasController(IAgenciaService agenciaService)
    {
        _agenciaService = agenciaService;
    }

    /// <summary>
    /// Vista principal de mantenimiento de agencias.
    /// Lista todas las agencias existentes.
    /// </summary>
    public async Task<IActionResult> Index()
    {
        var agencias = await _agenciaService.ObtenerAgenciasAsync();
        return View(agencias);
    }

    /// <summary>
    /// Vista del formulario para agregar una nueva agencia.
    /// </summary>
    public IActionResult Agregar()
    {
        return View();
    }

    /// <summary>
    /// Procesa el formulario de nueva agencia.
    /// Verifica si el centro de costo ya existe antes de insertar.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Agregar(AgenciaModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        // Validar que no exista la agencia
        if (_agenciaService.ExisteCentroCosto(model.Codcco))
        {
            ModelState.AddModelError("", "Ya existe una agencia con ese centro de costo.");
            return View(model);
        }

        // Insertar agencia
        bool insertado = _agenciaService.InsertarAgencia(model);
        if (insertado)
        {
            TempData["Mensaje"] = "Agencia agregada correctamente.";
            return RedirectToAction("Index");
        }

        ModelState.AddModelError("", "Ocurrió un error al agregar la agencia.");
        return View(model);
    }

    /// <summary>
    /// Procesa la actualización de una agencia desde vista en tabla editable.
    /// </summary>
    [HttpPost]
    public IActionResult Editar(AgenciaModel model)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Datos inválidos al actualizar.";
            return RedirectToAction("Index");
        }

        var actualizado = _agenciaService.ActualizarAgencia(model);
        TempData["Mensaje"] = actualizado
            ? "Agencia actualizada correctamente."
            : "Ocurrió un error al actualizar la agencia.";

        return RedirectToAction("Index");
    }

    /// <summary>
    /// Procesa la eliminación de una agencia por su código.
    /// </summary>
    [HttpPost]
    public IActionResult Eliminar(string codcco)
    {
        if (string.IsNullOrEmpty(codcco))
        {
            TempData["Error"] = "Código de agencia no válido.";
            return RedirectToAction("Index");
        }

        var eliminado = _agenciaService.EliminarAgencia(codcco);
        TempData["Mensaje"] = eliminado
            ? "Agencia eliminada correctamente."
            : "No se pudo eliminar la agencia.";

        return RedirectToAction("Index");
    }
}
