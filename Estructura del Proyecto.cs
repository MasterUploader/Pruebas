using RestUtilities.QueryBuilder.Builders;
using RestUtilities.QueryBuilder.Models;

namespace RestUtilities.QueryBuilder.Services
{
    /// <summary>
    /// Servicio encargado de construir consultas SQL dinámicas utilizando modelos definidos por el usuario.
    /// Este servicio actúa como una fachada para los distintos tipos de operaciones (SELECT, INSERT, UPDATE),
    /// delegando la construcción real al servicio `IQueryBuilderService`.
    /// </summary>
    public class SqlQueryService
    {
        private readonly IQueryBuilderService _queryBuilder;

        /// <summary>
        /// Inicializa una nueva instancia de la clase <see cref="SqlQueryService"/>.
        /// </summary>
        /// <param name="queryBuilder">Instancia de <see cref="IQueryBuilderService"/> utilizada para construir las consultas SQL.</param>
        public SqlQueryService(IQueryBuilderService queryBuilder)
        {
            _queryBuilder = queryBuilder;
        }

        /// <summary>
        /// Construye una consulta SQL de tipo SELECT basada en el modelo y un filtro opcional.
        /// </summary>
        /// <typeparam name="TModel">Tipo del modelo definido por el usuario que representa la tabla SQL.</typeparam>
        /// <param name="filter">
        /// Expresión lambda que representa los criterios WHERE de la consulta.
        /// Si es <c>null</c>, se generará una consulta sin filtro (SELECT * FROM Tabla).
        /// </param>
        /// <returns>Cadena con la consulta SQL SELECT generada.</returns>
        public string BuildSelect<TModel>(Func<TModel, bool>? filter = null)
        {
            return _queryBuilder.BuildSelectQuery(filter);
        }

        /// <summary>
        /// Construye una consulta SQL de tipo INSERT basada en los valores proporcionados.
        /// </summary>
        /// <typeparam name="TModel">Tipo del modelo definido por el usuario que representa la tabla SQL.</typeparam>
        /// <param name="insertValues">Objeto que contiene los valores a insertar en la tabla.</param>
        /// <returns>Cadena con la consulta SQL INSERT generada.</returns>
        public string BuildInsert<TModel>(object insertValues)
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
        public string BuildUpdate<TModel>(object updateValues, Func<TModel, bool> filter)
        {
            return _queryBuilder.BuildUpdateQuery(updateValues, filter);
        }
    }
}
