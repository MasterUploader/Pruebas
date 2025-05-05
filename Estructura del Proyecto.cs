<div class="mb-3">
    <label for="codcco" class="form-label">Agencia</label>
    <select id="codcco" name="Codcco" class="form-select" asp-for="Codcco" required>
        <option value="">Seleccione una agencia</option>
        @foreach (var agencia in agencias)
        {
            <option value="@agencia.Value">@agencia.Text</option>
        }
    </select>
    <span asp-validation-for="Codcco" class="text-danger"></span>
</div>


[Required(ErrorMessage = "Debe seleccionar una agencia.")]
public string Codcco { get; set; } = string.Empty;
