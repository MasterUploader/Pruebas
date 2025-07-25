/// <summary>
/// Construye y retorna la consulta INSERT generada junto con la lista de parámetros si corresponde.
/// </summary>
public QueryResult Build()
{
    // Validaciones básicas
    if (string.IsNullOrWhiteSpace(_tableName))
        throw new InvalidOperationException("Debe especificarse el nombre de la tabla para INSERT.");

    if (_columns.Count == 0)
        throw new InvalidOperationException("Debe especificar al menos una columna para el INSERT.");

    if (_selectSource != null && _rows.Count > 0)
        throw new InvalidOperationException("No se puede usar 'VALUES' y 'FROM SELECT' al mismo tiempo.");

    if (_selectSource == null && _rows.Count == 0 && _values.Count == 0)
        throw new InvalidOperationException("Debe especificar al menos una fila de valores para el INSERT.");

    // Validar filas manuales
    foreach (var fila in _rows)
    {
        if (fila.Count != _columns.Count)
            throw new InvalidOperationException($"El número de valores ({fila.Count}) no coincide con las columnas ({_columns.Count}).");
    }

    // Validar valores RAW si existen
    foreach (var fila in _values)
    {
        if (fila.Count != _columns.Count)
            throw new InvalidOperationException($"El número de valores RAW ({fila.Count}) no coincide con las columnas ({_columns.Count}).");
    }

    var sb = new StringBuilder();
    var parameters = new List<object?>();

    // Comentario si se agregó
    if (!string.IsNullOrWhiteSpace(_comment))
        sb.AppendLine(_comment);

    // Cláusula INSERT
    sb.Append("INSERT ");
    if (_insertIgnore) sb.Append("IGNORE ");
    sb.Append("INTO ");
    if (!string.IsNullOrWhiteSpace(_library))
        sb.Append($"{_library}.");
    sb.Append(_tableName);
    sb.Append(" (");
    sb.Append(string.Join(", ", _columns));
    sb.Append(")");

    if (_selectSource != null)
    {
        sb.AppendLine();
        sb.Append(_selectSource.Build().Sql);

        if (!string.IsNullOrWhiteSpace(_whereClause))
            sb.Append($" WHERE {_whereClause}");
    }
    else
    {
        sb.Append(" VALUES ");

        var valueLines = new List<string>();

        foreach (var row in _rows)
        {
            var placeholders = new List<string>();

            foreach (var value in row)
            {
                placeholders.Add("?");
                parameters.Add(value); // Aquí agregamos a la lista de parámetros
            }

            valueLines.Add($"({string.Join(", ", placeholders)})");
        }

        // También soportamos filas de valores RAW (ej: funciones SQL)
        foreach (var row in _values)
        {
            valueLines.Add($"({string.Join(", ", row.Select(SqlHelper.FormatValue))})");
        }

        sb.Append(string.Join(", ", valueLines));
    }

    // ON DUPLICATE KEY UPDATE
    if (_onDuplicateUpdate.Count > 0)
    {
        sb.Append(" ON DUPLICATE KEY UPDATE ");
        sb.Append(string.Join(", ", _onDuplicateUpdate.Select(kv =>
        {
            if (kv.Value is string str && str.Trim().StartsWith("(")) // función como (GETDATE())
                return $"{kv.Key} = {str}";
            return $"{kv.Key} = {SqlHelper.FormatValue(kv.Value)}";
        })));
    }

    return new QueryResult
    {
        Sql = sb.ToString(),
        Parameters = parameters // Se asignan los parámetros aquí
    };
}
