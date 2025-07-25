/// <summary>
/// Consolida y guarda el log estructurado si no ha sido registrado aún.
/// Se asegura de que no se genere una excepción incluso si la conexión está cerrada o es nula.
/// </summary>
private void FinalizeAndLog()
{
    lock (_lock)
    {
        if (_isFinalized || _executionCount == 0)
            return;

        _stopwatch.Stop();
        _isFinalized = true;

        try
        {
            var connection = _innerCommand.Connection;

            var log = new SqlLogModel
            {
                Sql = _commandText,
                ExecutionCount = _executionCount,
                TotalAffectedRows = _totalAffectedRows,
                StartTime = _startTime,
                Duration = _stopwatch.Elapsed,
                DatabaseName = connection?.Database ?? "Desconocida",
                Ip = connection?.DataSource ?? "Desconocida",
                Port = 0,
                TableName = ExtraerNombreTablaDesdeSql(_commandText),
                Schema = ExtraerEsquemaDesdeSql(_commandText)
            };

            _loggingService.LogDatabaseSuccess(log, _httpContextAccessor?.HttpContext);
        }
        catch (Exception ex)
        {
            // Se captura cualquier excepción interna para evitar que se propague fuera del Dispose.
            _loggingService.LogDatabaseError(_innerCommand, ex, _httpContextAccessor?.HttpContext);
        }
    }
}
