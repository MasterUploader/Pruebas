<form method="get" asp-action="Index" class="row mb-3">
    <div class="col-md-4">
        <label for="codcco" class="form-label">Filtrar por Agencia</label>
        <select id="codcco" name="codcco" class="form-select" onchange="this.form.submit()">
            <option value="" @(ViewBag.CodccoSeleccionado == null ? "selected" : "")>-- Todas las Agencias --</option>

            @foreach (var agencia in (List<SelectListItem>)ViewBag.AgenciasFiltro)
            {
                var isSelected = ViewBag.CodccoSeleccionado?.ToString() == agencia.Value ? "selected" : "";
                <option value="@agencia.Value" @Html.Raw(isSelected)>@agencia.Text</option>
            }
        </select>
    </div>
</form>
