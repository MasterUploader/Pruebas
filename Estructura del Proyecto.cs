namespace RestUtilities.Connections.Interfaces
{
    /// <summary>
    /// Contrato para manejar una conexión a base de datos, incluyendo soporte para DbContext.
    /// </summary>
    public interface IDatabaseConnection
    {
        /// <summary>
        /// Abre la conexión, si aplica.
        /// </summary>
        void Open();

        /// <summary>
        /// Cierra la conexión, si aplica.
        /// </summary>
        void Close();

        /// <summary>
        /// Devuelve el DbContext asociado a la conexión.
        /// </summary>
        DbContext GetDbContext();
    }
}



using Microsoft.EntityFrameworkCore;
using RestUtilities.Connections.Interfaces;

namespace RestUtilities.Connections.Providers.Database
{
    /// <summary>
    /// Proveedor de conexión para usar un DbContext externo gestionado por el consumidor del paquete.
    /// </summary>
    /// <typeparam name="TContext">Tipo del DbContext definido externamente.</typeparam>
    public class ExternalDbContextConnectionProvider<TContext> : IDatabaseConnection
        where TContext : DbContext
    {
        private readonly TContext _dbContext;

        public ExternalDbContextConnectionProvider(TContext dbContext)
        {
            _dbContext = dbContext;
        }

        public void Open()
        {
            // No se requiere acción al usar un DbContext externo
        }

        public void Close()
        {
            _dbContext?.Dispose();
        }

        public DbContext GetDbContext()
        {
            return _dbContext;
        }
    }
}
