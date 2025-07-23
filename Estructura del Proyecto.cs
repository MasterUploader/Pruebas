using QueryBuilder.Expressions;
using QueryBuilder.Models;
using System.Linq.Expressions;
using System.Text;

namespace QueryBuilder.Builders;

/// <summary>
/// Constructor especializado para generar consultas SELECT dinámicas con soporte de parámetros posicionales o nombrados.
/// </summary>
public class SelectQueryBuilder
{
    private readonly string _fullTableName;
    private readonly List<string> _columns = new();
    private string? _whereClause;
    private List<object?> _orderedParameters = new();
    private Dictionary<string, object?> _namedParameters = new();
    private readonly bool _usePositionalParameters;

    /// <summary>
    /// Inicializa un nuevo generador SELECT para una tabla específica.
    /// </summary>
    /// <param name="tableName">Nombre de la tabla.</param>
    /// <param name="library">Nombre de la biblioteca (opcional para AS400).</param>
    /// <param name="usePositionalParameters">Indica si se deben usar parámetros por posición (?) o nombrados (@p0).</param>
    public SelectQueryBuilder(string tableName, string? library = null, bool usePositionalParameters = true)
    {
        _usePositionalParameters = usePositionalParameters;
        _fullTableName = string.IsNullOrWhiteSpace(library)
            ? tableName
            : $"{library}.{tableName}";
    }

    /// <summary>
    /// Define las columnas a seleccionar.
    /// </summary>
    public SelectQueryBuilder Select(params string[] columns)
    {
        _columns.AddRange(columns);
        return this;
    }

    /// <summary>
    /// Define la cláusula WHERE utilizando una expresión lambda.
    /// </summary>
    public SelectQueryBuilder Where<T>(Expression<Func<T, bool>> expression)
    {
        var result = LambdaWhereTranslator.Translate(expression, _usePositionalParameters);
        _whereClause = result.Sql;
        _orderedParameters = result.OrderedParameters;
        _namedParameters = result.NamedParameters;
        return this;
    }

    /// <summary>
    /// Construye la consulta SQL y devuelve el resultado con parámetros.
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

        return new QueryResult
        {
            Sql = sb.ToString(),
            UsePositionalParameters = _usePositionalParameters,
            OrderedParameters = _orderedParameters,
            NamedParameters = _namedParameters
        };
    }
}
