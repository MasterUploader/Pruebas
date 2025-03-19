namespace RestUtilities.Connections.Models
{
    /// <summary>
    /// Configuración para conexiones Redis.
    /// </summary>
    public class RedisSettings
    {
        /// <summary>
        /// Dirección del servidor Redis.
        /// </summary>
        public string Host { get; set; }

        /// <summary>
        /// Puerto del servidor Redis.
        /// </summary>
        public int Port { get; set; }

        /// <summary>
        /// Contraseña para autenticación en Redis.
        /// </summary>
        public string Password { get; set; }

        /// <summary>
        /// Base de datos a utilizar en Redis.
        /// </summary>
        public int Database { get; set; }
    }
}
