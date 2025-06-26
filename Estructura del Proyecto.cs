using System;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Common.Helpers;
using RestUtilities.Logging;

namespace Connections.Logging
{
    /// <summary>
    /// Comando decorador que intercepta y registra automáticamente las operaciones SQL ejecutadas.
    /// Compatible con múltiples proveedores y diseñado para ser reutilizable con la librería Connections.
    /// </summary>
    public class LoggingDbCommand : DbCommand
    {
        private readonly DbCommand _innerCommand;
        private readonly HttpContext? _context;
        private readonly ILoggingService _loggingService;

        /// <summary>
        /// Inicializa una nueva instancia de <see cref="LoggingDbCommand"/>.
        /// </summary>
        /// <param name="innerCommand">Instancia original de DbCommand que se desea envolver.</param>
        /// <param name="context">Contexto actual HTTP, si está disponible (puede ser null en procesos internos).</param>
        /// <param name="loggingService">Servicio de logging donde se registrará la ejecución del comando.</param>
        public LoggingDbCommand(DbCommand innerCommand, HttpContext? context, ILoggingService loggingService)
        {
            _innerCommand = innerCommand ?? throw new ArgumentNullException(nameof(innerCommand));
            _context = context;
            _loggingService = loggingService ?? throw new ArgumentNullException(nameof(loggingService));
        }

        #region Propiedades sobrescritas

        public override string CommandText
        {
            get => _innerCommand.CommandText;
            set => _innerCommand.CommandText = value;
        }

        public override int CommandTimeout
        {
            get => _innerCommand.CommandTimeout;
            set => _innerCommand.CommandTimeout = value;
        }

        public override CommandType CommandType
        {
            get => _innerCommand.CommandType;
            set => _innerCommand.CommandType = value;
        }

        public override bool DesignTimeVisible
        {
            get => _innerCommand.DesignTimeVisible;
            set => _innerCommand.DesignTimeVisible = value;
        }

        protected override DbConnection DbConnection
        {
            get => _innerCommand.Connection!;
            set => _innerCommand.Connection = value;
        }

        protected override DbTransaction? DbTransaction
        {
            get => _innerCommand.Transaction;
            set => _innerCommand.Transaction = value;
        }

        protected override DbParameterCollection DbParameterCollection => _innerCommand.Parameters;

        public override UpdateRowSource UpdatedRowSource
        {
            get => _innerCommand.UpdatedRowSource;
            set => _innerCommand.UpdatedRowSource = value;
        }

        #endregion

        #region Métodos de ejecución con logging

        /// <inheritdoc />
        public override int ExecuteNonQuery()
        {
            using var sw = StopwatchHelper.StartNew("ExecuteNonQuery");
            try
            {
                int result = _innerCommand.ExecuteNonQuery();
                _loggingService.WriteLog(_context, FormatSuccessLog(sw, result));
                return result;
            }
            catch (Exception ex)
            {
                _loggingService.WriteLog(_context, FormatErrorLog(sw, ex));
                throw;
            }
        }

        /// <inheritdoc />
        public override object? ExecuteScalar()
        {
            using var sw = StopwatchHelper.StartNew("ExecuteScalar");
            try
            {
                var result = _innerCommand.ExecuteScalar();
                _loggingService.WriteLog(_context, FormatSuccessLog(sw, result));
                return result;
            }
            catch (Exception ex)
            {
                _loggingService.WriteLog(_context, FormatErrorLog(sw, ex));
                throw;
            }
        }

        /// <inheritdoc />
        protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior)
        {
            using var sw = StopwatchHelper.StartNew("ExecuteReader");
            try
            {
                var reader = _innerCommand.ExecuteReader(behavior);
                _loggingService.WriteLog(_context, FormatSuccessLog(sw, "DbDataReader opened"));
                return reader;
            }
            catch (Exception ex)
            {
                _loggingService.WriteLog(_context, FormatErrorLog(sw, ex));
                throw;
            }
        }

        /// <inheritdoc />
        protected override async Task<DbDataReader> ExecuteDbDataReaderAsync(CommandBehavior behavior, CancellationToken cancellationToken)
        {
            using var sw = StopwatchHelper.StartNew("ExecuteReaderAsync");
            try
            {
                var reader = await _innerCommand.ExecuteReaderAsync(behavior, cancellationToken);
                _loggingService.WriteLog(_context, FormatSuccessLog(sw, "DbDataReader opened async"));
                return reader;
            }
            catch (Exception ex)
            {
                _loggingService.WriteLog(_context, FormatErrorLog(sw, ex));
                throw;
            }
        }

        #endregion

        #region Métodos auxiliares

        /// <inheritdoc />
        public override void Cancel() => _innerCommand.Cancel();

        /// <inheritdoc />
        public override void Prepare() => _innerCommand.Prepare();

        /// <inheritdoc />
        protected override DbParameter CreateDbParameter() => _innerCommand.CreateParameter();

        /// <inheritdoc />
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _innerCommand.Dispose();
            }

            base.Dispose(disposing);
        }

        /// <summary>
        /// Formatea un mensaje de éxito para ser registrado en el log.
        /// </summary>
        /// <param name="sw">Instancia de StopwatchHelper utilizada para medir el tiempo.</param>
        /// <param name="result">Resultado devuelto por el comando.</param>
        private string FormatSuccessLog(StopwatchHelper sw, object? result)
        {
            return $"[DB SUCCESS]\nSQL: {CommandText}\nResultado: {result}\nDuración: {sw.ElapsedMilliseconds} ms";
        }

        /// <summary>
        /// Formatea un mensaje de error para ser registrado en el log.
        /// </summary>
        /// <param name="sw">Instancia de StopwatchHelper utilizada para medir el tiempo.</param>
        /// <param name="ex">Excepción capturada durante la ejecución del comando.</param>
        private string FormatErrorLog(StopwatchHelper sw, Exception ex)
        {
            return $"[DB ERROR]\nSQL: {CommandText}\nExcepción: {ex.Message}\nDuración: {sw.ElapsedMilliseconds} ms";
        }

        #endregion
    }
}
