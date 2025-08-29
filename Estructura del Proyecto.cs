/// <summary>
/// Agrega un comentario SQL al inicio del comando para trazabilidad.
/// Se sanea a una sola línea para evitar inyección de comentarios.
/// </summary>
public InsertQueryBuilder WithComment(string? comment)
{
    if (string.IsNullOrWhiteSpace(comment))
        return this;

    // Saneamos: sin saltos de línea y sin secuencias peligrosas
    var sanitized = comment
        .Replace("\r", " ")
        .Replace("\n", " ")
        .Replace("--", "- -")
        .Trim();

    _comment = "-- " + sanitized;
    return this;
}


// Validar _rows con LINQ puro
int? badRowIndex = _rows
    .Select((row, idx) => new { row, idx })
    .Where(x => x.row == null || x.row.Count != _columns.Count)
    .Select(x => (int?)x.idx)
    .FirstOrDefault();

if (badRowIndex.HasValue)
{
    int idx = badRowIndex.Value;
    int count = _rows[idx]?.Count ?? 0;
    throw new InvalidOperationException(
        $"La fila #{idx} tiene {count} valores; se esperaban {_columns.Count}."
    );
}

// Validar _valuesRaw con LINQ puro
int? badRawIndex = _valuesRaw
    .Select((row, idx) => new { row, idx })
    .Where(x => x.row == null || x.row.Count != _columns.Count)
    .Select(x => (int?)x.idx)
    .FirstOrDefault();

if (badRawIndex.HasValue)
{
    int idx = badRawIndex.Value;
    int count = _valuesRaw[idx]?.Count ?? 0;
    throw new InvalidOperationException(
        $"La fila RAW #{idx} tiene {count} valores; se esperaban {_columns.Count}."
    );
}
