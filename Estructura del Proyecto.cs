using QueryBuilder.Helpers;
using QueryBuilder.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace QueryBuilder.Builders;

/// <summary>
/// Generador de sentencias <c>DELETE</c> compatible con múltiples motores (AS400/DB2, SQL Server, etc.).
/// <para>
/// Objetivos principales:
/// <list type="bullet">
///   <item><description>Soportar <c>WHERE</c> con expresiones lambda tipadas o SQL crudo.</description></item>
///   <item><description>Parámetros seguros con marcadores <c>?</c> (útil para OleDb/AS400).</description></item>
///   <item><description>Azúcar sintáctico para condiciones comunes: <c>=, &lt;&gt;, IN, NOT IN, BETWEEN</c>.</description></item>
///   <item><description>Mantener compatibilidad con tu implementación previa.</description></item>
/// </list>
/// </para>
/// </summary>
public class DeleteQueryBuilder
{
    private readonly string _tableName;
    private readonly string? _library;

    // Comentario opcional (trazabilidad).
    private string? _comment;

    // Predicados de WHERE acumulados (cada entrada es un fragmento de SQL ya formado).
    private readonly List<string> _predicates = new();

    // Lista de parámetros en el mismo orden en que aparecen los marcadores '?' del SQL.
    private readonly List<object?> _parameters = new();

    /// <summary>
    /// Inicializa una nueva instancia del generador DELETE.
    /// </summary>
    /// <param name="tableName">Nombre de la tabla sobre la que se ejecutará el DELETE.</param>
    /// <param name="library">Nombre de la biblioteca/esquema (opcional, por ejemplo en AS400).</param>
    public DeleteQueryBuilder(string tableName, string? library = null)
    {
        _tableName = tableName ?? throw new ArgumentNullException(nameof(tableName));
        _library = library;
    }

    /// <summary>
    /// Agrega un comentario SQL al inicio de la consulta para trazabilidad (no afecta la ejecución).
    /// </summary>
    /// <param name="comment">Comentario a incluir (se antepone con <c>--</c>).</param>
    public DeleteQueryBuilder WithComment(string comment)
    {
        if (!string.IsNullOrWhiteSpace(comment))
            _comment = $"-- {comment}";
        return this;
    }

    // ======== API COMPATIBLE (mantenida) ========

    /// <summary>
    /// Agrega una cláusula WHERE con condición en formato SQL crudo.
    /// <para>Compatibilidad con versión anterior. Ahora se acumula (AND) con condiciones previas en vez de sobrescribir.</para>
    /// </summary>
    /// <param name="sqlWhere">Condición WHERE en formato de texto (por ejemplo: <c>"ESTADO = 'A'"</c>).</param>
    public DeleteQueryBuilder Where(string sqlWhere)
    {
        if (string.IsNullOrWhiteSpace(sqlWhere)) return this;
        _predicates.Add(sqlWhere.Trim());
        return this;
    }

    /// <summary>
    /// Agrega una cláusula WHERE utilizando una expresión lambda tipada.
    /// <para>
    /// Usa el convertidor de expresiones existente para generar SQL. (Los valores se inyectan en el texto resultante,
    /// por lo que esta ruta no aporta parámetros <c>?</c>).
    /// </para>
    /// </summary>
    /// <typeparam name="T">Tipo de entidad utilizada en la expresión.</typeparam>
    /// <param name="expression">Expresión booleana (ej: <c>x =&gt; x.CODCCO != valor</c>).</param>
    public DeleteQueryBuilder Where<T>(Expression<Func<T, bool>> expression)
    {
        if (expression == null) return this;
        var sql = ExpressionToSqlConverter.Convert(expression);
        if (!string.IsNullOrWhiteSpace(sql))
            _predicates.Add(sql);
        return this;
    }

    // ======== API NUEVA (segura con parámetros) ========

    /// <summary>
    /// Agrega un fragmento WHERE en SQL crudo con parámetros seguros (marcadores <c>?</c>).
    /// </summary>
    /// <param name="sqlWhere">Fragmento SQL que debe contener tantos <c>?</c> como valores proporciones.</param>
    /// <param name="args">Valores para los parámetros en el mismo orden que sus marcadores.</param>
    public DeleteQueryBuilder WhereRaw(string sqlWhere, params object?[] args)
    {
        if (string.IsNullOrWhiteSpace(sqlWhere)) return this;
        _predicates.Add(sqlWhere.Trim());
        if (args is { Length: > 0 })
            _parameters.AddRange(args);
        return this;
    }

    /// <summary>
    /// Agrega un predicado de igualdad <c>columna = ?</c>.
    /// </summary>
    public DeleteQueryBuilder WhereEqual(string column, object? value)
        => AddBinary(column, "=", value);

    /// <summary>
    /// Agrega un predicado de desigualdad <c>columna &lt;&gt; ?</c>.
    /// </summary>
    public DeleteQueryBuilder WhereNotEqual(string column, object? value)
        => AddBinary(column, "<>", value);

    /// <summary>
    /// Agrega un predicado <c>columna &gt; ?</c>.
    /// </summary>
    public DeleteQueryBuilder WhereGreaterThan(string column, object? value)
        => AddBinary(column, ">", value);

