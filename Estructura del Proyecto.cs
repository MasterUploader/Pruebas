// Indica el tipo de fuente USING
private enum SourceKind { None, Values, Select }

// Si ya tienes estos, mantén los tuyos; muestro los relevantes:
private SourceKind _sourceKind = SourceKind.None;
private readonly List<string> _sourceColumns = [];            // AS S(col1, col2, ...)
private readonly List<object?[]> _sourceValuesRows = [];      // Soporte legacy: VALUES con "?" sin tipo
private readonly List<List<(string Sql, object? Val)>> _sourceValuesTypedRows = []; // NUEVO: filas tipadas


/// <summary>
/// Define la fila de <c>USING (VALUES ...)</c> con placeholders **tipados** (DB2 i).
/// Ejemplo:
/// <code>
/// .UsingValuesTyped(
///     ("UID",  Db2ITyped.VarChar(userId, 20)),
///     ("NOWTS",Db2ITyped.Timestamp(now)),
///     ("EXI",  Db2ITyped.Char(exitoso, 1)),
///     ("IP",   Db2ITyped.VarChar(ip, 64)),
///     ("DEV",  Db2ITyped.VarChar(device, 64)),
///     ("BRO",  Db2ITyped.VarChar(browser, 64)),
///     ("TOK",  Db2ITyped.VarChar(token, 512))
/// )
/// </code>
/// Genera: <c>USING (VALUES (CAST(? AS VARCHAR(20)), TIMESTAMP(?), CAST(? AS CHAR(1)), ...)) AS S(...)</c>
/// </summary>
public MergeQueryBuilder UsingValuesTyped(params (string Column, Db2ITyped Value)[] values)
{
    if (values is null || values.Length == 0)
        throw new ArgumentException("Debe especificar al menos un valor para USING (VALUES ...).", nameof(values));

    _sourceKind = SourceKind.Values;
    _sourceColumns.Clear();
    _sourceValuesRows.Clear();        // limpiamos modo legacy
    _sourceValuesTypedRows.Clear();   // reiniciamos filas tipadas

    var row = new List<(string Sql, object? Val)>(values.Length);

    foreach (var (col, typed) in values)
    {
        if (string.IsNullOrWhiteSpace(col))
            throw new ArgumentException("El nombre de columna en USING no puede ser vacío.", nameof(values));

        _sourceColumns.Add(col);
        row.Add((typed.Sql, typed.Value)); // guardamos el fragmento SQL y el valor
    }

    _sourceValuesTypedRows.Add(row);
    return this;
}

