using QueryBuilder.Enums;
using QueryBuilder.Helpers;
using QueryBuilder.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace QueryBuilder.Builders
{
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
    public sealed class MergeQueryBuilder
    {
        private readonly string _targetTable;
        private readonly string? _targetLibrary;
        private readonly SqlDialect _dialect;

        // Alias de la tabla destino
        private string _targetAlias = "T";

        // Fuente: SELECT o VALUES
        private enum SourceKind { None, Values, Select }
        private SourceKind _sourceKind = SourceKind.None;

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

        /// <summary>
        /// Crea un nuevo <see cref="MergeQueryBuilder"/>.
        /// </summary>
        /// <param name="targetTable">Tabla destino del MERGE.</param>
        /// <param name="targetLibrary">Biblioteca/Esquema del destino (DB2 i).</param>
        /// <param name="dialect">Dialecto SQL. Enfocado a <see cref="SqlDialect.Db2i"/>.</param>
        public MergeQueryBuilder(string targetTable, string? targetLibrary = null, SqlDialect dialect = SqlDialect.Db2i)
        {
            _targetTable = targetTable ?? throw new ArgumentNullException(nameof(targetTable));
            _targetLibrary = targetLibrary;
            _dialect = dialect;
        }

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
            foreach (var r in rows ?? Enumerable.Empty<object?[]>())
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
                        SetValueKind.Raw       => s.Expression!,   // ej: CASE ...
                        SetValueKind.Param     => "?",             // parametrizado
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
                        InsertValueKind.Raw       => m.Expression!, // ej: ''
                        InsertValueKind.Param     => "?",           // parametrizado
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
}



// ======================================================================
// 1) INSERT ... SELECT  → BCAH96DTA.ETD01LOG (correlativo + datos de trazabilidad)
// ======================================================================

// Sello de tiempo y datos de máquina
var now = DateTime.Now;
string tsDb2 = now.ToString("yyyy-MM-dd-HH.mm.ss"); // para TIMESTAMP('yyyy-mm-dd-hh.mm.ss')

// Por simplicidad, dejo estos helpers de escape de comillas
static string esc(string? s) => (s ?? "").Replace("'", "''");

// SELECT que produce exactamente las 13 columnas a insertar, en el MISMO orden
var selectInsert = new SelectQueryBuilder("ETD01LOG", "BCAH96DTA")
    .As("T")
    .Select("COALESCE(MAX(T.LOGA01AID), 0) + 1")              // 1: LOGA01AID correlativo
    .Select($"'{esc(userID)}'")                                // 2: LOGA02UID
    .Select($"TIMESTAMP('{tsDb2}')")                           // 3: LOGA03TST
    .Select($"'{esc(exitoso)}'")                               // 4: LOGA04SUC
    .Select($"'{esc(machine.ClientIPAddress)}'")               // 5: LOGA05IPA
    .Select($"'{esc(machine.HostName)}'")                      // 6: LOGA06MNA
    .Select($"'{esc(idSesion)}'")                              // 7: LOGA07SID
    .Select($"'{esc(motivo)}'")                                // 8: LOGA08FRE
    .Select("0")                                               // 9: LOGA09ACO
    .Select($"'{esc(machine.UserAgent)}'")                     // 10: LOGA10UAG
    .Select($"'{esc(machine.Browser)}'")                       // 11: LOGA11BRO
    .Select($"'{esc(machine.OS)}'")                            // 12: LOGA12SOP
    .Select($"'{esc(machine.Device)}'")                        // 13: LOGA13DIS
;

// INSERT con FromSelect (columnas explícitas)
var insertLogGeneral = new InsertQueryBuilder("ETD01LOG", "BCAH96DTA")
    .IntoColumns(
        "LOGA01AID","LOGA02UID","LOGA03TST","LOGA04SUC","LOGA05IPA","LOGA06MNA","LOGA07SID",
        "LOGA08FRE","LOGA09ACO","LOGA10UAG","LOGA11BRO","LOGA12SOP","LOGA13DIS"
    )
    .FromSelect(selectInsert)
    .Build();

// Ejecutar (tu provider ya setea CommandText/Parameters con el overload de QueryResult)
using (var cmd1 = _connection.GetDbCommand(insertLogGeneral, _contextAccessor.HttpContext!))
{
    var aff1 = cmd1.ExecuteNonQuery();
}



// ======================================================================
// 2) MERGE (UPSERT) → BCAH96DTA.ETD02LOG  (intentos y sesión activa)
//    - Parametrizado con USING (VALUES ...) + columnas de la fuente
// ======================================================================

// Construimos la fuente S con nombres de columnas y UNA fila de valores.
// OJO: aquí pasamos DateTime 'now' como parámetro: DB2 i (vía OleDb) lo bindeará como TIMESTAMP.
var merge = new MergeQueryBuilder("ETD02LOG", "BCAH96DTA")
    .UsingValues(
        sourceColumns: new[] { "UID", "NOWTS", "EXI", "IP", "DEV", "BRO", "TOK" },
        rows: new[]
        {
            new object?[]
            {
                userID,                 // UID
                now,                    // NOWTS  (TIMESTAMP)
                exitoso,                // EXI    ('1'/'0')
                machine.ClientIPAddress,// IP
                machine.Device,         // DEV
                machine.Browser,        // BRO
                idSesion                // TOK
            }
        },
        alias: "S"
    )
    .On("T.LOGB01UID = S.UID") // alias destino por defecto es T

    // WHEN MATCHED THEN UPDATE
    .SetFromSource("LOGB02UIL", "S.NOWTS")
    .SetRaw       ("LOGB03TIL", "CASE WHEN S.EXI = '1' THEN COALESCE(T.LOGB03TIL, 0) + 1 ELSE 0 END")
    .SetFromSource("LOGB04SEA", "S.EXI")
    .SetFromSource("LOGB05UDI", "S.IP")
    .SetFromSource("LOGB06UTD", "S.DEV")
    .SetFromSource("LOGB07UNA", "S.BRO")
    .SetRaw       ("LOGB09UIF", "COALESCE(T.LOGB02UIL, S.NOWTS)")
    .SetFromSource("LOGB10TOK", "S.TOK")

    // WHEN NOT MATCHED THEN INSERT (...)
    .Map("LOGB01UID", "S.UID")
    .Map("LOGB02UIL", "S.NOWTS")
    .MapRaw("LOGB03TIL", "CASE WHEN S.EXI = '1' THEN 1 ELSE 0 END")
    .Map("LOGB04SEA", "S.EXI")
    .Map("LOGB05UDI", "S.IP")
    .Map("LOGB06UTD", "S.DEV")
    .Map("LOGB07UNA", "S.BRO")
    .MapRaw("LOGB08CBI", "''")
    .Map("LOGB09UIF", "S.NOWTS")
    .Map("LOGB10TOK", "S.TOK")
    .Build();

// Ejecutar
using (var cmd2 = _connection.GetDbCommand(merge, _contextAccessor.HttpContext!))
{
    var aff2 = cmd2.ExecuteNonQuery();
}


