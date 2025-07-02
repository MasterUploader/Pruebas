using RestUtilities.QueryBuilder.Models;

namespace RestUtilities.QueryBuilder.Interfaces
{
    /// <summary>
    /// Define los métodos básicos para construir consultas SQL de manera dinámica.
    /// </summary>
    public interface IQueryBuilderService
    {
        /// <summary>
        /// Construye una sentencia SELECT basada en los filtros proporcionados.
        /// </summary>
        QueryMetadata BuildSelect<T>(T criteria) where T : class;

        /// <summary>
        /// Construye una sentencia INSERT basada en la entidad proporcionada.
        /// </summary>
        QueryMetadata BuildInsert<T>(T entity) where T : class;

        /// <summary>
        /// Construye una sentencia UPDATE con base en la entidad y sus claves primarias.
        /// </summary>
        QueryMetadata BuildUpdate<T>(T entity, object keys) where T : class;

        /// <summary>
        /// Construye una sentencia DELETE con base en las claves proporcionadas.
        /// </summary>
        QueryMetadata BuildDelete<T>(object keys) where T : class;
    }
}

using RestUtilities.QueryBuilder.Models;

namespace RestUtilities.QueryBuilder.Interfaces
{
    /// <summary>
    /// Define la interfaz que todo dialecto SQL debe implementar.
    /// </summary>
    public interface ISqlDialectService
    {
        QueryMetadata BuildSelect<T>(T criteria) where T : class;
        QueryMetadata BuildInsert<T>(T entity) where T : class;
        QueryMetadata BuildUpdate<T>(T entity, object keys) where T : class;
        QueryMetadata BuildDelete<T>(object keys) where T : class;
    }
}
