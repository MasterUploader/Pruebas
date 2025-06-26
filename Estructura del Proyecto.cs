public override CommandType CommandType
{
    get => _innerCommand.CommandType;
    set => _innerCommand.CommandType = value;
}

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

public override bool DesignTimeVisible
{
    get => _innerCommand.DesignTimeVisible;
    set => _innerCommand.DesignTimeVisible = value;
}

public override UpdateRowSource UpdatedRowSource
{
    get => _innerCommand.UpdatedRowSource;
    set => _innerCommand.UpdatedRowSource = value;
}

public override void Cancel() => _innerCommand.Cancel();

public override int ExecuteNonQuery() => _innerCommand.ExecuteNonQuery();

public override object ExecuteScalar() => _innerCommand.ExecuteScalar();

public override Task<int> ExecuteNonQueryAsync(CancellationToken cancellationToken)
    => _innerCommand.ExecuteNonQueryAsync(cancellationToken);

public override Task<object> ExecuteScalarAsync(CancellationToken cancellationToken)
    => _innerCommand.ExecuteScalarAsync(cancellationToken);

public override void Prepare() => _innerCommand.Prepare();

protected override DbParameter CreateDbParameter() => _innerCommand.CreateParameter();

protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior)
    => _innerCommand.ExecuteReader(behavior);

protected override Task<DbDataReader> ExecuteDbDataReaderAsync(CommandBehavior behavior, CancellationToken cancellationToken)
    => _innerCommand.ExecuteReaderAsync(behavior, cancellationToken);
