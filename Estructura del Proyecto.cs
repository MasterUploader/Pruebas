<!-- Para Marquesina -->
<td>
    <input type="hidden" name="MarqCheck" value="false" />
    <input type="checkbox" name="MarqCheck" value="true" class="form-check-input" @(item.MarqCheck ? "checked" : "") />
</td>

<!-- Para RstBranch -->
<td>
    <input type="hidden" name="RstCheck" value="false" />
    <input type="checkbox" name="RstCheck" value="true" class="form-check-input" @(item.RstCheck ? "checked" : "") />
</td>
