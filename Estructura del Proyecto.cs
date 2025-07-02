using RestUtilities.QueryBuilder.Interfaces;
using RestUtilities.QueryBuilder.Models;
using System.Collections.Generic;

namespace RestUtilities.QueryBuilder.Services
{
    /// <summary>
    /// Servicio principal para la construcción de consultas SQL.
    /// Expone métodos para generar dinámicamente sentencias SELECT, INSERT, UPDATE y DELETE.
    /// </summary>
    public class QueryBuilderService : IQueryBuilderService
    {
        private readonly ISqlDialectService _dialect;

        /// <summary>
        /// Inicializa una nueva instancia de QueryBuilderService con un dialecto específico.
        /// </summary>
        /// <param name="dialect">Servicio de dialecto SQL para el motor correspondiente.</param>
        public QueryBuilderService(ISqlDialectService dialect)
        {
            _dialect = dialect;
        }

        /// <inheritdoc/>
        public QueryMetadata BuildSelect<T>(T criteria) where T : class
        {
            return _dialect.BuildSelect(criteria);
        }

        /// <inheritdoc/>
        public QueryMetadata BuildInsert<T>(T entity) where T : class
        {
            return _dialect.BuildInsert(entity);
        }

        /// <inheritdoc/>
        public QueryMetadata BuildUpdate<T>(T entity, object keys) where T : class
        {
            return _dialect.BuildUpdate(entity, keys);
        }

        /// <inheritdoc/>
        public QueryMetadata BuildDelete<T>(object keys) where T : class
        {
            return _dialect.BuildDelete<T>(keys);
        }
    }
}

using RestUtilities.QueryBuilder.Interfaces;
using RestUtilities.QueryBuilder.Models;

namespace RestUtilities.QueryBuilder.Services
{
    /// <summary>
    /// Clase base para los servicios de dialecto SQL por motor.
    /// Define la estructura común para crear consultas dinámicamente.
    /// </summary>
    public abstract class SqlDialectServiceBase : ISqlDialectService
    {
        public abstract QueryMetadata BuildSelect<T>(T criteria) where T : class;
        public abstract QueryMetadata BuildInsert<T>(T entity) where T : class;
        public abstract QueryMetadata BuildUpdate<T>(T entity, object keys) where T : class;
        public abstract QueryMetadata BuildDelete<T>(object keys) where T : class;
    }
}

using RestUtilities.QueryBuilder.Models;

namespace RestUtilities.QueryBuilder.Services
{
    /// <summary>
    /// Implementación del dialecto SQL para AS400 (DB2 for i).
    /// Genera queries compatibles con la sintaxis específica de este motor.
    /// </summary>
    public class As400DialectService : SqlDialectServiceBase
    {
        public override QueryMetadata BuildSelect<T>(T criteria)
        {
            // Implementación simplificada inicial.
            return new QueryMetadata
            {
                Sql = $"SELECT * FROM {typeof(T).Name}" // Ejemplo simple, reemplazable por lógica de reflexión avanzada
            };
        }

        public override QueryMetadata BuildInsert<T>(T entity)
        {
            // Generar INSERT dinámico a partir del objeto
            return new QueryMetadata
            {
                Sql = $"INSERT INTO {typeof(T).Name} (...) VALUES (...)"
            };
        }

        public override QueryMetadata BuildUpdate<T>(T entity, object keys)
        {
            return new QueryMetadata
            {
                Sql = $"UPDATE {typeof(T).Name} SET ... WHERE ..."
            };
        }

        public override QueryMetadata BuildDelete<T>(object keys)
        {
            return new QueryMetadata
            {
                Sql = $"DELETE FROM {typeof(T).Name} WHERE ..."
            };
        }
    }
}