/// <summary>
/// Agrega **otra** fila tipada a <c>USING (VALUES ...)</c>.
/// Debe llamarse después de <see cref="UsingValuesTyped((string, Db2ITyped)[])"/>.
/// </summary>
public MergeQueryBuilder UsingValuesTypedRow(params (string Column, Db2ITyped Value)[] values)
{
    if (_sourceKind != SourceKind.Values || _sourceColumns.Count == 0)
        throw new InvalidOperationException("Debe llamar primero a UsingValuesTyped(...) para definir columnas de la fuente.");

    if (values is null || values.Length != _sourceColumns.Count)
        throw new ArgumentException($"Se esperaban {_sourceColumns.Count} valores para la fila adicional de USING (VALUES ...).");

    var row = new List<(string Sql, object? Val)>(values.Length);

    for (int i = 0; i < values.Length; i++)
    {
        var (col, typed) = values[i];

        // Validación opcional: el nombre debe coincidir con el definido en la primera fila
        if (!col.Equals(_sourceColumns[i], StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException($"La columna #{i + 1} ('{col}') no coincide con '{_sourceColumns[i]}' definida en la primera fila.");

        row.Add((typed.Sql, typed.Value));
    }

    _sourceValuesTypedRows.Add(row);
    return this;
}

/// <summary>
/// Construye el SQL MERGE final y su lista de parámetros (para DB2 i: placeholders <c>?</c>).
/// <para>Orden de parámetros:</para>
/// <list type="number">
/// <item><description>Parámetros de <c>USING</c> (SELECT o VALUES), en el orden en que aparecen en el SQL.</description></item>
/// <item><description>Parámetros de <c>UPDATE SET</c> agregados con <c>SetParam</c> (en el orden de las asignaciones).</description></item>
/// <item><description>Parámetros de <c>INSERT VALUES</c> agregados con <c>MapParam</c> (en el orden de los mapeos).</description></item>
/// </list>
/// </summary>
public QueryResult Build()
{
    // Limpiar el acumulador por si reusan la misma instancia
    _parameters.Clear();

    // Validaciones mínimas
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
        sb.Append("VALUES").Append('\n');

        // Si hay filas tipadas, se priorizan; si no, caemos al modo legacy de object?[] con "?"
        if (_sourceValuesTypedRows.Count > 0)
        {
            var rowSqls = new List<string>(_sourceValuesTypedRows.Count);
            foreach (var row in _sourceValuesTypedRows)
            {
                // Agregamos la fila (CAST(? AS ...), TIMESTAMP(?), ...)
                rowSqls.Add("(" + string.Join(", ", row.Select(e => e.Sql)) + ")");

                // Cargamos los parámetros en el mismo orden
                foreach (var e in row) _parameters.Add(e.Val);
            }
            sb.Append(string.Join(",\n", rowSqls));
        }
        else
        {
            // Legacy: VALUES (?, ?, ...), (?, ?, ...)
            var rowSqls = new List<string>(_sourceValuesRows.Count);
            foreach (var row in _sourceValuesRows)
            {
                var placeholders = Enumerable.Repeat("?", row.Length);
                rowSqls.Add("(" + string.Join(", ", placeholders) + ")");
                _parameters.AddRange(row); // parámetros de USING(VALUES)
            }
            sb.Append(string.Join(",\n", rowSqls));
        }
    }
    else
    {
        // USING ( <SELECT...> )
        var sel = _sourceSelect!.Build();

        if (sel.Parameters is { Count: > 0 })
            _parameters.AddRange(sel.Parameters); // parámetros del SELECT

        sb.Append(sel.Sql);
    }
    sb.Append(')');

    // Alias y columnas de la fuente
    sb.Append(' ').Append("AS ").Append(_sourceAlias);
    if (_sourceColumns.Count > 0)
        sb.Append('(').Append(string.Join(", ", _sourceColumns)).Append(')');
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

        var assigns = new List<string>(_matchedSet.Count);
        foreach (var s in _matchedSet)
        {
            string rhs = s.Kind switch
            {
                SetValueKind.SourceExpr => s.Expression!,  // ej: S.COL
                SetValueKind.Raw       => s.Expression!,  // ej: CASE ...
                SetValueKind.Param     => "?",            // parametrizado
                _ => throw new InvalidOperationException("Tipo de asignación inválido.")
            };

            if (s.Kind == SetValueKind.Param)
                _parameters.Add(s.Value); // parámetros de SET

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

        var vals = new List<string>(_notMatchedInsert.Count);
        foreach (var m in _notMatchedInsert)
        {
            string rhs = m.Kind switch
            {
                InsertValueKind.SourceExpr => m.Expression!, // ej: S.UID
                InsertValueKind.Raw       => m.Expression!, // ej: ''
                InsertValueKind.Param     => "?",           // parametrizado
                _ => throw new InvalidOperationException("Tipo de mapeo inválido.")
            };

            if (m.Kind == InsertValueKind.Param)
                _parameters.Add(m.Value); // parámetros de INSERT

            vals.Add(rhs);
        }

        sb.Append('(').Append(string.Join(", ", vals)).Append(')').Append('\n');
    }

    // Sin ';' final (DB2/OleDb es sensible). El caller puede añadirlo si hace falta.
    return new QueryResult
    {
        Sql = sb.ToString(),
        Parameters = _parameters
    };
}




