using QueryBuilder.Enums;
using QueryBuilder.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace QueryBuilder.Builders
{
    /// <summary>
    /// Generador de sentencias <c>MERGE</c> pensado para DB2 for i (AS/400) y usable también
    /// con otros motores que acepten la gramática estándar de <c>MERGE</c>.
    ///
    /// <para>Características principales:</para>
    /// <list type="bullet">
    ///   <item>
    ///     <description>
    ///       <b>USING (VALUES ...)</b> con columnas y alias de la fuente: puede ser en modo
    ///       parametrizado simple (placeholders <c>?</c>) o en modo <i>tipado</i> para DB2 i
    ///       mediante <c>Db2ITyped</c> (p. ej. <c>CAST(? AS VARCHAR(20))</c>, <c>TIMESTAMP(?)</c>).
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <description>
    ///       <b>USING (SELECT ...)</b> con <see cref="SelectQueryBuilder"/>; los parámetros del
    ///       <c>SELECT</c> se fusionan automáticamente en <see cref="QueryResult.Parameters"/>
    ///       <b>antes</b> de los de <c>UPDATE</c>/<c>INSERT</c>, respetando el orden posicional
    ///       requerido por DB2 i (OleDb).
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <description>
    ///       Bloques <b>WHEN MATCHED</b> (UPDATE) y <b>WHEN NOT MATCHED</b> (INSERT), con
    ///       tres modalidades de valores:
    ///       <i>desde la fuente</i> (<c>S.COL</c>), <i>expresión RAW</i> (SQL libre) o
    ///       <i>parametrizado</i> (<c>?</c> + valor).
    ///     </description>
    ///   </item>
    /// </list>
    ///
    /// <para>
    /// Orden de los parámetros en <see cref="QueryResult.Parameters"/>:
    /// 1) Primero los del <c>USING</c> (ya sea <c>VALUES</c> o <c>SELECT</c>),
    /// 2) luego los de <c>UPDATE SET</c> agregados con <see cref="SetParam(string, object?)"/>,
    /// 3) finalmente los de <c>INSERT VALUES</c> agregados con <see cref="MapParam(string, object?)"/>.
    /// </para>
    ///
    /// <para>La clase NO agrega punto y coma al final; si el caller lo requiere, puede hacerlo.</para>
    /// </summary>
    public sealed class MergeQueryBuilder
    {
        // ---------------------------------------------------------------------
        // Campos de configuración
        // ---------------------------------------------------------------------

        private readonly string _targetTable;
        private readonly string? _targetLibrary;
        private readonly SqlDialect _dialect;

        // Alias por defecto
        private string _targetAlias = "T";
        private string _sourceAlias = "S";

        // Comentario opcional (línea superior)
        private string? _comment;

        // Fuente USING: KIND y estructuras asociadas
        private enum SourceKind { None, Values, Select }
        private SourceKind _sourceKind = SourceKind.None;

        // USING (VALUES) — columnas visibles de la fuente S
        private readonly List<string> _sourceColumns = [];

        // Modo VALUES parametrizado simple: cada fila es un object?[] y se emiten "?, ?, ?"
        private readonly List<object?[]> _sourceValuesRows = [];

        // Modo VALUES tipado (DB2 i): cada fila es una lista de (SQL de tipo, valor)
        //   ej.: ( "CAST(? AS VARCHAR(20))", "abc" )  /  ( "TIMESTAMP(?)", DateTime.Now )
        private readonly List<List<(string Sql, object? Val)>> _sourceValuesTypedRows = [];

        // Modo USING (SELECT)
        private SelectQueryBuilder? _sourceSelect;

        // Condición ON
        private string? _onConditionSql;

        // ---------------------------------------------------------------------
        // Estructuras para UPDATE / INSERT
        // ---------------------------------------------------------------------

        private enum SetValueKind { SourceExpr, Raw, Param }
        private sealed record SetAssignment(string TargetColumn, SetValueKind Kind, string? Expression, object? Value);

        private readonly List<SetAssignment> _matchedSet = [];
        private string? _matchedAndCondition;

        private enum InsertValueKind { SourceExpr, Raw, Param }
        private sealed record InsertMapping(string TargetColumn, InsertValueKind Kind, string? Expression, object? Value);

        private readonly List<InsertMapping> _notMatchedInsert = [];
        private string? _notMatchedAndCondition;

        // Asignaciones RAW completas (modo “rápido” para UPDATE): "T.COL = expr"
        private readonly List<string> _updateAssignmentsRaw = [];

        // Acumulador de parámetros en el orden posicional necesario para DB2 i
        private readonly List<object?> _parameters = [];

        // ---------------------------------------------------------------------
        // ctor
        // ---------------------------------------------------------------------

        /// <summary>
        /// Crea un nuevo generador de <c>MERGE</c>.
        /// </summary>
        /// <param name="targetTable">Nombre de la tabla destino.</param>
        /// <param name="targetLibrary">Biblioteca/esquema (DB2 i).</param>
        /// <param name="dialect">Dialecto SQL (por defecto <see cref="SqlDialect.Db2i"/>).</param>
        /// <exception cref="ArgumentNullException">Si no se especifica <paramref name="targetTable"/>.</exception>
        public MergeQueryBuilder(string targetTable, string? targetLibrary = null, SqlDialect dialect = SqlDialect.Db2i)
        {
            _targetTable = targetTable ?? throw new ArgumentNullException(nameof(targetTable));
            _targetLibrary = targetLibrary;
            _dialect = dialect;
        }

        // ---------------------------------------------------------------------
        // Configuración general
        // ---------------------------------------------------------------------

        /// <summary>
        /// Agrega un comentario (una línea) al inicio del SQL generado. Se “saneá” para evitar
        /// inyección de comentarios (se eliminan saltos de línea y se separan dobles guiones).
        /// </summary>
        public MergeQueryBuilder WithComment(string? comment)
        {
            if (string.IsNullOrWhiteSpace(comment))
                return this;

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
            if (string.IsNullOrWhiteSpace(alias))
                throw new ArgumentNullException(nameof(alias));
            _targetAlias = alias.Trim();
            return this;
        }

        /// <summary>Define el alias de la fuente USING (por defecto <c>S</c>).</summary>
        public MergeQueryBuilder AsSource(string alias)
        {
            if (string.IsNullOrWhiteSpace(alias))
                throw new ArgumentNullException(nameof(alias));
            _sourceAlias = alias.Trim();
            return this;
        }

        // ---------------------------------------------------------------------
        // USING (VALUES ...) — modos simple y tipado
        // ---------------------------------------------------------------------

        /// <summary>
        /// Define la fuente <c>USING (VALUES ...)</c> en modo <b>parametrizado simple</b>
        /// (placeholders <c>?</c>), indicando los nombres de columnas de la fuente
        /// y una o más filas de valores.
        /// </summary>
        /// <param name="sourceColumns">Nombres de columnas expuestas por la fuente <c>S</c>.</param>
        /// <param name="rows">
        /// Filas de valores. Cada fila debe tener el mismo número de elementos que
        /// <paramref name="sourceColumns"/>.
        /// </param>
        /// <param name="alias">Alias de la fuente (por defecto <c>S</c>).</param>
        /// <exception cref="InvalidOperationException">Si no hay columnas o si alguna fila no coincide en longitud.</exception>
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
        /// Define la fuente <c>USING (VALUES ...)</c> en modo <b>tipado</b> para DB2 i:
        /// cada valor se expresa como un fragmento SQL de tipo (p. ej. <c>CAST(? AS VARCHAR(20))</c>,
        /// <c>TIMESTAMP(?)</c>) más su valor.
        /// </summary>
        /// <param name="values">
        /// Par(es) (NombreDeColumna, ValorTipado). El orden define también el orden de parámetros.
        /// </param>
        /// <exception cref="ArgumentException">Si no se envía ningún valor o hay nombres de columna vacíos.</exception>
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
        /// Agrega otra fila tipada a <c>USING (VALUES ...)</c>. Debe llamarse
        /// después de <see cref="UsingValuesTyped((string, Db2ITyped)[])"/>.
        /// </summary>
        /// <param name="values">Valores tipados en el MISMO orden de columnas ya definido.</param>
        /// <exception cref="InvalidOperationException">Si no se definió previamente la primera fila tipada.</exception>
        /// <exception cref="ArgumentException">Si la cantidad o el orden de columnas no coincide.</exception>
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
        /// Agrega otra fila (no tipada) a <c>USING (VALUES ...)</c> en modo parametrizado simple.
        /// Debe usarse solo si la fuente se definió con <see cref="UsingValues(IEnumerable{string}, IEnumerable{object?[]}, string)"/>.
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

        // ---------------------------------------------------------------------
        // USING (SELECT ...)
        // ---------------------------------------------------------------------

        /// <summary>
        /// Define la fuente <c>USING (SELECT ...)</c> con alias. Las columnas de la fuente
        /// no se listan explícitamente en el SQL (puedes usar la otra sobrecarga si las necesitas).
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
        /// Define la fuente <c>USING (SELECT ...)</c> indicando además los nombres de columnas
        /// expuestas por la fuente (se emitirán como <c>AS S(col1, col2, ...)</c>).
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

        // ---------------------------------------------------------------------
        // ON
        // ---------------------------------------------------------------------

        /// <summary>Define la condición <c>ON</c> del <c>MERGE</c> como SQL crudo.</summary>
        public MergeQueryBuilder On(string onConditionSql)
        {
            if (string.IsNullOrWhiteSpace(onConditionSql))
                throw new ArgumentNullException(nameof(onConditionSql));
            _onConditionSql = onConditionSql.Trim();
            return this;
        }

        // ---------------------------------------------------------------------
        // WHEN MATCHED (UPDATE)
        // ---------------------------------------------------------------------

        /// <summary>
        /// Agrega una asignación a <c>UPDATE SET</c> tomando el valor desde la fuente (ej.: <c>S.COL</c>).
        /// </summary>
        public MergeQueryBuilder SetFromSource(string targetColumn, string sourceExpression)
        {
            AddMatchedSet(targetColumn, sourceExpression, SetValueKind.SourceExpr, null);
            return this;
        }

        /// <summary>Agrega una asignación RAW (SQL libre, sin parámetros) a <c>UPDATE SET</c>.</summary>
        public MergeQueryBuilder SetRaw(string targetColumn, string sqlExpression)
        {
            AddMatchedSet(targetColumn, sqlExpression, SetValueKind.Raw, null);
            return this;
        }

        /// <summary>Agrega una asignación parametrizada (<c>?</c>) a <c>UPDATE SET</c>.</summary>
        public MergeQueryBuilder SetParam(string targetColumn, object? value)
        {
            AddMatchedSet(targetColumn, "?", SetValueKind.Param, value);
            return this;
        }

        /// <summary>
        /// Condición adicional para <c>WHEN MATCHED</c> (se emite como
        /// <c>WHEN MATCHED AND (...)</c>).
        /// </summary>
        public MergeQueryBuilder WhenMatchedAnd(string andConditionSql)
        {
            _matchedAndCondition = string.IsNullOrWhiteSpace(andConditionSql) ? null : andConditionSql.Trim();
            return this;
        }

        /// <summary>
        /// Variante “rápida” para <c>UPDATE SET</c>: recibe asignaciones ya formateadas
        /// (p. ej. <c>"T.COL = S.COL"</c>, <c>"T.COUNT = T.COUNT + 1"</c>).
        /// </summary>
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

                _updateAssignmentsRaw.Add(a);
            }

            return this;
        }

        /// <summary>
        /// Variante con diccionario <c>columna → expresión</c> para <c>UPDATE SET</c>.
        /// </summary>
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

        // ---------------------------------------------------------------------
        // WHEN NOT MATCHED (INSERT)
        // ---------------------------------------------------------------------

        /// <summary>Mapea una columna de <c>INSERT</c> (NO-MATCH) desde la fuente (ej.: <c>S.COL</c>).</summary>
        public MergeQueryBuilder Map(string targetColumn, string sourceExpression)
        {
            AddInsertMap(targetColumn, sourceExpression, InsertValueKind.SourceExpr, null);
            return this;
        }

        /// <summary>Mapea una columna de <c>INSERT</c> (NO-MATCH) con una expresión RAW.</summary>
        public MergeQueryBuilder MapRaw(string targetColumn, string rawSql)
        {
            AddInsertMap(targetColumn, rawSql, InsertValueKind.Raw, null);
            return this;
        }

        /// <summary>Mapea una columna de <c>INSERT</c> (NO-MATCH) a un parámetro (<c>?</c>).</summary>
        public MergeQueryBuilder MapParam(string targetColumn, object? value)
        {
            AddInsertMap(targetColumn, "?", InsertValueKind.Param, value);
            return this;
        }

        /// <summary>
        /// Condición adicional para <c>WHEN NOT MATCHED</c> (se emite como
        /// <c>WHEN NOT MATCHED AND (...)</c>).
        /// </summary>
        public MergeQueryBuilder WhenNotMatchedAnd(string andConditionSql)
        {
            _notMatchedAndCondition = string.IsNullOrWhiteSpace(andConditionSql) ? null : andConditionSql.Trim();
            return this;
        }

        /// <summary>
        /// Variante “rápida” para <c>INSERT</c> (NO-MATCH): recibe pares (columna destino, expresión SQL).
        /// </summary>
        public MergeQueryBuilder WhenNotMatchedInsert(params (string TargetColumn, string Expression)[] mappings)
        {
            if (mappings is null || mappings.Length == 0) return this;

            foreach (var (col, expr) in mappings)
                AddInsertMap(col, expr, InsertValueKind.Raw, null);

            return this;
        }

        // ---------------------------------------------------------------------
        // Build
        // ---------------------------------------------------------------------

        /// <summary>
        /// Construye el SQL <c>MERGE</c> final y el arreglo de parámetros en el orden posicional
        /// adecuado para DB2 i (OleDb).
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Si faltan datos esenciales (tabla, fuente USING, condición ON, etc.).
        /// </exception>
        public QueryResult Build()
        {
            _parameters.Clear();

            if (string.IsNullOrWhiteSpace(_targetTable))
                throw new InvalidOperationException("Debe especificarse la tabla destino.");
            if (_sourceKind == SourceKind.None)
                throw new InvalidOperationException("Debe especificarse la fuente USING (VALUES o SELECT).");
            if (string.IsNullOrWhiteSpace(_onConditionSql))
                throw new InvalidOperationException("Debe especificarse la condición ON.");

            if (_sourceKind == SourceKind.Values && _sourceColumns.Count == 0)
                throw new InvalidOperationException("Para USING (VALUES) debe definir nombres de columnas de la fuente.");

            var sb = new StringBuilder();

            // Comentario (si lo hay)
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
                sb.Append("VALUES").Append('\n');

                if (_sourceValuesTypedRows.Count > 0)
                {
                    // Modo tipado: se emite el SQL de tipo y se cargan los valores
                    var rowSqls = new List<string>(_sourceValuesTypedRows.Count);
                    foreach (var row in _sourceValuesTypedRows)
                    {
                        rowSqls.Add("(" + string.Join(", ", row.Select(e => e.Sql)) + ")");
                        foreach (var e in row)
                            _parameters.Add(e.Val); // 1) parámetros de USING (tipados)
                    }
                    sb.Append(string.Join(",\n", rowSqls));
                }
                else
                {
                    // Modo simple: "?, ?, ?" + parámetros en orden
                    var rowSqls = new List<string>(_sourceValuesRows.Count);
                    foreach (var row in _sourceValuesRows)
                    {
                        rowSqls.Add("(" + string.Join(", ", Enumerable.Repeat("?", row.Length)) + ")");
                        _parameters.AddRange(row); // 1) parámetros de USING (simples)
                    }
                    sb.Append(string.Join(",\n", rowSqls));
                }
            }
            else // SELECT
            {
                var sel = _sourceSelect!.Build();
                if (sel.Parameters is { Count: > 0 })
                    _parameters.AddRange(sel.Parameters); // 1) parámetros del SELECT

                sb.Append(sel.Sql);
            }
            sb.Append(')');

            // Alias y columnas de la fuente S
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

                // Asignaciones tipadas (SourceExpr / Raw / Param)
                foreach (var s in _matchedSet)
                {
                    string rhs = s.Kind switch
                    {
                        SetValueKind.SourceExpr => s.Expression!, // ej. S.COL
                        SetValueKind.Raw       => s.Expression!, // ej. CASE ...
                        SetValueKind.Param     => "?",           // parametrizado
                        _ => throw new InvalidOperationException("Tipo de asignación inválido.")
                    };

                    if (s.Kind == SetValueKind.Param)
                        _parameters.Add(s.Value); // 2) parámetros de UPDATE

                    assigns.Add($"{_targetAlias}.{s.TargetColumn} = {rhs}");
                }

                // Asignaciones RAW completas (si las hay)
                assigns.AddRange(_updateAssignmentsRaw);

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
                        InsertValueKind.SourceExpr => m.Expression!, // ej. S.UID
                        InsertValueKind.Raw       => m.Expression!,  // ej. '' o CASE ...
                        InsertValueKind.Param     => "?",            // parametrizado
                        _ => throw new InvalidOperationException("Tipo de mapeo inválido.")
                    };

                    if (m.Kind == InsertValueKind.Param)
                        _parameters.Add(m.Value); // 3) parámetros de INSERT

                    vals.Add(rhs);
                }

                sb.Append('(').Append(string.Join(", ", vals)).Append(')').Append('\n');
            }

            return new QueryResult
            {
                Sql = sb.ToString(),
                Parameters = _parameters
            };
        }

        // ---------------------------------------------------------------------
        // Helpers privados
        // ---------------------------------------------------------------------

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
    }
}
