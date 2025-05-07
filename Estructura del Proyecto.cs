<td>
    <!-- Valor por defecto si no se marca -->
    <input type="hidden" name="MarqCheck" value="false" />
    <!-- Checkbox que se enlaza a MarqCheck (lo que actualiza Marquesina internamente) -->
    <input type="checkbox" name="MarqCheck" value="true" class="form-check-input" @(item.MarqCheck ? "checked" : "") />
</td>
<td>
    <!-- Valor por defecto si no se marca -->
    <input type="hidden" name="RstCheck" value="false" />
    <!-- Checkbox que se enlaza a RstCheck (lo que actualiza RstBranch internamente) -->
    <input type="checkbox" name="RstCheck" value="true" class="form-check-input" @(item.RstCheck ? "checked" : "") />
</td>
