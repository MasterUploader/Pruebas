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

        if (codcco.HasValue && codcco.Value > 0)
            agencias = agencias.Where(a => a.Codcco == codcco.Value).ToList();

        ViewBag.AgenciasFiltro = agencias
            .Select(a => new SelectListItem
            {
                Value = a.Codcco.ToString(),
                Text = $"{a.Codcco} - {a.NomAge}"
            })
            .OrderBy(a => a.Text)
            .ToList();

        ViewBag.CodccoSeleccionado = codcco;
        ViewBag.EditId = editId; // <<=== Este es el que activa el modo edición

        int pageSize = 10;
        int pageNumber = page ?? 1;
        return View(agencias.ToPagedList(pageNumber, pageSize));
    }
    //[HttpGet]
    //[AutorizarPorTipoUsuario("1")]
    //public async Task<IActionResult> Index(int? page, int? codcco)
    //{
    //    var agencias = await _agenciaService.ObtenerAgenciasAsync();

    //    // Filtro por código de agencia si se selecciona
    //    if (codcco.HasValue && codcco.Value > 0)
    //        agencias = agencias.Where(a => a.Codcco == codcco.Value).ToList();

    //    // Generar lista desplegable para el filtro
    //    ViewBag.AgenciasFiltro = agencias
    //        .Select(a => new SelectListItem
    //        {
    //            Value = a.Codcco.ToString(),
    //            Text = $"{a.Codcco} - {a.NomAge}"
    //        })
    //        .OrderBy(a => a.Text)
    //        .ToList();

    //    ViewBag.CodccoSeleccionado = codcco;

    //    int pageSize = 10;
    //    int pageNumber = page ?? 1;
    //    return View(agencias.ToPagedList(pageNumber, pageSize));
    //}

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
    public async Task<IActionResult> GuardarEdicion(IFormCollection form)
    {
        var model = new AgenciaModel
        {
            Codcco = int.Parse(form["Codcco"]),
            NomAge = form["NomAge"],
            Zona = int.Parse(form["Zona"]),
            IpSer = form["IpSer"],
            NomSer = form["NomSer"],
            NomBD = form["NomBD"],
            Marquesina = form["Marquesina"] == "SI" ? "SI" : "NO",
            RstBranch = form["RstBranch"] == "SI" ? "SI" : "NO"
        };

        var actualizado = _agenciaService.ActualizarAgencia(model);
        TempData["Mensaje"] = actualizado
            ? "Agencia actualizada correctamente."
            : "Ocurrió un error al actualizar.";

        var agencias = await _agenciaService.ObtenerAgenciasAsync();
        ViewBag.AgenciasFiltro = agencias
            .Select(a => new SelectListItem
            {
                Value = a.Codcco.ToString(),
                Text = $"{a.Codcco} - {a.NomAge}"
            })
            .OrderBy(a => a.Text)
            .ToList();
        ViewBag.CodccoSeleccionado = null;

        return View("Index", agencias.ToPagedList(1, 50));
    }
    //[HttpPost]
    //[AutorizarPorTipoUsuario("1")]
    //public async Task<IActionResult> GuardarEdicion(AgenciaModel model)
    //{
    //    if (ModelState.IsValid)
    //    {
    //        var actualizado = _agenciaService.ActualizarAgencia(model);
    //        TempData["Mensaje"] = actualizado
    //            ? "Agencia actualizada correctamente."
    //            : "Ocurrió un error al actualizar.";
    //    }

    //    var agencias = await _agenciaService.ObtenerAgenciasAsync();

    //    ViewBag.AgenciasFiltro = agencias
    //        .Select(a => new SelectListItem
    //        {
    //            Value = a.Codcco.ToString(),
    //            Text = $"{a.Codcco} - {a.NomAge}"
    //        })
    //        .OrderBy(a => a.Text)
    //        .ToList();

    //    ViewBag.CodccoSeleccionado = null;

    //    return View("Index", agencias.ToPagedList(1, 50));
    //}
    //[HttpPost]
    //[AutorizarPorTipoUsuario("1")]
    //public async Task<IActionResult> GuardarEdicion(AgenciaModel model)
    //{
    //    if (ModelState.IsValid)
    //    {
    //        var actualizado = _agenciaService.ActualizarAgencia(model);
    //        if (actualizado)
    //            TempData["Mensaje"] = "Agencia actualizada correctamente.";
    //        else
    //            TempData["Mensaje"] = "Ocurrió un error al actualizar.";
    //    }

    //    var agencias = await _agenciaService.ObtenerAgenciasAsync();
    //    return View("Index", agencias.ToPagedList(1, 50)); // Asegúrate de mantener la paginación si aplica
    //}


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

<form method="get" asp-action="Index" class="row mb-3">
    <div class="col-md-4">
        <label for="codcco" class="form-label">Filtrar por Agencia</label>
        <select id="codcco" name="codcco" class="form-select" onchange="this.form.submit()">
            <option value="">-- Todas las Agencias --</option>
            @foreach (var agencia in (List<SelectListItem>)ViewBag.AgenciasFiltro)
               {
                string selected = ViewBag.CodccoSeleccionado?.ToString() == agencia.Value ? "selected" : "";
                @:<option value="@agencia.Value" @selected>@agencia.Text</option>
               }

        </select>
    </div>
</form>


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
                        <td>
                            <input type="hidden" name="Marquesina" value="NO" />
                            <input type="checkbox" name="Marquesina" value="SI" class="form-check-input" @(item.Marquesina == "SI" ? "checked" : "") />
                        </td>
                        <td>
                            <input type="hidden" name="RstBranch" value="NO" />
                            <input type="checkbox" name="RstBranch" value="SI" class="form-check-input" @(item.RstBranch == "SI" ? "checked" : "") />
                        </td>
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
                        <td>
                            <input type="hidden" name="Marquesina" value="NO" />
                            <input type="checkbox" name="Marquesina" value="SI" class="form-check-input" @(item.Marquesina == "SI" ? "checked" : "") />
                        </td>
                        <td>
                            <input type="hidden" name="RstBranch" value="NO" />
                            <input type="checkbox" name="RstBranch" value="SI" class="form-check-input" @(item.RstBranch == "SI" ? "checked" : "") />
                        </td>
                        <td>@item.IpSer</td>
                        <td>@item.NomSer</td>
                        <td>@item.NomBD</td>
                        <td class="text-center">
                            <a asp-action="Index" asp-route-editId="@item.Codcco" class="btn btn-sm btn-warning me-1">Editar</a>
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
                page => Url.Action("Index", new { page, codcco = ViewBag.CodccoSeleccionado }),
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
