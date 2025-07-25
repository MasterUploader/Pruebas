Así tengo el codigo, es posible que le apliques los cambios al mismo.

using Connections.Interfaces;
using Logging.Abstractions;
using Logging.Decorators;
using Microsoft.AspNetCore.Http;
using QueryBuilder.Models;
using System.Data.Common;
using System.Data.OleDb;

namespace Connections.Providers.Database;

/// <summary>
/// Proveedor de conexión a base de datos AS400 utilizando OleDb.
/// Esta implementación está optimizada para ejecución directa de comandos SQL y logging estructurado.
/// </summary>
public partial class AS400ConnectionProvider : IDatabaseConnection, IDisposable
{
    private readonly OleDbConnection _oleDbConnection;
    private readonly ILoggingService _loggingService;

    /// <summary>
    /// Inicializa una nueva instancia de <see cref="AS400ConnectionProvider"/>.
    /// </summary>
    /// <param name="connectionString">Cadena de conexión a AS400 en formato OleDb.</param>
    /// <param name="loggingService">Servicio de logging estructurado.</param>
    public AS400ConnectionProvider(string connectionString, ILoggingService loggingService)
    {
        _oleDbConnection = new OleDbConnection(connectionString);
        _loggingService = loggingService;
    }

    /// <summary>
    /// Abre la conexión si no está ya abierta.
    /// </summary>
    public void Open()
    {
        if (_oleDbConnection.State != System.Data.ConnectionState.Open)
            _oleDbConnection.Open();
    }

    /// <summary>
    /// Cierra la conexión si está abierta.
    /// </summary>
    public void Close()
    {
        if (_oleDbConnection.State != System.Data.ConnectionState.Closed)
            _oleDbConnection.Close();
    }

    /// <summary>
    /// Indica si la conexión está actualmente abierta.
    /// </summary>
    public bool IsConnected => _oleDbConnection?.State == System.Data.ConnectionState.Open;

    /// <summary>
    /// Obtiene un <see cref="DbCommand"/> decorado con soporte de logging estructurado.
    /// </summary>
    /// <param name="context">Contexto HTTP opcional para trazabilidad adicional.</param>
    /// <returns>Comando decorado con logging.</returns>
    public DbCommand GetDbCommand(HttpContext? context = null)
    {
        var command = _oleDbConnection.CreateCommand();
        return new LoggingDbCommandWrapper(command, _loggingService); // ← El decorador correcto
    }

    /// <summary>
    /// Obtiene un <see cref="DbCommand"/> configurado con la consulta y los parámetros generados por QueryBuilder.
    /// </summary>
    /// <param name="queryResult">Consulta SQL generada mediante QueryBuilder, incluyendo parámetros.</param>
    /// <param name="context">Contexto HTTP actual, necesario para trazabilidad o uso interno de conexión.</param>
    /// <returns>Una instancia de <see cref="DbCommand"/> con SQL y parámetros listos para ejecutarse.</returns>
    public DbCommand GetDbCommand(QueryResult queryResult, HttpContext context)
    {
        // Obtiene el comando base desde la implementación existente
        var command = GetDbCommand(context);

        // Asigna el SQL generado por QueryBuilder
        command.CommandText = queryResult.Sql;

        return command;
    }

    /// <summary>
    /// Libera los recursos de la conexión.
    /// </summary>
    public void Dispose()
    {
        _oleDbConnection?.Dispose();
    }
}


using Microsoft.AspNetCore.Http;
using System.Data.Common;

namespace Connections.Interfaces;

/// <summary>
/// Define las operaciones básicas para una conexión de base de datos compatible con múltiples motores.
/// </summary>
public interface IDatabaseConnection : IDisposable
{
    /// <summary>
    /// Abre la conexión a la base de datos.
    /// </summary>
    void Open();

    /// <summary>
    /// Cierra la conexión actual a la base de datos.
    /// </summary>
    void Close();

    /// <summary>
    /// Indica si la conexión está activa.
    /// </summary>
    bool IsConnected { get; }

    /// <summary>
    /// Crea un nuevo comando de base de datos, decorado con soporte de logging si está habilitado.
    /// </summary>
    /// <param name="context">Contexto HTTP opcional para registrar logs por petición.</param>
    /// <returns>Comando decorado con logging.</returns>
    DbCommand GetDbCommand(HttpContext context);
}


