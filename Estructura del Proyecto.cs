using System;
using System.Collections.Generic;

namespace RestUtilities.QueryBuilder.Interfaces
{
    /// <summary>
    /// Define las operaciones principales que debe implementar un generador de consultas SQL.
    /// Permite construir sentencias SELECT, INSERT, UPDATE y DELETE de forma fluida.
    /// </summary>
    public interface IQueryBuilder
    {
        /// <summary>
        /// Define la tabla principal sobre la que se construirá la consulta.
        /// </summary>
        /// <param name="tableName">Nombre de la tabla.</param>
        /// <returns>Instancia fluida del generador.</returns>
        IQueryBuilder From(string tableName);

        /// <summary>
        /// Agrega una lista de columnas a seleccionar.
        /// </summary>
        /// <param name="columns">Nombres de columnas.</param>
        /// <returns>Instancia fluida del generador.</returns>
        IQueryBuilder Select(params string[] columns);

        /// <summary>
        /// Agrega condiciones a la cláusula WHERE.
        /// </summary>
        /// <param name="condition">Condición SQL como texto.</param>
        /// <returns>Instancia fluida del generador.</returns>
        IQueryBuilder Where(string condition);

        /// <summary>
        /// Agrega una cláusula ORDER BY a la consulta.
        /// </summary>
        /// <param name="column">Nombre de la columna.</param>
        /// <param name="direction">Dirección del ordenamiento.</param>
        /// <returns>Instancia fluida del generador.</returns>
        IQueryBuilder OrderBy(string column, Enums.SqlSortDirection direction);

        /// <summary>
        /// Establece un valor para OFFSET (paginación).
        /// </summary>
        /// <param name="offset">Cantidad de filas a omitir.</param>
        IQueryBuilder Offset(int offset);

        /// <summary>
        /// Establece un valor para FETCH NEXT (paginación).
        /// </summary>
        /// <param name="size">Cantidad de filas a obtener.</param>
        IQueryBuilder FetchNext(int size);

        /// <summary>
        /// Devuelve la consulta SQL generada como string.
        /// </summary>
        /// <returns>Consulta SQL completa.</returns>
        string Build();
    }
}

using System;
using System.Linq.Expressions;

namespace RestUtilities.QueryBuilder.Interfaces
{
    /// <summary>
    /// Proporciona una interfaz para traducir expresiones lambda a condiciones SQL.
    /// Es utilizada por la capa de expresión avanzada para facilitar el uso de expresiones tipadas.
    /// </summary>
    public interface ISqlExpressionEvaluator
    {
        /// <summary>
        /// Traduce una expresión lambda a su equivalente en SQL.
        /// </summary>
        /// <param name="expression">Expresión lambda.</param>
        /// <returns>Condición SQL generada.</returns>
        string Translate(Expression expression);
    }
}
namespace RestUtilities.QueryBuilder.Interfaces
{
    /// <summary>
    /// Define la interfaz que traduce sentencias SQL a su sintaxis específica
    /// dependiendo del motor de base de datos (AS400, SQL Server, Oracle, etc.).
    /// </summary>
    public interface ISqlEngineTranslator
    {
        /// <summary>
        /// Traduce la consulta SQL genérica a la sintaxis del motor objetivo.
        /// </summary>
        /// <param name="query">Consulta SQL generada.</param>
        /// <returns>Consulta adaptada al motor SQL.</returns>
        string TranslateEngineSpecific(string query);
    }
}
