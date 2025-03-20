using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using RestUtilities.Connections.Models;

namespace RestUtilities.Connections
{
    /// <summary>
    /// Clase para manejar la configuración de conexiones desde archivos JSON.
    /// </summary>
    public class ConnectionSettings
    {
        private readonly IConfiguration _configuration;

        /// <summary>
        /// Constructor que inyecta la configuración de la aplicación.
        /// </summary>
        public ConnectionSettings(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        /// <summary>
        /// Carga la configuración de conexiones desde `Connection.json` y la retorna como un diccionario.
        /// </summary>
        public Dictionary<string, ConnectionInfo> LoadConnections()
        {
            var connections = new Dictionary<string, ConnectionInfo>();

            var section = _configuration.GetSection("Connections");
            if (section.Exists())
            {
                section.Bind(connections);
            }

            return connections;
        }

        /// <summary>
        /// Obtiene la configuración de una conexión específica.
        /// </summary>
        public ConnectionInfo GetConnection(string name)
        {
            var connections = LoadConnections();
            return connections.TryGetValue(name, out var connection) ? connection : null;
        }
    }
}
