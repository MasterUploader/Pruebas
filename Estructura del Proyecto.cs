using Connections.Managers;
using Logging.Abstractions;
using System.Data;
using System.Data.Common;
using System.Data.OleDb;
using System.Diagnostics;

namespace Connections.Providers.Database;

/// <summary>
/// Proveedor de conexión a base de datos AS400 utilizando OleDb.
/// Esta implementación está optimizada para ejecución directa de comandos SQL y registra automáticamente logs estructurados.
/// </summary>
public class As400ConnectionProvider : LoggingDatabaseConnection
{
    private readonly OleDbConnection _oleDbConnection;
    private readonly ILoggingService _loggingService;

    /// <summary>
    /// Inicializa una nueva instancia de <see cref="As400ConnectionProvider"/>.
    /// </summary>
    /// <param name="connectionString">Cadena de conexión a AS400 en formato OleDb.</param>
    /// <param name="loggingService">Instancia del servicio de logging para registrar las operaciones SQL.</param>
    public As400ConnectionProvider(string connectionString, ILoggingService loggingService)
        : base(new OleDbConnection(connectionString), loggingService)
    {
        _oleDbConnection = new OleDbConnection(connectionString);
        _loggingService = loggingService;
    }

    /// <summary>
    /// Abre la conexión a AS400 si aún no está abierta.
    /// </summary>
    public new void Open()
    {
        if (_oleDbConnection.State != ConnectionState.Open)
            _oleDbConnection.Open();
    }

    /// <summary>
    /// Cierra la conexión a AS400 si se encuentra abierta.
    /// </summary>
    public new void Close()
    {
        if (_oleDbConnection.State == ConnectionState.Open)
            _oleDbConnection.Close();
    }

    /// <summary>
    /// Indica si la conexión a AS400 está actualmente activa.
    /// </summary>
    /// <returns>True si la conexión está abierta; False en caso contrario.</returns>
    public new bool IsConnected()
    {
        return _oleDbConnection.State == ConnectionState.Open;
    }

    /// <summary>
    /// Libera los recursos de la conexión AS400 y su conexión base.
    /// </summary>
    public new void Dispose()
    {
        base.Dispose();
        _oleDbConnection.Dispose();
    }

    /// <summary>
    /// Obtiene un comando OleDb que está envuelto para registrar automáticamente logs de ejecución SQL.
    /// El comando intercepta los métodos Execute* para registrar éxito o error.
    /// </summary>
    /// <returns>Instancia de <see cref="DbCommand"/> que registra logs automáticamente al ejecutarse.</returns>
    public override DbCommand GetDbCommand()
    {
        Open();
        var command = _oleDbConnection.CreateCommand();
        return new LoggingDbCommandWrapper(command, _loggingService);
    }

    /// <summary>
    /// Clase interna que envuelve un DbCommand real e intercepta los métodos Execute para registrar logs automáticamente.
    /// No altera la firma pública del comando y permite usarse de forma transparente.
    /// </summary>
    private class LoggingDbCommandWrapper : DbCommand
    {
        private readonly DbCommand _inner;
        private readonly ILoggingService _loggingService;

        /// <summary>
        /// Inicializa un nuevo comando que encapsula la ejecución real con soporte de logging estructurado.
        /// </summary>
        /// <param name="inner">Instancia real de DbCommand.</param>
        /// <param name="loggingService">Servicio de logging a utilizar.</param>
        public LoggingDbCommandWrapper(DbCommand inner, ILoggingService loggingService)
        {
            _inner = inner;
            _loggingService = loggingService;
        }

        public override string CommandText { get => _inner.CommandText; set => _inner.CommandText = value; }
        public override int CommandTimeout { get => _inner.CommandTimeout; set => _inner.CommandTimeout = value; }
        public override CommandType CommandType { get => _inner.CommandType; set => _inner.CommandType = value; }
        public override UpdateRowSource UpdatedRowSource { get => _inner.UpdatedRowSource; set => _inner.UpdatedRowSource = value; }

