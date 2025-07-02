/// <summary>
/// Ejecuta el lector de datos con comportamiento específico y registra la ejecución.
/// </summary>
/// <param name="behavior">Comportamiento del lector (por ejemplo, CloseConnection).</param>
/// <returns>Lector de datos de la consulta ejecutada.</returns>
protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior)
{
    StartIfNeeded();

    try
    {
        var reader = _innerCommand.ExecuteReader(behavior);
        return reader;
    }
    catch (Exception ex)
    {
        _loggingService.LogDatabaseError(_innerCommand, ex, _httpContextAccessor?.HttpContext);
        throw;
    }
}
