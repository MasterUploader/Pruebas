using QueryBuilder.Enums;
using QueryBuilder.Helpers;
using QueryBuilder.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices; // ITuple
using System.Text;

namespace QueryBuilder.Builders
{
    /// <summary>
    /// Generador de sentencias INSERT compatibles con múltiples motores (DB2 for i, SQL Server, MySQL, etc.).
    /// Para DB2 for i se generan placeholders (?) y <see cref="QueryResult.Parameters"/> con los valores
    /// en el mismo orden, de modo que puedan ser enlazados con OleDbCommand/DbCommand de forma segura.
    /// </summary>
    /// <remarks>
    /// Características:
    /// <list type="bullet">
    /// <item><description>INSERT con columnas definidas vía <see cref="IntoColumns(string[])"/>.</description></item>
    /// <item><description>Soporte para múltiples filas con <see cref="Values((string, object?)[])"/> encadenado,
    /// <see cref="Row(object?[])"/>, <see cref="Rows(IEnumerable{object?[]})"/>, <see cref="ListValues(object?[][])"/>.</description></item>
    /// <item><description>Atajos para listas de tuplas: <see cref="ListValuesFromTuples{TTuple}(IEnumerable{TTuple})"/>.</description></item>
    /// <item><description>Atajos para listas de objetos: <see cref="RowsFromObjects{T}(IEnumerable{T})"/> mapeando por nombres de propiedad.</description></item>
    /// <item><description>INSERT ... SELECT con <see cref="FromSelect(SelectQueryBuilder)"/> y <see cref="WhereNotExists(Subquery)"/>.</description></item>
    /// <item><description>Comentarios de trazabilidad con <see cref="WithComment(string)"/> (sanitizados).</description></item>
    /// <item><description>Soporte opcional de dialecto para <c>INSERT IGNORE</c> y <c>ON DUPLICATE KEY UPDATE</c> (no DB2 i).</description></item>
    /// </list>
    /// </remarks>
    public class InsertQueryBuilder
    {
        private readonly string _tableName;
        private readonly string? _library;
        private readonly SqlDialect _dialect;

        private readonly List<string> _columns = new();
        private readonly List<List<object?>> _rows = new();     // VALUES parametrizados (placeholders)
        private readonly List<List<object>> _valuesRaw = new();  // VALUES RAW (funciones SQL ya formateadas)
        private SelectQueryBuilder? _selectSource;

        private string? _comment;
        private string? _whereClause;
        private bool _insertIgnore = false; // No se emite en Db2i
        private readonly Dictionary<string, object?> _onDuplicateUpdate = new(StringComparer.OrdinalIgnoreCase); // No Db2i

        /// <summary>
        /// Crea un nuevo generador de sentencia INSERT.
        /// </summary>
        /// <param name="tableName">Nombre de la tabla (obligatorio).</param>
        /// <param name="library">Nombre de la librería/esquema (opcional). Para DB2 i suele ser la biblioteca.</param>
        /// <param name="dialect">Dialecto SQL. Por defecto <see cref="SqlDialect.Db2i"/>.</param>
        public InsertQueryBuilder(string tableName, string? library = null, SqlDialect dialect = SqlDialect.Db2i)
        {
            _tableName = tableName;
            _library = library;
            _dialect = dialect;
        }

        /// <summary>
        /// Agrega un comentario SQL al inicio del comando (una línea), útil para trazabilidad.
        /// Se sanitiza para evitar inyección de comentarios.
        /// </summary>
        /// <param name="comment">Texto del comentario. Se ignorará si es nulo o vacío.</param>
        public InsertQueryBuilder WithComment(string? comment)
        {
            if (string.IsNullOrWhiteSpace(comment))
                return this;

            // Saneamos: sin saltos de línea y sin secuencias peligrosas
            var sanitized = comment
                .Replace("\r", " ")
                .Replace("\n", " ")
                .Replace("--", "- -")
                .Trim();

            _comment = "-- " + sanitized;
            return this;
        }

        /// <summary>
        /// Habilita INSERT IGNORE (solo motores compatibles, por ejemplo MySQL).
        /// No se emite en DB2 for i.
        /// </summary>
        public InsertQueryBuilder InsertIgnore()
        {
            _insertIgnore = true;
            return this;
        }

        /// <summary>
        /// Define una columna y valor para "ON DUPLICATE KEY UPDATE" (solo motores compatibles).
        /// No se emite en DB2 for i.
        /// </summary>
        public InsertQueryBuilder OnDuplicateKeyUpdate(string column, object? value)
        {
            _onDuplicateUpdate[column] = value;
            return this;
        }

