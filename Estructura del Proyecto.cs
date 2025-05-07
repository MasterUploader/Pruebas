<td>
    <select asp-for="Marquesina" class="form-select">
        <option value="NO">NO</option>
        <option value="SI">SI</option>
    </select>
</td>
<td>
    <select asp-for="RstBranch" class="form-select">
        <option value="NO">NO</option>
        <option value="SI">SI</option>
    </select>
</td>

ViewBag.Model = agenciaSeleccionada;
return View("Index", modeloPaginado);
