Al ejecutar el siguiente código:


            // ======================================================================
            // 2) MERGE (UPSERT) → BCAH96DTA.ETD02LOG  (intentos y sesión activa)
            //    - Parametrizado con USING (VALUES ...) + columnas de la fuente
            //    Lógica de “intentos”:
            //       - Si exitoso = '1' → intentos = intentos(previos) + 1
            //       - Si exitoso = '0' → intentos = 0
            //
            //    Campos:
            //     - LOGB02UIL (último login)      ← now
            //     - LOGB03TIL (intentos)          ← CASE basado en exitoso
            //     - LOGB04SEA (sesión activa)     ← exitoso
            //     - LOGB05UDI (IP)                ← machine.ClientIPAddress
            //     - LOGB06UTD (Device)            ← machine.Device
            //     - LOGB07UNA (Browser)           ← machine.Browser
            //     - LOGB08CBI (Bloqueo intento)   ← '' (vacío en tu inserción)
            //     - LOGB09UIF (último intento)    ← COALESCE(previo, now)  (si no hay previo, cae en now)
            //     - LOGB10TOK (token/sesión)      ← idSesion
            // ======================================================================

            // Construimos la fuente S con nombres de columnas y UNA fila de valores.
            // OJO: aquí pasamos DateTime 'now' como parámetro: DB2 i (vía OleDb) lo bindeará como TIMESTAMP.
            var merge = new MergeQueryBuilder("ETD02LOG", "BCAH96DTA")
                    .UsingValuesTyped(
                        ("UID", Db2ITyped.VarChar(userID, 20)),
                        ("NOWTS", Db2ITyped.Timestamp(now)),
                        ("EXI", Db2ITyped.Decimal(exitoso, 10, 0)),
                        ("IP", Db2ITyped.VarChar(machine.ClientIPAddress, 20)),
                        ("DEV", Db2ITyped.VarChar(machine.Device, 20)),
                        ("BRO", Db2ITyped.VarChar(machine.Browser, 20)),
                        ("TOK", Db2ITyped.VarChar(idSesion, 2000))
                    )
                    .On("T.LOGB01UID = S.UID")
                    .WhenMatchedUpdate(
                        "T.LOGB02UIL = S.NOWTS",
                        "T.LOGB03TIL = CASE WHEN S.EXI = '1' THEN COALESCE(T.LOGB03TIL, 0) + 1 ELSE 0 END",
                        "T.LOGB04SEA = S.EXI",
                        "T.LOGB05UDI = S.IP",
                        "T.LOGB06UTD = S.DEV",
                        "T.LOGB07UNA = S.BRO",
                        "T.LOGB09UIF = COALESCE(T.LOGB02UIL, S.NOWTS)",
                        "T.LOGB10TOK = S.TOK"
                    )
                    // Usa la sobrecarga con tuplas (también evita arrays)
                    .WhenNotMatchedInsert(
                        ("LOGB01UID", "S.UID"),
                        ("LOGB02UIL", "S.NOWTS"),
                        ("LOGB03TIL", "CASE WHEN S.EXI = '1' THEN 1 ELSE 0 END"),
                        ("LOGB04SEA", "S.EXI"),
                        ("LOGB05UDI", "S.IP"),
                        ("LOGB06UTD", "S.DEV"),
                        ("LOGB07UNA", "S.BRO"),
                        ("LOGB08CBI", "''"),          // vacío en inserción
                        ("LOGB09UIF", "S.NOWTS"),
                        ("LOGB10TOK", "S.TOK")
                    )
                    .Build();

            // Ejecutar
            var cmd2 = _connection.GetDbCommand(merge, _contextAccessor.HttpContext!);



Da el siguiente error:

System.InvalidOperationException: 'Debe especificarse la fuente USING (VALUES o SELECT).'


Actualmente el MergeQueryBuilder lo tengo así:

using QueryBuilder.Enums;
using QueryBuilder.Helpers;
using QueryBuilder.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace QueryBuilder.Builders;

