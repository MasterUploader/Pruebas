using Connections.Interfaces;
using Connections.Logging;
using Logging.Helpers;
using Microsoft.AspNetCore.Http;
using System.Data.Common;

namespace Logging.Decorators;

/// <summary>
/// Decorador que intercepta las llamadas a la conexión de base de datos para registrar automáticamente los comandos ejecutados.
/// Este decorador permite agregar funcionalidades de logging sin modificar la implementación original de la conexión.
/// </summary>
public class LoggingDatabaseConnectionDecorator : IDatabaseConnection
{
    private readonly IDatabaseConnection _innerConnection;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly QueryExecutionLogger _queryLogger;

    /// <summary>
    /// Inicializa una nueva instancia del decorador de conexión.
    /// </summary>
    /// <param name="innerConnection">Conexión original que se desea envolver con funcionalidades de logging.</param>
    /// <param name="httpContextAccessor">Contexto HTTP para obtener información adicional del request.</param>
    /// <param name="queryLogger">Servicio responsable de registrar las operaciones ejecutadas.</param>
    /// <exception cref="ArgumentNullException">Se lanza si alguno de los parámetros es nulo.</exception>
    public LoggingDatabaseConnectionDecorator(
        IDatabaseConnection innerConnection,
        IHttpContextAccessor httpContextAccessor,
        QueryExecutionLogger queryLogger)
    {
        _innerConnection = innerConnection ?? throw new ArgumentNullException(nameof(innerConnection));
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        _queryLogger = queryLogger ?? throw new ArgumentNullException(nameof(queryLogger));
    }

    /// <summary>
    /// Abre la conexión original subyacente.
    /// </summary>
    public void Open()
    {
        _innerConnection.Open();
    }

    /// <summary>
    /// Cierra la conexión original subyacente.
    /// </summary>
    public void Close()
    {
        _innerConnection.Close();
    }

    /// <summary>
    /// Verifica si la conexión original está activa.
    /// </summary>
    /// <returns>True si la conexión está abierta; de lo contrario, False.</returns>
    /// 
    public bool IsConnected => _innerConnection.IsConnected();

    /// <summary>
    /// Obtiene un comando de base de datos con capacidades de logging.
    /// </summary>
    /// <param name="context">Contexto HTTP actual para capturar información adicional como headers o traceId.</param>
    /// <returns>Comando de base de datos envuelto con lógica de registro.</returns>
    public DbCommand GetDbCommand(HttpContext context)
    {
        var originalCommand = _innerConnection.GetDbCommand(context);
        return new LoggingDbCommand(originalCommand, context, _queryLogger);
    }

    /// <summary>
    /// Libera los recursos utilizados por la conexión original.
    /// </summary>
    public void Dispose()
    {
        _innerConnection.Dispose();
    }
}
