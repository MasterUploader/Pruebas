<td>
    <input type="hidden" name="Marquesina" value="NO" />
    <input type="checkbox" name="Marquesina" value="SI" class="form-check-input" @(item.Marquesina == "SI" ? "checked" : "") />
</td>
<td>
    <input type="hidden" name="RstBranch" value="NO" />
    <input type="checkbox" name="RstBranch" value="SI" class="form-check-input" @(item.RstBranch == "SI" ? "checked" : "") />
</td>