/// <summary>
/// Generador de sentencias MERGE compatibles con DB2 for i (AS/400).
///
/// <para>Soporta:</para>
/// <list type="bullet">
/// <item><description><c>USING (VALUES ...)</c> con columnas y alias de la fuente (parametrizado con <c>?</c>).</description></item>
/// <item><description><c>USING (SELECT ...)</c> con <see cref="SelectQueryBuilder"/>; sus parámetros se fusionan automáticamente en <see cref="QueryResult.Parameters"/> en el orden en que aparecen en el SQL.</description></item>
/// <item><description><c>WHEN MATCHED THEN UPDATE SET ...</c> mediante <see cref="SetFromSource"/>, <see cref="SetRaw"/> o <see cref="SetParam"/>.</description></item>
/// <item><description><c>WHEN NOT MATCHED THEN INSERT (...)</c> mediante <see cref="Map"/>, <see cref="MapRaw"/> o <see cref="MapParam"/>.</description></item>
/// </list>
///
/// <para>En DB2 i, los parámetros se emiten como placeholders <c>?</c> y el orden en <see cref="QueryResult.Parameters"/> coincide con el orden en el SQL:
/// primero los de <c>USING</c> (SELECT o VALUES), luego los de UPDATE/INSERT.</para>
/// </summary>
/// <remarks>
/// Crea un nuevo <see cref="MergeQueryBuilder"/>.
/// </remarks>
/// <param name="targetTable">Tabla destino del MERGE.</param>
/// <param name="targetLibrary">Biblioteca/Esquema del destino (DB2 i).</param>
/// <param name="dialect">Dialecto SQL. Enfocado a <see cref="SqlDialect.Db2i"/>.</param>
public sealed class MergeQueryBuilder(string targetTable, string? targetLibrary = null, SqlDialect dialect = SqlDialect.Db2i)
{
    private readonly List<string> _usingSourceColumns = [];   // alias S(...) – nombres de columnas de la fuente
    private readonly List<string> _usingValueSql = [];        // cada item: CAST(? AS ...), TIMESTAMP(?), etc.
    private readonly List<string> _updateAssignments = [];
    private readonly List<string> _insertColumns = [];
    private readonly List<string> _insertValues = [];
    private enum SourceKind { None, Values, Select }

    // Si ya tienes estos, mantén los tuyos; muestro los relevantes:
    private SourceKind _sourceKind = SourceKind.None;
    private readonly List<List<(string Sql, object? Val)>> _sourceValuesTypedRows = []; // NUEVO: filas tipadas

    private enum MergeUsingKind { None, Values, Select }

    private MergeUsingKind _usingKind = MergeUsingKind.None;

    // Cada fila VALUES se guarda como una lista con los fragmentos SQL tipados (CAST(? AS ...), TIMESTAMP(?), etc.)
    private readonly List<List<string>> _usingValueRows = [];

    private readonly string _targetTable = targetTable ?? throw new ArgumentNullException(nameof(targetTable));
    private readonly string? _targetLibrary = targetLibrary;
    private readonly SqlDialect _dialect = dialect;

    // Alias de la tabla destino
    private string _targetAlias = "T";
    private string _sourceAlias = "S";

    /// <summary>
    /// Nombres de columnas visibles de la tabla fuente (S).
    /// <para>
    /// - Para <c>USING (VALUES ...)</c> son <b>obligatorias</b> y se emitirán como <c>AS S(col1, col2, ...)</c>.
    /// - Para <c>USING (SELECT ...)</c> son <b>opcionales</b>: si las proporcionas, también se emiten como <c>AS S(col1, col2, ...)</c>.
    ///   Esto ayuda cuando el SELECT no expone alias claros o quieres renombrar columnas.
    /// </para>
    /// </summary>
    private readonly List<string> _sourceColumns = [];

    // Filas VALUES para USING (VALUES ...)
    private readonly List<object?[]> _sourceValuesRows = [];

    // SELECT fuente para USING (SELECT ...)
    private SelectQueryBuilder? _sourceSelect;

    // Condición ON
    private string? _onConditionSql;

    // WHEN MATCHED THEN UPDATE
    private readonly List<SetAssignment> _matchedSet = [];
    private string? _matchedAndCondition;

    // WHEN NOT MATCHED THEN INSERT
    private readonly List<InsertMapping> _notMatchedInsert = [];
    private string? _notMatchedAndCondition;

    // Acumulador de parámetros (orden posicional en DB2 i)
    private readonly List<object?> _parameters = [];

    /// <summary>Define el alias de la tabla destino (por defecto <c>T</c>).</summary>
    public MergeQueryBuilder AsTarget(string alias)
    {
        if (string.IsNullOrWhiteSpace(alias)) throw new ArgumentNullException(nameof(alias));
        _targetAlias = alias.Trim();
        return this;
    }