        protected override DbConnection DbConnection { get => _inner.Connection; set => _inner.Connection = value; }
        protected override DbTransaction DbTransaction { get => _inner.Transaction; set => _inner.Transaction = value; }
        public override bool DesignTimeVisible { get => _inner.DesignTimeVisible; set => _inner.DesignTimeVisible = value; }

        protected override DbParameterCollection DbParameterCollection => _inner.Parameters;

        public override void Cancel() => _inner.Cancel();
        public override void Prepare() => _inner.Prepare();
        protected override DbParameter CreateDbParameter() => _inner.CreateParameter();

        /// <summary>
        /// Ejecuta el comando como operación no consultiva (INSERT, UPDATE, DELETE) y registra automáticamente el log.
        /// </summary>
        public override int ExecuteNonQuery()
        {
            var sw = Stopwatch.StartNew();
            try
            {
                int result = _inner.ExecuteNonQuery();
                sw.Stop();
                _loggingService?.LogDatabaseSuccess(_inner, sw.ElapsedMilliseconds, null, $"Filas afectadas: {result}");
                return result;
            }
            catch (Exception ex)
            {
                sw.Stop();
                _loggingService?.LogDatabaseError(_inner, ex);
                throw;
            }
        }

        /// <summary>
        /// Ejecuta el comando y retorna el primer valor de la primera fila del resultado.
        /// Registra automáticamente el log de ejecución.
        /// </summary>
        public override object? ExecuteScalar()
        {
            var sw = Stopwatch.StartNew();
            try
            {
                var result = _inner.ExecuteScalar();
                sw.Stop();
                _loggingService?.LogDatabaseSuccess(_inner, sw.ElapsedMilliseconds, null, $"Resultado: {result}");
                return result;
            }
            catch (Exception ex)
            {
                sw.Stop();
                _loggingService?.LogDatabaseError(_inner, ex);
                throw;
            }
        }

        /// <summary>
        /// Ejecuta el comando como consulta con lectura de datos y registra automáticamente el log.
        /// </summary>
        protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior)
        {
            var sw = Stopwatch.StartNew();
            try
            {
                var reader = _inner.ExecuteReader(behavior);
                sw.Stop();
                _loggingService?.LogDatabaseSuccess(_inner, sw.ElapsedMilliseconds);
                return reader;
            }
            catch (Exception ex)
            {
                sw.Stop();
                _loggingService?.LogDatabaseError(_inner, ex);
                throw;
            }
        }

        // Métodos asincrónicos

        public override async Task<int> ExecuteNonQueryAsync(CancellationToken cancellationToken)
        {
            var sw = Stopwatch.StartNew();
            try
            {
                int result = await ((OleDbCommand)_inner).ExecuteNonQueryAsync(cancellationToken);
                sw.Stop();
                _loggingService?.LogDatabaseSuccess(_inner, sw.ElapsedMilliseconds, null, $"Filas afectadas: {result}");
                return result;
            }
            catch (Exception ex)
            {
                sw.Stop();
                _loggingService?.LogDatabaseError(_inner, ex);
                throw;
            }
        }

        public override async Task<object?> ExecuteScalarAsync(CancellationToken cancellationToken)
        {
            var sw = Stopwatch.StartNew();
            try
            {
                var result = await ((OleDbCommand)_inner).ExecuteScalarAsync(cancellationToken);
                sw.Stop();
                _loggingService?.LogDatabaseSuccess(_inner, sw.ElapsedMilliseconds, null, $"Resultado: {result}");
                return result;
            }
            catch (Exception ex)
            {
                sw.Stop();
                _loggingService?.LogDatabaseError(_inner, ex);
                throw;
            }
        }

        protected override async Task<DbDataReader> ExecuteDbDataReaderAsync(CommandBehavior behavior, CancellationToken cancellationToken)
        {
            var sw = Stopwatch.StartNew();
            try
            {
                var reader = await ((OleDbCommand)_inner).ExecuteReaderAsync(behavior, cancellationToken);
                sw.Stop();
                _loggingService?.LogDatabaseSuccess(_inner, sw.ElapsedMilliseconds);
                return reader;
            }
            catch (Exception ex)
            {
                sw.Stop();
                _loggingService?.LogDatabaseError(_inner, ex);
                throw;
            }
        }
    }
}
