En esta filas tengo la advertencia S3267

// Validaciones
foreach (var fila in _rows)
{
    if (fila.Count != _columns.Count)
        throw new InvalidOperationException($"El número de valores ({fila.Count}) no coincide con las columnas ({_columns.Count}).");
}

foreach (var fila in _valuesRaw)
{
    if (fila.Count != _columns.Count)
        throw new InvalidOperationException($"El número de valores RAW ({fila.Count}) no coincide con las columnas ({_columns.Count}).");
}

Mejoralas para que deje de aparecer

Y En esta Linea:
 public InsertQueryBuilder WithComment(string comment)
 { if (!string.IsNullOrWhiteSpace(comment)) _comment = $"-- {comment}"; return this; }
me dice advertencia S2681