    /// <summary>
    /// Define <c>USING (VALUES ...)</c> como fuente del MERGE, con columnas y alias.
    /// Cada valor se parametriza con <c>?</c> y se agrega a <see cref="QueryResult.Parameters"/> en el mismo orden.
    /// </summary>
    /// <param name="sourceColumns">Lista de columnas de la fuente S (ordenadas).</param>
    /// <param name="rows">Filas de valores; cada fila debe tener el mismo tamaño que <paramref name="sourceColumns"/>.</param>
    /// <param name="alias">Alias de la fuente (por defecto <c>S</c>).</param>
    public MergeQueryBuilder UsingValues(IEnumerable<string> sourceColumns, IEnumerable<object?[]> rows, string alias = "S")
    {
        if (sourceColumns is null || !sourceColumns.Any())
            throw new InvalidOperationException("Debe definir al menos una columna de la fuente para USING (VALUES).");

        _sourceKind = SourceKind.Values;
        _sourceAlias = string.IsNullOrWhiteSpace(alias) ? "S" : alias.Trim();

        _sourceColumns.Clear();
        _sourceColumns.AddRange(sourceColumns);

        _sourceValuesRows.Clear();
        foreach (var r in rows ?? [])
        {
            if (r is null || r.Length != _sourceColumns.Count)
                throw new InvalidOperationException($"Cada fila VALUES debe tener {_sourceColumns.Count} valores.");
            _sourceValuesRows.Add(r);
        }

        if (_sourceValuesRows.Count == 0)
            throw new InvalidOperationException("Debe especificar al menos una fila para USING (VALUES).");

        _sourceSelect = null; // No se puede mezclar con SELECT
        return this;
    }

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
    /// Define <c>USING (SELECT ...)</c> como fuente del MERGE, con alias.
    /// <para>Los parámetros del <paramref name="select"/> se fusionan automáticamente en <see cref="QueryResult.Parameters"/>,
    /// antes de los parámetros de UPDATE/INSERT.</para>
    /// </summary>
    /// <param name="select">Consulta SELECT fuente (puede contener parámetros).</param>
    /// <param name="alias">Alias de la fuente (por defecto <c>S</c>).</param>
    public MergeQueryBuilder UsingSelect(SelectQueryBuilder select, string alias = "S")
    {
        _sourceKind = SourceKind.Select;
        _sourceAlias = string.IsNullOrWhiteSpace(alias) ? "S" : alias.Trim();
        _sourceSelect = select ?? throw new ArgumentNullException(nameof(select));

        _sourceColumns.Clear();     // opcional: podrían definirse vía la otra sobrecarga
        _sourceValuesRows.Clear();  // no aplica
        return this;
    }

    /// <summary>
    /// Define <c>USING (SELECT ...)</c> como fuente del MERGE, con alias y <b>lista de columnas</b> de la fuente.
    /// <para>
    /// Esto emite <c>AS S(col1, col2, ...)</c> tras el SELECT, lo que es útil si el SELECT no tiene alias claros
    /// o deseas normalizar nombres. Los parámetros del SELECT se fusionan automáticamente en <see cref="QueryResult.Parameters"/>.
    /// </para>
    /// </summary>
    /// <param name="select">Consulta SELECT fuente (puede contener parámetros).</param>
    /// <param name="sourceColumns">Nombres de columnas expuestas por la fuente S.</param>
    /// <param name="alias">Alias de la fuente (por defecto <c>S</c>).</param>
    public MergeQueryBuilder UsingSelect(SelectQueryBuilder select, IEnumerable<string> sourceColumns, string alias = "S")
    {
        _sourceKind = SourceKind.Select;
        _sourceAlias = string.IsNullOrWhiteSpace(alias) ? "S" : alias.Trim();
        _sourceSelect = select ?? throw new ArgumentNullException(nameof(select));

        _sourceColumns.Clear();
        if (sourceColumns != null)
            _sourceColumns.AddRange(sourceColumns.Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s.Trim()));

