using System;
using System.Data.Common;
using System.Threading.Tasks;
using IBM.Data.DB2.Core;
using RestUtilities.Connections.Interfaces;

namespace RestUtilities.Connections.Providers.Database
{
    /// <summary>
    /// Proveedor de conexión para AS400, permitiendo consultas SQL y ejecución de CLLE/RPG.
    /// </summary>
    public class AS400ConnectionProvider : IDatabaseConnection
    {
        private readonly string _connectionString;
        private DB2Connection _connection;

        public AS400ConnectionProvider(string connectionString)
        {
            _connectionString = connectionString;
            _connection = new DB2Connection(_connectionString);
        }

        public async Task OpenAsync()
        {
            if (_connection.State != System.Data.ConnectionState.Open)
                await _connection.OpenAsync();
        }

        public void Close()
        {
            if (_connection.State == System.Data.ConnectionState.Open)
                _connection.Close();
        }

        public async Task<DbDataReader> ExecuteQueryAsync(string query)
        {
            await OpenAsync();
            using var command = new DB2Command(query, _connection);
            return await command.ExecuteReaderAsync();
        }

        public async Task<int> ExecuteNonQueryAsync(string query)
        {
            await OpenAsync();
            using var command = new DB2Command(query, _connection);
            return await command.ExecuteNonQueryAsync();
        }

        public void Dispose()
        {
            Close();
            _connection.Dispose();
        }
    }
}
