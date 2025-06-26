using Connections.Managers;
using Logging.Abstractions;
using System.Data;
using System.Data.Common;
using System.Data.OleDb;

namespace Connections.Providers.Database;

/// <summary>
/// Proveedor de conexión para AS400 usando únicamente OleDbCommand.
/// No utiliza DbContext ni Entity Framework.
/// </summary>
public class As400ConnectionProvider : LoggingDatabaseConnection
{
    private readonly OleDbConnection _oleDbConnection;

    /// <summary>
    /// Inicializa una nueva instancia de <see cref="As400ConnectionProvider"/>.
    /// </summary>
    /// <param name="connectionString">Cadena de conexión a AS400.</param>
    /// <param name="loggingService">Servicio de logging para registrar consultas.</param>
    public As400ConnectionProvider(string connectionString, ILoggingService loggingService)
        : base(new OleDbConnection(connectionString), loggingService)
    {
        _oleDbConnection = new OleDbConnection(connectionString);
    }

    /// <summary>
    /// Abre la conexión OleDb si aún no está abierta.
    /// </summary>
    public new void Open()
    {
        if (_oleDbConnection.State != ConnectionState.Open)
            _oleDbConnection.Open();
    }

    /// <summary>
    /// Cierra y limpia la conexión si está activa.
    /// </summary>
    public new void Close()
    {
        if (_oleDbConnection.State == ConnectionState.Open)
            _oleDbConnection.Close();
    }

    /// <summary>
    /// Verifica si la conexión está actualmente abierta y operativa.
    /// </summary>
    /// <returns>True si la conexión está abierta, false en caso contrario.</returns>
    public new bool IsConnected()
    {
        return _oleDbConnection.State == ConnectionState.Open;
    }

    /// <summary>
    /// Retorna un comando OleDb envuelto en logging para ejecutar SQL directamente.
    /// </summary>
    /// <returns>Instancia de <see cref="DbCommand"/> con soporte de logging.</returns>
    public  DbCommand GetDbCommand()
    {
        Open();
        return _oleDbConnection.CreateCommand();
    }

    /// <summary>
    /// Libera la conexión OleDb.
    /// </summary>
    public new void Dispose()
    {
        base.Dispose();
        _oleDbConnection.Dispose();
    }
}





using Connections.Interfaces;

namespace Connections.Providers.Database;

/// <summary>
/// Fábrica para la creación de conexiones a bases de datos según el tipo configurado.
/// </summary>
public class DatabaseConnectionFactory
{
    private readonly Dictionary<string, Func<string, IDatabaseConnection>> _providers;

    public DatabaseConnectionFactory()
    {
        _providers = new Dictionary<string, Func<string, IDatabaseConnection>>
        {
            { "AS400", connectionString => new AS400ConnectionProvider(connectionString) }
            //{ "MSSQL", connectionString => new MSSQLConnectionProvider(connectionString) },
            //{ "Oracle", connectionString => new OracleConnectionProvider(connectionString) }
            // Se pueden agregar más motores de BD aquí
        };
    }

    /// <summary>
    /// Crea una conexión a la base de datos según el tipo configurado.
    /// </summary>
    /// <param name="dbType">Tipo de base de datos (ejemplo: "MSSQL", "AS400").</param>
    /// <param name="connectionString">Cadena de conexión.</param>
    /// <returns>Instancia de `IDatabaseConnection`.</returns>
    public IDatabaseConnection CreateConnection(string dbType, string connectionString)
    {
        if (_providers.TryGetValue(dbType, out var provider))
        {
            return provider(connectionString);
        }

        throw new ArgumentException($"No se encontró un proveedor para el tipo de base de datos: {dbType}");
    }
}
