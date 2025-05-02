public List<SelectListItem> ObtenerAgenciasSelectList()
{
    var agencias = new List<SelectListItem>();

    try
    {
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
    }
    catch (Exception ex)
    {
        // Puedes loguearlo si tienes un sistema de logging, por ahora solo devolvemos una opción informativa
        agencias.Clear();
        agencias.Add(new SelectListItem
        {
            Value = "",
            Text = "Error al obtener agencias: " + ex.Message
        });
    }

    return agencias;
}


@if (ViewBag.Agencias is List<SelectListItem> listaAgencias)
{
    var esError = listaAgencias.Count == 1 && string.IsNullOrEmpty(listaAgencias[0].Value);

    <select class="form-select" id="codcco" name="codcco" required>
        @foreach (var agencia in listaAgencias)
        {
            <option value="@agencia.Value">@agencia.Text</option>
        }
    </select>

    @if (esError)
    {
        <div class="text-danger mt-1">@listaAgencias[0].Text</div>
    }
}
