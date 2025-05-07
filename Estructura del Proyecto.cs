using X.PagedList;

namespace CAUAdministracion.Models
{
    public class AgenciaIndexViewModel
    {
        /// <summary>
        /// Lista paginada de agencias.
        /// </summary>
        public IPagedList<AgenciaModel> Lista { get; set; }

        /// <summary>
        /// Agencia actualmente en edición.
        /// </summary>
        public AgenciaModel? AgenciaEnEdicion { get; set; }

        /// <summary>
        /// Código seleccionado en el filtro.
        /// </summary>
        public string? CodccoSeleccionado { get; set; }
    }
}

public IActionResult Index(string? codcco, int? editId, int page = 1)
{
    var agencias = _servicio.ObtenerAgencias(); // tu método de obtención
    var listaPaginada = agencias.ToPagedList(page, 10);

    var modelo = new AgenciaIndexViewModel
    {
        Lista = listaPaginada,
        AgenciaEnEdicion = agencias.FirstOrDefault(a => a.Codcco == editId),
        CodccoSeleccionado = codcco
    };

    ViewBag.AgenciasFiltro = ObtenerSelectListAgencias(); // método que llena el combo
    return View(modelo);
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
            @foreach (var agencia in (List<SelectListItem>)ViewBag.AgenciasFiltro)
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
