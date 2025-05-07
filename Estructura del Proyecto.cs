using CAUAdministracion.Helpers;
using CAUAdministracion.Models;
using CAUAdministracion.Services.Agencias;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using X.PagedList.Extensions;

namespace CAUAdministracion.Controllers;

/// <summary>
/// Controlador responsable de gestionar las agencias (agregar, listar, editar, eliminar).
/// </summary>
[Authorize]
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
    [HttpGet]
    [AutorizarPorTipoUsuario("1")]
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

    /// <summary>
    /// Vista del formulario para agregar una nueva agencia.
    /// </summary>
    [AutorizarPorTipoUsuario("1")]
    [HttpGet]
    public IActionResult Agregar()
    {
        return View();
    }

    /// <summary>
    /// Procesa el formulario de nueva agencia.
    /// Verifica si el centro de costo ya existe antes de insertar.
    /// </summary>
    [HttpPost]
    [AutorizarPorTipoUsuario("1")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Agregar(AgenciaModel model)
    {
        try
        {
            if (!ModelState.IsValid)
                return View(model);

            if (_agenciaService.ExisteCentroCosto(model.Codcco))
            {
                ModelState.AddModelError("Codcco", "Ya existe una agencia con ese centro de costo.");
                return View(model);
            }

            if (_agenciaService.InsertarAgencia(model))
            {
                TempData["Mensaje"] = "Agencia agregada correctamente.";
                return RedirectToAction("Index");
            }
            ModelState.AddModelError("", "Ocurrió un error al agregar la agencia.");
            return View(model);
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, $"Error al guardar: {ex.Message}");
            return View(model);
        }
    }



    /// <summary>
    /// Procesa la actualización de una agencia desde vista en tabla editable.
    /// </summary>
    [AutorizarPorTipoUsuario("1")]
    [HttpGet]
    public async Task<IActionResult> Editar(int id)
    {
        var agencia = await _agenciaService.ObtenerAgenciaPorIdAsync(id);
        if (agencia == null)
        {
            TempData["Mensaje"] = "Agencia no encontrada.";
            return RedirectToAction("Index");
        }

        return View(agencia);
    }

    [HttpPost]
    [AutorizarPorTipoUsuario("1")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Editar(AgenciaModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var resultado = _agenciaService.ActualizarAgencia(model);

        if (resultado)
        {
            TempData["Mensaje"] = "Agencia actualizada exitosamente.";
            return RedirectToAction("Index");
        }
        else
        {
            ModelState.AddModelError("", "Error al actualizar la agencia.");
            return View(model);
        }
    }

    /// <summary>
    /// Metodo para guardar 
    /// </summary>
    /// <param name="model"></param>
    /// <returns></returns>
    [HttpPost]
    [AutorizarPorTipoUsuario("1")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GuardarEdicion(AgenciaModel model)
    {
        if (!ModelState.IsValid)
        {
            // Si el modelo no es válido, se reconstruye el view model
            var agencias = await _agenciaService.ObtenerAgenciasAsync();

            var viewModel = new AgenciaIndexViewModel
            {
                AgenciaEnEdicion = model,
                Lista = agencias.ToPagedList(1, 10),
                CodccoSeleccionado = null,
                AgenciasFiltro = agencias.Select(a => new SelectListItem
                {
                    Value = a.Codcco.ToString(),
                    Text = $"{a.Codcco} - {a.NomAge}"
                }).ToList()
            };

            return View("Index", viewModel);
        }

        // Actualiza la agencia
        var actualizado = _agenciaService.ActualizarAgencia(model);

        TempData["Mensaje"] = actualizado
            ? "Agencia actualizada correctamente."
            : "Ocurrió un error al actualizar.";

        // Redirige a Index limpio tras guardar
        return RedirectToAction("Index");
    }

    /// <summary>
    /// Procesa la eliminación de una agencia por su código.
    /// </summary>
    [HttpPost]
    [AutorizarPorTipoUsuario("1")]
    public IActionResult Eliminar(int id)
    {
        if (id < 0)
        {
            TempData["Error"] = "Código de agencia no válido.";
            return RedirectToAction("Index");
        }

        var eliminado = _agenciaService.EliminarAgencia(id);
        TempData["Mensaje"] = eliminado
            ? "Agencia eliminada correctamente."
            : "No se pudo eliminar la agencia.";

        return RedirectToAction("Index");
    }
}

@model CAUAdministracion.Models.AgenciaIndexViewModel
@using X.PagedList.Mvc.Core
@using Microsoft.AspNetCore.Mvc.Rendering

@{
    ViewData["Title"] = "Mantenimiento de Agencias";
    var lista = Model.Lista;
    var agenciaEditar = Model.AgenciaEnEdicion;
    var codccoSel = Model.CodccoSeleccionado;
}

<h2 class="text-danger mb-4">@ViewData["Title"]</h2>

@if (TempData["Mensaje"] != null)
{
    <div class="alert alert-info alert-dismissible fade show" role="alert">
        @TempData["Mensaje"]
        <button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Close"></button>
    </div>
}

<!-- Filtro -->
<div class="mb-3">
    <form method="get" asp-controller="Agencias" asp-action="Index">
        <label for="codcco">Agencia:</label>
        <select name="codcco" class="form-select" style="width: 300px; display:inline-block;" onchange="this.form.submit()">
            <option value="">-- Seleccione Agencia --</option>
            @foreach (var agencia in Model.AgenciasFiltro)
            {
                var selected = (agencia.Value == codccoSel) ? "selected" : "";
                @:<option value="@agencia.Value" @selected>@agencia.Text</option>
            }
        </select>
    </form>
</div>

<!-- Tabla -->
<table class="table table-bordered table-hover table-striped">
    <thead class="table-dark text-center align-middle">
        <tr>
            <th>Código</th>
            <th>Nombre</th>
            <th>Zona</th>
            <th>Marquesina</th>
            <th>RST Branch</th>
            <th>IP Server</th>
            <th>Nom. Server</th>
            <th>Base Datos</th>
            <th style="width: 120px">Acciones</th>
        </tr>
    </thead>
    <tbody>
        @foreach (var item in lista)
        {
            if (agenciaEditar != null && item.Codcco == agenciaEditar.Codcco)
            {
                <tr>
                    <form asp-action="GuardarEdicion" method="post">
                        <td>
                            <input type="hidden" asp-for="AgenciaEnEdicion.Codcco" />
                            @agenciaEditar.Codcco
                        </td>
                        <td><input asp-for="AgenciaEnEdicion.NomAge" class="form-control" /></td>
                        <td>
                            <select asp-for="AgenciaEnEdicion.Zona" class="form-select" required>
                                <option value="1">CENTRO SUR</option>
                                <option value="2">NOR OCCIDENTE</option>
                                <option value="3">NOR ORIENTE</option>
                            </select>
                        </td>
                        <td>
                            <select name="Marquesina" class="form-select" required>
                                <option value="SI" selected="@(agenciaEditar.Marquesina == "SI")">SI</option>
                                <option value="NO" selected="@(agenciaEditar.Marquesina == "NO")">NO</option>
                            </select>
                        </td>
                        <td>
                            <select name="RstBranch" class="form-select" required>
                                <option value="SI" selected="@(agenciaEditar.RstBranch == "SI")">SI</option>
                                <option value="NO" selected="@(agenciaEditar.RstBranch == "NO")">NO</option>
                            </select>
                        </td>
                        <td><input asp-for="AgenciaEnEdicion.IpSer" class="form-control" /></td>
                        <td><input asp-for="AgenciaEnEdicion.NomSer" class="form-control" /></td>
                        <td><input asp-for="AgenciaEnEdicion.NomBD" class="form-control" /></td>
                        <td class="text-center">
                            <button type="submit" class="btn btn-success btn-sm me-1">Guardar</button>
                            <a asp-action="Index" class="btn btn-secondary btn-sm">Cancelar</a>
                        </td>
                    </form>
                </tr>
            }
            else
            {
                <tr>
                    <td>@item.Codcco</td>
                    <td>@item.NomAge</td>
                    <td>@(item.Zona switch {
                        1 => "CENTRO SUR",
                        2 => "NOR OCCIDENTE",
                        3 => "NOR ORIENTE",
                        _ => "DESCONOCIDA"
                    })</td>
                    <td>@item.Marquesina</td>
                    <td>@item.RstBranch</td>
                    <td>@item.IpSer</td>
                    <td>@item.NomSer</td>
                    <td>@item.NomBD</td>
                    <td class="text-center">
                        <a asp-action="Index" asp-route-editId="@item.Codcco" asp-route-codcco="@codccoSel" class="btn btn-warning btn-sm me-1">Editar</a>
                        <form asp-action="Eliminar" asp-route-id="@item.Codcco" method="post" class="d-inline" onsubmit="return confirm('¿Está seguro de eliminar esta agencia?');">
                            <button type="submit" class="btn btn-danger btn-sm">Eliminar</button>
                        </form>
                    </td>
                </tr>
            }
        }
    </tbody>
</table>

<!-- Paginación -->
@if (lista != null && lista.PageCount > 1)
{
    <div class="d-flex justify-content-center">
        @Html.PagedListPager(
            lista,
            page => Url.Action("Index", new { page, codcco = codccoSel }),
            new PagedListRenderOptions
            {
                UlElementClasses = new[] { "pagination", "justify-content-center" },
                LiElementClasses = new[] { "page-item" },
                PageClasses = new[] { "page-link" }
            }
        )
    </div>
}

<!-- Botón agregar -->
<div class="mt-4">
    <a asp-action="Agregar" class="btn btn-primary">Agregar Nueva Agencia</a>
</div>
