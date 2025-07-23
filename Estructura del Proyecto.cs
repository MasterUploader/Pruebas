using QueryBuilder.Expressions;
using QueryBuilder.Models;
using System.Linq.Expressions;
using System.Text;

namespace QueryBuilder.Builders;

/// <summary>
/// Generador de consultas SELECT compatible con AS400, incluyendo ORDER BY, FETCH FIRST y alias.
/// </summary>
public class SelectQueryBuilder
{
    private readonly string _fullTableName;
    private readonly List<string> _columns = new();
    private string? _whereClause;
    private readonly List<string> _orderBy = new();
    private int? _limit;

    /// <summary>
    /// Inicializa una nueva instancia de <see cref="SelectQueryBuilder"/>.
    /// </summary>
    /// <param name="tableName">Nombre de la tabla a consultar.</param>
    /// <param name="library">Nombre de la biblioteca en AS400 (opcional).</param>
    public SelectQueryBuilder(string tableName, string? library = null)
    {
        _fullTableName = string.IsNullOrWhiteSpace(library)
            ? tableName
            : $"{library}.{tableName}";
    }

    /// <summary>
    /// Define las columnas a seleccionar. Pueden incluir alias como "CAMPO AS Alias".
    /// </summary>
    public SelectQueryBuilder Select(params string[] columns)
    {
        _columns.AddRange(columns);
        return this;
    }

    /// <summary>
    /// Agrega una cláusula WHERE utilizando una expresión lambda.
    /// </summary>
    public SelectQueryBuilder Where<T>(Expression<Func<T, bool>> expression)
    {
        _whereClause = LambdaWhereTranslator.Translate(expression);
        return this;
    }

    /// <summary>
    /// Define una cláusula ORDER BY para la consulta.
    /// </summary>
    /// <param name="column">Nombre de la columna.</param>
    /// <param name="descending">Indica si el orden es descendente.</param>
    public SelectQueryBuilder OrderBy(string column, bool descending = false)
    {
        var direction = descending ? "DESC" : "ASC";
        _orderBy.Add($"{column} {direction}");
        return this;
    }

    /// <summary>
    /// Limita el número de filas devueltas por la consulta.
    /// En AS400 se utiliza "FETCH FIRST N ROWS ONLY".
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
        sb.Append(_columns.Count > 0 ? string.Join(", ", _columns) : "*");
        sb.Append(" FROM ").Append(_fullTableName);

        if (!string.IsNullOrWhiteSpace(_whereClause))
        {
            sb.Append(" WHERE ").Append(_whereClause);
        }

        if (_orderBy.Count > 0)
        {
            sb.Append(" ORDER BY ").Append(string.Join(", ", _orderBy));
        }

        if (_limit.HasValue)
        {
            sb.Append($" FETCH FIRST {_limit.Value} ROWS ONLY");
        }

        return new QueryResult
        {
            Sql = sb.ToString()
        };
    }
}
