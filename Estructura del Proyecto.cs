using System.Data.Common;
using Microsoft.EntityFrameworkCore;

namespace RestUtilities.Connections.Interfaces
{
    /// <summary>
    /// Contrato base para conexiones a bases de datos dentro de la librería.
    /// Soporta tanto Entity Framework como ejecución tradicional con comandos SQL.
    /// </summary>
    public interface IDatabaseConnection : IDisposable
    {
        /// <summary>
        /// Inicializa la conexión, ya sea tradicional o mediante DbContext.
        /// </summary>
        void Open();

        /// <summary>
        /// Cierra y libera recursos de la conexión.
        /// </summary>
        void Close();

        /// <summary>
        /// Verifica si la conexión está disponible y funcional.
        /// </summary>
        bool IsConnected();

        /// <summary>
        /// Retorna un DbContext configurado para acceso con modelos y LINQ.
        /// </summary>
        DbContext GetDbContext();

        /// <summary>
        /// Retorna un comando directo para ejecutar sentencias SQL en texto plano (SELECT, UPDATE, CALL, etc.).
        /// </summary>
        DbCommand GetDbCommand();
    }
}










using System.Data.Common;
using System.Data;
using IBM.Data.DB2;
using Microsoft.EntityFrameworkCore;
using RestUtilities.Connections.Interfaces;

namespace RestUtilities.Connections.Providers.Database
{
    /// <summary>
    /// Proveedor híbrido de conexión para AS400.
    /// Soporta tanto Entity Framework (DbContext) como comandos tradicionales (DbCommand).
    /// </summary>
    public class AS400ConnectionProvider : IDatabaseConnection
    {
        private readonly string _connectionString;
        private DB2Connection _connection;
        private AS400DbContext _context;
        private readonly DbContextOptions<AS400DbContext> _options;

        /// <summary>
        /// Constructor que recibe la cadena de conexión desde la configuración.
        /// </summary>
        public AS400ConnectionProvider(string connectionString)
        {
            _connectionString = connectionString;

            // Prepara opciones del DbContext para uso con EF Core
            _options = new DbContextOptionsBuilder<AS400DbContext>()
                .UseDb2(_connectionString, o => o.SetServerInfo(IBMDBServerType.AS400))
                .Options;
        }

        /// <summary>
        /// Abre ambas conexiones si no están inicializadas.
        /// </summary>
        public void Open()
        {
            // Inicializa DbContext para EF Core si no existe
            if (_context == null)
                _context = new AS400DbContext(_options);

            // Inicializa conexión tradicional si no existe
            if (_connection == null)
                _connection = new DB2Connection(_connectionString);

            if (_connection.State != ConnectionState.Open)
                _connection.Open();
        }

        /// <summary>
        /// Cierra y libera los recursos de ambas conexiones.
        /// </summary>
        public void Close()
        {
            _context?.Dispose();
            _context = null;

            if (_connection?.State == ConnectionState.Open)
                _connection.Close();
        }

        /// <summary>
        /// Verifica si al menos una de las dos conexiones está operativa.
        /// </summary>
        public bool IsConnected()
        {
            try
            {
                return (_context?.Database?.CanConnect() ?? false)
                    || (_connection?.State == ConnectionState.Open);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Retorna un DbContext configurado con IBM.EntityFrameworkCore para AS400.
        /// </summary>
        public DbContext GetDbContext()
        {
            if (_context == null)
                Open();

            return _context;
        }

        /// <summary>
        /// Retorna un DbCommand para ejecutar SQL directamente sin EF.
        /// </summary>
        public DbCommand GetDbCommand()
        {
            if (_connection == null)
                _connection = new DB2Connection(_connectionString);

            if (_connection.State != ConnectionState.Open)
                _connection.Open();

            return _connection.CreateCommand();
        }

        /// <summary>
        /// Libera todos los recursos de la conexión.
        /// </summary>
        public void Dispose()
        {
            Close();
            _connection?.Dispose();
        }
    }

    /// <summary>
    /// DbContext base para AS400. Puedes registrar aquí tus DbSet<> si deseas usar modelos.
    /// </summary>
    public class AS400DbContext : DbContext
    {
        public AS400DbContext(DbContextOptions<AS400DbContext> options) : base(options) { }

        // Puedes definir DbSet<Usuario> si deseas usar EF directamente:
        // public DbSet<Usuario> Usuarios { get; set; }
    }
}
