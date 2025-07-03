public void AddOleDbParameter(DbCommand command, string paramName, OleDbType type, object value, int size = 36)
{
    var param = new OleDbParameter
    {
        OleDbType = type,
        Size = size,
        Value = value
    };

    command.Parameters.Add(param);
}

public void AddOleDbParameter(DbCommand command, string paramName, OleDbType type, object value, int size = 36)
{
    var param = new OleDbParameter
    {
        OleDbType = type,
        Size = size,
        Value = value
    };

    command.Parameters.Add(param);
}

param.AddOleDbParameter(command, "HDP00GUID", OleDbType.Char, guid.ToString(), 36);
