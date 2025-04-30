using System;
using System.Data;
using System.Data.Common;
using System.Data.OleDb;
using RestUtilities.Connections.Interfaces;

namespace RestUtilities.Connections.Providers.Database
{
    /// <summary>
    /// Proveedor de conexión para AS400 usando únicamente OleDbCommand.
    /// No utiliza DbContext ni Entity Framework.
    /// </summary>
    public class AS400ConnectionProvider : IDatabaseConnection
    {
        private readonly string _connectionString;
        private OleDbConnection _oleDbConnection;

        /// <summary>
        /// Constructor que recibe la cadena de conexión desde Connection.json.
        /// </summary>
        /// <param name="connectionString">Cadena de conexión ya desencriptada</param>
        public AS400ConnectionProvider(string connectionString)
        {
            _connectionString = connectionString;
        }

        /// <summary>
        /// Abre la conexión OleDb si aún no está abierta.
        /// </summary>
        public void Open()
        {
            if (_oleDbConnection == null)
                _oleDbConnection = new OleDbConnection(_connectionString);

            if (_oleDbConnection.State != ConnectionState.Open)
                _oleDbConnection.Open();
        }

        /// <summary>
        /// Cierra y limpia la conexión si está activa.
        /// </summary>
        public void Close()
        {
            if (_oleDbConnection?.State == ConnectionState.Open)
                _oleDbConnection.Close();
        }

        /// <summary>
        /// Verifica si la conexión está actualmente abierta y operativa.
        /// </summary>
        public bool IsConnected()
        {
            return _oleDbConnection?.State == ConnectionState.Open;
        }

        /// <summary>
        /// Retorna un OleDbCommand para ejecutar SQL directamente en AS400.
        /// </summary>
        public DbCommand GetDbCommand()
        {
            if (_oleDbConnection == null)
                _oleDbConnection = new OleDbConnection(_connectionString);

            if (_oleDbConnection.State != ConnectionState.Open)
                _oleDbConnection.Open();

            return _oleDbConnection.CreateCommand();
        }

        /// <summary>
        /// No implementado porque no se usa EF Core.
        /// </summary>
        public DbContext GetDbContext()
        {
            throw new NotSupportedException("Este proveedor no soporta DbContext. Usa GetDbCommand().");
        }

        /// <summary>
        /// Libera la conexión OleDb.
        /// </summary>
        public void Dispose()
        {
            Close();
            _oleDbConnection?.Dispose();
        }
    }
}
