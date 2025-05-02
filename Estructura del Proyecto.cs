using Microsoft.AspNetCore.Mvc.Rendering;

/// <summary>
/// Obtiene las agencias en formato SelectListItem para desplegar en el formulario
/// </summary>
public List<SelectListItem> ObtenerAgenciasSelectList()
{
    var agencias = new List<SelectListItem>();

    _as400.Open();
    using var command = _as400.GetDbCommand();
    command.CommandText = "SELECT CODCCO, NOMAGE FROM BCAH96DTA.RSAGE01 ORDER BY NOMAGE";

    if (command.Connection.State == ConnectionState.Closed)
        command.Connection.Open();

    using var reader = command.ExecuteReader();
    while (reader.Read())
    {
        agencias.Add(new SelectListItem
        {
            Value = reader["CODCCO"].ToString(),
            Text = reader["NOMAGE"].ToString()
        });
    }

    return agencias;
}




[HttpGet]
public IActionResult Agregar()
{
    var agencias = _videoService.ObtenerAgenciasSelectList();
    ViewBag.Agencias = agencias;
    return View();
}





<!-- Campo: Código de Agencia -->
<div class="mb-3">
    <label for="codcco" class="form-label">Código de Agencia</label>
    <select class="form-select" id="codcco" name="codcco" required>
        <option value="">Seleccione una agencia...</option>
        @foreach (var agencia in ViewBag.Agencias as List<SelectListItem>)
        {
            <option value="@agencia.Value">@agencia.Text</option>
        }
    </select>
    <small class="form-text text-muted">Seleccione una o ingrese 0 si aplica a todas las agencias.</small>
</div>
