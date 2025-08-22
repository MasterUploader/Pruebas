Quedo así

using CAUAdministracion.Helpers;
using CAUAdministracion.Models;
using CAUAdministracion.Services.Agencias;
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
    public async Task<IActionResult> Index( int? editId, string codcco = null )
    {

        // Obtener todas las agencias para el filtro
        var agencias = _mensajeService.ObtenerAgenciasSelectList();
        ViewBag.Agencias = agencias;
        ViewBag.CodigoAgenciaSeleccionado = codcco;
        ViewBag.EditId = editId;

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


@model List<CAUAdministracion.Models.MensajeModel>
@using Microsoft.AspNetCore.Mvc.Rendering

@{
    ViewData["Title"] = "Mantenimiento de Mensajes";
    var agencias = ViewBag.Agencias as List<SelectListItem>;
    var codccoSel = ViewBag.CodccoSeleccionado as string;
    int? editId = ViewBag.EditId as int?;
}

<h2 class="text-danger">@ViewData["Title"]</h2>

<!-- Filtro por Agencia -->
<div class="mb-3">
    <form method="get" asp-controller="Messages" asp-action="Index" class="d-inline">
        <label for="codcco">Agencia:</label>
        <select id="codcco" name="codcco" class="form-select d-inline-block" style="width: 320px" onchange="this.form.submit()">
            <option value="">-- Seleccione Agencia --</option>
            @if (agencias != null)
            {
                foreach (var a in agencias)
                {
                    var selected = (a.Value == codccoSel) ? "selected" : "";
                    @:<option value="@a.Value" @selected>@a.Text</option>
                }
            }
        </select>
    </form>
</div>

@if (Model != null && Model.Any())
{
    <table class="table table-bordered table-striped align-middle">
        <thead class="table-dark">
            <tr>
                <th style="width:110px">Código</th>
                <th style="width:120px">Secuencia</th>
                <th>Mensaje</th>
                <th style="width:160px">Estado</th>
                <th style="width:180px">Acciones</th>
            </tr>
        </thead>
        <tbody>
            @foreach (var item in Model)
            {
                var formUpdateId = $"f-upd-{item.Codcco}-{item.CodMsg}";
                var formDeleteId = $"f-del-{item.Codcco}-{item.CodMsg}";

                <!-- Formularios “invisibles” por fila -->
                <tr class="d-none">
                    <td colspan="5" class="p-0">
                        <form id="@formUpdateId" asp-controller="Messages" asp-action="Actualizar" method="post">
                            @Html.AntiForgeryToken()
                            <input type="hidden" name="Codcco" value="@item.Codcco" />
                            <input type="hidden" name="CodMsg" value="@item.CodMsg" />
                        </form>

                        <form id="@formDeleteId" asp-controller="Messages" asp-action="Eliminar" method="post">
                            @Html.AntiForgeryToken()
                            <input type="hidden" name="Codcco" value="@item.Codcco" />
                            <input type="hidden" name="CodMsg" value="@item.CodMsg" />
                        </form>
                    </td>
                </tr>

                @if (editId.HasValue && editId.Value == item.CodMsg)
                {
                    <!-- Fila en modo edición -->
                    <tr>
                        <td>@item.CodMsg</td>
                        <td>@item.Seq</td>
                        <td>
                            <input type="text"
                                   name="Mensaje"
                                   form="@formUpdateId"
                                   value="@item.Mensaje"
                                   class="form-control" />
                        </td>
                        <td>
                            <select name="Estado" form="@formUpdateId" class="form-select form-select-sm">
                            @if (item.Estado == "A")
                        {
                            <option value="A" selected>Activo</option>
                            <option value="I">Inactivo</option>
                        }
                        else
                        {
                            <option value="A">Activo</option>
                            <option value="I" selected>Inactivo</option>
                        }
                        </select>
                        </td>
                        <td class="text-nowrap">
                            <button type="submit"
                                    form="@formUpdateId"
                                    class="btn btn-sm btn-success me-2">
                                Guardar
                            </button>

                            <a class="btn btn-sm btn-secondary"
                               asp-controller="Messages"
                               asp-action="Index"
                               asp-route-codcco="@codccoSel">
                                Cancelar
                            </a>
                        </td>
                    </tr>
                }
                else
                {
                    <!-- Fila normal -->
                    <tr>
                        <td>@item.CodMsg</td>
                        <td>@item.Seq</td>
                        <td>@item.Mensaje</td>
                        <td>@(item.Estado == "A" ? "Activo" : "Inactivo")</td>
                        <td class="text-nowrap">
                            <a class="btn btn-warning btn-sm me-2"
                               asp-controller="Messages"
                               asp-action="Index"
                               asp-route-codcco="@codccoSel"
                               asp-route-editId="@item.CodMsg">
                                Editar
                            </a>

                            <button type="submit"
                                    form="@formDeleteId"
                                    class="btn btn-danger btn-sm"
                                    onclick="return confirm('¿Está seguro de eliminar este mensaje?');">
                                Eliminar
                            </button>
                        </td>
                    </tr>
                }
            }
        </tbody>
    </table>
}
else
{
    <div class="alert alert-info">No se encontraron mensajes para esta agencia.</div>
}

<a href="@Url.Action("Agregar", "Messages")" class="btn btn-primary">Agregar Nuevo Mensaje</a>


Pero al seleccionar una fila carga todas las que tenga el mismo codigo, la seleccion se debe de realizar por codigo y secuencia para evitar seleccionar 2 filas o más.
    
