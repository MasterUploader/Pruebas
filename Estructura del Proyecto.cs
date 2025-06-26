using Microsoft.AspNetCore.Http;
using System.Data.Common;
using Connections.Interfaces;
using Logging.Commands;
using Logging.Helpers;

namespace Logging.Decorators
{
    /// <summary>
    /// Decorador que intercepta las llamadas a la conexión de base de datos para registrar automáticamente los comandos ejecutados.
    /// Compatible con todas las conexiones que implementen <see cref="IDatabaseConnection"/>.
    /// </summary>
    public class LoggingDatabaseConnectionDecorator : IDatabaseConnection
    {
        private readonly IDatabaseConnection _innerConnection;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly QueryExecutionLogger _queryLogger;

        /// <summary>
        /// Inicializa una nueva instancia de la clase <see cref="LoggingDatabaseConnectionDecorator"/>.
        /// </summary>
        /// <param name="innerConnection">Conexión original que se desea decorar.</param>
        /// <param name="httpContextAccessor">Contexto HTTP para capturar información del request actual.</param>
        /// <param name="queryLogger">Servicio de logging especializado para registrar las consultas.</param>
        /// <exception cref="ArgumentNullException">Si alguno de los parámetros es nulo.</exception>
        public LoggingDatabaseConnectionDecorator(
            IDatabaseConnection innerConnection,
            IHttpContextAccessor httpContextAccessor,
            QueryExecutionLogger queryLogger)
        {
            _innerConnection = innerConnection ?? throw new ArgumentNullException(nameof(innerConnection));
            _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
            _queryLogger = queryLogger ?? throw new ArgumentNullException(nameof(queryLogger));
        }

        /// <summary>
        /// Abre la conexión de base de datos subyacente.
        /// </summary>
        public void Open()
        {
            _innerConnection.Open();
        }

        /// <summary>
        /// Cierra la conexión de base de datos subyacente.
        /// </summary>
        public void Close()
        {
            _innerConnection.Close();
        }

        /// <summary>
        /// Indica si la conexión se encuentra actualmente abierta y operativa.
        /// </summary>
        /// <returns>True si está conectada, false si está cerrada o inactiva.</returns>
        public bool IsConnected()
        {
            return _innerConnection.IsConnected();
        }

        /// <summary>
        /// Obtiene un <see cref="DbCommand"/> decorado que incluye funcionalidad de logging automático.
        /// </summary>
        /// <returns>Una instancia de <see cref="DbCommand"/> envuelta en <see cref="LoggingDbCommand"/>.</returns>
        public DbCommand GetDbCommand()
        {
            var originalCommand = _innerConnection.GetDbCommand();
            return new LoggingDbCommand(originalCommand, _httpContextAccessor.HttpContext!, _queryLogger);
        }
    }
}
