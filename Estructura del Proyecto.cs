/// <summary>
/// Agrega un parámetro a un DbCommand, compatible con OleDbCommand y decoradores como LoggingDatabaseCommand.
/// </summary>
/// <param name="cmd">Comando al cual se agregará el parámetro.</param>
/// <param name="name">Nombre del parámetro.</param>
/// <param name="type">Tipo OleDb del parámetro.</param>
/// <param name="value">Valor del parámetro.</param>
/// <param name="size">Tamaño fijo del campo (por ejemplo, 100 para CHAR(100)).</param>
public void AddOleDbParameter(DbCommand cmd, string name, OleDbType type, object? value, int? size = null)
{
    var param = cmd.CreateParameter();
    param.ParameterName = name;

    if (param is OleDbParameter oleParam)
    {
        oleParam.OleDbType = type;

        // Solo aplica el Size si se especifica, sin modificar el valor
        if (size.HasValue)
            oleParam.Size = size.Value;
    }

    param.Value = value ?? DBNull.Value;
    cmd.Parameters.Add(param);
}
