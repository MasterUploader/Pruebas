using Connections.Interfaces;
using Connections.Providers.Database;
using Microsoft.AspNetCore.Http;
using QueryBuilder.Models;
using System.Data.Common;

namespace Connections.Extensions;

/// <summary>
/// Métodos de extensión para facilitar el uso de <see cref="IDatabaseConnection"/> con consultas generadas por QueryBuilder.
/// </summary>
public static class DatabaseConnectionExtensions
{
    /// <summary>
    /// Genera un <see cref="DbCommand"/> a partir de un <see cref="QueryResult"/> generado por QueryBuilder,
    /// asignando automáticamente el SQL y sus parámetros si el proveedor es compatible.
    /// </summary>
    /// <param name="connection">Instancia de <see cref="IDatabaseConnection"/>.</param>
    /// <param name="queryResult">Consulta SQL generada por QueryBuilder con parámetros.</param>
    /// <param name="context">Contexto HTTP actual, utilizado para trazabilidad y logging.</param>
    /// <returns>Comando listo para ejecutar.</returns>
    /// <exception cref="NotSupportedException">Se lanza si el tipo de conexión no soporta asignación automática desde QueryBuilder.</exception>
    public static DbCommand GetDbCommandFromQuery(this IDatabaseConnection connection, QueryResult queryResult, HttpContext context)
    {
        if (connection is AS400ConnectionProvider as400)
        {
            return as400.GetDbCommand(queryResult, context);
        }

        throw new NotSupportedException("Este proveedor de conexión no soporta asignación automática desde QueryBuilder.");
    }
}
