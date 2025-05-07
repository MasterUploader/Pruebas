<td>
    <input asp-for="MarqCheck" class="form-check-input" />
</td>
<td>
    <input asp-for="RstCheck" class="form-check-input" />
</td>


[HttpPost]
[AutorizarPorTipoUsuario("1")]
[ValidateAntiForgeryToken]
public async Task<IActionResult> GuardarEdicion(AgenciaModel model)
{
    if (!ModelState.IsValid)
    {
        TempData["Mensaje"] = "Datos inválidos, por favor revise el formulario.";
        var agenciasInvalidas = await _agenciaService.ObtenerAgenciasAsync();
        return View("Index", agenciasInvalidas.ToPagedList(1, 50));
    }

    var actualizado = _agenciaService.ActualizarAgencia(model);

    TempData["Mensaje"] = actualizado
        ? "Agencia actualizada correctamente."
        : "Ocurrió un error al actualizar.";

    var agencias = await _agenciaService.ObtenerAgenciasAsync();
    return View("Index", agencias.ToPagedList(1, 50));
    }
