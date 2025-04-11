using RestUtilities.Connections.Interfaces;
using IBM.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace RestUtilities.Connections.Providers.Database
{
    /// <summary>
    /// Proveedor de conexión a AS400 que implementa IDatabaseConnection,
    /// utilizando IBM.EntityFrameworkCore para compatibilidad con .NET.
    /// </summary>
    public class AS400ConnectionProvider : IDatabaseConnection, IDisposable
    {
        private readonly string _connectionString;
        private DbContextOptions<AS400DbContext> _options;
        private AS400DbContext _context;

        /// <summary>
        /// Constructor que recibe la cadena de conexión al AS400.
        /// </summary>
        /// <param name="connectionString">Cadena de conexión generada dinámicamente.</param>
        public AS400ConnectionProvider(string connectionString)
        {
            _connectionString = connectionString;

            // Construye opciones de DbContext con el provider IBM iSeries
            _options = new DbContextOptionsBuilder<AS400DbContext>()
                .UseDb2(_connectionString, p => p.SetServerInfo(IBMDBServerType.AS400))
                .Options;
        }

        /// <summary>
        /// Abre la conexión al AS400 instanciando el DbContext.
        /// </summary>
        public void Open()
        {
            if (_context == null)
                _context = new AS400DbContext(_options);
        }

        /// <summary>
        /// Cierra y elimina el contexto para liberar recursos.
        /// </summary>
        public void Close()
        {
            if (_context != null)
            {
                _context.Dispose();
                _context = null;
            }
        }

        /// <summary>
        /// Retorna el DbContext activo para realizar consultas o ejecuciones CLLE/RPG.
        /// </summary>
        /// <returns>Instancia de AS400DbContext.</returns>
        public DbContext GetDbContext()
        {
            if (_context == null)
                Open();

            return _context;
        }

        /// <summary>
        /// Libera los recursos del contexto si quedan abiertos.
        /// </summary>
        public void Dispose()
        {
            Close();
        }
    }

    /// <summary>
    /// DbContext especializado para conexiones a AS400.
    /// </summary>
    public class AS400DbContext : DbContext
    {
        public AS400DbContext(DbContextOptions<AS400DbContext> options)
            : base(options)
        {
        }

        // Puedes agregar DbSet<T> si deseas usar modelos mapeados
    }
}
