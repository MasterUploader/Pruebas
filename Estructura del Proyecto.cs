using QueryBuilder.Builders;

namespace QueryBuilder.Core;

/// <summary>
/// Punto de entrada principal para construir consultas SQL.
/// </summary>
public static class QueryBuilder
{
    /// <summary>
    /// Inicia la construcción de una consulta SELECT.
    /// </summary>
    /// <param name="tableName">Nombre de la tabla.</param>
    /// <param name="library">Nombre opcional de la biblioteca (solo para AS400).</param>
    /// <returns>Instancia de SelectQueryBuilder.</returns>
    public static SelectQueryBuilder From(string tableName, string? library = null)
    {
        return new SelectQueryBuilder(tableName, library);
    }
}

using QueryBuilder.Expressions;
using QueryBuilder.Models;
using System.Linq.Expressions;
using System.Text;

namespace QueryBuilder.Builders;

/// <summary>
/// Constructor especializado para generar consultas SELECT dinámicamente.
/// </summary>
public class SelectQueryBuilder
{
    private readonly string _fullTableName;
    private readonly List<string> _columns = new();
    private string? _whereClause;

    /// <summary>
    /// Inicializa un nuevo generador SELECT para una tabla dada.
    /// </summary>
    /// <param name="tableName">Nombre de la tabla.</param>
    /// <param name="library">Nombre opcional de la biblioteca (como CYBERDTA para AS400).</param>
    public SelectQueryBuilder(string tableName, string? library = null)
    {
        _fullTableName = string.IsNullOrWhiteSpace(library)
            ? tableName
            : $"{library}.{tableName}";
    }

    /// <summary>
    /// Define las columnas a seleccionar.
    /// </summary>
    /// <param name="columns">Listado de columnas a incluir en el SELECT.</param>
    /// <returns>Instancia de SelectQueryBuilder para encadenamiento.</returns>
    public SelectQueryBuilder Select(params string[] columns)
    {
        _columns.AddRange(columns);
        return this;
    }

    /// <summary>
    /// Agrega una cláusula WHERE a partir de una expresión lambda.
    /// </summary>
    /// <typeparam name="T">Tipo del objeto que representa las columnas.</typeparam>
    /// <param name="expression">Expresión condicional.</param>
    /// <returns>Instancia de SelectQueryBuilder para encadenamiento.</returns>
    public SelectQueryBuilder Where<T>(Expression<Func<T, bool>> expression)
    {
        _whereClause = LambdaWhereTranslator.Translate(expression);
        return this;
    }

    /// <summary>
    /// Construye y retorna la consulta SQL final como un QueryResult.
    /// </summary>
    /// <returns>Instancia de QueryResult con el SQL generado.</returns>
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
            Sql = sb.ToString()
        };
    }
}
