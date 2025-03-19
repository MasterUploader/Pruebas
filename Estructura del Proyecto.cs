using System;
using System.Data.Common;
using System.Data.SqlClient;
using System.Threading.Tasks;
using RestUtilities.Connections.Interfaces;

namespace RestUtilities.Connections.Providers.Database
{
    /// <summary>
    /// Proveedor de conexión para Microsoft SQL Server.
    /// </summary>
    public class MSSQLConnectionProvider : IDatabaseConnection
    {
        private readonly string _connectionString;
        private SqlConnection _connection;

        public MSSQLConnectionProvider(string connectionString)
        {
            _connectionString = connectionString;
            _connection = new SqlConnection(_connectionString);
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
            using var command = new SqlCommand(query, _connection);
            return await command.ExecuteReaderAsync();
        }

        public async Task<int> ExecuteNonQueryAsync(string query)
        {
            await OpenAsync();
            using var command = new SqlCommand(query, _connection);
            return await command.ExecuteNonQueryAsync();
        }

        public void Dispose()
        {
            Close();
            _connection.Dispose();
        }
    }
}
