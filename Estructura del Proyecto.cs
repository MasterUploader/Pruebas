using QueryBuilder.Interfaces;
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
    public string BuildInsertQuery<TModel>(object insertValues)
    {
        return _queryBuilder.BuildInsertQuery<TModel>(insertValues);
    }

    /// <summary>
    /// Construye una consulta SQL de tipo UPDATE basada en los valores a actualizar y el filtro de selección.
    /// </summary>
    /// <typeparam name="TModel">Tipo del modelo definido por el usuario que representa la tabla SQL.</typeparam>
    /// <param name="updateValues">Objeto con las propiedades y nuevos valores que serán actualizados.</param>
    /// <param name="filter">Expresión lambda que representa los criterios WHERE de la actualización.</param>
    /// <returns>Cadena con la consulta SQL UPDATE generada.</returns>
    public string BuildUpdateQuery<TModel>(object updateValues, Expression<Func<TModel, bool>> filter)
    {
        return _queryBuilder.BuildUpdateQuery<TModel>(updateValues, filter);
    }
}


using System.Linq.Expressions;

namespace QueryBuilder.Interfaces;

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



using QueryBuilder.Interfaces;
using QueryBuilder.Models;
using static QueryBuilder.Compatibility.SqlCompatibilityService;
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

namespace QueryBuilder.Interfaces;

/// <summary>
/// Contrato para definir métodos que generan sentencias SQL específicas para cada motor.
/// </summary>
public interface ISqlEngine
{
    string GenerateSelectQuery<TModel>(System.Linq.Expressions.Expression<System.Func<TModel, bool>>? filter = null);
    string GenerateInsertQuery<TModel>(TModel insertValues);
    string GenerateUpdateQuery<TModel>(TModel updateValues, System.Linq.Expressions.Expression<System.Func<TModel, bool>> filter);
}
