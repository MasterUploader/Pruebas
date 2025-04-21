using System;
using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using RestUtilities.Connections.Interfaces;

namespace RestUtilities.Connections.Providers.Database
{
    /// <summary>
    /// Adaptador para reutilizar un DbContext externo como una conexión estándar de la librería.
    /// Este proveedor no implementa comandos tradicionales (DbCommand).
    /// </summary>
    /// <typeparam name="TContext">Tipo del DbContext externo (por ejemplo, As400DbContext).</typeparam>
    public class ExternalDbContextConnectionProvider<TContext> : IDatabaseConnection
        where TContext : DbContext
    {
        private readonly TContext _dbContext;

        /// <summary>
        /// Constructor que recibe el contexto externo ya configurado.
        /// </summary>
        public ExternalDbContextConnectionProvider(TContext dbContext)
        {
            _dbContext = dbContext;
        }

        /// <summary>
        /// No realiza acción porque el DbContext ya está inicializado externamente.
        /// </summary>
        public void Open() { }

        /// <summary>
        /// Libera recursos asociados al contexto.
        /// </summary>
        public void Close()
        {
            _dbContext?.Dispose();
        }

        /// <summary>
        /// Verifica si el contexto puede conectarse al origen de datos.
        /// </summary>
        public bool IsConnected()
        {
            try
            {
                return _dbContext?.Database?.CanConnect() == true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Retorna el DbContext externo para uso con EF Core.
        /// </summary>
        public DbContext GetDbContext() => _dbContext;

        /// <summary>
        /// Este proveedor no soporta acceso por DbCommand.
        /// </summary>
        public DbCommand GetDbCommand()
        {
            throw new NotSupportedException("ExternalDbContextConnectionProvider no soporta comandos directos. Usa GetDbContext().");
        }

        /// <summary>
        /// Libera el DbContext externo.
        /// </summary>
        public void Dispose()
        {
            Close();
        }
    }
}
