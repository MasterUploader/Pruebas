using System.Data;
using System.Data.Common;
using IBM.Data.DB2;
using RestUtilities.Connections.Interfaces;

namespace RestUtilities.Connections.Providers.Database
{
    /// <summary>
    /// Proveedor de conexión para AS400 utilizando IBM.Data.DB2.
    /// Soporta conexiones tradicionales y ejecución de comandos SQL.
    /// </summary>
    public class AS400ConnectionProvider : IDatabaseConnection, IDisposable
    {
        private readonly string _connectionString;
        private DB2Connection _connection;

        public AS400ConnectionProvider(string connectionString)
        {
            _connectionString = connectionString;
        }

        /// <summary>
        /// Abre la conexión al AS400 si no está abierta.
        /// </summary>
        public void Open()
        {
            if (_connection == null)
                _connection = new DB2Connection(_connectionString);

            if (_connection.State != ConnectionState.Open)
                _connection.Open();
        }

        /// <summary>
        /// Cierra la conexión si está abierta.
        /// </summary>
        public void Close()
        {
            if (_connection?.State == ConnectionState.Open)
                _connection.Close();
        }

        /// <summary>
        /// Verifica si la conexión está abierta y funcional.
        /// </summary>
        public bool IsConnected()
        {
            return _connection != null && _connection.State == ConnectionState.Open;
        }

        /// <summary>
        /// Retorna un DbCommand asociado a la conexión activa.
        /// Útil para ejecutar SQL directamente (SELECT, UPDATE, CALL, etc.).
        /// </summary>
        public DbCommand GetDbCommand()
        {
            // Asegura que la conexión esté lista
            if (_connection == null)
                _connection = new DB2Connection(_connectionString);

            if (_connection.State != ConnectionState.Open)
                _connection.Open();

            return _connection.CreateCommand();
        }

        public DbContext GetDbContext()
        {
            throw new NotImplementedException("Este proveedor no implementa DbContext");
        }

        public void Dispose()
        {
            Close();
            _connection?.Dispose();
        }
    }
}
