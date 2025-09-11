using QueryBuilder.Enums;
using QueryBuilder.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace QueryBuilder.Builders
{
    /// <summary>
    /// Generador de sentencias <c>MERGE</c> orientado a DB2 for i (AS/400) y usable en otros motores
    /// que acepten la gramática estándar. Soporta:
    /// <list type="bullet">
    ///   <item><description><b>USING (VALUES ...)</b> en modo parametrizado simple o <i>tipado</i> para DB2 i.</description></item>
    ///   <item><description><b>USING (SELECT ...)</b> con <see cref="SelectQueryBuilder"/> (fusión automática de parámetros).</description></item>
    ///   <item><description><b>WHEN MATCHED</b> (UPDATE) y <b>WHEN NOT MATCHED</b> (INSERT) con valores desde la fuente, RAW o parametrizados.</description></item>
    /// </list>
    ///
    /// <para><b>Orden de parámetros</b> en <see cref="QueryResult.Parameters"/>:</para>
    /// <list type="number">
    ///   <item><description>Primero los del <c>USING</c> (SELECT o VALUES), en el orden en que aparecen en el SQL.</description></item>
    ///   <item><description>Luego los de <c>UPDATE SET</c> agregados con <see cref="SetParam(string, object?)"/> (orden de las asignaciones).</description></item>
    ///   <item><description>Por último los de <c>INSERT VALUES</c> agregados con <see cref="MapParam(string, object?)"/> (orden de los mapeos).</description></item>
    /// </list>
    ///
    /// <remarks>
    /// En DB2 for i, los <i>marcadores de parámetro</i> tienen restricciones en ciertos contextos (p. ej. <c>USING (VALUES ...)</c>).
    /// Esta clase evita el error SQL0418 convirtiendo automáticamente <c>VALUES</c> parametrizados a
    /// <c>SELECT ... FROM SYSIBM.SYSDUMMY1</c> (con <c>CAST(? AS ...)</c> o <c>CAST(? AS TIMESTAMP)</c>) cuando
    /// el dialecto es <see cref="SqlDialect.Db2i"/>. De ese modo, los parámetros quedan permitidos por el optimizador.
    /// </remarks>
    /// </summary>
    public sealed class MergeQueryBuilder
    {
        // --------- Configuración base ----------
        private readonly string _targetTable;
        private readonly string? _targetLibrary;
        private readonly SqlDialect _dialect;

        // Comentario (línea superior)
        private string? _comment;

        // Aliases
        private string _targetAlias = "T";
        private string _sourceAlias = "S";

        // --------- Fuente USING ----------
        private enum SourceKind { None, Values, Select }
        private SourceKind _sourceKind = SourceKind.None;

        // Nombres de columnas expuestas por la fuente S
        private readonly List<string> _sourceColumns = [];

        // USING (VALUES ...) modo simple: filas con parámetros "?"
        private readonly List<object?[]> _sourceValuesRows = [];

        // USING (VALUES ...) modo tipado (DB2 i): cada valor lleva su SQL de tipo y su valor
        // ej.: ( "CAST(? AS VARCHAR(20))", "abc" ), ( "CAST(? AS TIMESTAMP)", DateTime.Now )
        private readonly List<List<(string Sql, object? Val)>> _sourceValuesTypedRows = [];

        // USING (SELECT ...)
        private SelectQueryBuilder? _sourceSelect;

        // Condición ON
        private string? _onConditionSql;

        // --------- WHEN MATCHED (UPDATE) ----------
        private enum SetValueKind { SourceExpr, Raw, Param }
        private sealed record SetAssignment(string TargetColumn, SetValueKind Kind, string? Expression, object? Value);
        private readonly List<SetAssignment> _matchedSet = [];
        private readonly List<string> _updateAssignmentsRaw = [];
        private string? _matchedAndCondition;

        // --------- WHEN NOT MATCHED (INSERT) ----------
        private enum InsertValueKind { SourceExpr, Raw, Param }
        private sealed record InsertMapping(string TargetColumn, InsertValueKind Kind, string? Expression, object? Value);
        private readonly List<InsertMapping> _notMatchedInsert = [];
        private string? _notMatchedAndCondition;

        // Acumulador de parámetros en orden posicional
        private readonly List<object?> _parameters = [];

        /// <summary>
        /// Crea un generador de sentencias <c>MERGE</c>.
        /// </summary>
        /// <param name="targetTable">Tabla destino del MERGE.</param>
        /// <param name="targetLibrary">Biblioteca/esquema (DB2 i).</param>
        /// <param name="dialect">Dialecto SQL. Por defecto <see cref="SqlDialect.Db2i"/>.</param>
        public MergeQueryBuilder(string targetTable, string? targetLibrary = null, SqlDialect dialect = SqlDialect.Db2i)
        {
            _targetTable = targetTable ?? throw new ArgumentNullException(nameof(targetTable));
            _targetLibrary = targetLibrary;
            _dialect = dialect;
        }

        // ===== Configuración general =====

        /// <summary>
        /// Agrega un comentario (una línea) al inicio del SQL. Se sanitiza para evitar inyección de comentarios.
        /// </summary>
        public MergeQueryBuilder WithComment(string? comment)
        {
            if (string.IsNullOrWhiteSpace(comment)) return this;

            var sanitized = comment
                .Replace("\r", " ")
                .Replace("\n", " ")
                .Replace("--", "- -")
                .Trim();

            _comment = "-- " + sanitized;
            return this;
        }

        /// <summary>Define el alias de la tabla destino (por defecto <c>T</c>).</summary>
        public MergeQueryBuilder AsTarget(string alias)
        {
            if (string.IsNullOrWhiteSpace(alias)) throw new ArgumentNullException(nameof(alias));
            _targetAlias = alias.Trim();
            return this;
        }

        /// <summary>Define el alias de la fuente USING (por defecto <c>S</c>).</summary>
        public MergeQueryBuilder AsSource(string alias)
        {
            if (string.IsNullOrWhiteSpace(alias)) throw new ArgumentNullException(nameof(alias));
            _sourceAlias = alias.Trim();
            return this;
        }

        // ===== USING (VALUES ...) =====

        /// <summary>
        /// Define <c>USING (VALUES ...)</c> en modo parametrizado simple (placeholders <c>?</c>), indicando
        /// nombres de columnas y filas de valores (todas del mismo ancho que la lista de columnas).
        /// </summary>
        public MergeQueryBuilder UsingValues(IEnumerable<string> sourceColumns, IEnumerable<object?[]> rows, string alias = "S")
        {
            if (sourceColumns is null || !sourceColumns.Any())
                throw new InvalidOperationException("Debe definir al menos una columna para USING (VALUES).");

            _sourceKind = SourceKind.Values;
            _sourceAlias = string.IsNullOrWhiteSpace(alias) ? "S" : alias.Trim();

            _sourceColumns.Clear();
            _sourceColumns.AddRange(sourceColumns.Select(s => s.Trim()));

            _sourceValuesRows.Clear();
            _sourceValuesTypedRows.Clear();

            foreach (var r in rows ?? [])
            {
                if (r is null || r.Length != _sourceColumns.Count)
                    throw new InvalidOperationException($"Cada fila VALUES debe tener exactamente {_sourceColumns.Count} valores.");
                _sourceValuesRows.Add(r);
            }

            if (_sourceValuesRows.Count == 0)
                throw new InvalidOperationException("Debe especificar al menos una fila para USING (VALUES).");

            _sourceSelect = null;
            return this;
        }

        /// <summary>
        /// Define <c>USING (VALUES ...)</c> en modo <b>tipado</b> (recomendado para DB2 i) pasando
        /// pares (NombreDeColumna, ValorTipado) donde <c>ValorTipado.Sql</c> es el fragmento
        /// de tipo (p. ej. <c>CAST(? AS VARCHAR(20))</c>, <c>CAST(? AS TIMESTAMP)</c>) y
        /// <c>ValorTipado.Value</c> es el valor a enlazar.
        /// </summary>
        public MergeQueryBuilder UsingValuesTyped(params (string Column, Db2ITyped Value)[] values)
        {
            if (values is null || values.Length == 0)
                throw new ArgumentException("Debe especificar al menos un valor para USING (VALUES ...).", nameof(values));

            _sourceKind = SourceKind.Values;

            _sourceColumns.Clear();
            _sourceValuesRows.Clear();
            _sourceValuesTypedRows.Clear();

            var row = new List<(string Sql, object? Val)>(values.Length);
            foreach (var (col, typed) in values)
            {
                if (string.IsNullOrWhiteSpace(col))
                    throw new ArgumentException("El nombre de columna en USING no puede ser vacío.", nameof(values));

                _sourceColumns.Add(col.Trim());
                row.Add((typed.Sql, typed.Value));
            }

            _sourceValuesTypedRows.Add(row);
            _sourceSelect = null;
            return this;
        }

        /// <summary>
        /// Agrega otra fila tipada a <c>USING (VALUES ...)</c> (mismo orden de columnas que la primera).
        /// </summary>
        public MergeQueryBuilder UsingValuesTypedRow(params (string Column, Db2ITyped Value)[] values)
        {
            if (_sourceKind != SourceKind.Values || _sourceColumns.Count == 0 || _sourceValuesTypedRows.Count == 0)
                throw new InvalidOperationException("Debe llamar primero a UsingValuesTyped(...) para definir columnas y primera fila.");

            if (values is null || values.Length != _sourceColumns.Count)
                throw new ArgumentException($"Se esperaban {_sourceColumns.Count} valores para la fila adicional de USING (VALUES ...).");

            var row = new List<(string Sql, object? Val)>(values.Length);
            for (int i = 0; i < values.Length; i++)
            {
                var (col, typed) = values[i];
                if (!col.Equals(_sourceColumns[i], StringComparison.OrdinalIgnoreCase))
                    throw new ArgumentException($"La columna #{i + 1} ('{col}') no coincide con '{_sourceColumns[i]}' definida en la primera fila.");

                row.Add((typed.Sql, typed.Value));
            }

            _sourceValuesTypedRows.Add(row);
            return this;
        }

        /// <summary>
        /// Agrega una nueva fila (no tipada) a <c>USING (VALUES ...)</c> en modo simple.
        /// </summary>
        public MergeQueryBuilder UsingValuesRow(params object?[] row)
        {
            if (_sourceKind != SourceKind.Values || _sourceColumns.Count == 0 || _sourceValuesRows.Count == 0)
                throw new InvalidOperationException("Debe definir primero UsingValues(columns, rows) para usar UsingValuesRow(...).");

            if (row is null || row.Length != _sourceColumns.Count)
                throw new ArgumentException($"La fila debe contener exactamente {_sourceColumns.Count} valores.");

            _sourceValuesRows.Add(row);
            return this;
        }

        // ===== USING (SELECT ...) =====

        /// <summary>
        /// Define <c>USING (SELECT ...)</c> con alias. Si necesitas exponer nombres de columnas,
        /// usa la otra sobrecarga con <paramref name="sourceColumns"/>.
        /// </summary>
        public MergeQueryBuilder UsingSelect(SelectQueryBuilder select, string alias = "S")
        {
            _sourceKind = SourceKind.Select;
            _sourceSelect = select ?? throw new ArgumentNullException(nameof(select));
            _sourceAlias = string.IsNullOrWhiteSpace(alias) ? "S" : alias.Trim();

            _sourceColumns.Clear();
            _sourceValuesRows.Clear();
            _sourceValuesTypedRows.Clear();
            return this;
        }

        /// <summary>
        /// Define <c>USING (SELECT ...)</c> indicando además los nombres de columnas a exponer.
        /// </summary>
        public MergeQueryBuilder UsingSelect(SelectQueryBuilder select, IEnumerable<string> sourceColumns, string alias = "S")
        {
            _sourceKind = SourceKind.Select;
            _sourceSelect = select ?? throw new ArgumentNullException(nameof(select));
            _sourceAlias = string.IsNullOrWhiteSpace(alias) ? "S" : alias.Trim();

            _sourceColumns.Clear();
            if (sourceColumns != null)
                _sourceColumns.AddRange(sourceColumns.Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s.Trim()));

            _sourceValuesRows.Clear();
            _sourceValuesTypedRows.Clear();
            return this;
        }

        // ===== ON =====

        /// <summary>Define la condición <c>ON</c> del MERGE como SQL crudo.</summary>
        public MergeQueryBuilder On(string onConditionSql)
        {
            if (string.IsNullOrWhiteSpace(onConditionSql))
                throw new ArgumentNullException(nameof(onConditionSql));
            _onConditionSql = onConditionSql.Trim();
            return this;
        }

        // ===== WHEN MATCHED (UPDATE) =====

        /// <summary>Asigna <c>T.col = S.expr</c> en <c>UPDATE SET</c>.</summary>
        public MergeQueryBuilder SetFromSource(string targetColumn, string sourceExpression)
        {
            AddMatchedSet(targetColumn, sourceExpression, SetValueKind.SourceExpr, null);
            return this;
        }

        /// <summary>Asigna <c>T.col = &lt;sql_raw&gt;</c> en <c>UPDATE SET</c> (sin parámetros).</summary>
        public MergeQueryBuilder SetRaw(string targetColumn, string sqlExpression)
        {
            AddMatchedSet(targetColumn, sqlExpression, SetValueKind.Raw, null);
            return this;
        }

        /// <summary>Asigna <c>T.col = ?</c> en <c>UPDATE SET</c> con el valor provisto.</summary>
        public MergeQueryBuilder SetParam(string targetColumn, object? value)
        {
            AddMatchedSet(targetColumn, "?", SetValueKind.Param, value);
            return this;
        }

        /// <summary>Condición adicional para <c>WHEN MATCHED</c> (se emite como <c>WHEN MATCHED AND (...)</c>).</summary>
        public MergeQueryBuilder WhenMatchedAnd(string andConditionSql)
        {
            _matchedAndCondition = string.IsNullOrWhiteSpace(andConditionSql) ? null : andConditionSql.Trim();
            return this;
        }

        /// <summary>
        /// Variante “rápida” para <c>UPDATE SET</c>: pasa asignaciones ya formateadas
        /// (p. ej. <c>"T.COL = S.COL"</c>, <c>"T.COUNT = T.COUNT + 1"</c>).
        /// </summary>
        public MergeQueryBuilder WhenMatchedUpdate(params string[] assignments)
        {
            if (assignments is null || assignments.Length == 0) return this;

            foreach (var raw in assignments)
            {
                var a = raw?.Trim();
                if (string.IsNullOrWhiteSpace(a)) continue;
                if (!a.Contains('='))
                    throw new ArgumentException($"La asignación '{a}' no contiene '='.", nameof(assignments));

                _updateAssignmentsRaw.Add(a);
            }
            return this;
        }

        /// <summary>Variante con diccionario <c>columna → expresión</c> para <c>UPDATE SET</c>.</summary>
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
                _updateAssignmentsRaw.Add($"{lhs} = {expr}");
            }
            return this;
        }

        // ===== WHEN NOT MATCHED (INSERT) =====

        /// <summary>Mapea <c>INSERT</c> (NO-MATCH) desde la fuente: <c>T.col ← S.expr</c>.</summary>
        public MergeQueryBuilder Map(string targetColumn, string sourceExpression)
        {
            AddInsertMap(targetColumn, sourceExpression, InsertValueKind.SourceExpr, null);
            return this;
        }

        /// <summary>Mapea <c>INSERT</c> (NO-MATCH) a una expresión RAW sin parámetros.</summary>
        public MergeQueryBuilder MapRaw(string targetColumn, string rawSql)
        {
            AddInsertMap(targetColumn, rawSql, InsertValueKind.Raw, null);
            return this;
        }

        /// <summary>Mapea <c>INSERT</c> (NO-MATCH) a un parámetro (<c>?</c>).</summary>
        public MergeQueryBuilder MapParam(string targetColumn, object? value)
        {
            AddInsertMap(targetColumn, "?", InsertValueKind.Param, value);
            return this;
        }

        /// <summary>Condición adicional para <c>WHEN NOT MATCHED</c> (se emite como <c>WHEN NOT MATCHED AND (...)</c>).</summary>
        public MergeQueryBuilder WhenNotMatchedAnd(string andConditionSql)
        {
            _notMatchedAndCondition = string.IsNullOrWhiteSpace(andConditionSql) ? null : andConditionSql.Trim();
            return this;
        }

        /// <summary>
        /// Variante “rápida” para <c>INSERT</c> (NO-MATCH): pares (columna destino, expresión SQL).
        /// </summary>
        public MergeQueryBuilder WhenNotMatchedInsert(params (string TargetColumn, string Expression)[] mappings)
        {
            if (mappings is null || mappings.Length == 0) return this;
            foreach (var (col, expr) in mappings)
                AddInsertMap(col, expr, InsertValueKind.Raw, null);
            return this;
        }

        // ===== Build =====

        /// <summary>
        /// Construye el SQL <c>MERGE</c> final y su lista de parámetros en el orden posicional correcto.
        /// Para DB2 i:
        /// <list type="bullet">
        ///   <item><description>
        ///     Si la fuente es <c>USING (VALUES ...)</c> con parámetros, se convierte automáticamente a
        ///     <c>USING (SELECT ... FROM SYSIBM.SYSDUMMY1 [UNION ALL SELECT ...])</c> para evitar SQL0418.
        ///   </description></item>
        /// </list>
        /// </summary>
        public QueryResult Build()
        {
            _parameters.Clear();

            // Validaciones mínimas
            if (string.IsNullOrWhiteSpace(_targetTable))
                throw new InvalidOperationException("Debe especificarse la tabla destino.");
            if (_sourceKind == SourceKind.None)
                throw new InvalidOperationException("Debe especificarse la fuente USING (VALUES o SELECT).");
            if (string.IsNullOrWhiteSpace(_onConditionSql))
                throw new InvalidOperationException("Debe especificarse la condición ON.");
            if (_sourceKind == SourceKind.Values && _sourceColumns.Count == 0)
                throw new InvalidOperationException("Para USING (VALUES) debe definir nombres de columnas de la fuente.");

            var sb = new StringBuilder();

            if (!string.IsNullOrWhiteSpace(_comment))
                sb.AppendLine(_comment);

            // MERGE INTO <lib.tabla> AS T
            sb.Append("MERGE INTO ");
            if (!string.IsNullOrWhiteSpace(_targetLibrary))
                sb.Append(_targetLibrary).Append('.');
            sb.Append(_targetTable).Append(' ').Append("AS ").Append(_targetAlias).Append('\n');

            // USING (...)
            sb.Append("USING (");

            if (_sourceKind == SourceKind.Values)
            {
                if (_dialect == SqlDialect.Db2i)
                {
                    // *** MODO DB2 i: Convertimos VALUES parametrizados a SELECT ... FROM SYSIBM.SYSDUMMY1 para permitir "?" ***
                    // - Si hay filas TIPADAS, usamos los fragmentos tal cual (ya llevan CAST/TIMESTAMP).
                    // - Si son filas simples, inferimos tipo por columna y emitimos CAST(? AS ...).

                    if (_sourceValuesTypedRows.Count > 0)
                    {
                        // SELECT fila1 UNION ALL SELECT fila2 ...
                        for (int r = 0; r < _sourceValuesTypedRows.Count; r++)
                        {
                            var row = _sourceValuesTypedRows[r];
                            if (r > 0) sb.Append("\nUNION ALL ");

                            sb.Append("SELECT ");

                            var cols = new List<string>(row.Count);
                            for (int c = 0; c < row.Count; c++)
                            {
                                var (sqlExpr, val) = row[c];
                                cols.Add($"{sqlExpr} AS {_sourceColumns[c]}");
                                _parameters.Add(val); // 1) parámetros de USING
                            }

                            sb.Append(string.Join(", ", cols)).Append(" FROM SYSIBM.SYSDUMMY1");
                        }
                    }
                    else
                    {
                        // Inferencia de tipos por columna a partir de la primera fila no nula
                        var colTypes = InferDb2iColumnTypes(_sourceValuesRows, _sourceColumns.Count);

                        // SELECT CAST(? AS <tipo>) AS COL1, ... FROM SYSIBM.SYSDUMMY1   UNION ALL ...
                        for (int r = 0; r < _sourceValuesRows.Count; r++)
                        {
                            var row = _sourceValuesRows[r];
                            if (r > 0) sb.Append("\nUNION ALL ");

                            sb.Append("SELECT ");

                            var cols = new List<string>(row.Length);
                            for (int c = 0; c < row.Length; c++)
                            {
                                string expr = RenderDb2iParamExpr(colTypes[c]); // CAST(? AS ...)
                                cols.Add($"{expr} AS {_sourceColumns[c]}");
                                _parameters.Add(row[c]); // 1) parámetros de USING
                            }

                            sb.Append(string.Join(", ", cols)).Append(" FROM SYSIBM.SYSDUMMY1");
                        }
                    }
                }
                else
                {
                    // *** Otros dialectos: podemos usar VALUES(?,?) directamente ***
                    sb.Append("VALUES").Append('\n');

                    if (_sourceValuesTypedRows.Count > 0)
                    {
                        var rowSqls = new List<string>(_sourceValuesTypedRows.Count);
                        foreach (var row in _sourceValuesTypedRows)
                        {
                            rowSqls.Add("(" + string.Join(", ", row.Select(e => e.Sql)) + ")");
                            foreach (var e in row) _parameters.Add(e.Val); // 1) parámetros
                        }
                        sb.Append(string.Join(",\n", rowSqls));
                    }
                    else
                    {
                        var rowSqls = new List<string>(_sourceValuesRows.Count);
                        foreach (var row in _sourceValuesRows)
                        {
                            rowSqls.Add("(" + string.Join(", ", Enumerable.Repeat("?", row.Length)) + ")");
                            _parameters.AddRange(row); // 1) parámetros
                        }
                        sb.Append(string.Join(",\n", rowSqls));
                    }
                }
            }
            else
            {
                // USING (SELECT ...)
                var sel = _sourceSelect!.Build();
                if (sel.Parameters is { Count: > 0 })
                    _parameters.AddRange(sel.Parameters); // 1) parámetros del SELECT
                sb.Append(sel.Sql);
            }

            sb.Append(')');
            sb.Append(' ').Append("AS ").Append(_sourceAlias);
            if (_sourceColumns.Count > 0)
                sb.Append('(').Append(string.Join(", ", _sourceColumns)).Append(')');
            sb.Append('\n');

            // ON ...
            sb.Append("ON ").Append(_onConditionSql).Append('\n');

            // WHEN MATCHED THEN UPDATE SET ...
            var anyUpdate = _matchedSet.Count > 0 || _updateAssignmentsRaw.Count > 0;
            if (anyUpdate)
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
                        SetValueKind.SourceExpr => s.Expression!, // S.COL
                        SetValueKind.Raw       => s.Expression!, // CASE ... / funciones
                        SetValueKind.Param     => "?",           // parametrizado
                        _ => throw new InvalidOperationException("Tipo de asignación inválido.")
                    };

                    if (s.Kind == SetValueKind.Param)
                        _parameters.Add(s.Value); // 2) parámetros de UPDATE

                    assigns.Add($"{_targetAlias}.{s.TargetColumn} = {rhs}");
                }

                assigns.AddRange(_updateAssignmentsRaw); // RAW completas si las hay
                sb.Append(string.Join(",\n", assigns)).Append('\n');
            }

            // WHEN NOT MATCHED THEN INSERT ...
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
                        InsertValueKind.SourceExpr => m.Expression!, // S.UID
                        InsertValueKind.Raw       => m.Expression!,  // '' o CASE ...
                        InsertValueKind.Param     => "?",            // parametrizado
                        _ => throw new InvalidOperationException("Tipo de mapeo inválido.")
                    };

                    if (m.Kind == InsertValueKind.Param)
                        _parameters.Add(m.Value); // 3) parámetros de INSERT

                    vals.Add(rhs);
                }

                sb.Append('(').Append(string.Join(", ", vals)).Append('\n');
            }

            return new QueryResult
            {
                Sql = sb.ToString(),
                Parameters = _parameters
            };
        }

        // ===== Helpers privados =====

        private void AddMatchedSet(string targetColumn, string? expression, SetValueKind kind, object? value)
        {
            if (string.IsNullOrWhiteSpace(targetColumn))
                throw new ArgumentNullException(nameof(targetColumn));
            if (kind != SetValueKind.Param && string.IsNullOrWhiteSpace(expression))
                throw new ArgumentNullException(nameof(expression), "La expresión no puede ser vacía para SourceExpr/Raw.");

            _matchedSet.Add(new SetAssignment(targetColumn.Trim(), kind, expression?.Trim(), value));
        }

        private void AddInsertMap(string targetColumn, string? expression, InsertValueKind kind, object? value)
        {
            if (string.IsNullOrWhiteSpace(targetColumn))
                throw new ArgumentNullException(nameof(targetColumn));
            if (kind != InsertValueKind.Param && string.IsNullOrWhiteSpace(expression))
                throw new ArgumentNullException(nameof(expression), "La expresión no puede ser vacía para SourceExpr/Raw.");

            _notMatchedInsert.Add(new InsertMapping(targetColumn.Trim(), kind, expression?.Trim(), value));
        }

        /// <summary>
        /// Infiera un tipo DB2 i por columna a partir de las filas simples (<c>object?[]</c>).
        /// Si no hay valor no nulo en una columna, asume VARCHAR(512).
        /// </summary>
        private static string[] InferDb2iColumnTypes(List<object?[]> rows, int colCount)
        {
            var result = new string[colCount];

            for (int c = 0; c < colCount; c++)
            {
                Type? t = null;

                // Encuentra el primer valor no nulo en esta columna
                for (int r = 0; r < rows.Count && t == null; r++)
                {
                    var v = rows[r][c];
                    if (v is not null) t = v.GetType();
                }

                result[c] = Db2iTypeNameFor(t);
            }

            return result;
        }

        /// <summary>
        /// Devuelve el nombre de tipo DB2 i recomendado para un <see cref="Type"/> .NET.
        /// </summary>
        private static string Db2iTypeNameFor(Type? t)
        {
            if (t == null) return "VARCHAR(512)";

            if (t == typeof(string) || t == typeof(char)) return "VARCHAR(512)";
            if (t == typeof(DateTime)) return "TIMESTAMP";
            if (t == typeof(decimal)) return "DECIMAL(31,8)";
            if (t == typeof(double) || t == typeof(float)) return "DOUBLE";
            if (t == typeof(long)) return "BIGINT";
            if (t == typeof(int) || t == typeof(short) || t == typeof(byte)) return "INTEGER";
            if (t == typeof(bool)) return "SMALLINT"; // 0 / 1
            if (t == typeof(Guid)) return "CHAR(36)";

            // Fallback genérico
            return "VARCHAR(512)";
        }

        /// <summary>
        /// Renderiza la expresión de parámetro para DB2 i (en SELECT) con el tipo indicado:
        /// <c>CAST(? AS {db2Type})</c>. Para TIMESTAMP usa <c>CAST(? AS TIMESTAMP)</c>.
        /// </summary>
        private static string RenderDb2iParamExpr(string db2Type) => $"CAST(? AS {db2Type})";
    }

    // ==============================================================
    // Tipos auxiliares para valores tipados en DB2 i
    // ==============================================================

    /// <summary>
    /// Representa un valor “tipado” para DB2 i que va a usarse en SELECT (USING) con marcador <c>?</c>
    /// y una anotación de tipo, por ejemplo <c>CAST(? AS VARCHAR(20))</c>, <c>CAST(? AS TIMESTAMP)</c>.
    /// </summary>
    public readonly struct Db2ITyped
    {
        /// <summary>Fragmento SQL del valor tipado (por ejemplo, <c>CAST(? AS VARCHAR(20))</c>).</summary>
        public string Sql { get; }

        /// <summary>Valor a enlazar al marcador <c>?</c>.</summary>
        public object? Value { get; }

        private Db2ITyped(string sql, object? value)
        {
            Sql = sql;
            Value = value;
        }

        /// <summary>Crea un tipado genérico con <c>CAST(? AS VARCHAR(n))</c>.</summary>
        public static Db2ITyped VarChar(object? value, int size) => new($"CAST(? AS VARCHAR({size}))", value);

        /// <summary>Crea un tipado genérico con <c>CAST(? AS CHAR(n))</c>.</summary>
        public static Db2ITyped Char(object? value, int size) => new($"CAST(? AS CHAR({size}))", value);

        /// <summary>Crea un tipado numérico con <c>CAST(? AS DECIMAL(p,s))</c>.</summary>
        public static Db2ITyped Decimal(object? value, int precision, int scale) => new($"CAST(? AS DECIMAL({precision},{scale}))", value);

        /// <summary>Crea un tipado para timestamp con <c>CAST(? AS TIMESTAMP)</c>.</summary>
        public static Db2ITyped Timestamp(object? value) => new("CAST(? AS TIMESTAMP)", value);

        /// <summary>Crea un tipado entero con <c>CAST(? AS INTEGER)</c>.</summary>
        public static Db2ITyped Integer(object? value) => new("CAST(? AS INTEGER)", value);

        /// <summary>Crea un tipado entero grande con <c>CAST(? AS BIGINT)</c>.</summary>
        public static Db2ITyped BigInt(object? value) => new("CAST(? AS BIGINT)", value);

        /// <summary>Crea un tipado para <c>DOUBLE</c> con <c>CAST(? AS DOUBLE)</c>.</summary>
        public static Db2ITyped Double(object? value) => new("CAST(? AS DOUBLE)", value);
    }
}
