<tbody>
@foreach (var item in Model)
{
    <tr>
        <td>@item.Codcco</td>
        <td>@item.CodVideo</td>
        <td>@item.Nombre</td>

        <td>
            <!-- Formulario para actualizar Estado y Seq -->
            <form asp-action="Actualizar" method="post" class="d-flex flex-column gap-1">
                <input type="hidden" name="codVideo" value="@item.CodVideo" />
                <input type="hidden" name="codcco" value="@item.Codcco" />

                <div class="input-group">
                    <input type="text" name="Estado" value="@item.Estado" class="form-control form-control-sm" />
                    <input type="number" name="Seq" value="@item.Seq" class="form-control form-control-sm" />
                </div>

                <button type="submit" class="btn btn-sm btn-success mt-1">Guardar</button>
            </form>
        </td>

        <td>
            <!-- Formulario para eliminar -->
            <form asp-action="Eliminar" method="post" onsubmit="return confirm('¿Estás seguro de eliminar este video?');">
                <input type="hidden" name="codVideo" value="@item.CodVideo" />
                <input type="hidden" name="codcco" value="@item.Codcco" />
                <button type="submit" class="btn btn-sm btn-danger">Eliminar</button>
            </form>
        </td>
    </tr>
}
</tbody>
