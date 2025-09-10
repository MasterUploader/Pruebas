// ================== CAMPOS PRIVADOS (agregar a la clase) ==================
private enum MergeUsingKind { None, Values, Select }

private MergeUsingKind _usingKind = MergeUsingKind.None;

// Nombres de columnas de la tabla S (USING ... AS S(col1, col2, ...))
private readonly List<string> _usingSourceColumns = [];

// Cada fila VALUES se guarda como una lista con los fragmentos SQL tipados (CAST(? AS ...), TIMESTAMP(?), etc.)
private readonly List<List<string>> _usingValueRows = [];

// Parámetros para los placeholders "?" en el mismo orden en que se generan.
private readonly List<object?> _parameters = [];

// Si alguna vez usas UsingSelect(...), limpia _usingValueRows y marca _usingKind = Select
// ==========================================================================



/// <summary>
/// Define la fila de <c>USING (VALUES ...)</c> con **placeholders tipados para DB2 for i**.
/// <para>
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
/// Genera:
/// <c>USING (VALUES (CAST(? AS VARCHAR(20)), TIMESTAMP(?), CAST(? AS CHAR(1)), ...)) AS S(UID, NOWTS, EXI, ...)</c>
/// y agrega los valores a <see cref="_parameters"/> en el mismo orden.
/// </para>
/// </summary>
/// <param name="values">
/// Pares (NombreDeColumna, ValorTipado) para la tabla fuente <c>S</c>.
/// El orden define el orden de las columnas y de los parámetros.
/// </param>
/// <returns>El propio <see cref="MergeQueryBuilder"/> para encadenamiento.</returns>
/// <exception cref="ArgumentException">Si no se envía ningún valor o si un nombre de columna está vacío.</exception>
public MergeQueryBuilder UsingValuesTyped(params (string Column, Db2ITyped Value)[] values)
{
    if (values is null || values.Length == 0)
        throw new ArgumentException("Debe especificar al menos un valor para USING (VALUES ...).", nameof(values));

    // Marcamos explícitamente que la fuente del MERGE es USING VALUES
    _usingKind = MergeUsingKind.Values;

    _usingSourceColumns.Clear();
    _usingValueRows.Clear();       // solo una fila (si quieres varias usa UsingValuesTypedRow debajo)

    var rowSql = new List<string>(values.Length);

    foreach (var (col, typed) in values)
    {
        if (string.IsNullOrWhiteSpace(col))
            throw new ArgumentException("El nombre de columna en USING no puede ser vacío.", nameof(values));

        _usingSourceColumns.Add(col);
        rowSql.Add(typed.Sql);              // p.ej. CAST(? AS VARCHAR(20)) / TIMESTAMP(?)
        _parameters.Add(typed.Value);       // valor para el marcador '?' correspondiente
    }

    _usingValueRows.Add(rowSql);            // registramos la única fila
    return this;
}

/// <summary>
/// Agrega **otra** fila a <c>USING (VALUES ...)</c> con placeholders tipados (DB2 i).
/// Úsalo si necesitas más de una fila en la fuente <c>S</c>.
/// </summary>
public MergeQueryBuilder UsingValuesTypedRow(params (string Column, Db2ITyped Value)[] values)
{
    if (_usingKind != MergeUsingKind.Values || _usingSourceColumns.Count == 0)
        throw new InvalidOperationException("Debe llamar primero a UsingValuesTyped(...) para la primera fila.");

    if (values is null || values.Length != _usingSourceColumns.Count)
        throw new ArgumentException($"Se esperaban {_usingSourceColumns.Count} valores para la fila adicional de USING (VALUES ...).");

    var rowSql = new List<string>(values.Length);

    for (int i = 0; i < values.Length; i++)
    {
        var (col, typed) = values[i];
        // Validación opcional: el nombre debe coincidir con el de la primera fila
        if (!col.Equals(_usingSourceColumns[i], StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException($"La columna #{i + 1} ('{col}') no coincide con '{ _usingSourceColumns[i] }' definida en la primera fila.");

        rowSql.Add(typed.Sql);
        _parameters.Add(typed.Value);
    }

    _usingValueRows.Add(rowSql);
    return this;
}





// dentro de Build()
if (_usingKind == MergeUsingKind.None)
    throw new InvalidOperationException("Debe especificarse la fuente USING (VALUES o SELECT).");

if (_usingKind == MergeUsingKind.Values)
{
    // USING (VALUES (...), (...), ...) AS S(col1, col2, ...)
    var rows = _usingValueRows.Select(r => $"({string.Join(", ", r)})");
    sb.Append(" USING (VALUES ")
      .Append(string.Join(", ", rows))
      .Append(") AS S(")
      .Append(string.Join(", ", _usingSourceColumns))
      .Append(')');
}
else /* Select */ { /* ... */ }
