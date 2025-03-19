using System;
using Microsoft.Extensions.Configuration;
using RabbitMQ.Client;
using System.Data.SqlClient;
using StackExchange.Redis;

namespace RestUtilities.Connections.Providers.Services
{
    /// <summary>
    /// Fábrica de conexiones para múltiples servicios, incluyendo RabbitMQ, SQL Server y Redis.
    /// Permite la creación dinámica de conexiones según el tipo requerido.
    /// </summary>
    public class ServiceConnectionFactory : IServiceConnectionFactory
    {
        private readonly IConfiguration _configuration;

        /// <summary>
        /// Constructor que inyecta la configuración de la aplicación.
        /// </summary>
        /// <param name="configuration">Objeto de configuración para obtener cadenas de conexión.</param>
        public ServiceConnectionFactory(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        /// <summary>
        /// Crea una conexión a un servicio específico basado en el tipo genérico `T`.
        /// </summary>
        /// <typeparam name="T">Tipo de la conexión a crear (ejemplo: IConnection para RabbitMQ, SqlConnection para SQL Server).</typeparam>
        /// <returns>Una instancia de la conexión especificada o lanza una excepción si no es compatible.</returns>
        public T CreateConnection<T>() where T : class
        {
            // Conexión a RabbitMQ
            if (typeof(T) == typeof(IConnection))
            {
                var factory = new ConnectionFactory
                {
                    Uri = new Uri(_configuration.GetConnectionString("RabbitMQ")), // Obtiene la URI desde la configuración
                    AutomaticRecoveryEnabled = true // Habilita la recuperación automática de la conexión
                };
                return factory.CreateConnection() as T;
            }
            // Conexión a SQL Server
            else if (typeof(T) == typeof(SqlConnection))
            {
                var connectionString = _configuration.GetConnectionString("SqlServer");
                return new SqlConnection(connectionString) as T;
            }
            // Conexión a Redis
            else if (typeof(T) == typeof(ConnectionMultiplexer))
            {
                var configurationOptions = ConfigurationOptions.Parse(_configuration.GetConnectionString("Redis"));
                return ConnectionMultiplexer.Connect(configurationOptions) as T;
            }
            else
            {
                // Si el tipo de conexión solicitado no está soportado, lanza una excepción
                throw new NotSupportedException($"El tipo de conexión '{typeof(T).Name}' no es compatible.");
            }
        }
    }
}
