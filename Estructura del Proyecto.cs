using System;

namespace RestUtilities.QueryBuilder.Services
{
    /// <summary>
    /// Define los métodos esenciales que debe implementar un generador de consultas SQL.
    /// Proporciona métodos para construir consultas SELECT, INSERT y UPDATE de forma dinámica y segura.
    /// </summary>
    public interface IQueryBuilderService
    {
        /// <summary>
        /// Genera una consulta SQL SELECT basada en un modelo de datos y un filtro opcional.
        /// </summary>
        /// <typeparam name="TModel">El tipo del modelo que representa la tabla SQL.</typeparam>
        /// <param name="filter">
        /// Una expresión opcional que representa la condición WHERE de la consulta.
        /// Si se omite, se generará un SELECT sin filtro.
        /// </param>
        /// <returns>Una cadena con la consulta SQL SELECT generada.</returns>
        string BuildSelectQuery<TModel>(Func<TModel, bool>? filter = null);

        /// <summary>
        /// Genera una consulta SQL INSERT para insertar datos en una tabla basada en el modelo proporcionado.
        /// </summary>
        /// <typeparam name="TModel">El tipo del modelo que representa la tabla SQL.</typeparam>
        /// <param name="insertValues">
        /// Un objeto que contiene los valores a insertar. Las propiedades deben coincidir con las columnas definidas.
        /// </param>
        /// <returns>Una cadena con la consulta SQL INSERT generada.</returns>
        string BuildInsertQuery<TModel>(object insertValues);

        /// <summary>
        /// Genera una consulta SQL UPDATE para actualizar registros en una tabla basada en el modelo proporcionado.
        /// </summary>
        /// <typeparam name="TModel">El tipo del modelo que representa la tabla SQL.</typeparam>
        /// <param name="updateValues">
        /// Un objeto con las propiedades y nuevos valores que serán actualizados.
        /// </param>
        /// <param name="filter">
        /// Una expresión lambda que representa los criterios WHERE de la actualización.
        /// </param>
        /// <returns>Una cadena con la consulta SQL UPDATE generada.</returns>
        string BuildUpdateQuery<TModel>(object updateValues, Func<TModel, bool> filter);
    }
}
