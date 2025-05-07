@using X.PagedList.Mvc.Core
@using X.PagedList.Mvc
@using X.PagedList
@model IPagedList<CAUAdministracion.Models.AgenciaModel>
@{
    ViewData["Title"] = "Mantenimiento de Agencias";
}

<h2 class="text-danger mb-4">@ViewData["Title"]</h2>

@if (TempData["Mensaje"] != null)
{
    <div class="alert alert-info alert-dismissible fade show" role="alert">
        @TempData["Mensaje"]
        <button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Close"></button>
    </div>
}

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
        @if (Model != null && Model.Any())
        {

            @foreach (var item in Model)
            {
                if (ViewBag.EditId != null && ViewBag.EditId == item.Codcco)
                {
                    <!-- FILA EN MODO EDICIÓN -->
                    <tr>
                        <form asp-action="GuardarEdicion" method="post">
                            <input type="hidden" name="Codcco" value="@item.Codcco" />
                        <td>@item.Codcco</td>
                        <td><input name="NomAge" value="@item.NomAge" maxlength="40" class="form-control" required /></td>
                        <td>
                            <select name="Zona" class="form-select" required>
                                <option value="1" selected="@(item.Zona == 1)">CENTRO SUR</option>
                                <option value="2" selected="@(item.Zona == 2)">NOR OCCIDENTE</option>
                                <option value="3" selected="@(item.Zona == 3)">NOR ORIENTE</option>
                            </select>
                        </td>
                        <td><input type="checkbox" name="Marquesina" class="form-check-input" @(item.Marquesina?.ToUpper() == "SI" ? "APLICA" : "NO APLICA") /></td>
                        <td><input type="checkbox" name="RstBranch" class="form-check-input" @(item.RstBranch?.ToUpper()== "SI" ? "APLICA" : "NO APLICA" ) /></td>
                        <td><input name="IpSer" value="@item.IpSer" maxlength="20" class="form-control" required /></td>
                        <td><input name="NomSer" value="@item.NomSer" maxlength="18" class="form-control" required /></td>
                        <td><input name="NomBD" value="@item.NomBD" maxlength="20" class="form-control" required /></td>
                        <td class="text-center">
                            <button type="submit" class="btn btn-success btn-sm me-1">Guardar</button>
                            <a asp-action="Index" class="btn btn-secondary btn-sm">Cancelar</a>
                        </td>
                        </form>
                    </tr>
                }
                else
                {
                    <!-- FILA NORMAL -->
                    <tr>
                        <td>@item.Codcco</td>
                        <td>@item.NomAge</td>
                        <td>
                            @{
                                string zonaTexto = item.Zona switch
                                {
                                    1 => "CENTRO SUR",
                                    2 => "NOR OCCIDENTE",
                                    3 => "NOR ORIENTE",
                                    _ => "DESCONOCIDA"
                                };
                            }
                            @zonaTexto
                        </td>
                        <td>@(item.Marquesina?.ToUpper() == "SI" ? "APLICA" : "NO APLICA")</td>
                        <td>@(item.RstBranch?.ToUpper() == "SI" ? "APLICA" : "NO APLICA")</td>
                        <td>@item.IpSer</td>
                        <td>@item.NomSer</td>
                        <td>@item.NomBD</td>
                        <td class="text-center">
                            <a asp-action="Editar" asp-route-id="@item.Codcco" class="btn btn-sm btn-warning me-1">Editar</a>
                            <form asp-action="Eliminar" asp-route-id="@item.Codcco" method="post" class="d-inline" onsubmit="return confirm('¿Está seguro de eliminar esta agencia?');">
                                <button type="submit" class="btn btn-sm btn-danger">Eliminar</button>
                            </form>
                        </td>
                    </tr>
                }
            }
            }
           
    </tbody>
</table>

<!-- Paginación -->
@if (Model != null && Model.PageCount > 1)
{
    <div class="d-flex justify-content-center">
        @Html.PagedListPager(
                Model,
                page => Url.Action("Index", new { page }),
                new PagedListRenderOptions
    {
        UlElementClasses = new[] { "pagination", "justify-content-center" },
        LiElementClasses = new[] { "page-item" },
        PageClasses = new[] { "page-link" },
        DisplayLinkToFirstPage = PagedListDisplayMode.Always,
        DisplayLinkToLastPage = PagedListDisplayMode.Always,

        
        DisplayLinkToPreviousPage = PagedListDisplayMode.Always,
        DisplayLinkToNextPage = PagedListDisplayMode.Always,
        MaximumPageNumbersToDisplay = 5
    }
                )
    </div>
}

<!-- Botón agregar -->
<div class="mt-4">
    <a asp-action="Agregar" class="btn btn-primary">Agregar Nueva Agencia</a>
</div>





using CAUAdministracion.Helpers;
using CAUAdministracion.Models;
using CAUAdministracion.Services.Agencias;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
    [AutorizarPorTipoUsuario("1")]
    [HttpGet]
    public async Task<IActionResult> Index(int? page)
    {
        var agencias = await _agenciaService.ObtenerAgenciasAsync();

        int pageSize = 10;
        int pageNumber = page ?? 1;

        var pagedAgencias = agencias.ToPagedList(pageNumber, pageSize);

        return View(pagedAgencias);
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
    public async Task<IActionResult> Editar(int id)
    {
        var agencias = await _agenciaService.ObtenerAgenciasAsync();
        ViewBag.EditId = id;
        return View("Index", agencias.ToPagedList(1, 50)); // O el número de página actual
    }

    // POST: Agencias/Editar
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Editar(AgenciaModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        try
        {
            _agenciaService.ActualizarAgencia(model);
            TempData["Mensaje"] = "Agencia actualizada correctamente.";
            return RedirectToAction("Index");
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, $"Error al actualizar: {ex.Message}");
            return View(model);
        }
    }

    /// <summary>
    /// Metodo para guardar 
    /// </summary>
    /// <param name="model"></param>
    /// <returns></returns>
    [HttpPost]
    public async Task<IActionResult> GuardarEdicion(AgenciaModel model)
    {
        if (ModelState.IsValid)
        {
            var actualizado =  _agenciaService.ActualizarAgencia(model);
            if (actualizado)
                TempData["Mensaje"] = "Agencia actualizada correctamente.";
            else
                TempData["Mensaje"] = "Ocurrió un error al actualizar.";
        }

        var agencias = await _agenciaService.ObtenerAgenciasAsync();
        return View("Index", agencias.ToPagedList(1, 50)); // Asegúrate de mantener la paginación si aplica
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

        var eliminado = _agenciaService.EliminarAgencia(int.Parse(codcco));
        TempData["Mensaje"] = eliminado
            ? "Agencia eliminada correctamente."
            : "No se pudo eliminar la agencia.";

        return RedirectToAction("Index");
    }
}