        _sourceValuesRows.Clear(); // no aplica
        return this;
    }

    /// <summary>Define la condición <c>ON</c> del MERGE (SQL crudo).</summary>
    public MergeQueryBuilder On(string onConditionSql)
    {
        if (string.IsNullOrWhiteSpace(onConditionSql))
            throw new ArgumentNullException(nameof(onConditionSql));
        _onConditionSql = onConditionSql.Trim();
        return this;
    }

    /// <summary>Agrega una asignación para <c>WHEN MATCHED THEN UPDATE</c> desde una expresión de la fuente (ej.: <c>S.COL</c>).</summary>
    public MergeQueryBuilder SetFromSource(string targetColumn, string sourceExpression)
    {
        AddMatchedSet(targetColumn, sourceExpression, SetValueKind.SourceExpr, null);
        return this;
    }

    /// <summary>Agrega una asignación RAW (no parametrizada) para <c>WHEN MATCHED THEN UPDATE</c>.</summary>
    public MergeQueryBuilder SetRaw(string targetColumn, string sqlExpression)
    {
        AddMatchedSet(targetColumn, sqlExpression, SetValueKind.Raw, null);
        return this;
    }

    /// <summary>Agrega una asignación parametrizada (<c>?</c>) para <c>WHEN MATCHED THEN UPDATE</c>.</summary>
    public MergeQueryBuilder SetParam(string targetColumn, object? value)
    {
        AddMatchedSet(targetColumn, "?", SetValueKind.Param, value);
        return this;
    }

    /// <summary>Condición adicional para <c>WHEN MATCHED</c> (se anexa como <c>WHEN MATCHED AND (...)</c>).</summary>
    public MergeQueryBuilder WhenMatchedAnd(string andConditionSql)
    {
        _matchedAndCondition = string.IsNullOrWhiteSpace(andConditionSql) ? null : andConditionSql.Trim();
        return this;
    }

    /// <summary>Mapea una columna de INSERT (NO-MATCH) desde la fuente (ej.: <c>S.COL</c>).</summary>
    public MergeQueryBuilder Map(string targetColumn, string sourceExpression)
    {
        AddInsertMap(targetColumn, sourceExpression, InsertValueKind.SourceExpr, null);
        return this;
    }

    /// <summary>Mapea una columna de INSERT (NO-MATCH) con una expresión RAW (no parametrizada).</summary>
    public MergeQueryBuilder MapRaw(string targetColumn, string rawSql)
    {
        AddInsertMap(targetColumn, rawSql, InsertValueKind.Raw, null);
        return this;
    }

    /// <summary>Mapea una columna de INSERT (NO-MATCH) a un parámetro (<c>?</c>).</summary>
    public MergeQueryBuilder MapParam(string targetColumn, object? value)
    {
        AddInsertMap(targetColumn, "?", InsertValueKind.Param, value);
        return this;
    }

    /// <summary>Condición adicional para <c>WHEN NOT MATCHED</c> (se anexa como <c>WHEN NOT MATCHED AND (...)</c>).</summary>
    public MergeQueryBuilder WhenNotMatchedAnd(string andConditionSql)
    {
        _notMatchedAndCondition = string.IsNullOrWhiteSpace(andConditionSql) ? null : andConditionSql.Trim();
        return this;
    }

    /// <summary>
    /// Agrega asignaciones para <c>WHEN MATCHED THEN UPDATE SET ...</c>.
    /// Cada item debe venir como una asignación SQL válida, por ejemplo:
    /// <c>"T.COL1 = S.COL1"</c>, <c>"T.COUNT = T.COUNT + 1"</c>, etc.
    /// </summary>
    /// <param name="assignments">
    /// Lista de asignaciones ya formateadas. Se validará que cada una contenga un <c>=</c>.
    /// </param>
    /// <returns>El mismo <see cref="MergeQueryBuilder"/> para encadenamiento.</returns>
    /// <exception cref="ArgumentException">Si alguna asignación no contiene <c>=</c>.</exception>
    public MergeQueryBuilder WhenMatchedUpdate(params string[] assignments)
    {
        if (assignments is null || assignments.Length == 0) return this;

        foreach (var raw in assignments)
        {
            var a = raw?.Trim();
            if (string.IsNullOrWhiteSpace(a)) continue;

            if (!a.Contains('='))
                throw new ArgumentException($"La asignación '{a}' no contiene '='.", nameof(assignments));

            _updateAssignments.Add(a);
        }

        return this;
    }

    /// <summary>
    /// Variante tipada: recibe un diccionario <c>columna → expresión</c> y arma
    /// <c>T.columna = expresión</c>. Si la columna ya viene calificada (ej. <c>X.COL</c>)
    /// no se antepone alias.
    /// </summary>
    /// <param name="setMap">Mapa de columna destino a expresión SQL (sin comillas extras).</param>
    /// <param name="targetAlias">Alias del target; por defecto <c>T</c>.</param>
    /// <returns>El mismo <see cref="MergeQueryBuilder"/>.</returns>
    public MergeQueryBuilder WhenMatchedUpdate(Dictionary<string, string> setMap, string targetAlias = "T")
    {
        if (setMap is null || setMap.Count == 0) return this;

        foreach (var kvp in setMap)
        {
            if (string.IsNullOrWhiteSpace(kvp.Key))
                throw new ArgumentException("Nombre de columna vacío en WhenMatchedUpdate(map).", nameof(setMap));

            var col = kvp.Key.Trim();
            var expr = (kvp.Value ?? "NULL").Trim();
            var lhs = col.Contains('.') ? col : $"{targetAlias}.{col}";

            _updateAssignments.Add($"{lhs} = {expr}");
        }

        return this;
    }

    /// <summary>
    /// Define las columnas y los valores para <c>WHEN NOT MATCHED THEN INSERT (... ) VALUES (...)</c>.
    /// La cantidad de columnas y valores debe coincidir.
    /// </summary>
    /// <param name="columns">Lista de columnas de destino.</param>
    /// <param name="values">
    /// Lista de valores/expresiones SQL correspondientes (por ejemplo <c>"S.UID"</c>, <c>"CURRENT_TIMESTAMP"</c>,
    /// <c>"CASE WHEN ... END"</c>, etc.).
    /// </param>
    /// <returns>El mismo <see cref="MergeQueryBuilder"/>.</returns>
    /// <exception cref="ArgumentNullException">Si <paramref name="columns"/> o <paramref name="values"/> son nulos.</exception>
    /// <exception cref="InvalidOperationException">Si las cantidades no coinciden o no hay columnas.</exception>
    public MergeQueryBuilder WhenNotMatchedInsert(IEnumerable<string> columns, IEnumerable<string> values)
    {
        ArgumentNullException.ThrowIfNull(columns);
        ArgumentNullException.ThrowIfNull(values);

        _insertColumns.Clear();
        _insertValues.Clear();

        _insertColumns.AddRange(columns.Select(c => c?.Trim()).Where(c => !string.IsNullOrWhiteSpace(c))!);
        _insertValues.AddRange(values.Select(v => v?.Trim()).Where(v => !string.IsNullOrWhiteSpace(v))!);

        if (_insertColumns.Count == 0)
            throw new InvalidOperationException("Debe especificar al menos una columna para INSERT.");
        if (_insertColumns.Count != _insertValues.Count)
            throw new InvalidOperationException(
                $"La cantidad de columnas ({_insertColumns.Count}) no coincide con los valores ({_insertValues.Count}).");

        return this;
    }

    /// <summary>
    /// Atajo: recibe un diccionario <c>columna → valor/expresión</c> y construye
    /// el par de listas para <c>INSERT (... ) VALUES (...)</c>.
    /// </summary>
    /// <param name="colValues">Mapa de columnas a valores/expresiones SQL.</param>
    /// <returns>El mismo <see cref="MergeQueryBuilder"/>.</returns>
    /// <exception cref="ArgumentException">Si el diccionario viene vacío.</exception>
    public MergeQueryBuilder WhenNotMatchedInsert(Dictionary<string, string> colValues)
    {
        if (colValues is null || colValues.Count == 0)
            throw new ArgumentException("Debe especificar al menos una columna/valor para INSERT.", nameof(colValues));

        return WhenNotMatchedInsert(colValues.Keys, colValues.Values);
    }

    /// <summary>
    /// Atajo con tuplas: define <c>INSERT</c> pasando pares <c>(Columna, Valor)</c>.
    /// </summary>
    /// <param name="pairs">Pares de columna y su valor/expresión SQL.</param>
    /// <returns>El mismo <see cref="MergeQueryBuilder"/>.</returns>
    public MergeQueryBuilder WhenNotMatchedInsert(params (string Column, string Value)[] pairs)
        => pairs is null || pairs.Length == 0
            ? this
            : WhenNotMatchedInsert(pairs.Select(p => p.Column), pairs.Select(p => p.Value));


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
                    SetValueKind.Raw => s.Expression!,  // ej: CASE ...
                    SetValueKind.Param => "?",            // parametrizado
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
                    InsertValueKind.Raw => m.Expression!, // ej: ''
                    InsertValueKind.Param => "?",           // parametrizado
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

    // --------------------- Tipos internos y helpers ---------------------

    private enum SetValueKind { SourceExpr, Raw, Param }
    private sealed record SetAssignment(string TargetColumn, string? Expression, SetValueKind Kind, object? Value);

    private void AddMatchedSet(string targetColumn, string? expression, SetValueKind kind, object? value)
    {
        if (string.IsNullOrWhiteSpace(targetColumn))
            throw new ArgumentNullException(nameof(targetColumn));
        if (kind != SetValueKind.Param && string.IsNullOrWhiteSpace(expression))
            throw new ArgumentNullException(nameof(expression));

        _matchedSet.Add(new SetAssignment(targetColumn.Trim(), expression?.Trim(), kind, value));
    }

    private enum InsertValueKind { SourceExpr, Raw, Param }
    private sealed record InsertMapping(string TargetColumn, string? Expression, InsertValueKind Kind, object? Value);

    private void AddInsertMap(string targetColumn, string? expression, InsertValueKind kind, object? value)
    {
        if (string.IsNullOrWhiteSpace(targetColumn))
            throw new ArgumentNullException(nameof(targetColumn));
        if (kind != InsertValueKind.Param && string.IsNullOrWhiteSpace(expression))
            throw new ArgumentNullException(nameof(expression));

        _notMatchedInsert.Add(new InsertMapping(targetColumn.Trim(), expression?.Trim(), kind, value));
    }
}

