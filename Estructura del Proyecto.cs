using QueryBuilder.Enums;
using QueryBuilder.Helpers;
using QueryBuilder.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace QueryBuilder.Builders;

/// <summary>
/// Generador de sentencias <c>UPDATE</c> compatible con AS/400 (DB2 for i) y otros motores.
/// <para>
/// Características principales:
/// </para>
/// <list type="bullet">
///   <item><description>Soporte de parámetros posicionales (<c>?</c>) con <see cref="QueryResult.Parameters"/> en el mismo orden del SQL.</description></item>
///   <item><description>Asignaciones con <see cref="Set(string, object?)"/> (parametrizado) o <see cref="SetRaw(string, string)"/> (expresión SQL sin parámetros).</description></item>
///   <item><description>Ayudas fluídas para <c>WHERE</c>: <see cref="WhereEq"/>, <see cref="WhereNot"/>, <see cref="WhereIn"/>, <see cref="WhereNotIn"/>, <see cref="WhereBetween"/>, <see cref="WhereNull"/>, <see cref="WhereNotNull"/>, <see cref="WhereRaw(string)"/> y <see cref="Where{T}(Expression{Func{T, bool}})"/>.</description></item>
///   <item><description>Comentario opcional con <see cref="WithComment(string?)"/> (saneado para evitar inyección en comentarios).</description></item>
/// </list>
/// </summary>
/// <param name="tableName">Nombre de la tabla destino del <c>UPDATE</c>.</param>
/// <param name="library">Biblioteca/Esquema (opcional). En DB2 i corresponde a la biblioteca.</param>
/// <param name="dialect">Dialecto SQL. Por defecto <see cref="SqlDialect.Db2i"/>.</param>
public class UpdateQueryBuilder(string tableName, string? library = null, SqlDialect dialect = SqlDialect.Db2i)
{
    private readonly string _tableName = tableName ?? throw new ArgumentNullException(nameof(tableName));
    private readonly string? _library = library;
    private readonly SqlDialect _dialect = dialect;

    private string? _comment;

    // --- Asignaciones SET ---
    private enum SetKind { Param, Raw }

    private readonly List<(SetKind Kind, string Column, object? Value, string? RawExpr)> _sets = [];

    // --- Condiciones WHERE (lista de fragmentos combinados con AND) ---
    private readonly List<(string Sql, List<object?>? Parameters)> _whereParts = [];

    /// <summary>
    /// Agrega un comentario (una sola línea) al inicio del comando para trazabilidad.
    /// Se sanea para evitar inyección en comentarios.
    /// </summary>
    public UpdateQueryBuilder WithComment(string? comment)
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

    /// <summary>
    /// Define una asignación <c>SET columna = ?</c> (parametrizada).
    /// </summary>
    /// <param name="column">Nombre de la columna destino.</param>
    /// <param name="value">Valor a establecer (se agregará a <see cref="QueryResult.Parameters"/>).</param>
    public UpdateQueryBuilder Set(string column, object? value)
    {
        if (string.IsNullOrWhiteSpace(column))
            throw new ArgumentNullException(nameof(column));
        _sets.Add((SetKind.Param, column.Trim(), value, null));
        return this;
    }

    /// <summary>
    /// Define una asignación RAW para <c>SET</c>, por ejemplo:
    /// <c>SetRaw("UPDATED_AT", "CURRENT_TIMESTAMP")</c> o
    /// <c>SetRaw("COUNT", "COUNT + 1")</c>.
    /// </summary>
    /// <param name="column">Columna destino.</param>
    /// <param name="rawSqlExpression">Expresión SQL que se colocará tal cual a la derecha del <c>=</c>.</param>
    public UpdateQueryBuilder SetRaw(string column, string rawSqlExpression)
    {
        if (string.IsNullOrWhiteSpace(column))
            throw new ArgumentNullException(nameof(column));
        if (string.IsNullOrWhiteSpace(rawSqlExpression))
            throw new ArgumentNullException(nameof(rawSqlExpression));

        _sets.Add((SetKind.Raw, column.Trim(), null, rawSqlExpression.Trim()));
        return this;
    }

    // ============== WHERE helpers (se combinan con AND) ==============

    /// <summary>
    /// Agrega una condición <c>WHERE</c> cruda (no parametrizada). Se concatena con AND con las previas.
    /// </summary>
    public UpdateQueryBuilder WhereRaw(string sql)
    {
        if (string.IsNullOrWhiteSpace(sql))
            return this;

        _whereParts.Add((sql.Trim(), null));
        return this;
    }

    /// <summary>
    /// Agrega una condición <c>WHERE columna = ?</c>.
    /// </summary>
    public UpdateQueryBuilder WhereEq(string column, object? value)
    {
        if (string.IsNullOrWhiteSpace(column))
            throw new ArgumentNullException(nameof(column));

        _whereParts.Add(($"{column.Trim()} = ?", new List<object?> { value }));
        return this;
    }

    /// <summary>
    /// Agrega una condición <c>WHERE columna &lt;&gt; ?</c>.
    /// </summary>
    public UpdateQueryBuilder WhereNot(string column, object? value)
    {
        if (string.IsNullOrWhiteSpace(column))
            throw new ArgumentNullException(nameof(column));

        _whereParts.Add(($"{column.Trim()} <> ?", new List<object?> { value }));
        return this;
    }

