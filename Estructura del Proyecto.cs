using QueryBuilder.Helpers;
using QueryBuilder.Models;
using System;
using System.Text;

namespace QueryBuilder.Builders;

/// <summary>
/// Generador de sentencias DELETE compatible con AS400 y otros motores SQL.
/// Permite agregar condiciones WHERE, subconsultas EXISTS y comentarios para trazabilidad.
/// </summary>
public class DeleteQueryBuilder
{
    private readonly string _tableName;
    private readonly string? _library;
    private string? _whereClause;
    private string? _comment;

    /// <summary>
    /// Inicializa una nueva instancia de <see cref="DeleteQueryBuilder"/>.
    /// </summary>
    /// <param name="tableName">Nombre de la tabla.</param>
    /// <param name="library">Nombre de la librería o esquema (opcional).</param>
    public DeleteQueryBuilder(string tableName, string? library = null)
    {
        _tableName = tableName;
        _library = library;
    }

    /// <summary>
    /// Agrega un comentario SQL al inicio de la consulta DELETE.
    /// </summary>
    /// <param name="comment">Comentario para trazabilidad.</param>
    /// <returns>Instancia modificada de <see cref="DeleteQueryBuilder"/>.</returns>
    public DeleteQueryBuilder WithComment(string comment)
    {
        if (!string.IsNullOrWhiteSpace(comment))
            _comment = $"-- {comment}";
        return this;
    }

    /// <summary>
    /// Agrega una condición WHERE a la sentencia DELETE.
    /// </summary>
    /// <param name="condition">Condición SQL en forma de string.</param>
    /// <returns>Instancia modificada de <see cref="DeleteQueryBuilder"/>.</returns>
    public DeleteQueryBuilder Where(string condition)
    {
        if (string.IsNullOrWhiteSpace(condition))
            return this;

        if (string.IsNullOrWhiteSpace(_whereClause))
            _whereClause = condition;
        else
            _whereClause += $" AND {condition}";

        return this;
    }

    /// <summary>
    /// Agrega una condición WHERE del tipo EXISTS con una subconsulta.
    /// </summary>
    /// <param name="subquery">Subconsulta que se evaluará con EXISTS.</param>
    /// <returns>Instancia modificada de <see cref="DeleteQueryBuilder"/>.</returns>
    public DeleteQueryBuilder WhereExists(Subquery subquery)
    {
        if (subquery != null && !string.IsNullOrWhiteSpace(subquery.Sql))
        {
            var clause = $"EXISTS ({subquery.Sql})";

            if (string.IsNullOrWhiteSpace(_whereClause))
                _whereClause = clause;
            else
                _whereClause += $" AND {clause}";
        }

        return this;
    }

    /// <summary>
    /// Agrega una condición WHERE del tipo NOT EXISTS con una subconsulta.
    /// </summary>
    /// <param name="subquery">Subconsulta que se evaluará con NOT EXISTS.</param>
    /// <returns>Instancia modificada de <see cref="DeleteQueryBuilder"/>.</returns>
    public DeleteQueryBuilder WhereNotExists(Subquery subquery)
    {
        if (subquery != null && !string.IsNullOrWhiteSpace(subquery.Sql))
        {
            var clause = $"NOT EXISTS ({subquery.Sql})";

            if (string.IsNullOrWhiteSpace(_whereClause))
                _whereClause = clause;
            else
                _whereClause += $" AND {clause}";
        }

        return this;
    }

    /// <summary>
    /// Construye y retorna la sentencia SQL DELETE.
    /// </summary>
    /// <returns>Instancia de <see cref="QueryResult"/> con la sentencia generada.</returns>
    /// <exception cref="InvalidOperationException">Si el nombre de la tabla no está definido.</exception>
    public QueryResult Build()
    {
        if (string.IsNullOrWhiteSpace(_tableName))
            throw new InvalidOperationException("Debe especificarse el nombre de la tabla para DELETE.");

        var sb = new StringBuilder();

        if (!string.IsNullOrWhiteSpace(_comment))
            sb.AppendLine(_comment);

        sb.Append("DELETE FROM ");
        if (!string.IsNullOrWhiteSpace(_library))
            sb.Append($"{_library}.");
        sb.Append(_tableName);

        if (!string.IsNullOrWhiteSpace(_whereClause))
            sb.Append($" WHERE {_whereClause}");

        return new QueryResult
        {
            Sql = sb.ToString()
        };
    }
}
