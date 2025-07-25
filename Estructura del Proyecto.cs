using QueryBuilder.Converters;
using QueryBuilder.Helpers;
using QueryBuilder.Models;
using System;
using System.Linq.Expressions;
using System.Text;

namespace QueryBuilder.Builders;

/// <summary>
/// Generador de sentencias DELETE compatibles con múltiples motores como AS400, SQL Server, etc.
/// </summary>
public class DeleteQueryBuilder
{
    private readonly string _tableName;
    private readonly string? _library;
    private string? _whereClause;
    private string? _comment;

    /// <summary>
    /// Inicializa una nueva instancia del generador DELETE.
    /// </summary>
    /// <param name="tableName">Nombre de la tabla sobre la que se ejecutará el DELETE.</param>
    /// <param name="library">Nombre de la biblioteca (opcional, usado por ejemplo en AS400).</param>
    public DeleteQueryBuilder(string tableName, string? library = null)
    {
        _tableName = tableName;
        _library = library;
    }

    /// <summary>
    /// Agrega un comentario SQL al inicio de la consulta para trazabilidad.
    /// </summary>
    /// <param name="comment">Comentario a incluir.</param>
    public DeleteQueryBuilder WithComment(string comment)
    {
        if (!string.IsNullOrWhiteSpace(comment))
            _comment = $"-- {comment}";
        return this;
    }

    /// <summary>
    /// Agrega una cláusula WHERE con condición en formato SQL crudo.
    /// </summary>
    /// <param name="sqlWhere">Condición WHERE en formato de texto.</param>
    public DeleteQueryBuilder Where(string sqlWhere)
    {
        _whereClause = sqlWhere;
        return this;
    }

    /// <summary>
    /// Agrega una cláusula WHERE utilizando una expresión lambda tipada.
    /// </summary>
    /// <typeparam name="T">Tipo de entidad utilizada en la expresión.</typeparam>
    /// <param name="expression">Expresión lambda que representa la condición.</param>
    public DeleteQueryBuilder Where<T>(Expression<Func<T, bool>> expression)
    {
        _whereClause = ExpressionToSqlConverter.Convert(expression);
        return this;
    }

    /// <summary>
    /// Construye la consulta DELETE y la encapsula en un objeto <see cref="QueryResult"/>.
    /// </summary>
    /// <returns>Objeto con el SQL generado y parámetros asociados.</returns>
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

        if (!string.IsNullOrWhiteSpace(_whereClause))
            sb.Append(" WHERE ").Append(_whereClause);

        return new QueryResult
        {
            Sql = sb.ToString()
        };
    }
}
