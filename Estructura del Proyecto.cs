using System;
using System.Data;
using System.Data.Common;
using System.Data.OleDb;
using Microsoft.EntityFrameworkCore;
using RestUtilities.Connections.Interfaces;

namespace RestUtilities.Connections.Providers.Database
{
    /// <summary>
    /// Proveedor híbrido para conexiones AS400 que soporta:
    /// - Conexiones tradicionales usando OleDbCommand.
    /// - Acceso moderno con EF Core (DbContext).
    /// </summary>
    public class AS400ConnectionProvider : IDatabaseConnection
    {
        private readonly string _connectionString;

        // Conexión tradicional (OleDb)
        private OleDbConnection _oleDbConnection;

        // Conexión EF Core (DbContext)
        private AS400DbContext _context;

        // Opciones para crear el DbContext
        private readonly DbContextOptions<AS400DbContext> _options;

        /// <summary>
        /// Constructor que recibe y prepara la cadena de conexión.
        /// </summary>
        public AS400ConnectionProvider(string connectionString)
        {
            _connectionString = connectionString;

            // Configura las opciones del DbContext
            _options = new DbContextOptionsBuilder<AS400DbContext>()
                .UseDbConnection(new OleDbConnection(_connectionString)) // Adaptación genérica
                .Options;
        }

        /// <summary>
        /// Abre las conexiones necesarias (OleDb y DbContext).
        /// </summary>
        public void Open()
        {
            // Inicializa el DbContext si no está disponible
            if (_context == null)
                _context = new AS400DbContext(_options);

            // Inicializa la conexión OleDb si no está disponible
            if (_oleDbConnection == null)
                _oleDbConnection = new OleDbConnection(_connectionString);

            if (_oleDbConnection.State != ConnectionState.Open)
                _oleDbConnection.Open();
        }

        /// <summary>
        /// Cierra y libera los recursos asociados a ambas conexiones.
        /// </summary>
        public void Close()
        {
            _context?.Dispose();
            _context = null;

            if (_oleDbConnection?.State == ConnectionState.Open)
                _oleDbConnection.Close();
        }

        /// <summary>
        /// Verifica si al menos una conexión está operativa.
        /// </summary>
        public bool IsConnected()
        {
            try
            {
                return (_context?.Database?.CanConnect() ?? false)
                    || (_oleDbConnection?.State == ConnectionState.Open);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Retorna una instancia de DbContext (opcionalmente con DbSet<> definidos por el usuario).
        /// </summary>
        public DbContext GetDbContext()
        {
            if (_context == null)
                Open();

            return _context;
        }

        /// <summary>
        /// Retorna un comando OleDb listo para ejecutar consultas SQL tradicionales.
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
        /// Libera los recursos utilizados por las conexiones.
        /// </summary>
        public void Dispose()
        {
            Close();
            _oleDbConnection?.Dispose();
        }
    }

    /// <summary>
    /// DbContext base para AS400 que puede ser extendido con DbSet<> si se desea.
    /// </summary>
    public class AS400DbContext : DbContext
    {
        public AS400DbContext(DbContextOptions<AS400DbContext> options) : base(options) { }

        // Puedes registrar modelos así si los defines externamente:
        // public DbSet<Usuario> Usuarios { get; set; }
    }
}
