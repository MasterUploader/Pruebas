try
{
    var log = new SqlLogModel
    {
        Sql = _commandText,
        ExecutionCount = _executionCount,
        TotalAffectedRows = _totalAffectedRows,
        StartTime = _startTime,
        Duration = _stopwatch.Elapsed,
        DatabaseName = _innerCommand.Connection?.Database ?? "Desconocida",
        Ip = _innerCommand.Connection?.DataSource ?? "Desconocida",
        Port = 0,
        TableName = ExtraerNombreTablaDesdeSql(_commandText),
        Schema = ExtraerEsquemaDesdeSql(_commandText)
    };

    _loggingService.LogDatabaseSuccess(log, _httpContextAccessor?.HttpContext);
}
catch (Exception ex)
{
    // Evita que se propague la excepción
    _loggingService.LogDatabaseError(_innerCommand, ex, _httpContextAccessor?.HttpContext);
}
