using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RestUtilities.QueryBuilder.DbContextSupport
{
    /// <summary>
    /// Servicio encargado de ejecutar consultas SQL generadas usando una instancia de DbContext.
    /// </summary>
    public class DbContextQueryExecutor
    {
        private readonly DbContext _context;

        /// <summary>
        /// Inicializa una nueva instancia del ejecutor con el contexto especificado.
        /// </summary>
        /// <param name="context">Instancia activa de DbContext.</param>
        public DbContextQueryExecutor(DbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Ejecuta una consulta SELECT usando FromSqlRaw y devuelve una lista de resultados tipados.
        /// </summary>
        /// <typeparam name="T">Tipo de entidad o DTO esperado como resultado.</typeparam>
        /// <param name="sql">Cadena SQL generada dinámicamente.</param>
        /// <param name="parameters">Parámetros opcionales para la consulta.</param>
        /// <returns>Lista de resultados de tipo T.</returns>
        public async Task<List<T>> ExecuteQueryAsync<T>(string sql, params object[]? parameters) where T : class
        {
            return await _context.Set<T>()
                                 .FromSqlRaw(sql, parameters ?? Array.Empty<object>())
                                 .ToListAsync();
        }

        /// <summary>
        /// Ejecuta una instrucción SQL de modificación (INSERT, UPDATE o DELETE).
        /// </summary>
        /// <param name="sql">Cadena SQL generada dinámicamente.</param>
        /// <param name="parameters">Parámetros opcionales.</param>
        /// <returns>Número de filas afectadas.</returns>
        public async Task<int> ExecuteCommandAsync(string sql, params object[]? parameters)
        {
            return await _context.Database.ExecuteSqlRawAsync(sql, parameters ?? Array.Empty<object>());
        }
    }
}


using Microsoft.EntityFrameworkCore;
using RestUtilities.QueryBuilder.Models;
using RestUtilities.QueryBuilder.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RestUtilities.QueryBuilder.DbContextSupport
{
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
            var sql = _queryService.BuildSelect<TModel>(filter);
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
            var sql = _queryService.BuildUpdate<TModel>(updateValues, filter);
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
            var sql = _queryService.BuildInsert<TModel>(insertValues);
            return await _executor.ExecuteCommandAsync(sql);
        }
    }
}