        /// <summary>
        /// Define varias columnas para "ON DUPLICATE KEY UPDATE" (solo motores compatibles).
        /// No se emite en DB2 for i.
        /// </summary>
        public InsertQueryBuilder OnDuplicateKeyUpdate(Dictionary<string, object?> updates)
        {
            if (updates is null) return this;
            foreach (var kvp in updates)
                _onDuplicateUpdate[kvp.Key] = kvp.Value;
            return this;
        }

        /// <summary>
        /// Define la lista de columnas para el INSERT. El orden debe coincidir con los valores que se agreguen.
        /// </summary>
        /// <param name="columns">Columnas de la tabla, en el orden deseado.</param>
        public InsertQueryBuilder IntoColumns(params string[] columns)
        {
            _columns.Clear();
            if (columns is { Length: > 0 })
                _columns.AddRange(columns);
            return this;
        }

        /// <summary>
        /// Agrega una fila de valores (modo parametrizado) recibiendo tuplas (columna, valor).
        /// Útil cuando quieres claridad explícita por nombre de columna en cada fila.
        /// </summary>
        /// <param name="values">Tuplas (columna, valor) en el mismo orden de <see cref="IntoColumns(string[])"/> cuando
        /// <c>_columns</c> ya fue definido. Si no hay columnas definidas, se infieren de las tuplas de la primera fila.</param>
        public InsertQueryBuilder Values(params (string Column, object? Value)[] values)
        {
            if (_columns.Count == 0)
                _columns.AddRange(values.Select(v => v.Column));
            else if (_columns.Count != values.Length)
                throw new InvalidOperationException($"Se esperaban {_columns.Count} columnas, pero se recibieron {values.Length}.");

            _rows.Add(new List<object?>(values.Select(v => v.Value)));
            return this;
        }

        /// <summary>
        /// Agrega valores "RAW" (por ejemplo funciones SQL como NOW(), CURRENT_TIMESTAMP, etc.).
        /// Estos valores se insertan sin parametrizar.
        /// </summary>
        public InsertQueryBuilder ValuesRaw(params string[] rawValues)
        {
            _valuesRaw.Add(new List<object>(rawValues.Cast<object>()));
            return this;
        }

        /// <summary>
        /// Define la fuente como un SELECT (INSERT ... SELECT).
        /// Limpia los valores RAW para evitar mezcla inconsistentes.
        /// </summary>
        public InsertQueryBuilder FromSelect(SelectQueryBuilder select)
        {
            _selectSource = select;
            _valuesRaw.Clear();
            return this;
        }

        /// <summary>
        /// Agrega una cláusula NOT EXISTS para escenarios de INSERT ... SELECT condicional.
        /// </summary>
        public InsertQueryBuilder WhereNotExists(Subquery subquery)
        {
            _whereClause = $"NOT EXISTS ({subquery.Sql})";
            return this;
        }

        // ============================
        // NUEVOS ATAJOS SOLICITADOS
        // ============================

        /// <summary>
        /// Agrega una fila por posición (parametrizada). El orden de los valores debe coincidir con <see cref="IntoColumns(string[])"/>.
        /// </summary>
        /// <param name="values">Arreglo de valores en el mismo orden de las columnas definidas.</param>
        /// <exception cref="InvalidOperationException">Si no se han definido columnas o la cantidad no coincide.</exception>
        public InsertQueryBuilder Row(params object?[] values)
        {
            if (_columns.Count == 0)
                throw new InvalidOperationException("Debe llamar primero a IntoColumns(...) antes de Row(...).");

            if (values is null || values.Length != _columns.Count)
                throw new InvalidOperationException($"Se esperaban {_columns.Count} valores, pero se recibieron {values?.Length ?? 0}.");

            _rows.Add(new List<object?>(values));
            return this;
        }

        /// <summary>
        /// Agrega múltiples filas por posición (parametrizadas).
        /// </summary>
        /// <param name="rows">Colección de filas; cada fila es un arreglo de valores en el orden de IntoColumns.</param>
        public InsertQueryBuilder Rows(IEnumerable<object?[]> rows)
        {
            if (rows is null) return this;
            foreach (var r in rows)
                Row(r); // reutiliza validación
            return this;
        }

