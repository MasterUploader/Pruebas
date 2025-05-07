<select id="codcco" name="codcco" class="form-select" onchange="this.form.submit()">
    @{
        var codccoSel = ViewBag.CodccoSeleccionado?.ToString();
    }

    @:<option value="" @(string.IsNullOrEmpty(codccoSel) ? "selected" : "")>-- Todas las Agencias --</option>

    @foreach (var agencia in (List<SelectListItem>)ViewBag.AgenciasFiltro)
    {
        var selected = agencia.Value == codccoSel ? "selected" : "";
        @:<option value="@agencia.Value" @selected>@agencia.Text</option>
    }
</select>
