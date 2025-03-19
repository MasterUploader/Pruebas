using System;
using System.Data.Common;
using System.Threading.Tasks;
using Oracle.ManagedDataAccess.Client;
using RestUtilities.Connections.Interfaces;

namespace RestUtilities.Connections.Providers.Database
{
    /// <summary>
    /// Proveedor de conexión para Oracle Database.
    /// </summary>
    public class OracleConnectionProvider : IDatabaseConnection
    {
        private readonly string _connectionString;
        private OracleConnection _connection;

        public OracleConnectionProvider(string connectionString)
        {
            _connectionString = connectionString;
            _connection = new OracleConnection(_connectionString);
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
            using var command = new OracleCommand(query, _connection);
            return await command.ExecuteReaderAsync();
        }

        public async Task<int> ExecuteNonQueryAsync(string query)
        {
            await OpenAsync();
            using var command = new OracleCommand(query, _connection);
            return await command.ExecuteNonQueryAsync();
        }

        public void Dispose()
        {
            Close();
            _connection.Dispose();
        }
    }
}