    /// <summary>
    /// Agrega una condición <c>WHERE columna IN (?, ?, ...)</c>. Si la colección está vacía,
    /// agrega la condición siempre falsa <c>1 = 0</c>.
    /// </summary>
    public UpdateQueryBuilder WhereIn(string column, IEnumerable<object?> values)
    {
        if (string.IsNullOrWhiteSpace(column))
            throw new ArgumentNullException(nameof(column));

        var list = (values ?? Enumerable.Empty<object?>()).ToList();
        if (list.Count == 0)
        {
            _whereParts.Add(("1 = 0", null));
            return this;
        }

        var placeholders = string.Join(", ", Enumerable.Repeat("?", list.Count));
        _whereParts.Add(($"{column.Trim()} IN ({placeholders})", new List<object?>(list)));
        return this;
    }

    /// <summary>
    /// Agrega una condición <c>WHERE columna NOT IN (?, ?, ...)</c>. Si la colección está vacía,
    /// no agrega nada (no tiene efecto).
    /// </summary>
    public UpdateQueryBuilder WhereNotIn(string column, IEnumerable<object?> values)
    {
        if (string.IsNullOrWhiteSpace(column))
            throw new ArgumentNullException(nameof(column));

        var list = (values ?? Enumerable.Empty<object?>()).ToList();
        if (list.Count == 0) return this;

        var placeholders = string.Join(", ", Enumerable.Repeat("?", list.Count));
        _whereParts.Add(($"{column.Trim()} NOT IN ({placeholders})", new List<object?>(list)));
        return this;
    }

    /// <summary>
    /// Agrega una condición <c>WHERE columna BETWEEN ? AND ?</c>.
    /// </summary>
    public UpdateQueryBuilder WhereBetween(string column, object? fromInclusive, object? toInclusive)
    {
        if (string.IsNullOrWhiteSpace(column))
            throw new ArgumentNullException(nameof(column));

        _whereParts.Add(($"{column.Trim()} BETWEEN ? AND ?", new List<object?> { fromInclusive, toInclusive }));
        return this;
    }

    /// <summary>
    /// Agrega una condición <c>WHERE columna IS NULL</c>.
    /// </summary>
    public UpdateQueryBuilder WhereNull(string column)
    {
        if (string.IsNullOrWhiteSpace(column))
            throw new ArgumentNullException(nameof(column));

        _whereParts.Add(($"{column.Trim()} IS NULL", null));
        return this;
    }

    /// <summary>
    /// Agrega una condición <c>WHERE columna IS NOT NULL</c>.
    /// </summary>
    public UpdateQueryBuilder WhereNotNull(string column)
    {
        if (string.IsNullOrWhiteSpace(column))
            throw new ArgumentNullException(nameof(column));

        _whereParts.Add(($"{column.Trim()} IS NOT NULL", null));
        return this;
    }

    /// <summary>
    /// Agrega una condición <c>WHERE</c> basada en una expresión lambda.
    /// <para>Nota: se convierte a SQL crudo mediante <see cref="ExpressionToSqlConverter"/> y no agrega parámetros.</para>
    /// </summary>
    public UpdateQueryBuilder Where<T>(Expression<Func<T, bool>> expression)
    {
        if (expression is null) return this;

        var sql = ExpressionToSqlConverter.Convert(expression);
        if (!string.IsNullOrWhiteSpace(sql))
            _whereParts.Add((sql.Trim(), null));

        return this;
    }

    /// <summary>
    /// Elimina todas las condiciones <c>WHERE</c> acumuladas.
    /// </summary>
    public UpdateQueryBuilder ClearWhere()
    {
        _whereParts.Clear();
        return this;
    }

    // ============================ BUILD ============================

    /// <summary>
    /// Construye y retorna el <see cref="QueryResult"/> con:
    /// <list type="number">
    /// <item><description>SQL generado (UPDATE ... SET ... WHERE ...).</description></item>
    /// <item><description>Lista de parámetros en el mismo orden que los marcadores <c>?</c> del SQL.</description></item>
    /// </list>
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Si no se especificó tabla o no hay ninguna asignación <c>SET</c>.
    /// </exception>
    public QueryResult Build()
    {
        if (string.IsNullOrWhiteSpace(_tableName))
            throw new InvalidOperationException("Debe especificarse el nombre de la tabla para UPDATE.");

        if (_sets.Count == 0)
            throw new InvalidOperationException("Debe especificar al menos una columna para actualizar (SET).");

        var sb = new StringBuilder();
        var parameters = new List<object?>();

        if (!string.IsNullOrWhiteSpace(_comment))
            sb.AppendLine(_comment);

        // Tabla completa
        var fullTable = string.IsNullOrWhiteSpace(_library) ? _tableName : $"{_library}.{_tableName}";

        // Cabecera
        sb.Append("UPDATE ").Append(fullTable).Append(" SET ");

        // SET col = ?, col2 = RAW, ...
        var setFragments = new List<string>();
        foreach (var s in _sets)
        {
            if (s.Kind == SetKind.Param)
            {
                setFragments.Add($"{s.Column} = ?");
                parameters.Add(s.Value);
            }
            else
            {
                // RAW: se emite tal cual
                setFragments.Add($"{s.Column} = {s.RawExpr}");
            }
        }
        sb.Append(string.Join(", ", setFragments));

        // WHERE (si hay)
        if (_whereParts.Count > 0)
        {
            sb.Append(" WHERE ");
            var whereFragments = new List<string>();
            foreach (var part in _whereParts)
            {
                whereFragments.Add(part.Sql);
                if (part.Parameters is { Count: > 0 })
                    parameters.AddRange(part.Parameters);
            }
            sb.Append(string.Join(" AND ", whereFragments));
        }

        return new QueryResult
        {
            Sql = sb.ToString(),
            Parameters = parameters
        };
    }
}
