<form method="get" asp-action="Index" class="row mb-3">
    <div class="col-md-4">
        <label for="codcco" class="form-label">Filtrar por Agencia</label>
        <select id="codcco" name="codcco" class="form-select" onchange="this.form.submit()">
            @{
                var codccoSel = ViewBag.CodccoSeleccionado?.ToString();
                var selectedDefault = string.IsNullOrEmpty(codccoSel) ? "selected" : "";
            }
            <option value="" @Html.Raw(selectedDefault)>-- Todas las Agencias --</option>

            @foreach (var agencia in (List<SelectListItem>)ViewBag.AgenciasFiltro)
            {
                var selected = agencia.Value == codccoSel ? "selected" : "";
                <option value="@agencia.Value" @Html.Raw(selected)>@agencia.Text</option>
            }
        </select>
    </div>
</form>
