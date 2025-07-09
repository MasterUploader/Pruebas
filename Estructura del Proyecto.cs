using QueryBuilder.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace QueryBuilder.DbContextSupport;

/// <summary>
/// Adaptador que combina la generación de consultas con su ejecución a través de Entity Framework.
/// </summary>
public class DbContextQueryBuilderAdapter
{
    private readonly SqlQueryService _queryService;
    private readonly DbContextQueryExecutor _executor;

    /// <summary>
    /// Inicializa una nueva instancia del adaptador para DbContext.
    /// </summary>
    /// <param name="queryService">Servicio que construye sentencias SQL.</param>
    /// <param name="executor">Ejecutor que utiliza DbContext para ejecutar las consultas.</param>
    public DbContextQueryBuilderAdapter(SqlQueryService queryService, DbContextQueryExecutor executor)
    {
        _queryService = queryService;
        _executor = executor;
    }

    /// <summary>
    /// Genera y ejecuta una consulta SELECT basada en un modelo definido por el usuario.
    /// </summary>
    /// <typeparam name="TModel">Modelo que representa la tabla base de datos.</typeparam>
    /// <typeparam name="TResult">Tipo esperado del resultado (entidad o DTO).</typeparam>
    /// <param name="filter">Filtro expresado como predicado.</param>
    /// <returns>Lista de resultados obtenidos desde la base de datos.</returns>
    public async Task<List<TResult>> SelectAsync<TModel, TResult>(Func<TModel, bool> filter)
        where TModel : class, new()
        where TResult : class
    {
        var sql = _queryService.BuildSelectQuery<TModel>(filter);
        return await _executor.ExecuteQueryAsync<TResult>(sql);
    }

    /// <summary>
    /// Ejecuta una consulta de actualización generada dinámicamente para el modelo proporcionado.
    /// </summary>
    /// <typeparam name="TModel">Modelo fuente.</typeparam>
    /// <param name="updateValues">Objeto con los valores a actualizar.</param>
    /// <param name="filter">Condición de filtro.</param>
    /// <returns>Cantidad de filas afectadas.</returns>
    public async Task<int> UpdateAsync<TModel>(object updateValues, Func<TModel, bool> filter)
        where TModel : class, new()
    {
        var sql = _queryService.BuildUpdateQuery<TModel>((TModel)updateValues, filter);
        return await _executor.ExecuteCommandAsync(sql);
    }

    /// <summary>
    /// Ejecuta una inserción de datos basada en el modelo proporcionado.
    /// </summary>
    /// <typeparam name="TModel">Modelo base.</typeparam>
    /// <param name="insertValues">Objeto con los valores a insertar.</param>
    /// <returns>Cantidad de filas insertadas.</returns>
    public async Task<int> InsertAsync<TModel>(object insertValues)
        where TModel : class, new()
    {
        var sql = _queryService.BuildInsertQuery<TModel>((TModel)insertValues);
        return await _executor.ExecuteCommandAsync(sql);
    }
}
