/// <summary>
/// Agrega una fila por posición (parametrizada). El orden debe coincidir con IntoColumns
/// o, si no se definió, se infiere desde los atributos [ColumnName].
/// </summary>
public InsertQueryBuilder Row(params object?[] values)
{
    // Inicialización perezosa de columnas:
    if (_columns.Count == 0)
    {
        if (_mappedColumns is { Count: > 0 })
        {
            _columns.AddRange(_mappedColumns.Select(c => c.ColumnName));
        }
        else
        {
            // Último intento: si Row fue llamado sin constructor por Type,
            // no sabemos de dónde inferir; en ese caso sí lanzamos.
            throw new InvalidOperationException("Debe llamar primero a IntoColumns(...) antes de Row(...).");
        }
    }

    if (values is null || values.Length != _columns.Count)
        throw new InvalidOperationException($"Se esperaban {_columns.Count} valores, pero se recibieron {values?.Length ?? 0}.");

    _rows.Add(new List<object?>(values));
    return this;
}


public InsertQueryBuilder FromObject(object entity)
{
    if (entity is null) return this;

    var t = entity.GetType();
    var cols = _mappedType == t && _mappedColumns is { Count: > 0 }
        ? _mappedColumns
        : ModelInsertMapper.GetColumns(t);

    var targetCols = _columns.Count > 0
        ? _columns
        : new List<string>(cols.Select(c => c.ColumnName));

    var map = cols.ToDictionary(c => c.ColumnName, StringComparer.OrdinalIgnoreCase);
    var row = new object?[targetCols.Count];
    for (int i = 0; i < targetCols.Count; i++)
    {
        if (!map.TryGetValue(targetCols[i], out var meta))
            throw new InvalidOperationException($"La columna '{targetCols[i]}' no existe como [ColumnName] en {t.Name}.");
        row[i] = meta.Property.GetValue(entity);
    }

    // Asegura columnas antes de llamar a Row
    if (_columns.Count == 0)
        _columns.AddRange(targetCols);

    return Row(row);
}
