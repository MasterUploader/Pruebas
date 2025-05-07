@foreach (var agencia in (List<SelectListItem>)ViewBag.AgenciasFiltro)
{
    var selected = ViewBag.CodccoSeleccionado?.ToString() == agencia.Value ? "selected" : "";

    <option value="@agencia.Value" selected="@selected">@agencia.Text</option>
    }
