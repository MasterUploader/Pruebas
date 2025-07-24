private SelectQueryBuilder? _selectSource;

/// <summary>
/// Define una subconsulta SELECT como fuente de datos a insertar.
/// Anula el uso de valores directos.
/// </summary>
/// <param name="select">Instancia de <see cref="SelectQueryBuilder"/> que genera el SELECT.</param>
public InsertQueryBuilder FromSelect(SelectQueryBuilder select)
{
    _selectSource = select;
    _values.Clear(); // Se eliminan valores si se usa FROM SELECT
    return this;
}


if (string.IsNullOrWhiteSpace(_tableName))
    throw new InvalidOperationException("Debe especificarse el nombre de la tabla para INSERT.");

if (_columns.Count == 0)
    throw new InvalidOperationException("Debe especificar al menos una columna para el INSERT.");

// No se pueden usar VALUES y SELECT al mismo tiempo
if (_selectSource != null && _values.Count > 0)
    throw new InvalidOperationException("No se puede usar 'VALUES' y 'FROM SELECT' al mismo tiempo.");

// Si no es FROM SELECT, validamos las filas
if (_selectSource == null)
{
    if (_values.Count == 0)
        throw new InvalidOperationException("Debe especificar al menos una fila de valores para el INSERT.");

    foreach (var fila in _values)
    {
        if (fila.Count != _columns.Count)
            throw new InvalidOperationException($"El número de valores ({fila.Count}) no coincide con las columnas ({_columns.Count}).");
    }
}

if (_selectSource != null)
{
    sb.AppendLine();
    sb.Append(_selectSource.Build().Sql);
}
else
{
    var valuesSql = _values
        .Select(row => $"({string.Join(", ", row.Select(SqlHelper.FormatValue))})");
    sb.Append(" VALUES ");
    sb.Append(string.Join(", ", valuesSql));
}
