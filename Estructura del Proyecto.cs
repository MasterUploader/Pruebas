using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Logging.Abstractions;

namespace Connections.Providers.Database
{
    /// <summary>
    /// Comando decorador que intercepta las ejecuciones SQL y acumula la información 
    /// para generar un log agrupado al final de un lote.
    /// </summary>
    public class LoggingDbCommandWrapper : DbCommand
    {
        private readonly DbCommand _inner;
        private readonly ILoggingService _loggingService;

        /// <summary>
        /// Lista estática que acumula la información de todas las ejecuciones realizadas 
        /// con este decorador durante el ciclo de vida de la operación.
        /// </summary>
        public static List<DbExecutionInfo> AccumulatedExecutions { get; } = new();

        /// <summary>
        /// Inicializa una nueva instancia del decorador de DbCommand.
        /// </summary>
        /// <param name="inner">Instancia real de DbCommand a decorar.</param>
        /// <param name="loggingService">Servicio de logging estructurado a utilizar.</param>
        public LoggingDbCommandWrapper(DbCommand inner, ILoggingService loggingService)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
            _loggingService = loggingService ?? throw new ArgumentNullException(nameof(loggingService));
        }

        /// <inheritdoc/>
        public override string CommandText
        {
            get => _inner.CommandText;
            set => _inner.CommandText = value;
        }

        /// <inheritdoc/>
        public override int CommandTimeout
        {
            get => _inner.CommandTimeout;
            set => _inner.CommandTimeout = value;
        }

        /// <inheritdoc/>
        public override CommandType CommandType
        {
            get => _inner.CommandType;
            set => _inner.CommandType = value;
        }

        /// <inheritdoc/>
        protected override DbConnection DbConnection
        {
            get => _inner.Connection!;
            set => _inner.Connection = value;
        }

        /// <inheritdoc/>
        protected override DbTransaction DbTransaction
        {
            get => _inner.Transaction!;
            set => _inner.Transaction = value;
        }

        /// <inheritdoc/>
        public override bool DesignTimeVisible
        {
            get => _inner.DesignTimeVisible;
            set => _inner.DesignTimeVisible = value;
        }

        /// <inheritdoc/>
        public override UpdateRowSource UpdatedRowSource
        {
            get => _inner.UpdatedRowSource;
            set => _inner.UpdatedRowSource = value;
        }

        /// <inheritdoc/>
        protected override DbParameterCollection DbParameterCollection => _inner.Parameters;

        /// <inheritdoc/>
        public override void Cancel() => _inner.Cancel();

        /// <inheritdoc/>
        public override int ExecuteNonQuery()
        {
            var sw = Stopwatch.StartNew();
            try
            {
                int result = _inner.ExecuteNonQuery();
                sw.Stop();

                AccumulatedExecutions.Add(new DbExecutionInfo
                {
                    Sql = _inner.CommandText,
                    DurationMs = sw.ElapsedMilliseconds,
                    Result = result,
                    StartTime = DateTime.Now,
                    CommandType = nameof(ExecuteNonQuery)
                });

                return result;
            }
            catch (Exception ex)
            {
                sw.Stop();
                _loggingService.LogDatabaseError(_inner, ex);
                throw;
            }
        }

        /// <inheritdoc/>
        public override object? ExecuteScalar()
        {
            var sw = Stopwatch.StartNew();
            try
            {
                object? result = _inner.ExecuteScalar();
                sw.Stop();

                AccumulatedExecutions.Add(new DbExecutionInfo
                {
                    Sql = _inner.CommandText,
                    DurationMs = sw.ElapsedMilliseconds,
                    Result = result,
                    StartTime = DateTime.Now,
                    CommandType = nameof(ExecuteScalar)
                });

                return result;
            }
            catch (Exception ex)
            {
                sw.Stop();
                _loggingService.LogDatabaseError(_inner, ex);
                throw;
            }
        }

        /// <inheritdoc/>
        protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior)
        {
            var sw = Stopwatch.StartNew();
            try
            {
                var reader = _inner.ExecuteReader(behavior);
                sw.Stop();

                AccumulatedExecutions.Add(new DbExecutionInfo
                {
                    Sql = _inner.CommandText,
                    DurationMs = sw.ElapsedMilliseconds,
                    Result = "Reader",
                    StartTime = DateTime.Now,
                    CommandType = nameof(ExecuteReader)
                });

                return reader;
            }
            catch (Exception ex)
            {
                sw.Stop();
                _loggingService.LogDatabaseError(_inner, ex);
                throw;
            }
        }

        /// <inheritdoc/>
        public override Task<int> ExecuteNonQueryAsync(CancellationToken cancellationToken)
        {
            return Task.Run(() => ExecuteNonQuery(), cancellationToken);
        }

        /// <inheritdoc/>
        public override Task<object?> ExecuteScalarAsync(CancellationToken cancellationToken)
        {
            return Task.Run(() => ExecuteScalar(), cancellationToken);
        }

        /// <inheritdoc/>
        protected override async Task<DbDataReader> ExecuteDbDataReaderAsync(CommandBehavior behavior, CancellationToken cancellationToken)
        {
            return await Task.Run(() => ExecuteDbDataReader(behavior), cancellationToken);
        }

        /// <inheritdoc/>
        public override void Prepare() => _inner.Prepare();

        /// <inheritdoc/>
        protected override DbParameter CreateDbParameter() => _inner.CreateParameter();

        /// <inheritdoc/>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
                _inner.Dispose();

            base.Dispose(disposing);
        }
    }
}
