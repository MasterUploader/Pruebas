using QueryBuilder.Expressions;
using QueryBuilder.Models;
using System.Linq.Expressions;
using System.Text;

namespace QueryBuilder.Builders;

/// <summary>
/// Generador de consultas SELECT compatible con AS400.
/// Soporta alias de tabla y columnas, ORDER BY y FETCH FIRST.
/// </summary>
public class SelectQueryBuilder
{
    private readonly string _tableName;
    private readonly string? _library;
    private string? _tableAlias;
    private readonly List<(string Column, string? Alias)> _columns = new();
    private string? _whereClause;
    private readonly List<string> _orderBy = new();
    private int? _limit;

    /// <summary>
    /// Inicializa una nueva instancia de <see cref="SelectQueryBuilder"/>.
    /// </summary>
    /// <param name="tableName">Nombre de la tabla.</param>
    /// <param name="library">Nombre de la librería en AS400 (opcional).</param>
    public SelectQueryBuilder(string tableName, string? library = null)
    {
        _tableName = tableName;
        _library = library;
    }

    /// <summary>
    /// Define un alias para la tabla.
    /// </summary>
    public SelectQueryBuilder As(string alias)
    {
        _tableAlias = alias;
        return this;
    }

    /// <summary>
    /// Define las columnas a seleccionar (sin alias).
    /// </summary>
    public SelectQueryBuilder Select(params string[] columns)
    {
        foreach (var column in columns)
        {
            _columns.Add((column, null));
        }
        return this;
    }

    /// <summary>
    /// Define columnas con alias.
    /// </summary>
    /// <param name="columns">Tuplas de columna original y alias.</param>
    public SelectQueryBuilder Select(params (string Column, string Alias)[] columns)
    {
        foreach (var (col, alias) in columns)
        {
            _columns.Add((col, alias));
        }
        return this;
    }

    /// <summary>
    /// Agrega una cláusula WHERE a partir de una expresión lambda.
    /// </summary>
    public SelectQueryBuilder Where<T>(Expression<Func<T, bool>> expression)
    {
        _whereClause = LambdaWhereTranslator.Translate(expression);
        return this;
    }

    /// <summary>
    /// Ordena por una sola columna.
    /// </summary>
    public SelectQueryBuilder OrderBy(string column, SortDirection direction = SortDirection.Asc)
    {
        _orderBy.Add($"{column} {direction.ToString().ToUpper()}");
        return this;
    }

    /// <summary>
    /// Ordena por múltiples columnas.
    /// </summary>
    public SelectQueryBuilder OrderBy(params (string Column, SortDirection Direction)[] columns)
    {
        foreach (var (column, direction) in columns)
        {
            _orderBy.Add($"{column} {direction.ToString().ToUpper()}");
        }
        return this;
    }

    /// <summary>
    /// Establece el límite de filas a devolver.
    /// </summary>
    public SelectQueryBuilder Limit(int rowCount)
    {
        _limit = rowCount;
        return this;
    }

    /// <summary>
    /// Construye y devuelve el resultado de la consulta.
    /// </summary>
    public QueryResult Build()
    {
        var sb = new StringBuilder();
        sb.Append("SELECT ");

        if (_columns.Count == 0)
        {
            sb.Append("*");
        }
        else
        {
            var columnSql = _columns.Select(c =>
                string.IsNullOrWhiteSpace(c.Alias)
                    ? c.Column
                    : $"{c.Column} AS {c.Alias}"
            );

            sb.Append(string.Join(", ", columnSql));
        }

        sb.Append(" FROM ");

        if (!string.IsNullOrWhiteSpace(_library))
            sb.Append($"{_library}.");

        sb.Append(_tableName);

        if (!string.IsNullOrWhiteSpace(_tableAlias))
            sb.Append($" {_tableAlias}");

        if (!string.IsNullOrWhiteSpace(_whereClause))
            sb.Append(" WHERE ").Append(_whereClause);

        if (_orderBy.Count > 0)
            sb.Append(" ORDER BY ").Append(string.Join(", ", _orderBy));

        if (_limit.HasValue)
            sb.Append($" FETCH FIRST {_limit.Value} ROWS ONLY");

        return new QueryResult
        {
            Sql = sb.ToString()
        };
    }
}
