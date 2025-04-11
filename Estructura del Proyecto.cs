using Microsoft.Extensions.Configuration;
using System.Text;

namespace RestUtilities.Connections
{
    /// <summary>
    /// Gestiona la configuración de conexiones según el ambiente actual.
    /// </summary>
    public class ConnectionSettings
    {
        private readonly IConfiguration _configuration;

        public ConnectionSettings(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        private IConfigurationSection CurrentEnvironment =>
            _configuration.GetSection(_configuration["ASPNETCORE_ENVIRONMENT"] ?? "DEV");

        /// <summary>
        /// Construye dinámicamente la cadena de conexión al AS400 desde campos individuales.
        /// </summary>
        /// <param name="connectionName">Nombre de la conexión (ej. "AS400").</param>
        public string GetAS400ConnectionString(string connectionName)
        {
            var section = CurrentEnvironment.GetSection("ConnectionSettings").GetSection(connectionName);

            string driver = section["DriverConnection"];
            string server = section["ServerName"];
            string userEncoded = section["User"];
            string passEncoded = section["Password"];

            // Decodifica Base64 o aplica tu propio descifrado aquí
            string user = Decode(userEncoded);
            string password = Decode(passEncoded);

            return $"Provider={driver};Data Source={server};User ID={user};Password={password};";
        }

        /// <summary>
        /// Obtiene configuración completa para un servicio externo.
        /// </summary>
        public IConfigurationSection GetServiceConfig(string serviceName)
        {
            return CurrentEnvironment.GetSection("Services").GetSection(serviceName);
        }

        private string Decode(string base64) =>
            Encoding.UTF8.GetString(Convert.FromBase64String(base64));
    }
}