    /// <summary>
    /// Agrega un predicado <c>columna &gt;= ?</c>.
    /// </summary>
    public DeleteQueryBuilder WhereGreaterOrEqual(string column, object? value)
        => AddBinary(column, ">=", value);

    /// <summary>
    /// Agrega un predicado <c>columna &lt; ?</c>.
    /// </summary>
    public DeleteQueryBuilder WhereLessThan(string column, object? value)
        => AddBinary(column, "<", value);

    /// <summary>
    /// Agrega un predicado <c>columna &lt;= ?</c>.
    /// </summary>
    public DeleteQueryBuilder WhereLessOrEqual(string column, object? value)
        => AddBinary(column, "<=", value);

    /// <summary>
    /// Agrega un predicado <c>columna BETWEEN ? AND ?</c>.
    /// </summary>
    public DeleteQueryBuilder WhereBetween(string column, object start, object end)
    {
        ValidateColumn(column);
        _predicates.Add($"{column} BETWEEN ? AND ?");
        _parameters.Add(start);
        _parameters.Add(end);
        return this;
    }

    /// <summary>
    /// Agrega un predicado <c>columna IN (?, ?, ...)</c>.
    /// Si la colección está vacía no agrega nada.
    /// </summary>
    public DeleteQueryBuilder WhereIn(string column, IEnumerable<object?> values)
    {
        ValidateColumn(column);
        if (values == null) return this;

        var list = values.ToList();
        if (list.Count == 0) return this;

        var placeholders = string.Join(", ", Enumerable.Repeat("?", list.Count));
        _predicates.Add($"{column} IN ({placeholders})");
        _parameters.AddRange(list);
        return this;
    }

    /// <summary>
    /// Agrega un predicado <c>columna NOT IN (?, ?, ...)</c>.
    /// Si la colección está vacía no agrega nada.
    /// </summary>
    public DeleteQueryBuilder WhereNotIn(string column, IEnumerable<object?> values)
    {
        ValidateColumn(column);
        if (values == null) return this;

        var list = values.ToList();
        if (list.Count == 0) return this;

        var placeholders = string.Join(", ", Enumerable.Repeat("?", list.Count));
        _predicates.Add($"{column} NOT IN ({placeholders})");
        _parameters.AddRange(list);
        return this;
    }

    /// <summary>
    /// Agrega <c>(columna &lt;&gt; ? OR columna IS NULL)</c>, útil cuando se desea considerar también valores <c>NULL</c>.
    /// </summary>
    public DeleteQueryBuilder WhereNotEqualOrNull(string column, object? value)
    {
        ValidateColumn(column);
        _predicates.Add($"({column} <> ? OR {column} IS NULL)");
        _parameters.Add(value);
        return this;
    }

    /// <summary>
    /// Limpia todas las condiciones WHERE acumuladas.
    /// </summary>
    public DeleteQueryBuilder ClearWhere()
    {
        _predicates.Clear();
        _parameters.Clear();
        return this;
    }

    // ======== Construcción del SQL ========

    /// <summary>
    /// Construye la consulta <c>DELETE</c> y la encapsula en <see cref="QueryResult"/>.
    /// </summary>
    /// <returns>
    /// <see cref="QueryResult.Sql"/> contiene el comando SQL con marcadores <c>?</c> si se usaron métodos paramétricos.
    /// <br/>
    /// <see cref="QueryResult.Parameters"/> contiene los valores en el mismo orden que los marcadores.
    /// </returns>
    public QueryResult Build()
    {
        if (string.IsNullOrWhiteSpace(_tableName))
            throw new InvalidOperationException("Debe especificar el nombre de la tabla.");

        var sb = new StringBuilder();

        if (!string.IsNullOrWhiteSpace(_comment))
            sb.AppendLine(_comment);

        sb.Append("DELETE FROM ");
        if (!string.IsNullOrWhiteSpace(_library))
            sb.Append($"{_library}.");
        sb.Append(_tableName);

        if (_predicates.Count > 0)
        {
            sb.Append(" WHERE ");
            sb.Append(string.Join(" AND ", _predicates));
        }

        return new QueryResult
        {
            Sql = sb.ToString(),
            Parameters = _parameters.Count > 0 ? new List<object?>(_parameters) : []
        };
    }

    // ======== Utilitarios internos ========

    /// <summary>
    /// Valida el nombre de columna (básico) para evitar strings vacíos.
    /// </summary>
    private static void ValidateColumn(string column)
    {
        if (string.IsNullOrWhiteSpace(column))
            throw new ArgumentException("El nombre de la columna no puede ser vacío.", nameof(column));
    }

    /// <summary>
    /// Agrega un predicado binario paramétrico (ej: <c>columna &lt;&gt; ?</c>).
    /// </summary>
    private DeleteQueryBuilder AddBinary(string column, string op, object? value)
    {
        ValidateColumn(column);
        if (string.IsNullOrWhiteSpace(op)) throw new ArgumentException("Operador inválido.", nameof(op));

        _predicates.Add($"{column} {op} ?");
        _parameters.Add(value);
        return this;
    }
}
