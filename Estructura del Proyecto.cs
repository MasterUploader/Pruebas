<td>
    <form asp-action="Actualizar" method="post" class="d-flex">
        <input type="hidden" name="codVideo" value="@item.CodVideo" />
        <input type="hidden" name="codcco" value="@item.Codcco" />
        <input type="hidden" name="Seq" value="@item.Seq" />

        <select name="Estado" class="form-select form-select-sm me-2">
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

        <button type="submit" class="btn btn-sm btn-success">Guardar</button>
    </form>
</td>
