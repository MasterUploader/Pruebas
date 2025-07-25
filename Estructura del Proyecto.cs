Así esta el codigo actualmente, que debo cambiar o mejorar

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
/// Esta implementación está optimizada para ejecución directa de comandos SQL
/// y permite logging estructurado si está disponible.
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

    /// <summary>
    /// Obtiene un <see cref="DbCommand"/> decorado con logging si está habilitado, o básico si no.
    /// </summary>
    /// <param name="context">Contexto HTTP opcional para trazabilidad.</param>
    /// <returns>Instancia de <see cref="DbCommand"/> con o sin decorador.</returns>
    public DbCommand GetDbCommand(HttpContext? context = null)
    {
        var command = _oleDbConnection.CreateCommand();

        // Si el servicio de logging está disponible, envolvemos el comando
        if (_loggingService != null)
        {
            return new LoggingDbCommandWrapper(command, _loggingService, _httpContextAccessor);
        }

        // Caso contrario, devolvemos el comando sin decorar
        return command;
    }

    /// <summary>
    /// Obtiene un <see cref="DbCommand"/> con la consulta generada por QueryBuilder.
    /// </summary>
    /// <param name="queryResult">Consulta SQL construida.</param>
    /// <param name="context">Contexto HTTP actual (opcional para trazabilidad).</param>
    /// <returns>Comando configurado con SQL listo para ejecutar.</returns>
    public DbCommand GetDbCommand(QueryResult queryResult, HttpContext context)
    {
        var command = GetDbCommand(context);
        command.CommandText = queryResult.Sql;
        return command;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _oleDbConnection?.Dispose();
    }
}
