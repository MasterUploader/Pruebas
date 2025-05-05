<table class="table table-bordered table-hover">
    <thead class="table-dark">
        <tr>
            <th>Agencia</th>
            <th>ID Video</th>
            <th>Nombre</th>
            <th>Ruta</th>
            <th>Estado</th>
            <th>Secuencia</th>
            <th>Acciones</th>
        </tr>
    </thead>
    <tbody>
    @foreach (var item in Model)
    {
        <tr>
            <td>@item.Codcco</td>
            <td>@item.CodVideo</td>
            <td>@item.Nombre</td>
            <td>@item.Ruta</td>

            <!-- Columna editable para actualizar solo el Estado -->
            <td>
                <form asp-action="Actualizar" method="post" class="d-flex">
                    <input type="hidden" name="codVideo" value="@item.CodVideo" />
                    <input type="hidden" name="codcco" value="@item.Codcco" />
                    <input type="hidden" name="Seq" value="@item.Seq" />

                    <input type="text" name="Estado" value="@item.Estado" class="form-control form-control-sm me-2" />
                    <button type="submit" class="btn btn-sm btn-success">Guardar</button>
                </form>
            </td>

            <!-- Solo lectura para Secuencia -->
            <td>@item.Seq</td>

            <!-- Botón para eliminar -->
            <td>
                <form asp-action="Eliminar" method="post" onsubmit="return confirm('¿Desea eliminar este video?');">
                    <input type="hidden" name="codVideo" value="@item.CodVideo" />
                    <input type="hidden" name="codcco" value="@item.Codcco" />
                    <button type="submit" class="btn btn-sm btn-danger">Eliminar</button>
                </form>
            </td>
        </tr>
    }
    </tbody>
</table>