        /// <summary>
        /// Azúcar sintáctico para agregar múltiples filas por posición usando <c>params</c>.
        /// </summary>
        /// <param name="rows">Cada elemento representa una fila; el orden de cada fila debe coincidir con IntoColumns.</param>
        public InsertQueryBuilder ListValues(params object?[][] rows)
        {
            return Rows(rows);
        }

        /// <summary>
        /// Agrega múltiples filas a partir de tuplas (ValueTuple). Evita crear <c>object[]</c> manuales.
        /// Cada tupla se convierte en una fila, respetando el orden de columnas.
        /// </summary>
        /// <typeparam name="TTuple">Tipo de tupla (ValueTuple) con aridad coincidente a las columnas.</typeparam>
        /// <param name="rows">Secuencia de tuplas.</param>
        /// <exception cref="InvalidOperationException">Si algún elemento no es tupla.</exception>
        public InsertQueryBuilder ListValuesFromTuples<TTuple>(IEnumerable<TTuple> rows)
        {
            if (rows is null) return this;

            foreach (var r in rows)
            {
                if (r is not ITuple tpl)
                    throw new InvalidOperationException("Cada elemento debe ser una tupla (ValueTuple).");

                var values = new object?[tpl.Length];
                for (int i = 0; i < tpl.Length; i++)
                    values[i] = tpl[i];

                Row(values);
            }
            return this;
        }

        /// <summary>
        /// Azúcar sintáctico (params) para agregar tuplas directamente.
        /// </summary>
        public InsertQueryBuilder ListValuesFromTuples<TTuple>(params TTuple[] rows)
            => ListValuesFromTuples((IEnumerable<TTuple>)rows);

        /// <summary>
        /// Agrega filas a partir de objetos mapeando propiedades públicas por nombre de columna (case-insensitive).
        /// Útil para insertar listas de DTOs/POCOs sin construir arreglos manuales.
        /// </summary>
        /// <typeparam name="T">Tipo de los objetos origen.</typeparam>
        /// <param name="items">Colección de objetos. Cada objeto debe exponer propiedades con los nombres definidos en <see cref="IntoColumns(string[])"/>.</param>
        /// <exception cref="InvalidOperationException">Si no se han definido columnas, o si falta una propiedad requerida.</exception>
        public InsertQueryBuilder RowsFromObjects<T>(IEnumerable<T> items)
        {
            if (items is null) return this;
            if (_columns.Count == 0)
                throw new InvalidOperationException("Debe llamar primero a IntoColumns(...) antes de RowsFromObjects(...).");

            // Mapa de propiedades por nombre (case-insensitive)
            var props = typeof(T)
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .ToDictionary(p => p.Name, p => p, StringComparer.OrdinalIgnoreCase);

            foreach (var item in items)
            {
                var values = new object?[_columns.Count];
                for (int i = 0; i < _columns.Count; i++)
                {
                    var col = _columns[i];
                    if (!props.TryGetValue(col, out var pi))
                        throw new InvalidOperationException($"No se encontró la propiedad '{col}' en el tipo {typeof(T).Name}.");

                    values[i] = pi.GetValue(item);
                }
                Row(values);
            }
            return this;
        }

        // ============================
        // FIN NUEVOS ATAJOS
        // ============================

