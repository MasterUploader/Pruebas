using Microsoft.Extensions.Configuration;
using RestUtilities.Connections.Services;

namespace RestUtilities.Connections.Helpers
{
    /// <summary>
    /// Permite acceder y modificar dinámicamente los parámetros del archivo Connection.json
    /// desde cualquier parte de la aplicación.
    /// </summary>
    public static class ConnectionManagerHelper
    {
        /// <summary>
        /// Asume que la configuración global fue cargada en Program.cs
        /// y está disponible mediante esta propiedad.
        /// </summary>
        public static ConnectionSettings ConnectionConfig { get; set; }

        /// <summary>
        /// Obtiene un valor de la configuración de conexión de un servidor específico.
        /// </summary>
        public static string? GetValue(string connectionName, string key)
        {
            return ConnectionConfig?
                .CurrentEnvironment
                .GetSection("ConnectionSettings")
                .GetSection(connectionName)?[key];
        }

        /// <summary>
        /// Establece (modifica en memoria) un valor de configuración para un servidor específico.
        /// </summary>
        public static void SetValue(string connectionName, string key, string newValue)
        {
            var section = ConnectionConfig?
                .CurrentEnvironment
                .GetSection("ConnectionSettings")
                .GetSection(connectionName);

            if (section != null)
            {
                section[key] = newValue;
            }
        }

        /// <summary>
        /// Obtiene toda la sección de un servidor específico como IConfigurationSection.
        /// </summary>
        public static IConfigurationSection? GetConnectionSection(string connectionName)
        {
            return ConnectionConfig?
                .CurrentEnvironment
                .GetSection("ConnectionSettings")
                .GetSection(connectionName);
        }
    }
}
