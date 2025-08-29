/// <summary>
/// Agrega UNA fila desde un objeto decorado con [ColumnName] (en el orden de IntoColumns si se definió,
/// de lo contrario en el orden de los atributos).
/// </summary>
public InsertQueryBuilder FromObject(object entity)
{
    if (entity is null) return this;

    var t = entity.GetType();
    var cols = _mappedType == t && _mappedColumns is { Count: > 0 }
        ? _mappedColumns
        : ModelInsertMapper.GetColumns(t);

    // Orden objetivo: IntoColumns() si ya lo definiste; si no, atributos.
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

    // 🔧 **Clave**: si no había columnas definidas aún, inicialízalas aquí
    if (_columns.Count == 0)
        _columns.AddRange(targetCols);

    return Row(row);
}