        /// <summary>
        /// Construye el SQL final y la lista de parámetros (para motores que usan placeholders '?', ej. DB2 i).
        /// </summary>
        /// <returns><see cref="QueryResult"/> con la consulta y parámetros en orden.</returns>
        /// <exception cref="InvalidOperationException">Si faltan datos esenciales o hay inconsistencias.</exception>
        public QueryResult Build()
        {
            if (string.IsNullOrWhiteSpace(_tableName))
                throw new InvalidOperationException("Debe especificarse el nombre de la tabla para INSERT.");
            if (_columns.Count == 0)
                throw new InvalidOperationException("Debe especificar al menos una columna para el INSERT.");
            if (_selectSource != null && _rows.Count > 0)
                throw new InvalidOperationException("No se puede usar 'VALUES' y 'FROM SELECT' al mismo tiempo.");
            if (_selectSource == null && _rows.Count == 0 && _valuesRaw.Count == 0)
                throw new InvalidOperationException("Debe especificar al menos una fila de valores para el INSERT.");

            // Validar _rows (LINQ puro)
            int? badRowIndex = _rows
                .Select((row, idx) => new { row, idx })
                .Where(x => x.row == null || x.row.Count != _columns.Count)
                .Select(x => (int?)x.idx)
                .FirstOrDefault();

            if (badRowIndex.HasValue)
            {
                int idx = badRowIndex.Value;
                int count = _rows[idx]?.Count ?? 0;
                throw new InvalidOperationException($"La fila #{idx} tiene {count} valores; se esperaban {_columns.Count}.");
            }

            // Validar _valuesRaw (LINQ puro)
            int? badRawIndex = _valuesRaw
                .Select((row, idx) => new { row, idx })
                .Where(x => x.row == null || x.row.Count != _columns.Count)
                .Select(x => (int?)x.idx)
                .FirstOrDefault();

            if (badRawIndex.HasValue)
            {
                int idx = badRawIndex.Value;
                int count = _valuesRaw[idx]?.Count ?? 0;
                throw new InvalidOperationException($"La fila RAW #{idx} tiene {count} valores; se esperaban {_columns.Count}.");
            }

            var sb = new StringBuilder();
            var parameters = new List<object?>();

            if (!string.IsNullOrWhiteSpace(_comment))
                sb.AppendLine(_comment);

            // Cabecera INSERT
            sb.Append("INSERT ");
            // INSERT IGNORE solo si dialecto lo soporta (p.ej. MySQL)
            if (_insertIgnore && _dialect == SqlDialect.MySql)
                sb.Append("IGNORE ");

            sb.Append("INTO ");
            if (!string.IsNullOrWhiteSpace(_library))
                sb.Append($"{_library}.");
            sb.Append(_tableName);
            sb.Append(" (").Append(string.Join(", ", _columns)).Append(')');

            if (_selectSource != null)
            {
                var sel = _selectSource.Build();
                sb.AppendLine().Append(sel.Sql);
                if (!string.IsNullOrWhiteSpace(_whereClause))
                    sb.Append(" WHERE ").Append(_whereClause);
            }
            else
            {
                sb.Append(" VALUES ");

                var valueLines = new List<string>();

                // Filas parametrizadas: generan placeholders y llenan "parameters" en el mismo orden
                foreach (var row in _rows)
                {
                    var placeholders = new string[row.Count];
                    for (int i = 0; i < row.Count; i++)
                    {
                        placeholders[i] = "?";
                        parameters.Add(row[i]);
                    }
                    valueLines.Add($"({string.Join(", ", placeholders)})");
                }

                // Filas RAW (funciones/literales ya formateados)
                foreach (var row in _valuesRaw)
                    valueLines.Add($"({string.Join(", ", row.Select(SqlHelper.FormatValue))})");

                sb.Append(string.Join(", ", valueLines));
            }

            // UPSERT (no DB2 i)
            if (_onDuplicateUpdate.Count > 0 && _dialect == SqlDialect.MySql)
            {
                sb.Append(" ON DUPLICATE KEY UPDATE ");
                sb.Append(string.Join(", ", _onDuplicateUpdate.Select(kv =>
                    $"{kv.Key} = {SqlHelper.FormatValue(kv.Value)}")));
            }

            return new QueryResult
            {
                Sql = sb.ToString(),
                Parameters = parameters
            };
        }
    }
}



var rows = new (string Name, string Contact, string Address, string City, string Postal, string Country)[]
{
    ("Cardinal", "Tom B. Erichsen", "Skagen 21", "Stavanger", "4006", "Norway"),
    ("Greasy Burger", "Per Olsen", "Gateveien 15", "Sandnes", "4306", "Norway")
};

var q1 = new InsertQueryBuilder("Customers")
    .IntoColumns("CustomerName", "ContactName", "Address", "City", "PostalCode", "Country")
    .ListValuesFromTuples(rows)
    .Build();

var data = GetCustomers(); // IEnumerable<CustomerDto>

var b = new InsertQueryBuilder("Customers")
    .IntoColumns("CustomerName", "ContactName", "Address", "City", "PostalCode", "Country");

foreach (var c in data)
    b.Row(c.Name, c.Contact, c.Address, c.City, c.Postal, c.Country);

var q2 = b.Build();


var list = new []
{
    new { CustomerName="Cardinal", ContactName="Tom B. Erichsen", Address="Skagen 21", City="Stavanger", PostalCode="4006", Country="Norway" },
    new { CustomerName="Greasy Burger", ContactName="Per Olsen", Address="Gateveien 15", City="Sandnes", PostalCode="4306", Country="Norway" }
};

var q3 = new InsertQueryBuilder("Customers")
    .IntoColumns("CustomerName", "ContactName", "Address", "City", "PostalCode", "Country")
    .RowsFromObjects(list)
    .Build();






