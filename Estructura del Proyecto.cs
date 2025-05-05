[HttpPost]
public IActionResult Agregar(MensajeModel model)
{
    // Validar datos obligatorios básicos
    if (string.IsNullOrWhiteSpace(model.Codcco) || string.IsNullOrWhiteSpace(model.Mensaje))
    {
        ModelState.AddModelError("", "Debe completar todos los campos.");
    }

    // Obtener secuencia antes de validar
    model.Seq = _mensajeService.GetSecuencia(model.Codcco); // <- Aquí estableces la secuencia

    // Si el modelo aún no es válido, regresar la vista
    if (!ModelState.IsValid)
    {
        ViewBag.Agencias = _mensajeService.ObtenerAgenciasSelectList();
        return View(model);
    }

    // Insertar mensaje
    bool ok = _mensajeService.InsertarMensaje(model);

    if (ok)
        return RedirectToAction("Index");

    ViewBag.Mensaje = "Error al guardar el mensaje.";
    ViewBag.Agencias = _mensajeService.ObtenerAgenciasSelectList();
    return View(model);
}
