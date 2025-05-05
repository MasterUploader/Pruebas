var errores = ModelState
    .Where(ms => ms.Value.Errors.Count > 0)
    .Select(ms => new { Campo = ms.Key, Errores = ms.Value.Errors.Select(e => e.ErrorMessage) })
    .ToList();
