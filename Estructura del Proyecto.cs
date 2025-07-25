/// <summary>
/// Obtiene un <see cref="DbCommand"/> con la consulta generada por QueryBuilder,
/// e inserta automáticamente los parámetros si el SQL contiene marcadores '?'.
/// </summary>
/// <param name="queryResult">Consulta SQL generada por QueryBuilder.</param>
/// <param name="context">Contexto HTTP actual, necesario para trazabilidad o uso interno de conexión.</param>
/// <returns>Comando configurado con SQL y parámetros listos para ejecutarse.</returns>
public DbCommand GetDbCommand(QueryResult queryResult, HttpContext context)
{
    var command = GetDbCommand(context);
    command.CommandText = queryResult.Sql;

    // Si hay parámetros, los insertamos automáticamente
    if (queryResult.Parameters?.Count > 0 && queryResult.Sql.Contains("?"))
    {
        foreach (var value in queryResult.Parameters)
        {
            var parameter = command.CreateParameter();
            parameter.Value = value ?? DBNull.Value;
            command.Parameters.Add(parameter);
        }
    }

    return command;
}

public QueryResult Build()
{
    // ... validaciones omitidas por brevedad

    var sb = new StringBuilder();
    var result = new QueryResult();

    if (!string.IsNullOrWhiteSpace(_comment))
        sb.AppendLine(_comment);

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
        var selectResult = _selectSource.Build();
        sb.Append(selectResult.Sql);
        result.Parameters = selectResult.Parameters; // ← hereda si SELECT tiene parámetros
        if (!string.IsNullOrWhiteSpace(_whereClause))
            sb.Append($" WHERE {_whereClause}");
    }
    else
    {
        // Insert con parámetros ? y carga de valores
        var allRows = _rows.Select(row =>
        {
            var rowPlaceholders = string.Join(", ", row.Select(_ => "?"));
            result.Parameters.AddRange(row); // ← agrega en el orden correcto
            return $"({rowPlaceholders})";
        });

        sb.Append(" VALUES ");
        sb.Append(string.Join(", ", allRows));
    }

    // ON DUPLICATE KEY UPDATE
    if (_onDuplicateUpdate.Count > 0)
    {
        sb.Append(" ON DUPLICATE KEY UPDATE ");
        sb.Append(string.Join(", ", _onDuplicateUpdate.Select(kv =>
        {
            result.Parameters.Add(kv.Value); // ← también los agrega aquí
            return $"{kv.Key} = ?";
        })));
    }

    result.Sql = sb.ToString();
    return result;
}
