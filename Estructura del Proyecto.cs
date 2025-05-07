@foreach (var agencia in (List<SelectListItem>)ViewBag.AgenciasFiltro)
{
    string selected = ViewBag.CodccoSeleccionado?.ToString() == agencia.Value ? "selected" : "";

    @:<option value="@agencia.Value" @selected>@agencia.Text</option>
}
