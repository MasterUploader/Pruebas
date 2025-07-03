private static DbParameter CreateParameter(DbCommand command, object value)
{
    var parameter = command.CreateParameter();
    parameter.Value = value ?? DBNull.Value;
    return parameter;
}
