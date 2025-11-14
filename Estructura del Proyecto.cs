#region Ejecuciones (bloque por ejecución)

/// <inheritdoc />
/// <summary>
/// Ejecuta un comando que devuelve un lector de datos (típicamente SELECT) y
/// registra la ejecución en el log SQL, tanto en caso de éxito como de error.
/// </summary>
protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior)
{
    var startedLocal = DateTime.Now;
    var startedUtc = startedLocal.ToUniversalTime();

    var ctx = _httpContextAccessor?.HttpContext;
    if (ctx is not null)
    {
        // Marca de tiempo de inicio para ordenar las ejecuciones en el middleware.
        ctx.Items["__SqlStartedUtc"] = startedUtc;
    }

    var sw = Stopwatch.StartNew();

    try
    {
        // Ejecución real del comando (SELECT…)
        var reader = _innerCommand.ExecuteReader(behavior);

        sw.Stop();

        // Registro estructurado de la ejecución EXITOSA (afectadas = 0 para SELECT).
        LogOneExecution(
            startedAt: startedLocal,
            duration: sw.Elapsed,
            affectedRows: 0,
            sqlRendered: RenderSqlWithParametersSafe(_innerCommand.CommandText, _innerCommand.Parameters)
        );

        return reader;
    }
    catch (Exception ex)
    {
        sw.Stop();

        // Registro estructurado de la ejecución FALLIDA en el bloque SQL.
        LogOneExecution(
            startedAt: startedLocal,
            duration: sw.Elapsed,
            affectedRows: 0,
            sqlRendered: RenderSqlWithParametersSafe(_innerCommand.CommandText, _innerCommand.Parameters)
        );

        // Registro adicional del detalle del error SQL.
        _loggingService?.LogDatabaseError(_innerCommand, ex, ctx);

        throw;
    }
}

/// <inheritdoc />
/// <summary>
/// Ejecuta una sentencia SQL que no retorna filas (INSERT/UPDATE/DELETE, CALL, etc.)
/// y registra la operación tanto en caso de éxito como de error.
/// </summary>
public override int ExecuteNonQuery()
{
    var startedLocal = DateTime.Now;
    var startedUtc = startedLocal.ToUniversalTime();

    var ctx = _httpContextAccessor?.HttpContext;
    if (ctx is not null)
    {
        ctx.Items["__SqlStartedUtc"] = startedUtc;
    }

    var sw = Stopwatch.StartNew();

    try
    {
        var result = _innerCommand.ExecuteNonQuery();

        sw.Stop();

        // Registro de la ejecución EXITOSA (incluye filas afectadas).
        LogOneExecution(
            startedAt: startedLocal,
            duration: sw.Elapsed,
            affectedRows: result,
            sqlRendered: RenderSqlWithParametersSafe(_innerCommand.CommandText, _innerCommand.Parameters)
        );

        return result;
    }
    catch (Exception ex)
    {
        sw.Stop();

        // Registro de la ejecución FALLIDA en el bloque SQL.
        LogOneExecution(
            startedAt: startedLocal,
            duration: sw.Elapsed,
            affectedRows: 0,
            sqlRendered: RenderSqlWithParametersSafe(_innerCommand.CommandText, _innerCommand.Parameters)
        );

        _loggingService?.LogDatabaseError(_innerCommand, ex, ctx);

        throw;
    }
}

/// <inheritdoc />
/// <summary>
/// Ejecuta un comando que devuelve un único valor escalar y registra la
/// operación en el log SQL tanto en éxito como en error.
/// </summary>
public override object? ExecuteScalar()
{
    var startedLocal = DateTime.Now;
    var startedUtc = startedLocal.ToUniversalTime();

    var ctx = _httpContextAccessor?.HttpContext;
    if (ctx is not null)
    {
        ctx.Items["__SqlStartedUtc"] = startedUtc;
    }

    var sw = Stopwatch.StartNew();

    try
    {
        var result = _innerCommand.ExecuteScalar();

        sw.Stop();

        // Para Scalar se considera filas afectadas = 0.
        LogOneExecution(
            startedAt: startedLocal,
            duration: sw.Elapsed,
            affectedRows: 0,
            sqlRendered: RenderSqlWithParametersSafe(_innerCommand.CommandText, _innerCommand.Parameters)
        );

        return result;
    }
    catch (Exception ex)
    {
        sw.Stop();

        LogOneExecution(
            startedAt: startedLocal,
            duration: sw.Elapsed,
            affectedRows: 0,
            sqlRendered: RenderSqlWithParametersSafe(_innerCommand.CommandText, _innerCommand.Parameters)
        );

        _loggingService?.LogDatabaseError(_innerCommand, ex, ctx);

        throw;
    }
}

