using Microsoft.EntityFrameworkCore;
using System;

namespace RestUtilities.Connections.Interfaces
{
    /// <summary>
    /// Define los métodos que debe implementar un proveedor de conexión a bases de datos.
    /// </summary>
    public interface IDatabaseConnection : IDisposable
    {
        void Open();
        void Close();
        DbContext GetDbContext();
    }
}



using IBM.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using RestUtilities.Connections.Interfaces;

namespace RestUtilities.Connections.Providers.Database
{
    /// <summary>
    /// Proveedor de conexión a AS400 mediante Entity Framework Core.
    /// </summary>
    public class AS400ConnectionProvider : IDatabaseConnection
    {
        private readonly DbContextOptionsBuilder _optionsBuilder;
        private DbContext _dbContext;

        public AS400ConnectionProvider(string connectionString)
        {
            _optionsBuilder = new DbContextOptionsBuilder()
                .UseDb2(connectionString, p => p.SetServerInfo(IBMDBServerType.AS400));
        }

        /// <summary>
        /// Abre la conexión a AS400.
        /// </summary>
        public void Open()
        {
            _dbContext = new DbContext(_optionsBuilder.Options);
            _dbContext.Database.OpenConnection();
        }

        /// <summary>
        /// Cierra la conexión a AS400.
        /// </summary>
        public void Close()
        {
            _dbContext?.Database.CloseConnection();
        }

        /// <summary>
        /// Obtiene el DbContext para que la API lo use según su necesidad.
        /// </summary>
        public DbContext GetDbContext()
        {
            return _dbContext;
        }

        /// <summary>
        /// Libera los recursos de la conexión.
        /// </summary>
        public void Dispose()
        {
            _dbContext?.Dispose();
        }
    }
}
