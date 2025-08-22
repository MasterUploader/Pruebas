using CAUAdministracion.Helpers;
using CAUAdministracion.Models;
using CAUAdministracion.Services.Usuarios;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using X.PagedList.Extensions;

namespace CAUAdministracion.Controllers;

[Authorize, AutorizarPorTipoUsuario("1")]
public class UsuariosController : Controller
{
    private readonly IUsuarioService _usuarioService;

    public UsuariosController(IUsuarioService usuarioService)
    {
        _usuarioService = usuarioService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        int? page,
        string? q,
        string? tipo,     // "","1","2","3"
        string? estado,   // "","A","I"
        string? edit      // usuario que se está editando
    )
    {
        // 1) Datos
        var usuarios = await _usuarioService.ObtenerUsuariosAsync();

        // 2) Filtros
        if (!string.IsNullOrWhiteSpace(q))
            usuarios = usuarios
                .Where(u => (u.Usuario ?? "")
                    .Contains(q, StringComparison.OrdinalIgnoreCase))
                .ToList();

        if (!string.IsNullOrEmpty(tipo) && int.TryParse(tipo, out var t))
            usuarios = usuarios.Where(u => u.TipoUsuario == t).ToList();

        if (!string.IsNullOrEmpty(estado))
            usuarios = usuarios.Where(u => string.Equals(u.Estado, estado, StringComparison.OrdinalIgnoreCase)).ToList();

        // 3) Combos del filtro (asp-items evita RZ1031)
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

        // 4) ViewBags auxiliares
        ViewBag.Busqueda  = q ?? "";
        ViewBag.TipoSel   = tipo ?? "";
        ViewBag.EstadoSel = estado ?? "";
        ViewBag.Edit      = edit; // usuario en edición (puede ser null)

        // 5) Paginación
        int pageNumber = page ?? 1;
        int pageSize   = 10;

        return View(usuarios.ToPagedList(pageNumber, pageSize));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Actualizar(string usuario, int tipoUsuario, string estado,
                                               int? page, string? q, string? tipo, string? estadoFiltro)
    {
        // Regla: no permitir dejar Inactivo al único usuario
        if (string.Equals(estado, "I", StringComparison.OrdinalIgnoreCase))
        {
            var total = await _usuarioService.ContarUsuariosAsync();
            if (total <= 1)
            {
                TempData["Mensaje"] = "No puede inactivar al único usuario del sistema.";
                return RedirectToAction(nameof(Index), new { page, q, tipo, estado = estadoFiltro });
            }
        }

        var model = new UsuarioModel
        {
            Usuario = usuario,
            TipoUsuario = tipoUsuario,
            Estado = estado
        };

        var ok = await _usuarioService.ActualizarAsync(model);
        TempData["Mensaje"] = ok ? "Usuario actualizado." : "No se pudo actualizar el usuario.";

        return RedirectToAction(nameof(Index), new { page, q, tipo, estado = estadoFiltro });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Eliminar(string usuario, int? page, string? q, string? tipo, string? estado)
    {
        // Regla: no eliminar al único usuario
        var total = await _usuarioService.ContarUsuariosAsync();
        if (total <= 1)
        {
            TempData["Mensaje"] = "No puede eliminar al único usuario del sistema.";
            return RedirectToAction(nameof(Index), new { page, q, tipo, estado });
        }

        var ok = await _usuarioService.EliminarAsync(usuario);
        TempData["Mensaje"] = ok ? "Usuario eliminado." : "No se pudo eliminar el usuario.";

        return RedirectToAction(nameof(Index), new { page, q, tipo, estado });
    }

    // (Opcional) pantallas de alta/edición por separado
    [HttpGet] public IActionResult Agregar() => View();
    [HttpGet] public async Task<IActionResult> Editar(string id)
        => View(await _usuarioService.ObtenerPorIdAsync(id));
}


@using X.PagedList
@using X.PagedList.Mvc.Core
@model IPagedList<CAUAdministracion.Models.UsuarioModel>

@{
    ViewData["Title"] = "Administración de Usuarios";

    var q         = (string)(ViewBag.Busqueda  ?? "");
    var tipoSel   = (string)(ViewBag.TipoSel   ?? "");
    var estadoSel = (string)(ViewBag.EstadoSel ?? "");
    var tipos     = ViewBag.Tipos   as List<SelectListItem>;
    var estados   = ViewBag.Estados as List<SelectListItem>;
    var editUser  = ViewBag.Edit    as string;

    string TipoTexto(int t) => t switch { 1 => "Administrador", 2 => "Admin. Videos", 3 => "Admin. Mensajes", _ => "-" };
    string EstadoTexto(string? e) => string.Equals(e, "A", StringComparison.OrdinalIgnoreCase) ? "Activo" : "Inactivo";
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
        <select name="tipo" class="form-select" asp-items="tipos"></select>
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
                <th style="width:30%">Usuario</th>
                <th style="width:22%">Tipo</th>
                <th style="width:18%">Estado</th>
                <th style="width:30%">Acciones</th>
            </tr>
        </thead>
        <tbody>
        @foreach (var u in Model)
        {
            var formUpdateId = $"f-upd-{u.Usuario}";
            var formDeleteId = $"f-del-{u.Usuario}";

            <!-- Formularios ocultos por fila -->
            <tr class="d-none">
                <td colspan="4" class="p-0">
                    <form id="@formUpdateId" asp-action="Actualizar" method="post">
                        @Html.AntiForgeryToken()
                        <input type="hidden" name="usuario" value="@u.Usuario" />
                        <!-- preserva filtros -->
                        <input type="hidden" name="page"        value="@Model.PageNumber" />
                        <input type="hidden" name="q"           value="@q" />
                        <input type="hidden" name="tipo"        value="@tipoSel" />
                        <input type="hidden" name="estadoFiltro" value="@estadoSel" />
                    </form>

                    <form id="@formDeleteId" asp-action="Eliminar" method="post">
                        @Html.AntiForgeryToken()
                        <input type="hidden" name="usuario" value="@u.Usuario" />
                        <input type="hidden" name="page"   value="@Model.PageNumber" />
                        <input type="hidden" name="q"      value="@q" />
                        <input type="hidden" name="tipo"   value="@tipoSel" />
                        <input type="hidden" name="estado" value="@estadoSel" />
                    </form>
                </td>
            </tr>

            @if (!string.IsNullOrEmpty(editUser) && string.Equals(editUser, u.Usuario, StringComparison.OrdinalIgnoreCase))
            {
                <!-- Fila en modo edición -->
                <tr>
                    <td>@u.Usuario</td>

                    <td>
                        <select name="tipoUsuario" form="@formUpdateId" class="form-select">
                            @{
                                var sel1 = u.TipoUsuario == 1 ? "selected" : "";
                                var sel2 = u.TipoUsuario == 2 ? "selected" : "";
                                var sel3 = u.TipoUsuario == 3 ? "selected" : "";
                            }
                            @: <option value="1" @sel1>Administrador</option>
                            @: <option value="2" @sel2>Admin. Videos</option>
                            @: <option value="3" @sel3>Admin. Mensajes</option>
                        </select>
                    </td>

                    <td>
                        @{
                            var sA = string.Equals(u.Estado, "A", StringComparison.OrdinalIgnoreCase) ? "selected" : "";
                            var sI = string.Equals(u.Estado, "I", StringComparison.OrdinalIgnoreCase) ? "selected" : "";
                        }
                        <select name="estado" form="@formUpdateId" class="form-select">
                            @: <option value="A" @sA>Activo</option>
                            @: <option value="I" @sI>Inactivo</option>
                        </select>
                    </td>

                    <td class="text-nowrap">
                        <button type="submit" form="@formUpdateId" class="btn btn-success btn-sm me-2">Guardar</button>
                        <a class="btn btn-secondary btn-sm"
                           asp-action="Index"
                           asp-route-page="@Model.PageNumber"
                           asp-route-q="@q"
                           asp-route-tipo="@tipoSel"
                           asp-route-estado="@estadoSel">
                           Cancelar
                        </a>
                    </td>
                </tr>
            }
            else
            {
                <!-- Fila normal -->
                <tr>
                    <td>@u.Usuario</td>
                    <td>@TipoTexto(u.TipoUsuario)</td>
                    <td>@EstadoTexto(u.Estado)</td>
                    <td class="text-nowrap">
                        <a class="btn btn-warning btn-sm me-2"
                           asp-action="Index"
                           asp-route-edit="@u.Usuario"
                           asp-route-page="@Model.PageNumber"
                           asp-route-q="@q"
                           asp-route-tipo="@tipoSel"
                           asp-route-estado="@estadoSel">
                           Editar
                        </a>

                        <button type="submit"
                                form="@formDeleteId"
                                class="btn btn-danger btn-sm"
                                onclick="return confirm('¿Eliminar el usuario @u.Usuario?');">
                            Eliminar
                        </button>
                    </td>
                </tr>
            }
        }
        </tbody>
    </table>

    <div class="d-flex justify-content-center">
        @Html.PagedListPager(
            Model,
            page => Url.Action("Index", new { page, q, tipo = tipoSel, estado = estadoSel })
        )
    </div>
}
else
{
    <div class="alert alert-info">No se encontraron usuarios con los filtros actuales.</div>
}

@section Scripts {
    <script>
        // cerrar alert en 5s
        setTimeout(() => {
            const el = document.getElementById('autoclose-alert');
            if (el) new bootstrap.Alert(el).close();
        }, 5000);
    </script>
}




