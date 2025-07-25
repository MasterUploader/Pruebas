Por lo que veo no tengo en la interfaz la sobrecargar de GetDbCommand, pero no puedo agregarla ya que no me reconoce using QueryBuilder.Models; dentro de IDatabaseConnection, porque esta en otro paquete, pero AS400ConnectionProvider si lo hace

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
/// Esta implementación permite la ejecución de comandos SQL con o sin logging estructurado.
/// </summary>
public partial class AS400ConnectionProvider : IDatabaseConnection, IDisposable
{
    private readonly OleDbConnection _oleDbConnection;
    private readonly ILoggingService? _loggingService;
    private readonly IHttpContextAccessor? _httpContextAccessor;

    /// <summary>
    /// Inicializa una nueva instancia de <see cref="AS400ConnectionProvider"/>.
    /// </summary>
    /// <param name="connectionString">Cadena de conexión a AS400 en formato OleDb.</param>
    /// <param name="loggingService">Servicio de logging estructurado (opcional).</param>
    /// <param name="httpContextAccessor">Accessor del contexto HTTP (opcional).</param>
    public AS400ConnectionProvider(
        string connectionString,
        ILoggingService? loggingService = null,
        IHttpContextAccessor? httpContextAccessor = null)
    {
        _oleDbConnection = new OleDbConnection(connectionString);
        _loggingService = loggingService;
        _httpContextAccessor = httpContextAccessor;
    }

    /// <inheritdoc />
    public void Open()
    {
        if (_oleDbConnection.State != System.Data.ConnectionState.Open)
            _oleDbConnection.Open();
    }

    /// <inheritdoc />
    public void Close()
    {
        if (_oleDbConnection.State != System.Data.ConnectionState.Closed)
            _oleDbConnection.Close();
    }

    /// <inheritdoc />
    public bool IsConnected => _oleDbConnection?.State == System.Data.ConnectionState.Open;

    /// <inheritdoc />
    public DbCommand GetDbCommand(HttpContext? context = null)
    {
        var command = _oleDbConnection.CreateCommand();

        // Si el servicio de logging está disponible, devolvemos el comando decorado
        if (_loggingService != null)
        {
            return new LoggingDbCommandWrapper(command, _loggingService, _httpContextAccessor);
        }

        // En caso contrario, devolvemos el comando básico
        return command;
    }

    /// <summary>
    /// Crea un comando configurado con la consulta SQL generada por QueryBuilder y sus parámetros asociados.
    /// </summary>
    /// <param name="queryResult">Objeto que contiene el SQL generado y la lista de parámetros.</param>
    /// <param name="context">Contexto HTTP actual para trazabilidad opcional.</param>
    /// <returns>DbCommand listo para ejecución.</returns>
    public DbCommand GetDbCommand(QueryResult queryResult, HttpContext? context)
    {
        var command = GetDbCommand(context);

        // Establece el SQL
        command.CommandText = queryResult.Sql;

        // Limpia cualquier parámetro anterior
        command.Parameters.Clear();

        // Agrega los parámetros a la posición correspondiente
        if (queryResult.Parameters is not null && queryResult.Parameters.Count > 0)
        {
            foreach (var paramValue in queryResult.Parameters)
            {
                var parameter = command.CreateParameter();
                parameter.Value = paramValue ?? DBNull.Value;
                command.Parameters.Add(parameter);
            }
        }

        return command;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _oleDbConnection?.Dispose();
    }
}
