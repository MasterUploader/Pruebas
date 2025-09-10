Así tengo el build

/// <summary>
/// Construye el SQL MERGE final y su lista de parámetros (para DB2 i: placeholders <c>?</c>).
/// <para>Orden de parámetros:</para>
/// <list type="number">
/// <item><description>Parámetros de <c>USING</c> (SELECT o VALUES), en el orden en que aparecen en el SQL.</description></item>
/// <item><description>Parámetros de <c>UPDATE SET</c> agregados con <see cref="SetParam"/> (en el orden de las asignaciones).</description></item>
/// <item><description>Parámetros de <c>INSERT VALUES</c> agregados con <see cref="MapParam"/> (en el orden de los mapeos).</description></item>
/// </list>
/// </summary>
public QueryResult Build()
{
    // Limpiar el acumulador por si reusan la misma instancia
    _parameters.Clear();

    // Validaciones
    if (string.IsNullOrWhiteSpace(_targetTable))
        throw new InvalidOperationException("Debe especificarse la tabla destino.");
    if (_sourceKind == SourceKind.None)
        throw new InvalidOperationException("Debe especificarse la fuente USING (VALUES o SELECT).");
    if (string.IsNullOrWhiteSpace(_onConditionSql))
        throw new InvalidOperationException("Debe especificarse la condición ON.");

    // Para USING(VALUES) las columnas son obligatorias
    if (_sourceKind == SourceKind.Values && _sourceColumns.Count == 0)
        throw new InvalidOperationException("Para USING (VALUES) debe definir nombres de columnas de la fuente.");

    var sb = new StringBuilder();

    // MERGE INTO <lib.tabla> AS T
    sb.Append("MERGE INTO ");
    if (!string.IsNullOrWhiteSpace(_targetLibrary))
        sb.Append(_targetLibrary).Append('.');
    sb.Append(_targetTable).Append(' ').Append("AS ").Append(_targetAlias).Append('\n');

    // USING (...)
    sb.Append("USING (");
    if (_sourceKind == SourceKind.Values)
    {
        // (VALUES (?, ?, ...), (?, ?, ...))
        sb.Append("VALUES").Append('\n');

        var lines = new List<string>();
        foreach (var row in _sourceValuesRows)
        {
            var placeholders = new string[row.Length];
            for (int i = 0; i < row.Length; i++)
            {
                placeholders[i] = "?";
                _parameters.Add(row[i]); // 1) Primero, parámetros de USING(VALUES)
            }
            lines.Add($"({string.Join(", ", placeholders)})");
        }
        sb.Append(string.Join(",\n", lines));
    }
    else
    {
        // USING ( <SELECT...> )
        var sel = _sourceSelect!.Build();

        // 1) Primero, los parámetros del SELECT en USING
        if (sel.Parameters is { Count: > 0 })
            _parameters.AddRange(sel.Parameters);

        sb.Append(sel.Sql);
    }
    sb.Append(')');

    // Alias y columnas de la fuente:
    // - Para VALUES: obligatorio. Ya validado arriba.
    // - Para SELECT: opcional. Si hay nombres, se emiten.
    sb.Append(' ').Append("AS ").Append(_sourceAlias);
    if (_sourceColumns.Count > 0)
    {
        sb.Append('(').Append(string.Join(", ", _sourceColumns)).Append(')');
    }
    sb.Append('\n');

    // ON ...
    sb.Append("ON ").Append(_onConditionSql).Append('\n');

    // WHEN MATCHED THEN UPDATE SET ...
    if (_matchedSet.Count > 0)
    {
        sb.Append("WHEN MATCHED");
        if (!string.IsNullOrWhiteSpace(_matchedAndCondition))
            sb.Append(" AND (").Append(_matchedAndCondition).Append(')');
        sb.Append(" THEN UPDATE SET").Append('\n');

        var assigns = new List<string>();
        foreach (var s in _matchedSet)
        {
            string rhs = s.Kind switch
            {
                SetValueKind.SourceExpr => s.Expression!,   // ej: S.COL
                SetValueKind.Raw => s.Expression!,   // ej: CASE ...
                SetValueKind.Param => "?",             // parametrizado
                _ => throw new InvalidOperationException("Tipo de asignación inválido.")
            };

            if (s.Kind == SetValueKind.Param)
                _parameters.Add(s.Value); // 2) Después de USING, parámetros de SET

            assigns.Add($"{_targetAlias}.{s.TargetColumn} = {rhs}");
        }
        sb.Append(string.Join(",\n", assigns)).Append('\n');
    }

    // WHEN NOT MATCHED THEN INSERT (...) VALUES (...)
    if (_notMatchedInsert.Count > 0)
    {
        sb.Append("WHEN NOT MATCHED");
        if (!string.IsNullOrWhiteSpace(_notMatchedAndCondition))
            sb.Append(" AND (").Append(_notMatchedAndCondition).Append(')');
        sb.Append(" THEN INSERT").Append('\n');

        var cols = _notMatchedInsert.Select(m => m.TargetColumn).ToArray();
        sb.Append('(').Append(string.Join(", ", cols)).Append(')').Append('\n');

        sb.Append("VALUES").Append('\n');

        var vals = new List<string>();
        foreach (var m in _notMatchedInsert)
        {
            string rhs = m.Kind switch
            {
                InsertValueKind.SourceExpr => m.Expression!, // ej: S.UID
                InsertValueKind.Raw => m.Expression!, // ej: ''
                InsertValueKind.Param => "?",           // parametrizado
                _ => throw new InvalidOperationException("Tipo de mapeo inválido.")
            };

            if (m.Kind == InsertValueKind.Param)
                _parameters.Add(m.Value); // 3) Finalmente, parámetros de INSERT

            vals.Add(rhs);
        }

        sb.Append('(').Append(string.Join(", ", vals)).Append(')').Append('\n');
    }

    // Nota: No se añade ';' aquí. De ser necesario, el caller puede agregarlo.
    return new QueryResult
    {
        Sql = sb.ToString(),
        Parameters = _parameters
    };
}