//
// OPCIONAL: versiones asincrónicas con el mismo patrón
//

/// <summary>
/// Versión asincrónica de <see cref="ExecuteNonQuery"/> con logging estructurado.
/// </summary>
public override async Task<int> ExecuteNonQueryAsync(CancellationToken cancellationToken)
{
    var startedLocal = DateTime.Now;
    var startedUtc = startedLocal.ToUniversalTime();

    var ctx = _httpContextAccessor?.HttpContext;
    if (ctx is not null)
    {
        ctx.Items["__SqlStartedUtc"] = startedUtc;
    }

    var sw = Stopwatch.StartNew();

    try
    {
        var result = await _innerCommand.ExecuteNonQueryAsync(cancellationToken);

        sw.Stop();

        LogOneExecution(
            startedAt: startedLocal,
            duration: sw.Elapsed,
            affectedRows: result,
            sqlRendered: RenderSqlWithParametersSafe(_innerCommand.CommandText, _innerCommand.Parameters)
        );

        return result;
    }
    catch (Exception ex)
    {
        sw.Stop();

        LogOneExecution(
            startedAt: startedLocal,
            duration: sw.Elapsed,
            affectedRows: 0,
            sqlRendered: RenderSqlWithParametersSafe(_innerCommand.CommandText, _innerCommand.Parameters)
        );

        _loggingService?.LogDatabaseError(_innerCommand, ex, ctx);

        throw;
    }
}

/// <summary>
/// Versión asincrónica de <see cref="ExecuteDbDataReader"/> con logging estructurado.
/// </summary>
protected override async Task<DbDataReader> ExecuteDbDataReaderAsync(
    CommandBehavior behavior,
    CancellationToken cancellationToken)
{
    var startedLocal = DateTime.Now;
    var startedUtc = startedLocal.ToUniversalTime();

    var ctx = _httpContextAccessor?.HttpContext;
    if (ctx is not null)
    {
        ctx.Items["__SqlStartedUtc"] = startedUtc;
    }

    var sw = Stopwatch.StartNew();

    try
    {
        var reader = await _innerCommand.ExecuteReaderAsync(behavior, cancellationToken);

        sw.Stop();

        LogOneExecution(
            startedAt: startedLocal,
            duration: sw.Elapsed,
            affectedRows: 0,
            sqlRendered: RenderSqlWithParametersSafe(_innerCommand.CommandText, _innerCommand.Parameters)
        );

        return reader;
    }
    catch (Exception ex)
    {
        sw.Stop();

        LogOneExecution(
            startedAt: startedLocal,
            duration: sw.Elapsed,
            affectedRows: 0,
            sqlRendered: RenderSqlWithParametersSafe(_innerCommand.CommandText, _innerCommand.Parameters)
        );

        _loggingService?.LogDatabaseError(_innerCommand, ex, ctx);

        throw;
    }
}

/// <summary>
/// Versión asincrónica de <see cref="ExecuteScalar"/> con logging estructurado.
/// </summary>
public override async Task<object?> ExecuteScalarAsync(CancellationToken cancellationToken)
{
    var startedLocal = DateTime.Now;
    var startedUtc = startedLocal.ToUniversalTime();

    var ctx = _httpContextAccessor?.HttpContext;
    if (ctx is not null)
    {
        ctx.Items["__SqlStartedUtc"] = startedUtc;
    }

    var sw = Stopwatch.StartNew();

    try
    {
        var result = await _innerCommand.ExecuteScalarAsync(cancellationToken);

        sw.Stop();

        LogOneExecution(
            startedAt: startedLocal,
            duration: sw.Elapsed,
            affectedRows: 0,
            sqlRendered: RenderSqlWithParametersSafe(_innerCommand.CommandText, _innerCommand.Parameters)
        );

        return result;
    }
    catch (Exception ex)
    {
        sw.Stop();

        LogOneExecution(
            startedAt: startedLocal,
            duration: sw.Elapsed,
            affectedRows: 0,
            sqlRendered: RenderSqlWithParametersSafe(_innerCommand.CommandText, _innerCommand.Parameters)
        );

        _loggingService?.LogDatabaseError(_innerCommand, ex, ctx);

        throw;
    }
}

#endregion
