using System;
using System.Data.OleDb;
using Microsoft.AspNetCore.Http;
using RestUtilities.Connections.Managers.Base;
using RestUtilities.Logging.Abstractions;

namespace RestUtilities.Connections.Providers.Database;

/// <summary>
/// Proveedor de conexión para bases de datos AS400 utilizando OleDb y soporte de logging automático.
/// </summary>
public class As400ConnectionProvider : LoggingDatabaseConnection
{
    /// <summary>
    /// Inicializa una nueva instancia de <see cref="As400ConnectionProvider"/>.
    /// </summary>
    /// <param name="connectionString">Cadena de conexión a AS400.</param>
    /// <param name="loggingService">Servicio de logging para registrar consultas.</param>
    public As400ConnectionProvider(string connectionString, ILoggingService loggingService)
        : base(new OleDbConnection(connectionString), loggingService)
    {
    }

    // Puedes agregar aquí métodos personalizados para AS400 si necesitas funcionalidad extra
}
