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
                <td><input type="checkbox" name="Marquesina" class="form-check-input" @(item.Marquesina ? "checked" : "") /></td>
                <td><input type="checkbox" name="RstBranch" class="form-check-input" @(item.RstBranch ? "checked" : "") /></td>
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
                @switch (item.Zona)
                {
                    case 1: @:CENTRO SUR; break;
                    case 2: @:NOR OCCIDENTE; break;
                    case 3: @:NOR ORIENTE; break;
                    default: @:DESCONOCIDA; break;
                }
            </td>
            <td>@(item.Marquesina ? "APLICA" : "NO APLICA")</td>
            <td>@(item.RstBranch ? "APLICA" : "NO APLICA")</td>
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



public async Task<IActionResult> Editar(int id)
{
    var agencias = await _agenciaService.ObtenerAgenciasAsync();
    ViewBag.EditId = id;
    return View("Index", agencias.ToPagedList(1, 50)); // O el número de página actual
}


public async Task<IActionResult> Editar(int id)
{
    var agencias = await _agenciaService.ObtenerAgenciasAsync();
    ViewBag.EditId = id;
    return View("Index", agencias.ToPagedList(1, 50)); // O el número de página actual
}
