using CAUAdministracion.Helpers;
using CAUAdministracion.Models;
using CAUAdministracion.Services.Usuarios; // <- tu servicio
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using X.PagedList.Extensions;

namespace CAUAdministracion.Controllers;

[Authorize, AutorizarPorTipoUsuario("1")] // ajusta si aplica
public class UsuariosController : Controller
{
    private readonly IUsuarioService _usuarioService;

    public UsuariosController(IUsuarioService usuarioService)
    {
        _usuarioService = usuarioService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(int? page, string? q, string? tipo, string? estado)
    {
        // 1) Cargar usuarios
        var lista = await _usuarioService.ObtenerUsuariosAsync(); // List<UsuarioModel>

        // 2) Filtros
        if (!string.IsNullOrWhiteSpace(q))
            lista = lista
                .Where(u => u.Usuario?.Contains(q, StringComparison.OrdinalIgnoreCase) == true)
                .ToList();

        if (!string.IsNullOrEmpty(tipo) && int.TryParse(tipo, out var tipoInt))
            lista = lista.Where(u => u.TipoUsuario == tipoInt).ToList();

        if (!string.IsNullOrEmpty(estado))
            lista = lista.Where(u => string.Equals(u.Estado, estado, StringComparison.OrdinalIgnoreCase)).ToList();

        // 3) Selects del filtro (usamos asp-items para evitar RZ1031)
        ViewBag.Tipos = new List<SelectListItem>
        {
            new("-- Todos --",""),
            new("Administrador","1", selected: tipo == "1"),
            new("Admin. Videos","2", selected: tipo == "2"),
            new("Admin. Mensajes","3", selected: tipo == "3"),
        };

        ViewBag.Estados = new List<SelectListItem>
        {
            new("-- Todos --",""),
            new("Activo","A", selected: estado == "A"),
            new("Inactivo","I", selected: estado == "I"),
        };

        ViewBag.Busqueda = q ?? "";
        ViewBag.TipoSel  = tipo ?? "";
        ViewBag.EstadoSel = estado ?? "";

        // 4) Paginación
        var pageNumber = page ?? 1;
        var pageSize = 10;

        return View(lista.ToPagedList(pageNumber, pageSize));
    }

    // (Opcional) Acciones de ejemplo para los botones
    [HttpGet] public IActionResult Agregar() => View();
    [HttpGet] public async Task<IActionResult> Editar(string id)
        => View(await _usuarioService.ObtenerPorIdAsync(id));

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Eliminar(string id)
    {
        var ok = await _usuarioService.EliminarAsync(id);
        TempData["Mensaje"] = ok ? "Usuario eliminado." : "No se pudo eliminar.";
        return RedirectToAction(nameof(Index));
    }
}




@using X.PagedList
@using X.PagedList.Mvc.Core
@model IPagedList<CAUAdministracion.Models.UsuarioModel>

@{
    ViewData["Title"] = "Usuarios";
    var q        = (string)(ViewBag.Busqueda ?? "");
    var tipoSel  = (string)(ViewBag.TipoSel  ?? "");
    var estadoSel= (string)(ViewBag.EstadoSel?? "");
    var tipos    = ViewBag.Tipos as List<SelectListItem>;
    var estados  = ViewBag.Estados as List<SelectListItem>;

    string TipoTexto(int t) => t switch { 1 => "Administrador", 2 => "Admin. Videos", 3 => "Admin. Mensajes", _ => "-" };
    string EstadoTexto(string? e) => e == "A" ? "Activo" : "Inactivo";
}

<h2 class="text-danger mb-3">@ViewData["Title"]</h2>

@if (TempData["Mensaje"] != null)
{
    <div id="autoclose-alert" class="alert alert-info alert-dismissible fade show" role="alert">
        @TempData["Mensaje"]
        <button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Close"></button>
    </div>
}

<form method="get" asp-action="Index" class="row g-2 align-items-end mb-3">
    <div class="col-md-4">
        <label class="form-label">Usuario</label>
        <input type="text" name="q" value="@q" class="form-control" placeholder="Buscar por usuario..." />
    </div>

    <div class="col-md-3">
        <label class="form-label">Tipo</label>
        <select name="tipo" class="form-select" asp-items="tipos">
            <!-- el primer item de ViewBag.Tipos ya es '-- Todos --' -->
        </select>
    </div>

    <div class="col-md-3">
        <label class="form-label">Estado</label>
        <select name="estado" class="form-select" asp-items="estados"></select>
    </div>

    <div class="col-md-2 d-grid">
        <button type="submit" class="btn btn-primary">Filtrar</button>
    </div>
</form>

<div class="mb-3">
    <a asp-action="Agregar" class="btn btn-success">Agregar Usuario</a>
</div>

@if (Model != null && Model.Any())
{
    <table class="table table-bordered table-hover align-middle">
        <thead class="table-dark">
            <tr>
                <th style="width:35%">Usuario</th>
                <th style="width:25%">Tipo</th>
                <th style="width:15%">Estado</th>
                <th style="width:25%">Acciones</th>
            </tr>
        </thead>
        <tbody>
        @foreach (var u in Model)
        {
            <tr>
                <td>@u.Usuario</td>
                <td>@TipoTexto(u.TipoUsuario)</td>
                <td>@EstadoTexto(u.Estado)</td>
                <td class="text-nowrap">
                    <a asp-action="Editar" asp-route-id="@u.Usuario" class="btn btn-warning btn-sm me-2">Editar</a>

                    <form asp-action="Eliminar" asp-route-id="@u.Usuario" method="post" class="d-inline"
                          onsubmit="return confirm('¿Eliminar el usuario @u.Usuario?');">
                        @Html.AntiForgeryToken()
                        <button type="submit" class="btn btn-danger btn-sm">Eliminar</button>
                    </form>
                </td>
            </tr>
        }
        </tbody>
    </table>

    <div class="d-flex justify-content-center">
        @Html.PagedListPager(
            Model,
            page => Url.Action("Index", new { page, q = q, tipo = tipoSel, estado = estadoSel })
        )
    </div>
}
else
{
    <div class="alert alert-info">No se encontraron usuarios con los filtros actuales.</div>
}

@section Scripts {
    <script>
        // Cerrar alert en 5s
        setTimeout(() => {
            const el = document.getElementById('autoclose-alert');
            if (el) new bootstrap.Alert(el).close();
        }, 5000);
    </script>
}


