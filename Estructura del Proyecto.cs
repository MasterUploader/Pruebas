using QueryBuilder.Interfaces;
using System;
using System.Linq.Expressions;

namespace QueryBuilder.Engines
{
    /// <summary>
    /// Motor de generación SQL específico para SQL Server.
    /// Implementa lógica para construir sentencias SQL basadas en modelos genéricos.
    /// </summary>
    public class SqlServerEngine : ISqlEngine
    {
        /// <inheritdoc />
        public string GenerateSelectQuery<TModel>(Expression<Func<TModel, bool>>? filter = null)
        {
            // Esto es un ejemplo básico. Deberías reemplazar con generación dinámica basada en el modelo.
            string tableName = typeof(TModel).Name;
            string whereClause = filter != null ? "-- WHERE (filtro dinámico aún no implementado)" : string.Empty;

            return $"SELECT * FROM {tableName} {whereClause}";
        }

        /// <inheritdoc />
        public string GenerateInsertQuery<TModel>(TModel insertValues)
        {
            // Esto es un ejemplo estático. Aquí normalmente usarías reflexión para construir el SQL dinámico.
            string tableName = typeof(TModel).Name;
            return $"-- Generar INSERT dinámico para {tableName}";
        }

        /// <inheritdoc />
        public string GenerateUpdateQuery<TModel>(TModel updateValues, Expression<Func<TModel, bool>> filter)
        {
            // Esto es un ejemplo estático. Aquí normalmente usarías reflexión para construir el SQL dinámico.
            string tableName = typeof(TModel).Name;
            return $"-- Generar UPDATE dinámico para {tableName} con filtro";
        }

        /// <inheritdoc />
        public string GenerateMetadataQuery(string tableName)
        {
            // Esta consulta obtiene las columnas de la tabla en SQL Server.
            return $@"
                SELECT COLUMN_NAME, DATA_TYPE, IS_NULLABLE
                FROM INFORMATION_SCHEMA.COLUMNS
                WHERE TABLE_NAME = '{tableName.Replace("'", "''")}'
            ";
        }
    }
}
