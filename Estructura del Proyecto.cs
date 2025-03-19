namespace RestUtilities.Connections.Models
{
    /// <summary>
    /// Representa la información general de una conexión.
    /// </summary>
    public class ConnectionInfo
    {
        /// <summary>
        /// Nombre de la conexión.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Tipo de conexión (Ejemplo: "MSSQL", "AS400", "Redis", "RabbitMQ").
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// Cadena de conexión completa si aplica.
        /// </summary>
        public string ConnectionString { get; set; }
    }
}






namespace RestUtilities.Connections.Models
{
    /// <summary>
    /// Configuración específica para conexiones a bases de datos relacionales.
    /// </summary>
    public class DatabaseSettings
    {
        /// <summary>
        /// Dirección del servidor de base de datos.
        /// </summary>
        public string Host { get; set; }

        /// <summary>
        /// Nombre de la base de datos.
        /// </summary>
        public string DatabaseName { get; set; }

        /// <summary>
        /// Nombre de usuario para autenticación.
        /// </summary>
        public string Username { get; set; }

        /// <summary>
        /// Contraseña de acceso a la base de datos.
        /// </summary>
        public string Password { get; set; }

        /// <summary>
        /// Indica si la conexión debe utilizar SSL.
        /// </summary>
        public bool UseSSL { get; set; }
    }
}
