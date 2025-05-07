using CAUAdministracion.Models;
using CAUAdministracion.Services.Agencias;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using X.PagedList;

[Authorize]
public class AgenciasController : Controller
{
    private readonly IAgenciaService _agenciaService;

    public AgenciasController(IAgenciaService agenciaService)
    {
        _agenciaService = agenciaService;
    }

    // GET: Agencias
    [HttpGet]
    public async Task<IActionResult> Index(int? page, int? codcco, int? editId)
    {
        var agencias = await _agenciaService.ObtenerAgenciasAsync();

        var listaSelect = agencias.Select(a => new SelectListItem
        {
            Value = a.Codcco.ToString(),
            Text = $"{a.Codcco} - {a.NomAge}"
        }).ToList();

        ViewBag.AgenciasFiltro = listaSelect;

        // Filtrar si se seleccionó una agencia
        if (codcco.HasValue && codcco.Value > 0)
            agencias = agencias.Where(a => a.Codcco == codcco.Value).ToList();

        // Obtener agencia en edición si se indicó editId
        AgenciaModel? agenciaEnEdicion = null;
        if (editId.HasValue)
            agenciaEnEdicion = agencias.FirstOrDefault(a => a.Codcco == editId.Value);

        var pageNumber = page ?? 1;
        var pageSize = 10;

        var modelo = new AgenciaIndexViewModel
        {
            Lista = agencias.ToPagedList(pageNumber, pageSize),
            AgenciaEnEdicion = agenciaEnEdicion,
            CodccoSeleccionado = codcco?.ToString()
        };

        return View("Index", modelo);
    }

    // POST: Guardar Edición
    [HttpPost]
    public async Task<IActionResult> GuardarEdicion(AgenciaModel model)
    {
        if (!ModelState.IsValid)
        {
            TempData["Mensaje"] = "Datos inválidos. Corrige los errores.";
            return RedirectToAction("Index", new { editId = model.Codcco });
        }

        bool actualizado = _agenciaService.ActualizarAgencia(model);

        TempData["Mensaje"] = actualizado
            ? "Agencia actualizada correctamente."
            : "Ocurrió un error al actualizar la agencia.";

        return RedirectToAction("Index");
    }
}
