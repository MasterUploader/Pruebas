<!-- Para Marquesina -->
<input type="hidden" name="Marquesina" value="NO" />
<input type="checkbox" name="Marquesina" value="SI" class="form-check-input" @(item.Marquesina == "SI" ? "checked" : "") />

<!-- Para RstBranch -->
<input type="hidden" name="RstBranch" value="NO" />
<input type="checkbox" name="RstBranch" value="SI" class="form-check-input" @(item.RstBranch == "SI" ? "checked" : "") />

    <option value="" selected="@(ViewBag.CodccoSeleccionado == null)">-- Todas las Agencias --</option>
