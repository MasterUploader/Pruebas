[HttpPost]
public IActionResult Eliminar(int codcco)
{
    if (codcco <= 0)
    {
        TempData["Error"] = "Código de agencia no válido.";
        return RedirectToAction("Index");
    }

    var eliminado = _agenciaService.EliminarAgencia(codcco);
    TempData["Mensaje"] = eliminado
        ? "Agencia eliminada correctamente."
        : "No se pudo eliminar la agencia.";

    return RedirectToAction("Index");
}
