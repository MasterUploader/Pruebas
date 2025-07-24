/// <summary>
/// Construye y retorna la consulta INSERT generada.
/// </summary>
public QueryResult Build()
{
    // Validaciones básicas
    if (string.IsNullOrWhiteSpace(_tableName))
        throw new InvalidOperationException("Debe especificarse el nombre de la tabla para INSERT.");

    if (_columns.Count == 0)
        throw new InvalidOperationException("Debe especificar al menos una columna para el INSERT.");

    if (_selectSource != null && _values.Count > 0)
        throw new InvalidOperationException("No se puede usar 'VALUES' y 'FROM SELECT' al mismo tiempo.");

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

    var sb = new StringBuilder();

    // Parte inicial del INSERT
    sb.Append($"INSERT INTO {_tableName} (");
    sb.Append(string.Join(", ", _columns));
    sb.Append(")");

    // 🔽 AQUÍ va el bloque que me preguntaste 🔽
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

    return new QueryResult
    {
        Sql = sb.ToString()
    };
}
