[AutorizarPorTipoUsuario("1", "3")]
[HttpGet]
public async Task<IActionResult> Index(int? editId, int? editSeq, string codcco = null)
{
    // Agencias para el filtro
    var agencias = _mensajeService.ObtenerAgenciasSelectList();
    ViewBag.Agencias = agencias;

    // Mantener selección actual del filtro
    ViewBag.CodccoSeleccionado = codcco;

    // Pasar identificador de fila en edición (CodMsg + Seq)
    ViewBag.EditId = editId;
    ViewBag.EditSeq = editSeq;

    // Cargar mensajes (y filtrar por agencia si corresponde)
    var mensajes = await _mensajeService.ObtenerMensajesAsync();
    if (!string.IsNullOrEmpty(codcco))
        mensajes = mensajes.Where(m => m.Codcco == codcco).ToList();

    return View(mensajes);
}




@model List<CAUAdministracion.Models.MensajeModel>
@using Microsoft.AspNetCore.Mvc.Rendering

@{
    ViewData["Title"] = "Mantenimiento de Mensajes";
    var agencias = ViewBag.Agencias as List<SelectListItem>;
    var codccoSel = ViewBag.CodccoSeleccionado as string;

    // Edit key: CodMsg + Seq
    int? editId = ViewBag.EditId as int?;
    int? editSeq = ViewBag.EditSeq as int?;
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
                // IDs únicos por fila, ahora considerando también la SEQ
                var formUpdateId = $"f-upd-{item.Codcco}-{item.CodMsg}-{item.Seq}";
                var formDeleteId = $"f-del-{item.Codcco}-{item.CodMsg}-{item.Seq}";

                <!-- Formularios “invisibles” por fila -->
                <tr class="d-none">
                    <td colspan="5" class="p-0">
                        <form id="@formUpdateId" asp-controller="Messages" asp-action="Actualizar" method="post">
                            @Html.AntiForgeryToken()
                            <input type="hidden" name="Codcco" value="@item.Codcco" />
                            <input type="hidden" name="CodMsg" value="@item.CodMsg" />
                            <input type="hidden" name="Seq"    value="@item.Seq" />
                        </form>

                        <form id="@formDeleteId" asp-controller="Messages" asp-action="Eliminar" method="post">
                            @Html.AntiForgeryToken()
                            <input type="hidden" name="Codcco" value="@item.Codcco" />
                            <input type="hidden" name="CodMsg" value="@item.CodMsg" />
                        </form>
                    </td>
                </tr>

                @if (editId.HasValue && editSeq.HasValue && editId.Value == item.CodMsg && editSeq.Value == item.Seq)
                {
                    <!-- Fila en modo edición (única, por CodMsg + Seq) -->
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
                                <option value="A" @(item.Estado == "A" ? "selected" : "")>Activo</option>
                                <option value="I" @(item.Estado == "I" ? "selected" : "")>Inactivo</option>
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
                               asp-route-editId="@item.CodMsg"
                               asp-route-editSeq="@item.Seq">
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
