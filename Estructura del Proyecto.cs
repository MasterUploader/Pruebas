using System;
using System.Linq.Expressions;

namespace QueryBuilder.Services;

/// <summary>
/// Interfaz que define métodos para construir sentencias SQL dinámicas a partir de modelos de datos.
/// </summary>
public interface IQueryBuilderService
{
    /// <summary>
    /// Genera una sentencia SQL SELECT basada en el tipo del modelo y una expresión de filtro opcional.
    /// </summary>
    /// <typeparam name="TModel">Tipo del modelo que representa la tabla.</typeparam>
    /// <param name="filter">Expresión lambda que representa los criterios de filtrado.</param>
    /// <returns>Cadena con la sentencia SQL SELECT generada.</returns>
    string BuildSelectQuery<TModel>(Expression<Func<TModel, bool>>? filter = null);

    /// <summary>
    /// Genera una sentencia SQL INSERT basada en un objeto con los valores a insertar.
    /// </summary>
    /// <typeparam name="TModel">Tipo del modelo que representa la tabla.</typeparam>
    /// <param name="insertValues">Objeto con las propiedades y valores a insertar.</param>
    /// <returns>Cadena con la sentencia SQL INSERT generada.</returns>
    string BuildInsertQuery<TModel>(TModel insertValues);

    /// <summary>
    /// Genera una sentencia SQL UPDATE basada en un objeto con los nuevos valores y una expresión de filtro.
    /// </summary>
    /// <typeparam name="TModel">Tipo del modelo que representa la tabla.</typeparam>
    /// <param name="updateValues">Objeto con las propiedades y nuevos valores.</param>
    /// <param name="filter">Expresión lambda que representa los criterios WHERE de actualización.</param>
    /// <returns>Cadena con la sentencia SQL UPDATE generada.</returns>
    string BuildUpdateQuery<TModel>(TModel updateValues, Expression<Func<TModel, bool>> filter);
}

using QueryBuilder.Engines;
using QueryBuilder.Services;
using System;
using System.Linq.Expressions;

namespace QueryBuilder.Services;

/// <summary>
/// Servicio que implementa la lógica para construir sentencias SQL basadas en modelos de datos.
/// </summary>
public class QueryBuilderService : IQueryBuilderService
{
    private readonly ISqlEngine _sqlEngine;

    /// <summary>
    /// Inicializa una nueva instancia del servicio con el motor SQL especificado.
    /// </summary>
    /// <param name="sqlEngine">Motor SQL que define la lógica específica según el proveedor (SQL Server, Oracle, AS400, etc.).</param>
    public QueryBuilderService(ISqlEngine sqlEngine)
    {
        _sqlEngine = sqlEngine;
    }

    /// <inheritdoc />
    public string BuildSelectQuery<TModel>(Expression<Func<TModel, bool>>? filter = null)
    {
        return _sqlEngine.GenerateSelectQuery(filter);
    }

    /// <inheritdoc />
    public string BuildInsertQuery<TModel>(TModel insertValues)
    {
        return _sqlEngine.GenerateInsertQuery(insertValues);
    }

    /// <inheritdoc />
    public string BuildUpdateQuery<TModel>(TModel updateValues, Expression<Func<TModel, bool>> filter)
    {
        return _sqlEngine.GenerateUpdateQuery(updateValues, filter);
    }
}
