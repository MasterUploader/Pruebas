using System;

namespace RestUtilities.Connections.Interfaces
{
    /// <summary>
    /// Define la gestión de conexiones en la aplicación.
    /// </summary>
    public interface IConnectionManager
    {
        /// <summary>
        /// Obtiene una conexión de base de datos según el nombre configurado.
        /// </summary>
        /// <param name="connectionName">Nombre de la conexión.</param>
        /// <returns>Instancia de `IDatabaseConnection`.</returns>
        IDatabaseConnection GetDatabaseConnection(string connectionName);

        /// <summary>
        /// Obtiene una conexión a un servicio externo según el nombre configurado.
        /// </summary>
        /// <param name="serviceName">Nombre del servicio.</param>
        /// <returns>Instancia de `IExternalServiceConnection`.</returns>
        IExternalServiceConnection GetServiceConnection(string serviceName);
    }
}