Necesito que revises el porque esta fallando porque cuando estaba así no fallaba:

            // =========================================================================
            // 2) MERGE (UPSERT) → BCAH96DTA.ETD02LOG
            //
            //    Lógica de “intentos”:
            //       - Si exitoso = '1' → intentos = intentos(previos) + 1
            //       - Si exitoso = '0' → intentos = 0
            //
            //    Campos:
            //     - LOGB02UIL (último login)      ← now
            //     - LOGB03TIL (intentos)          ← CASE basado en exitoso
            //     - LOGB04SEA (sesión activa)     ← exitoso
            //     - LOGB05UDI (IP)                ← machine.ClientIPAddress
            //     - LOGB06UTD (Device)            ← machine.Device
            //     - LOGB07UNA (Browser)           ← machine.Browser
            //     - LOGB08CBI (Bloqueo intento)   ← '' (vacío en tu inserción)
            //     - LOGB09UIF (último intento)    ← COALESCE(previo, now)  (si no hay previo, cae en now)
            //     - LOGB10TOK (token/sesión)      ← idSesion
            //
            //    Nota: Usamos VALUES(...) como tabla fuente “S” para MERGE.
            // =========================================================================

            // Escapes simples para literales:
            static string esc(string? s) => (s ?? "").Replace("'", "''");

            var mergeUpsert = $@"
                            MERGE INTO BCAH96DTA.ETD02LOG AS T
                            USING (VALUES(
                                '{esc(userID)}',
                                TIMESTAMP('{now:yyyy-MM-dd-HH.mm.ss}'),
                                '{esc(exitoso)}',
                                '{esc(machine.ClientIPAddress)}',
                                '{esc(machine.Device)}',
                                '{esc(machine.Browser)}',
                                '{esc(idSesion)}'
                            )) AS S(UID, NOWTS, EXI, IP, DEV, BRO, TOK)
                            ON T.LOGB01UID = S.UID

                            WHEN MATCHED THEN UPDATE SET
                                T.LOGB02UIL = S.NOWTS,
                                T.LOGB03TIL = CASE WHEN S.EXI = '1' THEN COALESCE(T.LOGB03TIL, 0) + 1 ELSE 0 END,
                                T.LOGB04SEA = S.EXI,
                                T.LOGB05UDI = S.IP,
                                T.LOGB06UTD = S.DEV,
                                T.LOGB07UNA = S.BRO,
                                T.LOGB09UIF = COALESCE(T.LOGB02UIL, S.NOWTS),
                                T.LOGB10TOK = S.TOK

                            WHEN NOT MATCHED THEN INSERT
                                (LOGB01UID, LOGB02UIL, LOGB03TIL, LOGB04SEA, LOGB05UDI, LOGB06UTD, LOGB07UNA, LOGB08CBI, LOGB09UIF, LOGB10TOK)
                            VALUES
                                (S.UID, S.NOWTS, CASE WHEN S.EXI = '1' THEN 1 ELSE 0 END, S.EXI, S.IP, S.DEV, S.BRO, '', S.NOWTS, S.TOK);
                            ";
