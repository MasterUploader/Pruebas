using QueryBuilder.Interfaces;
using System;
using System.Linq.Expressions;

namespace QueryBuilder.Services;

/// <summary>
/// Servicio que facilita la construcción de consultas SQL (SELECT, INSERT, UPDATE)
/// a partir de modelos genéricos usando un motor de generación inyectado.
/// </summary>
public class SqlQueryService
{
    private readonly IQueryBuilderService _queryBuilder;

    /// <summary>
    /// Constructor que recibe una instancia de <see cref="IQueryBuilderService"/>.
    /// </summary>
    /// <param name="queryBuilder">Instancia del generador de consultas SQL.</param>
    public SqlQueryService(IQueryBuilderService queryBuilder)
    {
        _queryBuilder = queryBuilder;
    }

    /// <summary>
    /// Construye una consulta SQL de tipo SELECT basada en un modelo y una expresión de filtro.
    /// </summary>
    /// <typeparam name="TModel">Tipo del modelo definido por el usuario que representa la tabla SQL.</typeparam>
    /// <param name="filter">Expresión lambda que representa los criterios WHERE de la consulta.</param>
    /// <returns>Cadena con la consulta SQL SELECT generada (SELECT * FROM tabla)</returns>
    public string BuildSelectQuery<TModel>(Expression<Func<TModel, bool>>? filter = null)
    {
        return _queryBuilder.BuildSelectQuery(filter);
    }

    /// <summary>
    /// Construye una consulta SQL de tipo INSERT basada en los valores proporcionados.
    /// </summary>
    /// <typeparam name="TModel">Tipo del modelo definido por el usuario que representa la tabla SQL.</typeparam>
    /// <param name="insertValues">Objeto que contiene los valores a insertar en la tabla.</param>
    /// <returns>Cadena con la consulta SQL INSERT generada.</returns>
    public string BuildInsertQuery<TModel>(TModel insertValues)
    {
        return _queryBuilder.BuildInsertQuery(insertValues);
    }

    /// <summary>
    /// Construye una consulta SQL de tipo UPDATE basada en los valores a actualizar y el filtro de selección.
    /// </summary>
    /// <typeparam name="TModel">Tipo del modelo definido por el usuario que representa la tabla SQL.</typeparam>
    /// <param name="updateValues">Objeto con las propiedades y nuevos valores que serán actualizados.</param>
    /// <param name="filter">Expresión lambda que representa los criterios WHERE de la actualización.</param>
    /// <returns>Cadena con la consulta SQL UPDATE generada.</returns>
    public string BuildUpdateQuery<TModel>(TModel updateValues, Expression<Func<TModel, bool>> filter)
    {
        return _queryBuilder.BuildUpdateQuery(updateValues, filter);
    }
}
